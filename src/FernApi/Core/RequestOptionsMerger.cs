using global::System;
using global::System.Collections.Generic;
using global::System.Linq;
using global::System.Net.Http;
using global::System.Text;
using global::System.Text.Json.Nodes;
using global::System.Threading;
using global::System.Threading.Tasks;

namespace FernApi.Core;

/// <summary>
/// Applies the Fern per-call <see cref="FernApi.RequestOptions"/> escape hatches —
/// AdditionalHeaders, AdditionalQueryParameters, AdditionalBodyProperties — to the outgoing request
/// built by the legacy runtime, so the fern client surface (FSM-141) can delegate through the proven
/// transport while still honoring per-call overrides (FSM-190). The options are threaded in from the
/// fern client via <see cref="WithRawResponseAdapter"/>'s ambient; this is a no-op for legacy
/// (non-fern) calls, whose ambient options are null.
///
/// Precedence mirrors Fern's HeadersBuilder / QueryStringBuilder / JsonConfiguration.MergeJsonObjects:
/// additional headers overwrite matching keys (case-insensitive); additional query parameters replace
/// all matching keys (case-sensitive); additional body properties deep-merge into a JSON body, winning
/// on scalar collisions and recursing into object-vs-object collisions.
/// </summary>
internal static class RequestOptionsMerger
{
    /// <summary>
    /// Key under which <see cref="ApplyAdditionalHeaders"/> records the header names the caller set
    /// for this one call, read back by the auth delegating handlers (<c>TokenHandler</c>,
    /// <c>BasicAuthHandler</c>, <c>OAuthHandler</c>) which run afterwards (FSDK-1588 / FSDK-1674).
    /// Shared as a constant rather than repeated as a literal in each of those templates: a
    /// producer/consumer mismatch degrades the guard to "never an override" — silently reinstating
    /// the credential-dropping bug — and against a literal that is caught only by a regenerated
    /// snapshot, whereas against this constant it does not compile.
    /// </summary>
    internal const string PerCallHeaderKeysKey = "_RequestOptions_PerCallHeaderKeys";

    /// <summary>
    /// Overlays the per-call <see cref="FernApi.RequestOptions"/> transport members
    /// (<c>Timeout</c>, <c>MaxRetries</c>, <c>BaseUrl</c>) onto the already-resolved
    /// <see cref="FernApi.Config.RequestConfig"/> so the legacy execution path honors them
    /// (FSM-1130). Called at the top of the base service's ExecuteAsync — before the timeout CTS and
    /// the config-override application — so the timeout, the retry loop, and the base-URL rewrite all
    /// observe the per-call values. Per-call wins over the resolved (request/method/service/SDK)
    /// config, mirroring Fern's <c>options?.X ?? Options.X</c>. A no-op returning the config unchanged
    /// when <paramref name="options"/> is null (legacy / non-fern calls).
    ///
    /// <c>RequestOptions.HttpClient</c> is intentionally NOT applied here: the legacy transport bakes
    /// the retry/auth delegating-handler pipeline into a single HttpClient at construction and exposes
    /// no per-call swap, so honoring it would bypass retries and auth. It is unsupported under fernMode
    /// and tracked separately (FSM-1131).
    /// </summary>
    public static global::FernApi.Config.RequestConfig? MergeConfig(
        global::FernApi.Config.RequestConfig? config,
        FernApi.RequestOptions? options
    )
    {
        if (options is null)
        {
            return config;
        }

        var resolved = config ?? new global::FernApi.Config.RequestConfig();
        return resolved with
        {
            Timeout = options.Timeout ?? resolved.Timeout,
            BaseUrl = options.BaseUrl ?? resolved.BaseUrl,
            RetryConfig = MergeRetryConfig(resolved.RetryConfig, options),
        };
    }

    /// <summary>
    /// Resolves the retry configuration for one call. <c>RequestOptions.Retry</c> replaces the
    /// service/method/SDK-level <see cref="FernApi.Config.RetryConfig"/> outright when it
    /// is set, so a per-call configuration is not silently half-inherited (FSDK-1521); the
    /// Fern-shaped <c>RequestOptions.MaxRetries</c> then overlays the attempt count on top, so the
    /// two spellings compose rather than one shadowing the other.
    ///
    /// MaxRetries is floored at 0 to match Fern's Math.Max(0, ...); RetryHandler treats
    /// MaxRetryAttempts as retries-beyond-first, same as Fern's attempt &lt;= maxRetries loop.
    /// </summary>
    private static global::FernApi.Config.RetryConfig? MergeRetryConfig(
        global::FernApi.Config.RetryConfig? resolved,
        FernApi.RequestOptions options
    )
    {
        var retry = options.Retry ?? resolved;
        if (!options.MaxRetries.HasValue)
        {
            return retry;
        }
        return (retry ?? new global::FernApi.Config.RetryConfig()) with
        {
            MaxRetryAttempts = Math.Max(0, options.MaxRetries.Value),
        };
    }

