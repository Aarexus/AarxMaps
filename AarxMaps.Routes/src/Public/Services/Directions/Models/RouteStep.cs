using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a single navigation instruction within a leg, such as a turn.
/// </summary>
public sealed class RouteStep {
    /// <summary>
    /// Gets the travel distance of the step, in meters.
    /// </summary>
    public int DistanceMeters { get; internal set; }

    /// <summary>
    /// Gets the time needed to travel the step without taking traffic into account.
    /// </summary>
    public TimeSpan StaticDuration { get; internal set; }

    /// <summary>
    /// Gets where the step starts.
    /// </summary>
    public Location? StartLocation { get; internal set; }

    /// <summary>
    /// Gets where the step ends.
    /// </summary>
    public Location? EndLocation { get; internal set; }

    /// <summary>
    /// Gets the path of the step as an encoded polyline.
    /// </summary>
    public string? EncodedPolyline { get; internal set; }

    /// <summary>
    /// Gets the instruction to show for the step.
    /// </summary>
    public NavigationInstruction? NavigationInstruction { get; internal set; }

    /// <summary>
    /// Gets display text for the step's distance and duration.
    /// </summary>
    public RouteLegStepLocalizedValues? LocalizedValues { get; internal set; }

    /// <summary>
    /// Gets the travel mode of the step. In transit routes, steps alternate between
    /// <see cref="TravelMode.Walk"/> and <see cref="TravelMode.Transit"/>.
    /// </summary>
    public TravelMode TravelMode { get; internal set; }

    /// <summary>
    /// Gets the transit line, stops and times of the step. Only set on transit steps.
    /// </summary>
    public TransitDetails? TransitDetails { get; internal set; }
}
