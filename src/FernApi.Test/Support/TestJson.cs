using System.Text.Json;

namespace FernApi.Tests;

/// <summary>
/// The exact <see cref="JsonSerializerOptions"/> the SDK serializes and deserializes with.
/// </summary>
/// <remarks>
/// Reached by deriving from the SDK's own <c>BaseService</c> rather than rebuilding an equivalent
/// options object here. Rebuilding would mean a second copy of the converter list, the naming
/// policy and the ignore conditions living in the test project, free to drift from the one the wire
/// actually uses — at which point a round-trip test proves something about the copy, not about the
/// SDK. <c>_jsonSerializerOptions</c> is <c>protected</c>, so inheritance is the only access there
/// is; the transport argument is never used, because nothing here ever sends a request.
/// </remarks>
internal sealed class TestJson : global::FernApi.Services.BaseService
{
    private TestJson()
        : base(null!) { }

    private static readonly TestJson Shared = new();

    internal static JsonSerializerOptions Options => Shared._jsonSerializerOptions;

    internal static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;

    internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
