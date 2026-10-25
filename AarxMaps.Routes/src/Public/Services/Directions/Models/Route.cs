using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a route from the origin to the destination, through any intermediate waypoints.
/// </summary>
/// <remarks>
/// Which properties are filled depends on the <see cref="RouteDetailLevel"/> of the request
/// (see <see cref="IDirectionsService.GetRoutesAsync"/>). Properties outside the level are empty, zero or <c>null</c>.
/// </remarks>
public sealed class Route {
    /// <summary>
    /// Gets the labels Google assigned to the route, such as <see cref="RouteLabel.DefaultRoute"/>.
    /// </summary>
    public IReadOnlyList<RouteLabel> Labels { get; internal set; } = [];

    /// <summary>
    /// Gets the travel distance of the route, in meters.
    /// </summary>
    public int DistanceMeters { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the route. Includes traffic when a traffic-aware routing
    /// preference was requested; otherwise it equals <see cref="StaticDuration"/>.
    /// </summary>
    public TimeSpan Duration { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the route without taking traffic into account.
    /// </summary>
    public TimeSpan StaticDuration { get; internal set; }

    /// <summary>
    /// Gets a short description of the route (e.g., the main road it takes), when Google returns one.
    /// </summary>
    public string? Description { get; internal set; }

    /// <summary>
    /// Gets warnings to show when displaying the route.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; internal set; } = [];

    /// <summary>
    /// Gets the bounding box of the route's polyline.
    /// </summary>
    public Viewport? Viewport { get; internal set; }

    /// <summary>
    /// Gets the overall path of the route as an encoded polyline
    /// (Google's polyline encoding algorithm).
    /// </summary>
    public string? EncodedPolyline { get; internal set; }

    /// <summary>
    /// Gets the legs of the route. There is one leg between every two consecutive waypoints that are not via waypoints.
    /// </summary>
    public IReadOnlyList<RouteLeg> Legs { get; internal set; } = [];

    /// <summary>
    /// Gets the new order of the intermediate waypoints when waypoint order optimization was requested.
    /// Empty otherwise.
    /// </summary>
    /// <remarks>
    /// Each value is a zero-based index into the request's intermediates. For example <c>[2, 0, 1]</c>
    /// means the route visits the third intermediate first.
    /// </remarks>
    public IReadOnlyList<int> OptimizedWaypointOrder { get; internal set; } = [];

    /// <summary>
    /// Gets additional information about the route, such as the transit fare.
    /// </summary>
    public RouteTravelAdvisory? TravelAdvisory { get; internal set; }

    /// <summary>
    /// Gets display text for the route's distance, durations and transit fare.
    /// </summary>
    public RouteLocalizedValues? LocalizedValues { get; internal set; }

    /// <summary>
    /// Gets an opaque token that lets the Google Navigation SDK rebuild this exact route, when Google returns one.
    /// </summary>
    public string? RouteToken { get; internal set; }
}
