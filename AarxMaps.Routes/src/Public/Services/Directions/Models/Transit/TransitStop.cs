using AarxMaps.Core.Models.Geometry;

namespace AarxMaps.Routes.Services.Directions;

/// <summary>
/// Represents a transit stop.
/// </summary>
public sealed class TransitStop {
    /// <summary>
    /// Gets the name of the stop.
    /// </summary>
    public string? Name { get; internal set; }

    /// <summary>
    /// Gets the coordinates of the stop.
    /// </summary>
    public LatLng? Location { get; internal set; }
}
