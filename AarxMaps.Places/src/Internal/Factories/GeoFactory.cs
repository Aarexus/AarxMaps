using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Models.Geometry;

namespace AarxMaps.Places.Internal.Factories;

internal static class GeoFactory {
    private const double MaxRadiusMeters = 50_000;

    internal static LocationBias CreateLocationBias(LatLng center, double radius)
        => new () { Circle = CreateCircle(center, radius) };
    internal static LocationRestriction CreateLocationRestriction(LatLng center, double radius)
        => new () { Circle = CreateCircle(center, radius) };

    private static GeoCircle CreateCircle(LatLng center, double radius) {
        if (center is null) throw new MapsInvalidRequestException("Circle center must be specified.");
        if (radius is <= 0 or > MaxRadiusMeters)
            throw new MapsInvalidRequestException($"Radius must be greater than 0 and at most {MaxRadiusMeters} meters.");

        return new () {
            Center = center,
            Radius = radius
        };
    }
}
