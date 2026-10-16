namespace FernApi;

/// <summary>
/// Fern-shape root client interface exposing the top-level sub-clients as their interface type. A
/// deeper client is reached through its parent (<c>client.Fleet.Attributes</c>), so only top-level
/// nodes appear here. FSM-144 / FSM-936.
/// </summary>
public partial interface IFernApiClient
{
    public IWidgetsClient Widgets { get; }
    public IGadgetsClient Gadgets { get; }
}
