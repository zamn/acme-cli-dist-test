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
/// Service class providing access to API endpoints for WidgetsService.
/// Inherits HTTP client management, JSON serialization, and streaming capabilities from the base service.
/// Each method corresponds to an API operation and handles request building, execution, and response parsing.
/// </summary>
internal class WidgetsService : BaseService
{
    private RequestConfig? _listWidgetsAsyncConfig;

    internal WidgetsService(global::FernApi.Http.Client httpClient)
        : base(httpClient) { }

    /// <summary>
    /// Sets method-level configuration for <c>ListWidgetsAsync</c>.
    /// Method-level config overrides service-level config but is overridden by per-request config.
    /// </summary>
    public WidgetsService SetListWidgetsAsyncConfig(RequestConfig config)
    {
        _listWidgetsAsyncConfig = config;
        return this;
    }

    public async global::System.Threading.Tasks.Task<global::FernApi.ListWidgetsResponse> ListWidgetsAsync(
        RequestConfig? requestConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        var resolvedConfig = GetResolvedConfig(_listWidgetsAsyncConfig, requestConfig);

        var request = new RequestBuilder(HttpMethod.Get, "widgets").Build();

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
        global::FernApi.ListWidgetsResponse result;
        try
        {
            result =
                global::System.Text.Json.JsonSerializer.Deserialize<global::FernApi.ListWidgetsResponse>(
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
