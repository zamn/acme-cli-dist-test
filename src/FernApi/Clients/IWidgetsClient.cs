using FernApi.Core;
using global::System.Threading;
using OneOf;

namespace FernApi;

/// <summary>
/// Fern-shape interface for WidgetsClient. Mirrors the public surface of <c>WidgetsClient</c>
/// (methods and child clients) so callers can depend on the abstraction (mockability). FSM-144.
/// </summary>
public partial interface IWidgetsClient
{
    /// <summary>
    /// Sets service-level configuration for every method on this client.
    /// Overridden by method-level config and by per-call <c>RequestOptions</c>.
    /// </summary>
    IWidgetsClient SetConfig(global::FernApi.Config.RequestConfig config);

    /// <summary>
    /// Sets method-level configuration for <c>ListWidgetsAsync</c>.
    /// Overrides service-level configuration; overridden by per-call <c>RequestOptions</c>.
    /// </summary>
    IWidgetsClient SetListWidgetsAsyncConfig(global::FernApi.Config.RequestConfig config);

    WithRawResponseTask<ListWidgetsResponse> ListWidgetsAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default
    );
}

public partial class WidgetsClient : IWidgetsClient { }
