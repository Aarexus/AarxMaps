using System.Text.Json.Serialization;

namespace AarxMaps.Routes.Internal.Common.DTOs;

// Request-side shapes shared by computeRoutes and computeRouteMatrix.
// false flags are left out of the body; Google treats a missing flag as false.

internal sealed class WaypointDto {
    public LocationDto? Location { get; set; }
    public string? PlaceId { get; set; }
    public string? Address { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Via { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool VehicleStopover { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SideOfRoad { get; set; }
}

internal sealed class RouteModifiersDto {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AvoidTolls { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AvoidHighways { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AvoidFerries { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AvoidIndoor { get; set; }

    public VehicleInfoDto? VehicleInfo { get; set; }
}

internal sealed class VehicleInfoDto {
    public string EmissionType { get; set; } = null!;
}

internal sealed class TransitPreferencesDto {
    public List<string>? AllowedTravelModes { get; set; }
    public string? RoutingPreference { get; set; }
}
