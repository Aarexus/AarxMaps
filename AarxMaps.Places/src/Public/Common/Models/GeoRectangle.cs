using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Places.Common.Models.Geometry;

/// <summary>
/// Represents a rectangular geographic area (viewport) defined by its south-west and north-east corners.
/// </summary>
/// <remarks>
/// When <see cref="Low"/> has a greater longitude than <see cref="High"/>, the rectangle
/// crosses the 180th meridian.
/// </remarks>
public sealed class GeoRectangle {
    /// <summary>
    /// Gets the south-west corner of the rectangle.
    /// </summary>
    public LatLng Low { get; internal set; } = null!;

    /// <summary>
    /// Gets the north-east corner of the rectangle.
    /// </summary>
    public LatLng High { get; internal set; } = null!;
}
