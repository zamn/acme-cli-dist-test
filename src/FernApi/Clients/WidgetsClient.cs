using FernApi.Core;
using FernApi.Services;
using global::System.Linq;
using global::System.Threading;
using OneOf;

namespace FernApi;

/// <summary>
/// Fern-shape client for WidgetsClient. Public surface mirrors Fern; execution delegates to the
/// internal legacy service runtime. Declared <c>partial</c> so the interface base
/// (<c>IWidgetsClient</c>, FSM-144) can be added in a sibling declaration.
/// </summary>
public partial class WidgetsClient
{
    private readonly WidgetsService _service;

    internal WidgetsClient(global::FernApi.Http.Client httpClient)
    {
        _service = new WidgetsService(httpClient);
    }

    /// <summary>
    /// Sets service-level configuration for every method on this client.
    /// Overridden by method-level config and by per-call <c>RequestOptions</c>.
    /// </summary>
    public IWidgetsClient SetConfig(global::FernApi.Config.RequestConfig config)
    {
        _service.SetConfig(config);
        return this;
    }

    /// <summary>
    /// Sets method-level configuration for <c>ListWidgetsAsync</c>.
    /// Overrides service-level configuration; overridden by per-call <c>RequestOptions</c>.
    /// </summary>
    public IWidgetsClient SetListWidgetsAsyncConfig(global::FernApi.Config.RequestConfig config)
    {
        _service.SetListWidgetsAsyncConfig(config);
        return this;
    }

    public WithRawResponseTask<ListWidgetsResponse> ListWidgetsAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default
    ) =>
        new WithRawResponseTask<ListWidgetsResponse>(
            WithRawResponseAdapter.Wrap(
                options,
                () => _service.ListWidgetsAsync(cancellationToken: cancellationToken)
            )
        );
}
