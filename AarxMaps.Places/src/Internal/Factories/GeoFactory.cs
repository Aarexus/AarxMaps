using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Models.Geometry;

namespace AarxMaps.Places.Internal.Factories;

internal static class GeoFactory {
    private const double MaxRadiusMeters = 50_000;

    // METHS
    internal static LocationBias CreateLocationBias(LatLng center, double radius)
        => new () { Circle = CreateCircle(center, radius) };
    internal static LocationBias CreateLocationBias(LatLng southWest, LatLng northEast)
        => new () { Rectangle = CreateRectangle(southWest, northEast) };

    internal static LocationRestriction CreateLocationRestriction(LatLng center, double radius)
        => new () { Circle = CreateCircle(center, radius) };
    internal static LocationRestriction CreateLocationRestriction(LatLng southWest, LatLng northEast)
        => new () { Rectangle = CreateRectangle(southWest, northEast) };

    // INNER METHS
    private static GeoCircle CreateCircle(LatLng center, double radius) {
        if (center is null) throw new MapsInvalidRequestException("Circle center must be specified.");
        if (radius is <= 0 or > MaxRadiusMeters)
            throw new MapsInvalidRequestException($"Radius must be greater than 0 and at most {MaxRadiusMeters} meters.");

        return new () {
            Center = center,
            Radius = radius
        };
    }
    private static GeoRectangle CreateRectangle(LatLng southWest, LatLng northEast) {
        if (southWest is null || northEast is null)
            throw new MapsInvalidRequestException("Both rectangle corners must be specified.");
        if (southWest.Latitude > northEast.Latitude)
            throw new MapsInvalidRequestException("The south-west corner latitude must not be greater than the north-east corner latitude.");

        return new () {
            Low = southWest,
            High = northEast
        };
    }
}
