namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents a point a route starts at, ends at or passes through.
/// </summary>
/// <remarks>
/// Built with <see cref="WaypointBuilder"/>. Exactly one of <see cref="PlaceId"/>, <see cref="Location"/>
/// and <see cref="Address"/> is set.
/// </remarks>
public sealed class Waypoint {
    internal Waypoint() {}

    /// <summary>
    /// Gets the place ID of the waypoint, or <c>null</c> when it is defined another way.
    /// </summary>
    public string? PlaceId { get; internal set; }

    /// <summary>
    /// Gets the coordinates (and optional heading) of the waypoint, or <c>null</c> when it is defined another way.
    /// </summary>
    public Location? Location { get; internal set; }

    /// <summary>
    /// Gets the address or plus code of the waypoint, or <c>null</c> when it is defined another way.
    /// </summary>
    /// <remarks>
    /// Google geocodes it on every request.
    /// </remarks>
    public string? Address { get; internal set; }

    /// <summary>
    /// Gets whether the route passes through this waypoint without stopping.
    /// </summary>
    /// <remarks>
    /// A via waypoint does not split the route into a new leg. Only valid on intermediate waypoints,
    /// and not together with waypoint order optimization.
    /// </remarks>
    public bool Via { get; internal set; }

    /// <summary>
    /// Gets whether the waypoint is a pickup or drop-off point.
    /// </summary>
    /// <remarks>
    /// Google avoids roads that are unsuitable for stopping. Only valid on waypoints defined by
    /// <see cref="Location"/>, with <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>.
    /// </remarks>
    public bool VehicleStopover { get; internal set; }

    /// <summary>
    /// Gets whether the vehicle should stop on the side of the road the waypoint is on.
    /// </summary>
    /// <remarks>
    /// Only valid with <see cref="TravelMode.Drive"/> or <see cref="TravelMode.TwoWheeler"/>.
    /// </remarks>
    public bool SideOfRoad { get; internal set; }
}
