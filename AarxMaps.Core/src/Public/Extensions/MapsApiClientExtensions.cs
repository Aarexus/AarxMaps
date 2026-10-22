using System.Runtime.CompilerServices;
using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Internal.Json;
using AarxMaps.Core.Models;

namespace AarxMaps.Core.Extensions;

/// <summary>
/// Extension methods over <see cref="IMapsApiClient"/> that add higher-level behavior on top of
/// the raw client, such as streaming deserialization.
/// </summary>
/// <remarks>
/// These methods work on any <see cref="IMapsApiClient"/> registration, including decorated
/// chains (for example caching), because they depend only on the interface contract.
/// <para>
/// <b>Ownership:</b> this class is the only place that holds a <see cref="StreamResponse"/>.
/// It creates the response, owns it for the full lifetime of the enumeration, and disposes it
/// in the same scope, so consumers never see or release any HTTP resource.
/// </para>
/// <para>
/// <b>Error mapping:</b> failures are surfaced as <see cref="MapsApiException"/> (or a subclass),
/// whether they occur while sending, while deserializing, or mid-stream. Caller cancellation is
/// the exception and propagates as <see cref="OperationCanceledException"/>.
/// </para>
/// </remarks>
public static class MapsApiClientExtensions {
    /// <summary>
    /// Sends <paramref name="request"/> and yields the elements of the top-level JSON array
    /// in the response as they arrive.
    /// </summary>
    /// <remarks>
    /// <b>Deferred execution:</b> nothing happens until enumeration starts. The HTTP request is
    /// sent on the first <c>MoveNextAsync</c>, so HTTP errors (<see cref="MapsApiException"/> and
    /// subclasses) surface on the first iteration.
    /// <para>
    /// <b>Ownership:</b> the response and its connection are released when enumeration
    /// completes, throws, is cancelled, or the consumer exits early (<c>break</c>/<c>return</c>).
    /// </para>
    /// <para>
    /// <b>Single use:</b> each enumeration sends a new request (and consumes quota). Enumerate once,
    /// or materialize the results.
    /// </para>
    /// <para>
    /// <b>Order:</b> elements arrive in no guaranteed order. Key on the element's own indices,
    /// never on its position in the stream.
    /// </para>
    /// <para>
    /// This path is never cached, and <c>null</c> elements are skipped.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="client"/> is <c>null</c> (thrown when enumeration starts).
    /// </exception>
    /// <exception cref="MapsApiException">
    /// HTTP error, malformed JSON, or the connection failed mid-stream.
    /// </exception>
    public static async IAsyncEnumerable<T> SendAndEnumerateAsync<T>(
        this IMapsApiClient client, MapsApiRequest request, [EnumeratorCancellation] CancellationToken ct
    ) {
        ArgumentNullException.ThrowIfNull(client);

        // Created and owned in this scope. Disposed in reverse order: enumerator, then response.
        var response = await client.SendStreamAsync(request, ct).ConfigureAwait(false);
        await using var responseLifetime = response.ConfigureAwait(false);

        var enumerator = MapsJsonSerializer
            .DeserializeStreamAsync<T>(response.Stream, ct)
            .GetAsyncEnumerator(ct);
        await using var enumeratorLifetime = enumerator.ConfigureAwait(false);

        while (true) {
            bool hasNext;
            try {
                hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
            }
            catch (IOException ex) {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException("The enumeration was cancelled.", ex, ct);

                throw new MapsApiException("The Maps API stream was interrupted.", inner: ex);
            }

            if (!hasNext) yield break;

            yield return enumerator.Current;
        }
    }
}