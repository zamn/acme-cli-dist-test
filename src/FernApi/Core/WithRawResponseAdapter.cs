using global::System;
using global::System.Collections.Generic;
using global::System.Net.Http;
using global::System.Threading;
using global::System.Threading.Tasks;

namespace FernApi.Core;

/// <summary>
/// Adapts a legacy <c>Task&lt;T&gt;</c> (or <c>Task</c>) from the internal HTTP runtime into the
/// Fern-shape <c>WithRawResponse&lt;T&gt;</c> / <c>RawResponse</c> envelope, so the public
/// <c>{Group}Client</c> surface returns <c>WithRawResponseTask&lt;T&gt;</c> while delegating execution
/// to the proven legacy runtime.
///
/// The legacy runtime deserializes and discards the <c>HttpResponseMessage</c>, so the real success
/// response is threaded out-of-band (FSM-189): each <c>Wrap*</c> installs an ambient <see cref="Capture"/>
/// before invoking the delegated call, the base service's <c>ExecuteAsync</c> records the live response
/// into it via <see cref="Record"/>, and its status/headers become the <c>RawResponse</c> — the same
/// shape the error path builds from a failed response (FSM-568), via the shared
/// <see cref="RawResponseFactory"/>. The delegated call is passed as a factory so the ambient is
/// installed before it starts running.
/// </summary>
internal static class WithRawResponseAdapter
{
    /// <summary>
    /// Mutable sink shared down the async call chain: the live success response threaded OUT of the
    /// legacy runtime (FSM-189) and the per-call <see cref="FernApi.RequestOptions"/> threaded
    /// IN to it (FSM-190).
    /// </summary>
    internal sealed class Capture
    {
        public HttpResponseMessage? Response;
        public FernApi.RequestOptions? Options;
    }

    private static readonly AsyncLocal<Capture?> Current = new();

    /// <summary>
    /// The per-call request options for the fern call currently in flight, or null when the call did not
    /// originate from a fern client. Read by the base service to apply the per-call escape hatches
    /// (additional headers / query / body) to the outgoing request (FSM-190).
    /// </summary>
    public static FernApi.RequestOptions? CurrentRequestOptions => Current.Value?.Options;

    /// <summary>
    /// Records the live success response into the ambient capture, if one is active. Called by the base
    /// service after the transport returns; a no-op when the call did not originate from a fern client.
    /// </summary>
    public static void Record(HttpResponseMessage response)
    {
        var capture = Current.Value;
        if (capture is not null)
        {
            capture.Response = response;
        }
    }

    public static async Task<WithRawResponse<T>> Wrap<T>(
        FernApi.RequestOptions? options,
        Func<Task<T>> call
    )
    {
        using var scope = CaptureScope.Install(options);
        var data = await call().ConfigureAwait(false);
        return new WithRawResponse<T> { Data = data, RawResponse = scope.ToRawResponse() };
    }

    /// <summary>
    /// Bridges a legacy <c>Task&lt;List&lt;T&gt;&gt;</c> to Fern's covariant <c>IEnumerable&lt;T&gt;</c>
    /// return: <c>Task&lt;List&lt;T&gt;&gt;</c> is invariant to <c>Task&lt;WithRawResponse&lt;IEnumerable&lt;T&gt;&gt;&gt;</c>,
    /// so the widening happens here at the boundary (the legacy runtime keeps using <c>List&lt;T&gt;</c>).
    /// </summary>
    public static async Task<WithRawResponse<IEnumerable<T>>> WrapEnumerable<T>(
        FernApi.RequestOptions? options,
        Func<Task<List<T>>> call
    )
    {
        using var scope = CaptureScope.Install(options);
        var data = await call().ConfigureAwait(false);
        return new WithRawResponse<IEnumerable<T>>
        {
            Data = data,
            RawResponse = scope.ToRawResponse(),
        };
    }

    /// <summary>
    /// Bridges a legacy binary download (<c>Task&lt;byte[]&gt;</c>) to Fern's closeable
    /// <c>WithRawResponse&lt;Stream&gt;</c> return. The legacy runtime reads the body to a <c>byte[]</c>;
    /// wrapping it in a <c>MemoryStream</c> gives the caller the same closeable-after-read response
    /// stream Fern hands back (FSDK-1859), while execution stays on the proven legacy runtime.
    /// </summary>
    public static async Task<WithRawResponse<global::System.IO.Stream>> WrapStream(
        FernApi.RequestOptions? options,
        Func<Task<byte[]>> call
    )
    {
        using var scope = CaptureScope.Install(options);
        var data = await call().ConfigureAwait(false);
        return new WithRawResponse<global::System.IO.Stream>
        {
            Data = new global::System.IO.MemoryStream(data ?? global::System.Array.Empty<byte>()),
            RawResponse = scope.ToRawResponse(),
        };
    }

    public static async Task<FernApi.RawResponse> WrapVoid(
        FernApi.RequestOptions? options,
        Func<global::System.Threading.Tasks.Task> call
    )
    {
        using var scope = CaptureScope.Install(options);
        await call().ConfigureAwait(false);
        return scope.ToRawResponse();
    }

    /// <summary>
    /// Installs the ambient <see cref="Capture"/> for the duration of one delegated call and restores the
    /// previous value on dispose. Shared by all <c>Wrap*</c> so the AsyncLocal save/restore lives in one
    /// place; each caller only expresses its differing await/return shape.
    /// </summary>
    private readonly struct CaptureScope : IDisposable
    {
        private readonly Capture? _previous;
        private readonly Capture _capture;

        private CaptureScope(Capture? previous, Capture capture)
        {
            _previous = previous;
            _capture = capture;
        }

        public static CaptureScope Install(FernApi.RequestOptions? options)
        {
            var previous = Current.Value;
            var capture = new Capture { Options = options };
            Current.Value = capture;
            return new CaptureScope(previous, capture);
        }

        // A success result always has a live response (the delegated method reaches the transport, which
        // Records it); a null capture means the call short-circuited without HTTP — fail loud rather than
        // fabricate a misleading status, matching Fern (which never builds a RawResponse without one).
        public FernApi.RawResponse ToRawResponse() =>
            _capture.Response is null
                ? throw new InvalidOperationException(
                    "No HTTP response was captured for the fern client call; the delegated method did not reach the transport."
                )
                : RawResponseFactory.FromHttpResponseMessage(_capture.Response);

        public void Dispose() => Current.Value = _previous;
    }
}
