using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a rectangular area defined by its south-west and north-east corners.
/// </summary>
public sealed class Viewport {
    /// <summary>
    /// Gets the south-west corner.
    /// </summary>
    public LatLng Low { get; internal set; } = null!;

    /// <summary>
    /// Gets the north-east corner.
    /// </summary>
    public LatLng High { get; internal set; } = null!;
}
