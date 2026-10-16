using System.Net;
using System.Text;

namespace FernApi.Tests;

/// <summary>
/// One outbound request, captured exactly as the SDK serialized it.
/// </summary>
/// <remarks>
/// The body is read here rather than lazily: <c>HttpRequestMessage.Content</c> is disposed once the
/// send completes, so a test that reads it afterwards sees an <c>ObjectDisposedException</c> instead
/// of the payload.
/// </remarks>
internal sealed class CapturedRequest
{
    internal CapturedRequest(
        string method,
        Uri uri,
        IReadOnlyList<KeyValuePair<string, string>> headers,
        string? body
    )
    {
        Method = method;
        Uri = uri;
        Headers = headers;
        Body = body;
    }

    internal string Method { get; }

    internal Uri Uri { get; }

    internal IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

    internal string? Body { get; }

    internal string Path => Uri.AbsolutePath;

    /// <summary>
    /// A header by name, case-insensitively — HTTP header names are case-insensitive (RFC 7230
    /// §3.2) and the SDK sets some of them with the spec's own casing, so an exact-case lookup
    /// reports a correctly-sent header as missing.
    /// </summary>
    internal string? Header(string name)
    {
        foreach (var header in Headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// A decoded query-string value by name, or <c>null</c> when the parameter was not sent.
    /// </summary>
    /// <remarks>
    /// Parsed here rather than via <c>HttpUtility</c> so the generated project needs no extra
    /// package reference. Last-value-wins on a repeated key, which is enough: every generated
    /// assertion checks a single scalar value, never a multi-value query string.
    /// </remarks>
    internal string? Query(string name)
    {
        var query = Uri.Query;
        if (query.Length == 0)
        {
            return null;
        }

        string? found = null;
        foreach (var pair in query.TrimStart('?').Split('&'))
        {
            if (pair.Length == 0)
            {
                continue;
            }

            var separator = pair.IndexOf('=');
            var rawKey = separator < 0 ? pair : pair.Substring(0, separator);
            var rawValue = separator < 0 ? string.Empty : pair.Substring(separator + 1);
            if (Uri.UnescapeDataString(rawKey.Replace("+", "%20")) == name)
            {
                found = Uri.UnescapeDataString(rawValue.Replace("+", "%20"));
            }
        }

        return found;
    }
}

/// <summary>
/// The wire-test seam: a mock transport that answers every request with a canned response instead
/// of sending it.
/// </summary>
/// <remarks>
/// Injected as the INNERMOST handler of the SDK's own <c>DelegatingHandler</c> chain, which is what
/// makes this a wire test rather than a stub: authentication, retry, hooks, and every other
/// generated handler still run for real above it, so what lands in <see cref="LastRequest"/> is the
/// fully-decorated request the SDK would have put on the socket — credentials included. Replacing
/// the chain instead (or wrapping it from the outside) would skip the handlers under test and let a
/// wire test pass against an SDK that sends no credentials at all.
/// </remarks>
internal sealed class MockTransport : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly IReadOnlyList<string> _bodies;
    private readonly string _contentType;
    private readonly List<CapturedRequest> _requests = new();

    /// <param name="contentType">
    /// The media type the canned response is served as. It has to be the one the spec declares, not
    /// a blanket <c>application/json</c>: the SDK looks its error mappings up by status code AND
    /// content type, so an endpoint declaring <c>application/problem+json</c> would miss its own
    /// mapping and surface a bare <c>ApiException</c> instead of the typed one.
    /// </param>
    internal MockTransport(int statusCode, string body, string contentType = "application/json")
        : this(statusCode, new[] { body }, contentType) { }

    /// <summary>
    /// Answers each successive request with the next body, repeating the last once they run out.
    /// </summary>
    /// <remarks>
    /// The primitive the single-body constructor delegates to. Serving a distinct body per request
    /// is what lets a test tell one request's response from another's; the pagination test does not
    /// need it (it serves one full page throughout and ends the walk itself), so nothing passes a
    /// list today.
    /// </remarks>
    internal MockTransport(
        int statusCode,
        IReadOnlyList<string> bodies,
        string contentType = "application/json"
    )
    {
        _statusCode = (HttpStatusCode)statusCode;
        _bodies = bodies;
        _contentType = contentType;
    }

    internal IReadOnlyList<CapturedRequest> Requests => _requests;

    internal CapturedRequest? LastRequest =>
        _requests.Count == 0 ? null : _requests[_requests.Count - 1];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var headers = new List<KeyValuePair<string, string>>();
        foreach (var header in request.Headers)
        {
            headers.Add(
                new KeyValuePair<string, string>(header.Key, string.Join(", ", header.Value))
            );
        }

        string? body = null;
        if (request.Content != null)
        {
            // Content headers (Content-Type above all) live on the content, not the message, so a
            // capture that reads only `request.Headers` reports them as absent.
            foreach (var header in request.Content.Headers)
            {
                headers.Add(
                    new KeyValuePair<string, string>(header.Key, string.Join(", ", header.Value))
                );
            }

            body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        _requests.Add(
            new CapturedRequest(
                request.Method.Method,
                request.RequestUri ?? new Uri("http://localhost/"),
                headers,
                body
            )
        );

        var index = Math.Min(_requests.Count - 1, _bodies.Count - 1);
        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_bodies[index], Encoding.UTF8, _contentType),
            RequestMessage = request,
        };
    }
}
