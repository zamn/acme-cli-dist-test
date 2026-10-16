using FernApi.Core;
using FernApi.Services;
using global::System.Linq;
using global::System.Threading;
using OneOf;

namespace FernApi;

/// <summary>
/// Fern-shape client for GadgetsClient. Public surface mirrors Fern; execution delegates to the
/// internal legacy service runtime. Declared <c>partial</c> so the interface base
/// (<c>IGadgetsClient</c>, FSM-144) can be added in a sibling declaration.
/// </summary>
public partial class GadgetsClient
{
    private readonly GadgetsService _service;

    internal GadgetsClient(global::FernApi.Http.Client httpClient)
    {
        _service = new GadgetsService(httpClient);
    }

    /// <summary>
    /// Sets service-level configuration for every method on this client.
    /// Overridden by method-level config and by per-call <c>RequestOptions</c>.
    /// </summary>
    public IGadgetsClient SetConfig(global::FernApi.Config.RequestConfig config)
    {
        _service.SetConfig(config);
        return this;
    }

    /// <summary>
    /// Sets method-level configuration for <c>ListGadgetsAsync</c>.
    /// Overrides service-level configuration; overridden by per-call <c>RequestOptions</c>.
    /// </summary>
    public IGadgetsClient SetListGadgetsAsyncConfig(global::FernApi.Config.RequestConfig config)
    {
        _service.SetListGadgetsAsyncConfig(config);
        return this;
    }

    public WithRawResponseTask<ListGadgetsResponse> ListGadgetsAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default
    ) =>
        new WithRawResponseTask<ListGadgetsResponse>(
            WithRawResponseAdapter.Wrap(
                options,
                () => _service.ListGadgetsAsync(cancellationToken: cancellationToken)
            )
        );
}
