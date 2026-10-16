using global::System.Linq;
using global::System.Net.Http;
using global::System.Net.Http.Headers;

namespace FernApi.Core;

/// <summary>
/// Retry-safe request-content helpers mirroring Fern's <c>RawClient.CloneRequestAsync</c> and
/// <c>IsRetryableContent</c>. A retried request must re-send its full body, but .NET consumes (and
/// under HTTP/2 disposes) a content stream on the first send, so re-sending the same request object
/// would ship an empty/consumed body on later attempts. Buffering retryable content into a fresh
/// <see cref="MemoryStream"/> per attempt (rebuilding multipart with its boundary and per-part
/// headers) makes a retried POST/multipart upload re-send the complete body. Content that cannot be
/// re-read (raw streams, multipart with stream parts) is classified non-retryable so the retry loop
/// sends it exactly once, matching Fern.
/// </summary>
internal static class RetryableRequestContent
{
    /// <summary>
    /// Whether <paramref name="request"/>'s content can be safely re-sent on a retry. Mirrors Fern's
    /// <c>IsRetryableContent</c>: raw streams and multipart with stream parts are one-shot (their
    /// underlying stream is consumed on first send and cannot be re-read); everything else buffers.
    /// </summary>
    internal static bool IsRetryable(HttpRequestMessage request)
    {
        return request.Content switch
        {
            IIsRetryableContent c => c.IsRetryable,
            StreamContent => false,
            MultipartContent content => !content.Any(c => c is StreamContent),
            _ => true,
        };
    }

    /// <summary>
    /// Clones <paramref name="request"/> onto a fresh envelope whose content is re-materialized into a
    /// new buffer, so the returned message can be sent without consuming the original (which stays
    /// re-readable for the next attempt). Multipart is rebuilt preserving the boundary and per-part
    /// headers; any other body is buffered into a <see cref="MemoryStream"/> with its content headers
    /// copied. Only call for content classified retryable by <see cref="IsRetryable"/>.
    /// </summary>
    internal static async global::System.Threading.Tasks.Task<HttpRequestMessage> CloneWithBufferedContentAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default
    )
    {
        var clonedRequest = new HttpRequestMessage(request.Method, request.RequestUri);
        clonedRequest.Version = request.Version;

        if (request.Content != null)
        {
            switch (request.Content)
            {
                case MultipartContent oldMultipartFormContent:
                    var originalBoundary =
                        oldMultipartFormContent
                            .Headers.ContentType?.Parameters.First(p =>
                                p.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase)
                            )
                            .Value?.Trim('"')
                        ?? Guid.NewGuid().ToString();
                    var newMultipartContent = oldMultipartFormContent switch
                    {
                        MultipartFormDataContent => new MultipartFormDataContent(originalBoundary),
                        _ => new MultipartContent(),
                    };
                    foreach (var content in oldMultipartFormContent)
                    {
                        var ms = new MemoryStream();
#if NET5_0_OR_GREATER
                        await content.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
#else
                        await content.CopyToAsync(ms).ConfigureAwait(false);
#endif
                        ms.Position = 0;
                        var newPart = new StreamContent(ms);
                        foreach (var header in content.Headers)
                        {
                            newPart.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }

                        newMultipartContent.Add(newPart);
                    }

                    clonedRequest.Content = newMultipartContent;
                    break;
                default:
                    var bodyStream = new MemoryStream();
#if NET5_0_OR_GREATER
                    await request
                        .Content.CopyToAsync(bodyStream, cancellationToken)
                        .ConfigureAwait(false);
#else
                    await request.Content.CopyToAsync(bodyStream).ConfigureAwait(false);
#endif
                    bodyStream.Position = 0;
                    var clonedContent = new StreamContent(bodyStream);
                    foreach (var header in request.Content.Headers)
                    {
                        clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    clonedRequest.Content = clonedContent;
                    break;
            }
        }

        foreach (var header in request.Headers)
        {
            clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clonedRequest;
    }
}
