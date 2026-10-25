using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Represents the route from one origin to one destination of a route matrix.
/// </summary>
/// <remarks>
/// The metrics are only meaningful when <see cref="Condition"/> is <see cref="RouteMatrixElementCondition.RouteExists"/>.
/// Google omits zero values, so an origin equal to its destination has a distance and durations of zero.
/// </remarks>
public sealed class RouteMatrixElement {
    /// <summary>
    /// Gets the zero-based index of the origin in the request.
    /// </summary>
    public int OriginIndex { get; internal set; }

    /// <summary>
    /// Gets the zero-based index of the destination in the request.
    /// </summary>
    public int DestinationIndex { get; internal set; }

    /// <summary>
    /// Gets whether a route was found.
    /// </summary>
    public RouteMatrixElementCondition Condition { get; internal set; }

    /// <summary>
    /// Gets the error of the element, or <c>null</c> when it was computed successfully.
    /// </summary>
    /// <remarks>
    /// One element failing (for example an address that cannot be geocoded) does not fail the others.
    /// </remarks>
    public RouteMatrixElementStatus? Status { get; internal set; }

    /// <summary>
    /// Gets the travel distance of the route, in meters.
    /// </summary>
    public int DistanceMeters { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the route. Includes traffic when a traffic-aware routing
    /// preference was requested.
    /// </summary>
    public TimeSpan Duration { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the route without taking traffic into account.
    /// Requested with <see cref="RouteDetailLevel.Standard"/> and above.
    /// </summary>
    public TimeSpan StaticDuration { get; internal set; }

    /// <summary>
    /// Gets additional information about the route, such as the transit fare.
    /// Requested with <see cref="RouteDetailLevel.Full"/>.
    /// </summary>
    public RouteTravelAdvisory? TravelAdvisory { get; internal set; }

    /// <summary>
    /// Gets how the element was computed when Google could not honor the requested routing preference,
    /// or <c>null</c> when no fallback happened.
    /// </summary>
    public FallbackInfo? FallbackInfo { get; internal set; }

    /// <summary>
    /// Gets display text for the distance, durations and transit fare.
    /// Requested with <see cref="RouteDetailLevel.Standard"/> and above.
    /// </summary>
    public RouteLocalizedValues? LocalizedValues { get; internal set; }
}
