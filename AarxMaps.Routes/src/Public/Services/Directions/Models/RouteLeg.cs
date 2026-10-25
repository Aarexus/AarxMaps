using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents the part of a route between two consecutive waypoints that are not via waypoints.
/// </summary>
public sealed class RouteLeg {
    /// <summary>
    /// Gets the travel distance of the leg, in meters.
    /// </summary>
    public int DistanceMeters { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the leg, including traffic when it was requested.
    /// </summary>
    public TimeSpan Duration { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the leg without taking traffic into account.
    /// </summary>
    public TimeSpan StaticDuration { get; internal set; }

    /// <summary>
    /// Gets where the leg starts (the waypoint snapped to the road).
    /// </summary>
    public Location? StartLocation { get; internal set; }

    /// <summary>
    /// Gets where the leg ends (the waypoint snapped to the road).
    /// </summary>
    public Location? EndLocation { get; internal set; }

    /// <summary>
    /// Gets the path of the leg as an encoded polyline. Requested with <see cref="RouteDetailLevel.Full"/>.
    /// </summary>
    public string? EncodedPolyline { get; internal set; }

    /// <summary>
    /// Gets the navigation steps of the leg. Requested with <see cref="RouteDetailLevel.Full"/>.
    /// </summary>
    public IReadOnlyList<RouteStep> Steps { get; internal set; } = [];

    /// <summary>
    /// Gets groups of consecutive steps that share a travel mode. Transit routes only, requested with
    /// <see cref="RouteDetailLevel.Full"/>.
    /// </summary>
    public IReadOnlyList<MultiModalSegment> Segments { get; internal set; } = [];

    /// <summary>
    /// Gets display text for the leg's distance and durations.
    /// </summary>
    public RouteLegLocalizedValues? LocalizedValues { get; internal set; }
}
