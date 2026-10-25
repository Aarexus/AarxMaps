using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Routes.Common.Models;

/// <summary>
/// Represents a point with an optional heading.
/// </summary>
/// <remarks>
/// Used as a waypoint source in requests (through <see cref="WaypointBuilder.FromLocation"/>)
/// and as start and end points of legs and steps in responses.
/// </remarks>
public sealed class Location {
    internal Location() {}

    /// <summary>
    /// Gets the coordinates of the point.
    /// </summary>
    public LatLng LatLng { get; internal set; } = null!;

    /// <summary>
    /// Gets the compass heading of the direction of travel, in degrees from 0 to 360
    /// (0 is north, 90 is east), or <c>null</c> when not set.
    /// </summary>
    /// <remarks>
    /// On a waypoint it tells Google which side of the road to pick up or drop off at.
    /// Only valid with <see cref="TravelMode.Drive"/> and <see cref="TravelMode.TwoWheeler"/>.
    /// </remarks>
    public int? Heading { get; internal set; }
}