    public static async global::System.Threading.Tasks.Task ApplyAsync(
        HttpRequestMessage message,
        FernApi.RequestOptions? options,
        CancellationToken cancellationToken = default
    )
    {
        if (options is null)
        {
            return;
        }
        // Body first: the body merge may replace message.Content with freshly-serialized content, so
        // applying headers afterward lets an AdditionalHeader that targets a content header (e.g.
        // Content-Type) survive rather than being dropped with the old content.
        await ApplyAdditionalBodyPropertiesAsync(message, options, cancellationToken)
            .ConfigureAwait(false);
        ApplyAdditionalHeaders(message, options);
        ApplyAdditionalQueryParameters(message, options);
    }

    // Additional headers overwrite any spec/auth header with a matching key (HttpHeaders keys are
    // case-insensitive). Null values are skipped, matching Fern's HeadersBuilder.
    //
    // The keys applied here are also recorded on the request under the
    // _RequestOptions_PerCallHeaderKeys option, because this merger runs while the request is still
    // being built and the auth delegating handlers run afterwards. Without the record,
    // TokenHandler/BasicAuthHandler re-add the SDK-level credential over the caller's, silently
    // discarding a per-call credential override -- the only per-call credential lever a caller has
    // (FSDK-1588).
    private static void ApplyAdditionalHeaders(
        HttpRequestMessage message,
        FernApi.RequestOptions options
    )
    {
        var applied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in options.AdditionalHeaders)
        {
            var value = header.Value;
            if (value is null)
            {
                continue;
            }
            // CR/LF would split the request: everything after the newline is read by the server as
            // further headers (CWE-113). `TryAddWithoutValidation` skips .NET's newline check, so each
            // caller-supplied header path guards it; see caller-header-crlf-guard.spec.ts for the
            // full set.
            if (value != null && (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0))
            {
                throw new ArgumentException(
                    $"Header '{header.Key}' has a value containing a CR or LF character, which would split the request."
                );
            }
            applied.Add(header.Key);
            RemoveHeaderIfPresent(message.Headers, header.Key);
            if (message.Content is not null)
            {
                RemoveHeaderIfPresent(message.Content.Headers, header.Key);
            }
            if (!message.Headers.TryAddWithoutValidation(header.Key, value))
            {
                message.Content?.Headers.TryAddWithoutValidation(header.Key, value);
            }
        }

