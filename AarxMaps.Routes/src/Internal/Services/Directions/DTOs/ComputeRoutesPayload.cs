using System.Text.Json.Serialization;
using AarxMaps.Routes.Internal.Common.DTOs;

namespace AarxMaps.Routes.Internal.Services.Directions.DTOs;

// computeRoutes request body. Enums and timestamps are already in Google's wire format.
internal sealed class ComputeRoutesPayload {
    public WaypointDto Origin { get; set; } = null!;
    public WaypointDto Destination { get; set; } = null!;
    public List<WaypointDto>? Intermediates { get; set; }
    public string? TravelMode { get; set; }
    public string? RoutingPreference { get; set; }
    public string? PolylineQuality { get; set; }
    public string? DepartureTime { get; set; }
    public string? ArrivalTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ComputeAlternativeRoutes { get; set; }

    public RouteModifiersDto? RouteModifiers { get; set; }
    public string? Units { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool OptimizeWaypointOrder { get; set; }

    public string? TrafficModel { get; set; }
    public TransitPreferencesDto? TransitPreferences { get; set; }
}
