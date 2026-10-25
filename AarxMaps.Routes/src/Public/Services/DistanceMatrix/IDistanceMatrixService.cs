using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Provides functionality for computing the distance and duration between multiple origins and destinations.
/// </summary>
/// <remarks>
/// This service wraps the Google Routes API <c>computeRouteMatrix</c> method.
/// <para>
/// <b>Registration:</b> available after calling <c>AddAarxMaps()</c> followed by <c>AddAarxMapsRoutes()</c>.
/// </para>
/// </remarks>
public interface IDistanceMatrixService {
    /// <summary>
    /// Computes the route between every origin and every destination of the request.
    /// </summary>
    /// <param name="distanceRequest">
    /// The origins, destinations and routing options, built with <see cref="DistanceRequestBuilder"/>.
    /// </param>
    /// <param name="detailLevel">
    /// How much of each element to request. Defaults to <see cref="RouteDetailLevel.Standard"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request, including reading the streamed response.
    /// </param>
    /// <returns>
    /// One <see cref="RouteMatrixElement"/> per origin and destination pair, ordered by
    /// <see cref="RouteMatrixElement.OriginIndex"/> and then <see cref="RouteMatrixElement.DestinationIndex"/>.
    /// </returns>
    /// <remarks>
    /// <para><b>Detail levels:</b></para>
    /// <list type="bullet">
    /// <item><description><see cref="RouteDetailLevel.Summary"/>: indices, condition, status, distance, duration
    /// and fallback info.</description></item>
    /// <item><description><see cref="RouteDetailLevel.Standard"/>: Summary plus the static duration and the
    /// localized values.</description></item>
    /// <item><description><see cref="RouteDetailLevel.Full"/>: Standard plus the travel advisory.</description></item>
    /// </list>
    /// <para><b>Partial failures:</b> an element that cannot be computed carries its error in
    /// <see cref="RouteMatrixElement.Status"/>; the other elements are still returned.</para>
    /// <para><b>Caching:</b> never cached. Google streams the matrix, and the streaming path bypasses
    /// any cache layer.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="distanceRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsInvalidRequestException">
    /// <paramref name="detailLevel"/> or an enum value in the request is not defined, or Google rejected
    /// the whole request (400).
    /// </exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout, an unreadable response or a stream
    /// interrupted while reading.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<IReadOnlyList<RouteMatrixElement>> GetDistancesAsync(
        DistanceRequest distanceRequest,
        RouteDetailLevel detailLevel = RouteDetailLevel.Standard,
        CancellationToken ct = default
    );
}
