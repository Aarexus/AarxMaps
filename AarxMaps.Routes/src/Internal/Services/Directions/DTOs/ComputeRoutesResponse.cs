using AarxMaps.Routes.Internal.Common.DTOs;

namespace AarxMaps.Routes.Internal.Services.Directions.DTOs;

// MAIN MODEL
// Google omits zero values (0, false), so value-type fields default to them
internal sealed class ComputeRoutesResponse {
    public List<RouteDto> Routes { get; set; } = [];
    public FallbackInfoDto? FallbackInfo { get; set; }
}

// ROUTE MODELS
internal sealed class RouteDto {
    public List<string> RouteLabels { get; set; } = [];
    public List<RouteLegDto> Legs { get; set; } = [];
    public int DistanceMeters { get; set; }
    public string? Duration { get; set; }
    public string? StaticDuration { get; set; }
    public PolylineDto? Polyline { get; set; }
    public string? Description { get; set; }
    public List<string> Warnings { get; set; } = [];
    public ViewportDto? Viewport { get; set; }
    public RouteTravelAdvisoryDto? TravelAdvisory { get; set; }
    public List<int> OptimizedIntermediateWaypointIndex { get; set; } = [];
    public LocalizedValuesDto? LocalizedValues { get; set; }
    public string? RouteToken { get; set; }
}

internal sealed class ViewportDto {
    public LatLngDto? Low { get; set; }
    public LatLngDto? High { get; set; }
}

internal sealed class PolylineDto {
    public string? EncodedPolyline { get; set; }
}

// LEG MODELS
internal sealed class RouteLegDto {
    public int DistanceMeters { get; set; }
    public string? Duration { get; set; }
    public string? StaticDuration { get; set; }
    public PolylineDto? Polyline { get; set; }
    public LocationDto? StartLocation { get; set; }
    public LocationDto? EndLocation { get; set; }
    public List<RouteLegStepDto> Steps { get; set; } = [];
    public LocalizedValuesDto? LocalizedValues { get; set; }
    public StepsOverviewDto? StepsOverview { get; set; }
}

internal sealed class StepsOverviewDto {
    public List<MultiModalSegmentDto> MultiModalSegments { get; set; } = [];
}

internal sealed class MultiModalSegmentDto {
    public int StepStartIndex { get; set; }
    public int StepEndIndex { get; set; }
    public NavigationInstructionDto? NavigationInstruction { get; set; }
    public string? TravelMode { get; set; }
}

// STEP MODELS
internal sealed class RouteLegStepDto {
    public int DistanceMeters { get; set; }
    public string? StaticDuration { get; set; }
    public PolylineDto? Polyline { get; set; }
    public LocationDto? StartLocation { get; set; }
    public LocationDto? EndLocation { get; set; }
    public NavigationInstructionDto? NavigationInstruction { get; set; }
    public LocalizedValuesDto? LocalizedValues { get; set; }
    public TransitDetailsDto? TransitDetails { get; set; }
    public string? TravelMode { get; set; }
}

internal sealed class NavigationInstructionDto {
    public string? Maneuver { get; set; }
    public string? Instructions { get; set; }
}

// TRANSIT MODELS
internal sealed class TransitDetailsDto {
    public TransitStopDetailsDto? StopDetails { get; set; }
    public TransitDetailsLocalizedValuesDto? LocalizedValues { get; set; }
    public string? Headsign { get; set; }
    public TransitLineDto? TransitLine { get; set; }
    public int StopCount { get; set; }
}

internal sealed class TransitStopDetailsDto {
    public TransitStopDto? ArrivalStop { get; set; }
    public string? ArrivalTime { get; set; }
    public TransitStopDto? DepartureStop { get; set; }
    public string? DepartureTime { get; set; }
}

internal sealed class TransitStopDto {
    public string? Name { get; set; }
    public LocationDto? Location { get; set; }
}

internal sealed class TransitDetailsLocalizedValuesDto {
    public LocalizedTimeDto? ArrivalTime { get; set; }
    public LocalizedTimeDto? DepartureTime { get; set; }
}

internal sealed class LocalizedTimeDto {
    public LocalizedTextDto? Time { get; set; }
}

internal sealed class TransitLineDto {
    public List<TransitAgencyDto> Agencies { get; set; } = [];
    public string? Name { get; set; }
    public string? NameShort { get; set; }
    public string? Color { get; set; }
    public string? TextColor { get; set; }
    public TransitVehicleDto? Vehicle { get; set; }
}

internal sealed class TransitAgencyDto {
    public string? Name { get; set; }
    public string? Uri { get; set; }
}

internal sealed class TransitVehicleDto {
    public LocalizedTextDto? Name { get; set; }
    public string? Type { get; set; }
}
