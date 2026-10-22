using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Places.Common.Models.Geometry;

/// <summary>
/// Represents a circular geographic area defined by a center point and radius.
/// </summary>
/// <remarks>
/// This model is used to define geographic constraints or biases
/// when performing location-based queries.
/// </remarks>
public sealed class GeoCircle {
    /// <summary>
    /// Gets the center point of the circle.
    /// </summary>
    public LatLng Center { get; internal set; } = null!;
    
    /// <summary>
    /// Gets the radius of the circle in meters.
    /// </summary>
    public double Radius { get; internal set; }
}