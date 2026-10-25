using AarxMaps.Core.Models.Geometry;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Common;
using AarxMaps.Routes.Internal.Services.Directions.DTOs;
using AarxMaps.Routes.Services.Directions;

namespace AarxMaps.Routes.Internal.Services.Directions;

// Builds new public models (and new lists) on every call, since cached DTOs are shared
internal static class DirectionsMapper {
    // -------------------- METHS --------------------
    internal static RouteResult ToRouteResult(ComputeRoutesResponse response)
        => new () {
            Routes = response.Routes.ConvertAll(ToRoute),
            FallbackInfo = CommonMapper.ToFallbackInfo(response.FallbackInfo)
        };

    // -------------------- INNER METHS --------------------
    // ROUTE
    private static Route ToRoute(RouteDto dto)
        => new () {
            Labels = dto.RouteLabels.ConvertAll(WireEnum<RouteLabel>.FromWire),
            DistanceMeters = dto.DistanceMeters,
            Duration = CommonMapper.ToDuration(dto.Duration),
            StaticDuration = CommonMapper.ToDuration(dto.StaticDuration),
            Description = dto.Description,
            Warnings = [.. dto.Warnings],
            Viewport = ToViewport(dto.Viewport),
            EncodedPolyline = dto.Polyline?.EncodedPolyline,
            Legs = dto.Legs.ConvertAll(ToRouteLeg),
            OptimizedWaypointOrder = [.. dto.OptimizedIntermediateWaypointIndex],
            TravelAdvisory = CommonMapper.ToTravelAdvisory(dto.TravelAdvisory),
            LocalizedValues = CommonMapper.ToLocalizedValues(dto.LocalizedValues),
            RouteToken = dto.RouteToken
        };

    private static Viewport? ToViewport(ViewportDto? dto)
        => CommonMapper.ToLatLng(dto?.Low) is LatLng low && CommonMapper.ToLatLng(dto?.High) is LatLng high
            ? new Viewport() { Low = low, High = high } : null;

    // LEG
    private static RouteLeg ToRouteLeg(RouteLegDto dto)
        => new () {
            DistanceMeters = dto.DistanceMeters,
            Duration = CommonMapper.ToDuration(dto.Duration),
            StaticDuration = CommonMapper.ToDuration(dto.StaticDuration),
            StartLocation = CommonMapper.ToLocation(dto.StartLocation),
            EndLocation = CommonMapper.ToLocation(dto.EndLocation),
            EncodedPolyline = dto.Polyline?.EncodedPolyline,
            Steps = dto.Steps.ConvertAll(ToRouteStep),
            Segments = dto.StepsOverview?.MultiModalSegments.ConvertAll(ToSegment) ?? [],
            LocalizedValues = dto.LocalizedValues is null ? null
                : new RouteLegLocalizedValues() {
                    Distance = dto.LocalizedValues.Distance?.Text,
                    Duration = dto.LocalizedValues.Duration?.Text,
                    StaticDuration = dto.LocalizedValues.StaticDuration?.Text
                }
        };

    private static MultiModalSegment ToSegment(MultiModalSegmentDto dto)
        => new () {
            StepStartIndex = dto.StepStartIndex,
            StepEndIndex = dto.StepEndIndex,
            NavigationInstruction = ToNavigationInstruction(dto.NavigationInstruction),
            TravelMode = WireEnum<TravelMode>.FromWire(dto.TravelMode)
        };

    // STEP
    private static RouteStep ToRouteStep(RouteLegStepDto dto)
        => new () {
            DistanceMeters = dto.DistanceMeters,
            StaticDuration = CommonMapper.ToDuration(dto.StaticDuration),
            StartLocation = CommonMapper.ToLocation(dto.StartLocation),
            EndLocation = CommonMapper.ToLocation(dto.EndLocation),
            EncodedPolyline = dto.Polyline?.EncodedPolyline,
            NavigationInstruction = ToNavigationInstruction(dto.NavigationInstruction),
            LocalizedValues = dto.LocalizedValues is null ? null
                : new RouteLegStepLocalizedValues() {
                    Distance = dto.LocalizedValues.Distance?.Text,
                    StaticDuration = dto.LocalizedValues.StaticDuration?.Text
                },
            TravelMode = WireEnum<TravelMode>.FromWire(dto.TravelMode),
            TransitDetails = ToTransitDetails(dto.TransitDetails)
        };

    private static NavigationInstruction? ToNavigationInstruction(NavigationInstructionDto? dto)
        => dto is null ? null
            : new NavigationInstruction() {
                Maneuver = WireEnum<Maneuver>.FromWire(dto.Maneuver),
                Instructions = dto.Instructions
            };

    // TRANSIT
    private static TransitDetails? ToTransitDetails(TransitDetailsDto? dto)
        => dto is null ? null
            : new TransitDetails() {
                DepartureStop = ToTransitStop(dto.StopDetails?.DepartureStop),
                ArrivalStop = ToTransitStop(dto.StopDetails?.ArrivalStop),
                DepartureTime = CommonMapper.ToTimestamp(dto.StopDetails?.DepartureTime),
                ArrivalTime = CommonMapper.ToTimestamp(dto.StopDetails?.ArrivalTime),
                DepartureTimeText = dto.LocalizedValues?.DepartureTime?.Time?.Text,
                ArrivalTimeText = dto.LocalizedValues?.ArrivalTime?.Time?.Text,
                Headsign = dto.Headsign,
                StopCount = dto.StopCount,
                Line = ToTransitLine(dto.TransitLine)
            };

    private static TransitStop? ToTransitStop(TransitStopDto? dto)
        => dto is null ? null
            : new TransitStop() { Name = dto.Name, Location = CommonMapper.ToLatLng(dto.Location?.LatLng) };

    private static TransitLine? ToTransitLine(TransitLineDto? dto)
        => dto is null ? null
            : new TransitLine() {
                Name = dto.Name,
                ShortName = dto.NameShort,
                Color = dto.Color,
                TextColor = dto.TextColor,
                VehicleType = WireEnum<TransitVehicleType>.FromWire(dto.Vehicle?.Type),
                VehicleName = dto.Vehicle?.Name?.Text,
                Agencies = dto.Agencies.ConvertAll(agency =>
                    new TransitAgency() { Name = agency.Name, Uri = CommonMapper.ToUri(agency.Uri) })
            };
}