        if (applied.Count == 0)
        {
            return;
        }
#if NET5_0_OR_GREATER
        message.Options.Set(
            new HttpRequestOptionsKey<HashSet<string>>(PerCallHeaderKeysKey),
            applied
        );
#else
        message.Properties[PerCallHeaderKeysKey] = applied;
#endif
    }

    /// <summary>
    /// Removes <paramref name="name"/> from <paramref name="headers"/> only if that collection
    /// actually holds it.
    /// </summary>
    /// <remarks>
    /// <c>HttpHeaders.Remove</c> throws <c>InvalidOperationException("Misused header name")</c> for a
    /// header the collection does not accept, in BOTH directions: removing <c>Authorization</c> from
    /// content headers throws, and so does removing <c>Content-Type</c> from request headers. Handing
    /// the name to both collections blind therefore failed outright for a per-call override of any
    /// known request header on a request that HAS a body (every POST/PUT/PATCH), and for any known
    /// content header on any request. Custom header names were never affected, which is why this
    /// survived.
    ///
    /// Catching that throw is preferred over probing first. <c>HttpHeaders.Contains</c> is NOT a
    /// usable guard — it throws the same <c>InvalidOperationException</c> for a misused name, so it
    /// only moves the failure. Enumerating and matching the key works, but forces every header in
    /// the collection through its parser on each per-call key, so keep the direct <c>Remove</c>:
    /// removing a name the collection accepts but does not hold simply returns <c>false</c>.
    /// </remarks>
    private static void RemoveHeaderIfPresent(
        global::System.Net.Http.Headers.HttpHeaders headers,
        string name
    )
    {
        try
        {
            headers.Remove(name);
        }
        catch (global::System.InvalidOperationException)
        {
            // This collection does not accept `name`, so it cannot be holding one to remove.
        }
    }

    // Additional query parameters replace every spec-derived occurrence of a matching key (case-
    // sensitive), then are appended; a key repeated in the options emits repeated pairs. Mirrors
    // Fern's QueryStringBuilder.MergeAdditional (Set = remove-all-then-add).
    private static void ApplyAdditionalQueryParameters(
        HttpRequestMessage message,
        FernApi.RequestOptions options
    )
    {
        var additional = options.AdditionalQueryParameters.ToList();
        if (additional.Count == 0 || message.RequestUri is null)
        {
            return;
        }

        var original = message.RequestUri.OriginalString;
        var fragmentIndex = original.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? original.Substring(fragmentIndex) : string.Empty;
        var withoutFragment = fragmentIndex >= 0 ? original.Substring(0, fragmentIndex) : original;
        var queryIndex = withoutFragment.IndexOf('?');
        var pathPart = queryIndex >= 0 ? withoutFragment.Substring(0, queryIndex) : withoutFragment;
        var queryPart = queryIndex >= 0 ? withoutFragment.Substring(queryIndex + 1) : string.Empty;

        var overriddenKeys = new HashSet<string>(
            additional.Select(kv => kv.Key),
            StringComparer.Ordinal
        );
        var segments =
            queryPart.Length == 0
                ? new List<string>()
                : queryPart
                    .Split('&')
                    .Where(segment =>
                        segment.Length > 0 && !overriddenKeys.Contains(DecodeKey(segment))
                    )
                    .ToList();

        foreach (var param in additional)
        {
            segments.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(param.Value)}");
        }

        var rebuilt =
            pathPart
            + (segments.Count > 0 ? "?" + string.Join("&", segments) : string.Empty)
            + fragment;
        message.RequestUri = new Uri(rebuilt, UriKind.RelativeOrAbsolute);
    }

    private static string DecodeKey(string segment)
    {
        var equalsIndex = segment.IndexOf('=');
        var rawKey = equalsIndex >= 0 ? segment.Substring(0, equalsIndex) : segment;
        return Uri.UnescapeDataString(rawKey);
    }

    // Additional body properties deep-merge into a JSON request body (per-call values win on
    // collision), reusing Fern's JsonUtils.SerializeWithAdditionalProperties so the merge/precedence is
    // byte-identical. Applied only to JSON content, matching Fern's "only applied to JSON requests"
    // guard; a bodyless or non-JSON request is left untouched.
    private static async global::System.Threading.Tasks.Task ApplyAdditionalBodyPropertiesAsync(
        HttpRequestMessage message,
        FernApi.RequestOptions options,
        CancellationToken cancellationToken
    )
    {
        if (options.AdditionalBodyProperties is null || message.Content is null)
        {
            return;
        }
        // JSON only, matching Fern's guard. A null/unknown media type is treated as non-JSON
        // (conservative: never consume/rewrite a body we can't confirm is JSON).
        var contentType = message.Content.Headers.ContentType;
        var mediaType = contentType?.MediaType;
        if (mediaType is null || !IsJsonMediaType(mediaType))
        {
            return;
        }

        var existing = await message
            .Content
#if NET5_0_OR_GREATER
            .ReadAsStringAsync(cancellationToken)
#else
        .ReadAsStringAsync()
#endif
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(existing))
        {
            return;
        }

        var bodyNode = JsonNode.Parse(existing);
        var merged = JsonUtils.SerializeWithAdditionalProperties(
            bodyNode,
            options.AdditionalBodyProperties
        );
        // Preserve the original JSON media type (e.g. application/vnd.api+json), not just application/json.
        //
        // FSDK-2255: rebuilding the content re-derives its Content-Type, and StringContent always
        // stamps a charset from the encoding it was handed -- putting back the `charset=utf-8` the
        // request builder deliberately removed. Carry the DECLARED charset across instead: its
        // encoding when the spec named one, blank when it did not. Same rule as Fern's JsonRequest,
        // which merges the additional properties into its single serialization and so never has to
        // restore anything. Reading it off the original header also stops a declared non-UTF-8
        // charset being silently rewritten to UTF-8 here.
        var charset = contentType?.CharSet;
        var rewritten = new StringContent(
            merged,
            string.IsNullOrEmpty(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset!),
            mediaType
        );
        if (string.IsNullOrEmpty(charset) && rewritten.Headers.ContentType is not null)
        {
            rewritten.Headers.ContentType.CharSet = "";
        }
        message.Content = rewritten;
    }

    private static bool IsJsonMediaType(string mediaType) =>
        mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
        || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
}
