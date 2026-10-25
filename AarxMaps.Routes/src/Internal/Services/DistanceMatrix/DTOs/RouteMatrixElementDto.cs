using AarxMaps.Routes.Internal.Common.DTOs;

namespace AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;

// One element of the streamed top-level array.
// Google omits zero values, so index 0 arrives absent and must default to 0.
internal sealed class RouteMatrixElementDto {
    public int OriginIndex { get; set; }
    public int DestinationIndex { get; set; }
    public StatusDto? Status { get; set; }
    public string? Condition { get; set; }
    public int DistanceMeters { get; set; }
    public string? Duration { get; set; }
    public string? StaticDuration { get; set; }
    public RouteTravelAdvisoryDto? TravelAdvisory { get; set; }
    public FallbackInfoDto? FallbackInfo { get; set; }
    public LocalizedValuesDto? LocalizedValues { get; set; }
}

// google.rpc.Status; a successful element sends an empty object (code 0 omitted)
internal sealed class StatusDto {
    public int Code { get; set; }
    public string? Message { get; set; }
}
