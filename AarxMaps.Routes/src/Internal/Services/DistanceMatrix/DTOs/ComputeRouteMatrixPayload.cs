using AarxMaps.Routes.Internal.Common.DTOs;

namespace AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;

// computeRouteMatrix request body. Enums and timestamps are already in Google's wire format.
internal sealed class ComputeRouteMatrixPayload {
    public List<RouteMatrixOriginDto> Origins { get; set; } = [];
    public List<RouteMatrixDestinationDto> Destinations { get; set; } = [];
    public string? TravelMode { get; set; }
    public string? RoutingPreference { get; set; }
    public string? DepartureTime { get; set; }
    public string? ArrivalTime { get; set; }
    public string? Units { get; set; }
    public string? TrafficModel { get; set; }
    public TransitPreferencesDto? TransitPreferences { get; set; }
}

internal sealed class RouteMatrixOriginDto {
    public WaypointDto Waypoint { get; set; } = null!;
    public RouteModifiersDto? RouteModifiers { get; set; }
}

internal sealed class RouteMatrixDestinationDto {
    public WaypointDto Waypoint { get; set; } = null!;
}
