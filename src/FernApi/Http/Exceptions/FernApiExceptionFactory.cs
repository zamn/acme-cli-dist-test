using System.Net;
using System.Text;

namespace FernApi.Http.Exceptions;

/// <summary>
/// fernMode error gateway. Builds and throws Fern's <see cref="FernApi.FernApiApiException"/>
/// from a failed response so consumers catch the Fern exception type. Captures the status code, URL,
/// body, and a detached header snapshot, then disposes the live response (no live response is exposed).
/// When a status maps to one of Fern's status-named subclasses (<c>NotFoundError</c>/etc.) it raises
/// that subclass with the deserialized (typed or <c>object</c>) body, falling back to the base
/// exception with the raw body when the body cannot be deserialized (FSM-176).
/// </summary>
internal static class FernApiExceptionFactory
{
    internal static async Task<FernApi.FernApiApiException> FromResponseAsync(
        HttpResponseMessage response,
        string? message = null
    )
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var statusCode = response.StatusCode;
        var url = response.RequestMessage?.RequestUri ?? new Uri("about:blank");
        // Snapshot the headers before disposing so the RawResponse holds no live response.
        var headers = response
            .Headers.Concat(response.Content.Headers)
            .GroupBy(header => header.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value.ToArray(),
                StringComparer.OrdinalIgnoreCase
            );
        response.Dispose();

        // Share the success-path construction (FSM-189): re-buffer the snapshotted status/body/headers
        // into a detached response, then build the same RawResponse shape, overriding the URL with the
        // original request URL (the detached copy has no RequestMessage).
        var rawResponse = FernApi.Core.RawResponseFactory.FromHttpResponseMessage(
            BuildBufferedResponse(statusCode, body, headers),
            url
        );

        return new FernApi.FernApiApiException(
            message ?? $"Error with status code {(int)statusCode}",
            (int)statusCode,
            body,
            null,
            rawResponse
        );
    }

    private static HttpResponseMessage BuildBufferedResponse(
        HttpStatusCode statusCode,
        string body,
        IReadOnlyDictionary<string, string[]> headers
    )
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        var buffered = new HttpResponseMessage(statusCode) { Content = content };
        foreach (var header in headers)
        {
            // Response headers reject content headers (e.g. Content-Type); fall back to content headers.
            if (!buffered.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return buffered;
    }
}
