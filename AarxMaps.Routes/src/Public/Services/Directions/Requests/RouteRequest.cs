using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a request for routes between an origin and a destination.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="RouteRequestBuilder"/>, which checks the combinations
/// Google rejects or silently ignores. <c>null</c> values are not sent, so Google's defaults apply.
/// </remarks>
public sealed class RouteRequest {
    internal RouteRequest() {}

    /// <summary>
    /// Gets the waypoint the route starts at.
    /// </summary>
    public Waypoint Origin { get; internal set; } = null!;

    /// <summary>
    /// Gets the waypoint the route ends at.
    /// </summary>
    public Waypoint Destination { get; internal set; } = null!;

    /// <summary>
    /// Gets the waypoints between the origin and the destination, in visiting order (up to 25).
    /// </summary>
    public IReadOnlyList<Waypoint> Intermediates { get; internal set; } = [];

    /// <summary>
    /// Gets the mode of transportation, or <c>null</c> for Google's default (<see cref="TravelMode.Drive"/>).
    /// </summary>
    public TravelMode? TravelMode { get; internal set; }

    /// <summary>
    /// Gets how traffic is taken into account, or <c>null</c> for Google's default
    /// (<see cref="RoutingPreference.TrafficUnaware"/>).
    /// </summary>
    public RoutingPreference? RoutingPreference { get; internal set; }

    /// <summary>
    /// Gets how detailed the returned polylines are, or <c>null</c> for Google's default
    /// (<see cref="PolylineQuality.Overview"/>).
    /// </summary>
    public PolylineQuality? PolylineQuality { get; internal set; }

    /// <summary>
    /// Gets the departure time, or <c>null</c> for "now".
    /// </summary>
    public DateTimeOffset? DepartureTime { get; internal set; }

    /// <summary>
    /// Gets the desired arrival time. Transit only.
    /// </summary>
    public DateTimeOffset? ArrivalTime { get; internal set; }

    /// <summary>
    /// Gets whether alternative routes are computed in addition to the main route.
    /// </summary>
    public bool ComputeAlternativeRoutes { get; internal set; }

    /// <summary>
    /// Gets whether Google may reorder the intermediate waypoints to minimize the route's cost.
    /// </summary>
    public bool OptimizeWaypointOrder { get; internal set; }

    /// <summary>
    /// Gets the conditions the route should satisfy, such as roads to avoid.
    /// </summary>
    public RouteModifiers? RouteModifiers { get; internal set; }

    /// <summary>
    /// Gets the units of measure for display text, or <c>null</c> to let Google infer them from the origin.
    /// </summary>
    public Units? Units { get; internal set; }

    /// <summary>
    /// Gets the assumptions used for travel time in traffic, or <c>null</c> for Google's default
    /// (<see cref="TrafficModel.BestGuess"/>).
    /// </summary>
    public TrafficModel? TrafficModel { get; internal set; }

    /// <summary>
    /// Gets the preferences for transit routes.
    /// </summary>
    public TransitPreferences? TransitPreferences { get; internal set; }
}
