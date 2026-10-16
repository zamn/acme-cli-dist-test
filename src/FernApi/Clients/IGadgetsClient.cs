using FernApi.Core;
using global::System.Threading;
using OneOf;

namespace FernApi;

/// <summary>
/// Fern-shape interface for GadgetsClient. Mirrors the public surface of <c>GadgetsClient</c>
/// (methods and child clients) so callers can depend on the abstraction (mockability). FSM-144.
/// </summary>
public partial interface IGadgetsClient
{
    /// <summary>
    /// Sets service-level configuration for every method on this client.
    /// Overridden by method-level config and by per-call <c>RequestOptions</c>.
    /// </summary>
    IGadgetsClient SetConfig(global::FernApi.Config.RequestConfig config);

    /// <summary>
    /// Sets method-level configuration for <c>ListGadgetsAsync</c>.
    /// Overrides service-level configuration; overridden by per-call <c>RequestOptions</c>.
    /// </summary>
    IGadgetsClient SetListGadgetsAsyncConfig(global::FernApi.Config.RequestConfig config);

    WithRawResponseTask<ListGadgetsResponse> ListGadgetsAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default
    );
}

public partial class GadgetsClient : IGadgetsClient { }
