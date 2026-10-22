using AarxMaps.Core.Exceptions;

namespace AarxMaps.Core.Models.Geometry;

/// <summary>
/// Represents a geographic coordinate defined by a latitude and longitude pair.
/// </summary>
/// <remarks>
/// This model is used throughout the SDK to represent locations returned by
/// Google Maps APIs or provided as input for geographic queries.
///
/// Instances are created internally by the SDK to ensure coordinate validity
/// and consistency across modules.
/// </remarks>
public sealed class LatLng {
    /// <summary>
    /// Gets the latitude component of the coordinate in decimal degrees.
    /// </summary>
    public double Latitude { get; }
    
    /// <summary>
    /// Gets the longitude component of the coordinate in decimal degrees.
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Initializes a coordinate with the specified latitude and longitude.
    /// </summary>
    /// <param name="latitude">The latitude in decimal degrees, from -90 to 90.</param>
    /// <param name="longitude">The longitude in decimal degrees, from -180 to 180.</param>
    /// <exception cref="MapsInvalidRequestException">A coordinate is NaN or outside its valid range.</exception>
    public LatLng(double latitude, double longitude) {
        // NaN fails every comparison, so it must be rejected explicitly
        if (double.IsNaN(latitude) || latitude is < -90 or > 90)
            throw new MapsInvalidRequestException("Latitude must be between -90 and 90.");
        if (double.IsNaN(longitude) || longitude is < -180 or > 180)
            throw new MapsInvalidRequestException("Longitude must be between -180 and 180.");  

        Latitude = latitude;
        Longitude = longitude;
    }
}