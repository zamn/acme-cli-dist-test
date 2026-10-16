using Environment = FernApi.Http.Environment;

namespace FernApi.Config;

/// <summary>
/// Configuration options for the FernApiClient.
/// </summary>
internal record FernApiConfig(
    /// <value>The environment to use for the SDK.</value>
    Environment? Environment = null
);
