using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.DistanceMatrix;

/// <summary>
/// Represents a row of the route matrix: an origin and the route modifiers used for its routes.
/// </summary>
public sealed class RouteMatrixOrigin {
    internal RouteMatrixOrigin() {}

    /// <summary>
    /// Gets the origin waypoint.
    /// </summary>
    public Waypoint Waypoint { get; internal set; } = null!;

    /// <summary>
    /// Gets the conditions applied to every route from this origin, such as roads to avoid.
    /// </summary>
    public RouteModifiers? RouteModifiers { get; internal set; }
}
