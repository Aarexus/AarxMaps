namespace AarxMaps.Places.Common.Models.Geometry;

/// <summary>
/// Represents a geographic bias applied to a Places request.
/// </summary>
/// <remarks>
/// A location bias influences the ranking of results toward a specific area
/// but does not strictly restrict results to that area. Exactly one of
/// <see cref="Circle"/> or <see cref="Rectangle"/> is set.
/// </remarks>
public sealed class LocationBias {
    /// <summary>
    /// Gets the circular area used to bias results, or <c>null</c> when a rectangle is used.
    /// </summary>
    public GeoCircle? Circle { get; internal set; }

    /// <summary>
    /// Gets the rectangular area used to bias results, or <c>null</c> when a circle is used.
    /// </summary>
    public GeoRectangle? Rectangle { get; internal set; }
}
