using FernApi.Core;

namespace FernApi;

[Serializable]
public partial class RequestOptions : IRequestOptions
{
    /// <summary>
    /// The Base URL for the API.
    /// </summary>
    public string? BaseUrl { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    }

    /// <summary>
    /// The http client used to make requests.
    /// </summary>
    public HttpClient? HttpClient { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    }

    /// <summary>
    /// Additional headers to be sent with the request.
    /// Headers previously set with matching keys will be overwritten.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string?>> AdditionalHeaders { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    } = [];

    /// <summary>
    /// The max number of retries to attempt.
    /// </summary>
    public int? MaxRetries { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    }

    /// <summary>
    /// The full retry configuration for this call: the delay, the maximum delay cap, the backoff
    /// multiplier, jitter, the retryable status codes, the retryable HTTP methods and the maximum
    /// server <c>Retry-After</c> delay to honor. Every member is optional and an unset one falls
    /// back to the service- or SDK-level configuration.
    /// </summary>
    /// <remarks>
    /// <see cref="MaxRetries"/> is the attempt count on its own and takes precedence over this
    /// object's <c>MaxRetryAttempts</c>, so the two compose: set <see cref="MaxRetries"/> for the
    /// count alone, or reach for <see cref="Retry"/> when the call needs the other knobs too.
    /// </remarks>
    public global::FernApi.Config.RetryConfig? Retry { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    }

    /// <summary>
    /// The timeout for the request.
    /// </summary>
    public TimeSpan? Timeout { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    }

    /// <summary>
    /// Additional query parameters sent with the request.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> AdditionalQueryParameters { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    } = Enumerable.Empty<KeyValuePair<string, string>>();

    /// <summary>
    /// Additional body properties sent with the request.
    /// This is only applied to JSON requests.
    /// </summary>
    public object? AdditionalBodyProperties { get;
#if NET5_0_OR_GREATER
        init;
#else
        set;
#endif
    }
}
