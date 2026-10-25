using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents consecutive steps of a leg that share the same travel mode (e.g., a walk, then a bus ride).
/// </summary>
public sealed class MultiModalSegment {
    /// <summary>
    /// Gets the index in <see cref="RouteLeg.Steps"/> of the first step of the segment.
    /// </summary>
    public int StepStartIndex { get; internal set; }

    /// <summary>
    /// Gets the index in <see cref="RouteLeg.Steps"/> of the last step of the segment.
    /// </summary>
    public int StepEndIndex { get; internal set; }

    /// <summary>
    /// Gets the instruction to show for the segment.
    /// </summary>
    public NavigationInstruction? NavigationInstruction { get; internal set; }

    /// <summary>
    /// Gets the travel mode shared by the steps of the segment.
    /// </summary>
    public TravelMode TravelMode { get; internal set; }
}
