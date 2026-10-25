using System.Text.Json;
using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Provides functionality for computing routes between locations.
/// </summary>
/// <remarks>
/// This service wraps the Google Routes API <c>computeRoutes</c> method.
/// <para>
/// <b>Registration:</b> available after calling <c>AddAarxMaps()</c> followed by <c>AddAarxMapsRoutes()</c>.
/// </para>
/// </remarks>
public interface IDirectionsService {
    /// <summary>
    /// Computes the route (and optionally alternatives) for the request.
    /// </summary>
    /// <param name="routeRequest">
    /// The waypoints and routing options, built with <see cref="RouteRequestBuilder"/>.
    /// </param>
    /// <param name="detailLevel">
    /// How much of each route to request. Defaults to <see cref="RouteDetailLevel.Standard"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="RouteResult"/> with the computed routes, empty when no route exists.
    /// </returns>
    /// <remarks>
    /// <para><b>Detail levels:</b></para>
    /// <list type="bullet">
    /// <item><description><see cref="RouteDetailLevel.Summary"/>: labels, distance, durations, description,
    /// warnings, travel advisory, localized values and the optimized waypoint order.</description></item>
    /// <item><description><see cref="RouteDetailLevel.Standard"/>: Summary plus the route polyline, the viewport
    /// and each leg's distance, durations, start and end locations and localized values.</description></item>
    /// <item><description><see cref="RouteDetailLevel.Full"/>: Summary plus the route polyline, the viewport,
    /// the route token and complete legs (polylines, steps, segments and transit details).</description></item>
    /// </list>
    /// <para><b>Caching:</b> when a cache layer is registered, identical requests are served from the cache.
    /// A request without a departure time means "now", so cached traffic-aware durations are as old as the
    /// cache entry. Returned instances may be shared and must not be mutated.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="routeRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsInvalidRequestException">
    /// <paramref name="detailLevel"/> or an enum value in the request is not defined, or Google rejected
    /// the request (400), for example because an address could not be geocoded.
    /// </exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<RouteResult> GetRoutesAsync(
        RouteRequest routeRequest,
        RouteDetailLevel detailLevel = RouteDetailLevel.Standard,
        CancellationToken ct = default
    );

    /// <summary>
    /// Computes routes for the request and returns Google's raw JSON, limited to the specified fields.
    /// </summary>
    /// <param name="routeRequest">
    /// The waypoints and routing options, built with <see cref="RouteRequestBuilder"/>.
    /// </param>
    /// <param name="fields">
    /// The response fields to return (e.g., "routes.duration", "routes.legs.steps"), as used in Google's
    /// field mask. Values are trimmed and duplicates removed. <c>"*"</c> returns every field.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="JsonDocument"/> containing the raw API response.
    /// </returns>
    /// <remarks>
    /// Useful for fields not mapped by the SDK.
    /// <para><b>Field mask:</b> when the request optimizes the waypoint order, the fields must include
    /// <c>routes.optimizedIntermediateWaypointIndex</c>, or Google rejects the request.</para>
    /// <para><b>Ownership:</b> the caller owns the returned document and must dispose it
    /// (<c>using var json = await ...</c>). Every call returns a new document, even from the cache.</para>
    /// <para><b>Caching:</b> responses are cached like other requests.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="routeRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsInvalidRequestException">
    /// <paramref name="fields"/> is null, empty or contains empty values, an enum value in the request is not
    /// defined, or Google rejected the request (400), for example because of an unknown field.
    /// </exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<JsonDocument> GetRoutesRawAsync(
        RouteRequest routeRequest,
        IEnumerable<string> fields,
        CancellationToken ct = default
    );
}
