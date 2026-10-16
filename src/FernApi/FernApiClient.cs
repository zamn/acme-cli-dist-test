using FernApi.Config;
using FernApi.Http;
using FernApi.Http.Extensions;
using FernApi.Http.Handlers;
using FernApi.Services;

namespace FernApi;

public partial class FernApiClient : IFernApiClient
{
    private readonly Client _httpClient;

    public IWidgetsClient Widgets { get; }
    public IGadgetsClient Gadgets { get; }

    public FernApiClient(ClientOptions? clientOptions = null)
    {
        clientOptions ??= new ClientOptions();
        FernApiConfig? config = null;
        HttpMessageHandler handler = new ClientOptionsTransport(clientOptions.HttpClient);

        var retryHandler = new RetryHandler(handler, clientOptions.MaxRetries);
        _httpClient = new Client(
            config,
            retryHandler,
            disposeHandler: false,
            defaultHeaders: clientOptions.AdditionalHeaders
        );
        _httpClient.SetBaseAddress(
            new global::System.Uri(clientOptions.BaseUrl).EnsureTrailingSlash()
        );
        _httpClient.SetTimeout(clientOptions.Timeout);

        Widgets = new WidgetsClient(_httpClient);
        Gadgets = new GadgetsClient(_httpClient);
    }
}

/// <summary>
/// Bridges Fern's <c>ClientOptions.HttpClient</c> to the legacy delegating-handler transport chain.
/// Each request is dispatched on a fresh clone because the retry handler re-sends the same request
/// object, which the caller's <see cref="HttpClient"/> would otherwise reject as already-sent. This
/// is also the single per-attempt content-cloning site (FSM-1136): retryable content is
/// re-materialized into a fresh buffer on every send so a retried request re-sends its full body,
/// mirroring Fern's <c>RawClient.CloneRequestAsync</c>.
/// </summary>
internal sealed class ClientOptionsTransport : HttpMessageHandler
{
    private readonly HttpClient _inner;

    internal ClientOptionsTransport(HttpClient inner)
    {
        _inner = inner;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        HttpRequestMessage clone;
        if (Core.RetryableRequestContent.IsRetryable(request))
        {
            // Re-materialize retryable content into a fresh buffer per send so a retried attempt
            // re-sends the full body (the original stays re-readable; the sent clone is what the
            // caller's HttpClient consumes/disposes). Mirrors Fern's RawClient.CloneRequestAsync.
            clone = await Core
                .RetryableRequestContent.CloneWithBufferedContentAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            // Non-retryable content (raw streams / multipart with stream parts) can't be re-read, so
            // reuse the original content object and forward it on a fresh envelope: streamed once,
            // never retried (RetryHandler skips the loop for it), preserving streaming semantics.
            clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version,
                Content = request.Content,
            };
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return await _inner.SendAsync(clone, cancellationToken).ConfigureAwait(false);
    }
}

// c029837e0e474b76bc487506e8799df5e3335891efe4fb02bda7a1441840310c
