namespace AarxMaps.Routes.Internal.Common.DTOs;

// Shapes used in both requests and responses.
// Google omits zero values, so a missing latitude or longitude is 0.

internal sealed class LocationDto {
    public LatLngDto? LatLng { get; set; }
    public int? Heading { get; set; }
}

internal sealed class LatLngDto {
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
