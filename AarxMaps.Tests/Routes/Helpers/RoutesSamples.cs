using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Models;

namespace AarxMaps.Tests.Routes.Helpers;

/// <summary>Reusable request inputs.</summary>
internal static class RoutesSamples {
    public static readonly LatLng Bogota = new(4.6097, -74.0817);
    public static readonly LatLng Usaquen = new(4.6951, -74.0305);
    public static readonly LatLng Chapinero = new(4.6533, -74.0636);

    public static Waypoint At(LatLng location, int? heading = null) => new WaypointBuilder().FromLocation(location, heading).Build();
    public static Waypoint Place(string placeId) => new WaypointBuilder().FromPlaceId(placeId).Build();
    public static Waypoint Address(string address) => new WaypointBuilder().FromAddress(address).Build();
    public static Waypoint Via(LatLng location) => new WaypointBuilder().FromLocation(location).AsVia().Build();
    public static Waypoint Stopover(LatLng location) => new WaypointBuilder().FromLocation(location).AsVehicleStopover().Build();
    public static Waypoint SideOfRoad(LatLng location) => new WaypointBuilder().FromLocation(location).WithSideOfRoad().Build();
}
