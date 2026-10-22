using System.Collections.Concurrent;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Models;
using NSubstitute;

namespace AarxMaps.Tests.Places.Helpers;

/// <summary>
/// NSubstitute IMapsApiClient that records every SendAsync call (thread-safe, so parallel calls are fine)
/// and answers per response type <c>T</c>. The last setup for a given <c>T</c> wins.
/// </summary>
internal sealed class ApiClientCapture {
    private readonly ConcurrentQueue<(MapsApiRequest Request, CancellationToken Ct)> _calls = new();

    public IMapsApiClient Client { get; } = Substitute.For<IMapsApiClient>();

    /// <summary>Snapshot of the recorded calls, in arrival order.</summary>
    public IReadOnlyList<(MapsApiRequest Request, CancellationToken Ct)> Calls => [.. _calls];

    public ApiClientCapture Handle<T>(Func<MapsApiRequest, CancellationToken, Task<T>> handler) {
        Client.SendAsync<T>(Arg.Any<MapsApiRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                var request = call.Arg<MapsApiRequest>();
                var ct = call.Arg<CancellationToken>();
                _calls.Enqueue((request, ct));
                return handler(request, ct);
            });
        return this;
    }

    public ApiClientCapture Respond<T>(T response)
        => Handle((_, _) => Task.FromResult(response));

    public ApiClientCapture Fail<T>(Exception exception)
        => Handle<T>((_, _) => Task.FromException<T>(exception));
}
