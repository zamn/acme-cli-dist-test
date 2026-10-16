using System.Net.Http.Json;
using FernApi;
using FernApi.Config;
using FernApi.Http;
using FernApi.Http.Exceptions;
using FernApi.Http.Extensions;
using FernApi.Http.Handlers;
using FernApi.Http.Serialization;

namespace FernApi.Services;

/// <summary>
/// Service class providing access to API endpoints for GadgetsService.
/// Inherits HTTP client management, JSON serialization, and streaming capabilities from the base service.
/// Each method corresponds to an API operation and handles request building, execution, and response parsing.
/// </summary>
internal class GadgetsService : BaseService
{
    private RequestConfig? _listGadgetsAsyncConfig;

    internal GadgetsService(global::FernApi.Http.Client httpClient)
        : base(httpClient) { }

    /// <summary>
    /// Sets method-level configuration for <c>ListGadgetsAsync</c>.
    /// Method-level config overrides service-level config but is overridden by per-request config.
    /// </summary>
    public GadgetsService SetListGadgetsAsyncConfig(RequestConfig config)
    {
        _listGadgetsAsyncConfig = config;
        return this;
    }

    public async global::System.Threading.Tasks.Task<global::FernApi.ListGadgetsResponse> ListGadgetsAsync(
        RequestConfig? requestConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        var resolvedConfig = GetResolvedConfig(_listGadgetsAsyncConfig, requestConfig);

        var request = new RequestBuilder(HttpMethod.Get, "gadgets").Build();

        var response = await ExecuteAsync(request, resolvedConfig, cancellationToken)
            .ConfigureAwait(false);

        // Standard deserialization
        // Read the body first so a decode failure can report the bytes that caused it.
        var responseBody = await response
            .Content
#if NET5_0_OR_GREATER
            .ReadAsStringAsync(cancellationToken)
#else
        .ReadAsStringAsync()
#endif
            .ConfigureAwait(false);
        global::FernApi.ListGadgetsResponse result;
        try
        {
            result =
                global::System.Text.Json.JsonSerializer.Deserialize<global::FernApi.ListGadgetsResponse>(
                    responseBody,
                    _jsonSerializerOptions
                )!;
        }
        catch (global::System.Text.Json.JsonException exception)
        {
            throw new FernApi.FernApiApiException(
                "Failed to deserialize response",
                (int)response.StatusCode,
                responseBody,
                exception,
                rawResponse: FernApi.Core.RawResponseFactory.FromHttpResponseMessage(response)
            );
        }

        return result;
    }
}
