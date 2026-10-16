using global::System;
using global::System.Net.Http;

namespace FernApi.Core;

/// <summary>
/// Builds the Fern-shape <see cref="FernApi.RawResponse"/> (status code, request URL, and a
/// <see cref="ResponseHeaders"/> snapshot) from an <see cref="HttpResponseMessage"/>. Shared by the
/// success path (<see cref="WithRawResponseAdapter"/>, FSM-189) and the error path
/// (<c>FernApi.Http.Exceptions</c> factory, FSM-568) so both surface an identical RawResponse.
/// </summary>
internal static class RawResponseFactory
{
    /// <param name="response">
    /// The response to read the status code and headers from. On the success path this is the live
    /// response; on the error path it is a detached, re-buffered copy of the failed response.
    /// </param>
    /// <param name="url">
    /// Overrides the request URL. The error path passes the original request URL (the detached copy has
    /// no <c>RequestMessage</c>); the success path omits it and reads it off the live response.
    /// </param>
    internal static FernApi.RawResponse FromHttpResponseMessage(
        HttpResponseMessage response,
        Uri? url = null
    ) =>
        new()
        {
            StatusCode = response.StatusCode,
            Url = url ?? response.RequestMessage?.RequestUri ?? new Uri("about:blank"),
            Headers = ResponseHeaders.FromHttpResponseMessage(response),
        };
}
