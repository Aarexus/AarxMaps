namespace AarxMaps.Places.Common.Models.Geometry;

/// <summary>
/// Represents a geographic restriction applied to a Places request.
/// </summary>
/// <remarks>
/// A location restriction strictly limits results to the specified area,
/// ensuring that returned places fall within the defined boundary. Exactly one of
/// <see cref="Circle"/> or <see cref="Rectangle"/> is set; which shape is allowed
/// depends on the endpoint.
/// </remarks>
public sealed class LocationRestriction {
    /// <summary>
    /// Gets the circular area used to restrict results, or <c>null</c> when a rectangle is used.
    /// </summary>
    public GeoCircle? Circle { get; internal set; }

    /// <summary>
    /// Gets the rectangular area used to restrict results, or <c>null</c> when a circle is used.
    /// </summary>
    public GeoRectangle? Rectangle { get; internal set; }
}
