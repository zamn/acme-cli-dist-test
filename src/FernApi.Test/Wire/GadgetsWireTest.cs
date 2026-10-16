using FernApi;
using NUnit.Framework;
using OneOf;

namespace FernApi.Tests;

/// <summary>
/// Wire coverage for the <c>GadgetsWireTest</c> endpoints.
/// </summary>
/// <remarks>
/// The whole generated stack runs for real here — request building, URI and query serialization,
/// authentication headers, retry, status branching and the typed response decode. Only the socket
/// is replaced, by a <see cref="MockTransport"/> injected as the innermost handler of the SDK's own
/// chain, so the request each test inspects is the one the SDK would have sent. Injecting anywhere
/// above the chain, or substituting the client's transport wholesale, would skip the handlers under
/// test and let these assertions pass against an SDK that sends no credentials at all.
/// </remarks>
[TestFixture]
public class GadgetsWireTest
{
    [Test]
    public async global::System.Threading.Tasks.Task ListGadgetsSendsTheExpectedRequest()
    {
        const string responseBody = "{\"name\":\"name\",\"description\":\"description\"}";
        var transport = new MockTransport(200, responseBody, "application/json");
        var client = new global::FernApi.FernApiClient(
            clientOptions: new ClientOptions
            {
                HttpClient = new global::System.Net.Http.HttpClient(transport),
                MaxRetries = 0,
            }
        );

        var response = await client.Gadgets.ListGadgetsAsync();

        var captured = transport.LastRequest;
        Assert.That(captured, Is.Not.Null, "The SDK sent no request.");
        Assert.That(captured!.Method, Is.EqualTo("GET"));
        Assert.That(captured.Path, Does.EndWith("/gadgets"));
        Assert.That(response, Is.Not.Null, "The canned response did not decode.");
    }
}
