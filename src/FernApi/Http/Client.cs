using System.Text.Json;
using FernApi.Config;
using FernApi.Http.Exceptions;

namespace FernApi.Http;

public class Client
{
    private HttpClient _httpClient;

    // Client-level default headers (ClientOptions.AdditionalHeaders), applied to every outgoing
    // request as a supplement — a matching spec/auth/per-call header wins (FSM-1134).
    private readonly global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<
        string,
        string?
    >> _defaultHeaders;

    /// <summary>
    /// Builds the default transport handler. A <see cref="SocketsHttpHandler"/> is used so pooled
    /// connections are recycled (picking up DNS changes on long-lived clients) and gzip/deflate/brotli
    /// responses are transparently decompressed.
    /// </summary>
    internal static HttpMessageHandler CreateDefaultTransport()
    {
#if NET5_0_OR_GREATER
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AutomaticDecompression = System.Net.DecompressionMethods.All,
        };
#else
        return new HttpClientHandler
        {
            AutomaticDecompression =
                System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        };
#endif
    }

    internal Client(
        FernApiConfig config,
        HttpMessageHandler? handler = null,
        bool disposeHandler = true,
        global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<
            string,
            string?
        >>? defaultHeaders = null
    )
    {
        _httpClient =
            handler == null
                ? new HttpClient(CreateDefaultTransport())
                : new HttpClient(handler, disposeHandler);
        _httpClient.BaseAddress = config?.Environment?.Uri ?? Environment.Default.Uri;
        _defaultHeaders = global::System.Linq.Enumerable.ToArray(
            defaultHeaders
                ?? global::System.Array.Empty<global::System.Collections.Generic.KeyValuePair<
                    string,
                    string?
                >>()
        );
    }

    /// <summary>
    /// Applies the client-level default headers to an outgoing request as a supplement: a header
    /// already present (spec/auth-derived or a per-call override) is left untouched, matching Fern's
    /// "headers set on the request overwrite matching client-level keys" precedence (FSM-1134).
    /// </summary>
    private void ApplyDefaultHeaders(HttpRequestMessage message)
    {
        foreach (var header in _defaultHeaders)
        {
            if (header.Value is null || message.Headers.TryGetValues(header.Key, out _))
            {
                continue;
            }
            if (message.Content?.Headers.TryGetValues(header.Key, out _) == true)
            {
                continue;
            }
            // CR/LF would split the request: everything after the newline is read by the
            // server as further headers (CWE-113). `TryAddWithoutValidation` skips .NET's
            // newline check, so each caller-supplied header path guards it; see
            // caller-header-crlf-guard.spec.ts for the full set.
            if (header.Value.IndexOf('\r') >= 0 || header.Value.IndexOf('\n') >= 0)
            {
                throw new ArgumentException(
                    $"Header '{header.Key}' has a value containing a CR or LF character, which would split the request."
                );
            }
            if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                message.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    public void SetBaseAddress(Uri uri)
    {
        _httpClient.BaseAddress = uri;
    }

    public void SetTimeout(TimeSpan timeout)
    {
        _httpClient.Timeout = timeout;
    }

    public async global::System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
        Request request,
        CancellationToken cancellationToken = default
    )
    {
        ApplyDefaultHeaders(request.HttpRequestMessage);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(request.HttpRequestMessage, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiTimeoutException(TimeoutMessage(), ex);
        }
        catch (HttpRequestException ex) when (ex is not FernApi.Http.Exceptions.ApiException)
        {
            throw new ApiConnectionException(ConnectionMessage(request), ex);
        }
        await HandleError(request, response).ConfigureAwait(false);
        return response;
    }

    public async global::System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
        Request request,
        HttpCompletionOption httpCompletionOptions,
        CancellationToken cancellationToken = default
    )
    {
        ApplyDefaultHeaders(request.HttpRequestMessage);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(request.HttpRequestMessage, httpCompletionOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiTimeoutException(TimeoutMessage(), ex);
        }
        catch (HttpRequestException ex) when (ex is not FernApi.Http.Exceptions.ApiException)
        {
            throw new ApiConnectionException(ConnectionMessage(request), ex);
        }
        await HandleError(request, response).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// The message for a client-level (<see cref="HttpClient.Timeout"/>) timeout. HttpClient cancels
    /// a token of its own when that elapses, so the token handed to <c>SendAsync</c> is still
    /// un-cancelled — the only way an <see cref="OperationCanceledException"/> reaches that catch
    /// without the caller (or a per-request timeout) having asked for it. A per-request timeout
    /// cancels through a linked source and is disambiguated one layer up, in
    /// <c>BaseService.ExecuteAsync</c>, the only place that still holds the caller's own token
    /// (FSDK-1585).
    /// </summary>
    private string TimeoutMessage() => $"The request timed out after {_httpClient.Timeout}.";

    /// <summary>
    /// The message for a connection-level failure — a refused connection, an unresolvable host, a
    /// TLS failure, a reset. The transport raises <see cref="HttpRequestException"/> for all of
    /// these, so a caller had to reference <c>System.Net.Http</c>'s own exception types to handle
    /// one (FSDK-1573). <see cref="ApiException"/> derives from <c>HttpRequestException</c> and is
    /// excluded from that catch: a real API error is not a connection failure.
    /// </summary>
    private static string ConnectionMessage(Request request) =>
        $"Could not connect to the server for {request.HttpRequestMessage.Method} {request.HttpRequestMessage.RequestUri}.";

    /// <summary>
    /// Handles error responses by checking configured error mappings and throwing appropriate exceptions.
    /// </summary>
    /// <param name="response">The HTTP response message to check for errors.</param>
    /// <exception cref="HttpRequestException">Thrown when the request fails and no matching error mapping is found, or when deserialization fails.</exception>
    /// <exception cref="Exception">Throws a specific exception type based on configured error mappings when a matching status code and content type is found.</exception>
    private async global::System.Threading.Tasks.Task HandleError(
        Request request,
        HttpResponseMessage response
    )
    {
        if (response.IsSuccessStatusCode)
        {
            return; // No error to handle
        }

        var statusCode = (int)response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

        var mapping = request.ErrorMappings?.FirstOrDefault(m =>
            m.StatusCode == statusCode && ContentTypes.Matches(m.ContentType, contentType)
        );

        if (mapping != null)
        {
            await DeserializeAndThrowException(response, mapping).ConfigureAwait(false);
        }

        if (request.DefaultErrorMapping != null)
        {
            await DeserializeAndThrowException(response, request.DefaultErrorMapping)
                .ConfigureAwait(false);
        }

        throw await FernApiExceptionFactory.FromResponseAsync(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes a given exception and throws it
    /// </summary>
    /// <param name="response">The HTTP response message to check for errors.</param>
    /// <param name="mapping">The error mapping to use for deserialization and exception creation.</param>
    /// <exception cref="Exception">Throws a specific exception type based on the provided error mapping.</exception>
    private async global::System.Threading.Tasks.Task DeserializeAndThrowException(
        HttpResponseMessage response,
        ErrorMapping mapping
    )
    {
        // TODO USE DESERIALIZATION LOGIC which will catch the case where we have non-json responses
        var contentString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var (statusCode, headers) = FernApi.Http.Exceptions.ApiException.CaptureAndDispose(
            response
        );
        try
        {
            var deserializedError = JsonSerializer.Deserialize(contentString, mapping.TargetType);

            if (deserializedError == null)
            {
                throw new FernApi.Http.Exceptions.ApiException(statusCode, contentString, headers);
            }
            var exception = (Exception)
                Activator.CreateInstance(
                    mapping.ExceptionType,
                    new object[] { deserializedError, statusCode, contentString, headers }
                )!;
            throw exception;
        }
        catch (JsonException)
        {
            throw new FernApi.Http.Exceptions.ApiException(statusCode, contentString, headers);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
