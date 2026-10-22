using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models;

namespace AarxMaps.Core.Abstractions;

/// <summary>
/// Client capable of sending requests to a Maps API endpoint. Implemented by the HTTP client and by
/// decorators such as caching.
/// </summary>
/// <remarks>
/// Implementations execute the HTTP request, map failures to <see cref="MapsApiException"/>,
/// and return either a deserialized model or an owned response stream.
/// <para>
/// <b>Contract for decorators:</b> a decorator must preserve the semantics documented on each
/// member (error mapping, ownership, cancellation).
/// </para>
/// </remarks>
public interface IMapsApiClient {
     /// <summary>
    /// Sends a request to the Maps API and deserializes the complete response body into
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response body into.</typeparam>
    /// <param name="apiRequest">
    /// The request description: endpoint, headers, payload, HTTP method, etc.
    /// </param>
    /// <param name="ct">A token to cancel the request and the body read.</param>
    /// <returns>The deserialized response. Never <c>null</c>.</returns>
    /// <remarks>
    /// The body is fully buffered before deserialization. The underlying HTTP response is
    /// owned and disposed by the implementation on every path, so the caller has nothing to release.
    /// <para>
    /// Implementations may serve this call from a cache (for example a caching decorator),
    /// unless <see cref="MapsApiRequest.Cacheable"/> is <c>false</c>.
    /// </para>
    /// <para>
    /// <b>Ownership:</b> when <typeparamref name="T"/> is <see cref="System.Text.Json.JsonDocument"/>,
    /// the caller owns the returned document and must dispose it. Caching decorators return a new
    /// document on every call, so disposing it never affects the cache.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="apiRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API responded 401 or 403.</exception>
    /// <exception cref="MapsRateLimitException">The API responded 429.</exception>
    /// <exception cref="MapsNotFoundException">The API responded 404.</exception>
    /// <exception cref="MapsInvalidRequestException">The API responded 400.</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, an <c>HttpClient.Timeout</c> expiry,
    /// an empty body, or a body that cannot be deserialized to <typeparamref name="T"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<T> SendAsync<T>(MapsApiRequest apiRequest, CancellationToken ct);

     /// <summary>
    /// Sends a request to the Maps API and returns the response body as an unread stream,
    /// without buffering it.
    /// </summary>
    /// <param name="apiRequest">
    /// The request description: endpoint, headers, payload, HTTP method, etc.
    /// </param>
    /// <param name="ct">A token to cancel sending the request and waiting for the response headers.</param>
    /// <returns>
    /// A <see cref="StreamResponse"/> positioned at the start of the body, once the response
    /// headers have arrived with a success status.
    /// </returns>
    /// <remarks>
    /// Intended for endpoints that return large or incrementally produced payloads, such as the
    /// Routes <c>computeRouteMatrix</c> top-level JSON array. Most callers should use the
    /// <c>StreamAsync&lt;T&gt;</c> extension instead, which manages the lifetime for them.
    /// <para>
    /// <b>Ownership:</b> on success, the caller owns the returned <see cref="StreamResponse"/> and
    /// must dispose it (<c>await using</c>), even if it never reads the stream. On failure, the
    /// implementation has already disposed the underlying response before throwing, so there is
    /// nothing to release.
    /// </para>
    /// <para>
    /// <b>Cancellation:</b> <paramref name="ct"/> covers only this call. To cancel body reads,
    /// the consumer must pass a token to whatever reads the stream. <c>HttpClient.Timeout</c> also
    /// ends when headers arrive and does not limit the body read.
    /// </para>
    /// <para>
    /// <b>Caching:</b> this path is never cached, and decorators must forward it unchanged.
    /// </para>
    /// <para>
    /// Errors that occur after this method returns, such as a dropped connection or an error
    /// payload inside a 200 response, are not reported here. The consumer sees them while reading.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="apiRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API responded 401 or 403.</exception>
    /// <exception cref="MapsRateLimitException">The API responded 429.</exception>
    /// <exception cref="MapsNotFoundException">The API responded 404.</exception>
    /// <exception cref="MapsInvalidRequestException">The API responded 400.</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, or an <c>HttpClient.Timeout</c> expiry
    /// before the headers arrived.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<StreamResponse> SendStreamAsync(MapsApiRequest apiRequest, CancellationToken ct);
}