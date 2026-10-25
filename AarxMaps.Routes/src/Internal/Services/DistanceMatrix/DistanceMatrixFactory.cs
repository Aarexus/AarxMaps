using System.Collections.Frozen;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Common;
using AarxMaps.Routes.Internal.Constants;
using AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;
using AarxMaps.Routes.Services.DistanceMatrix;

namespace AarxMaps.Routes.Internal.Services.DistanceMatrix;

internal static class DistanceMatrixFactory {
    // -------------------- INIT --------------------
    // Indices, status and condition are in every level: without them the elements cannot be placed or trusted
    private static readonly string[] _summaryFields = [
        "originIndex", "destinationIndex", "status", "condition", "distanceMeters", "duration", "fallbackInfo"
    ];
    private static readonly string[] _standardFields = [.. _summaryFields, "staticDuration", "localizedValues"];
    private static readonly FrozenDictionary<string, string> _summaryHeaders = CreateHeaders(_summaryFields);
    private static readonly FrozenDictionary<string, string> _standardHeaders = CreateHeaders(_standardFields);
    private static readonly FrozenDictionary<string, string> _fullHeaders = CreateHeaders([.. _standardFields, "travelAdvisory"]);

    // -------------------- METHS --------------------
    internal static MapsApiRequest CreateMatrixRequest(DistanceRequest request, RouteDetailLevel detailLevel)
        => new () {
            Method = HttpMethod.Post,
            BaseUrl = RoutesEndpoints.BaseUrl,
            Endpoint = RoutesEndpoints.ComputeRouteMatrix,
            Headers = detailLevel switch {
                RouteDetailLevel.Summary => _summaryHeaders,
                RouteDetailLevel.Standard => _standardHeaders,
                RouteDetailLevel.Full => _fullHeaders,
                _ => throw new MapsInvalidRequestException($"{detailLevel} is not a valid RouteDetailLevel.")
            },
            Payload = ToPayload(request)
        };

    internal static ComputeRouteMatrixPayload ToPayload(DistanceRequest request)
        => new () {
            Origins = [.. request.Origins.Select(origin => new RouteMatrixOriginDto() {
                Waypoint = CommonMapper.ToWaypointDto(origin.Waypoint),
                RouteModifiers = CommonMapper.ToRouteModifiersDto(origin.RouteModifiers)
            })],
            Destinations = [.. request.Destinations.Select(destination =>
                new RouteMatrixDestinationDto() { Waypoint = CommonMapper.ToWaypointDto(destination) })],
            TravelMode = WireEnum<TravelMode>.ToWire(request.TravelMode),
            RoutingPreference = WireEnum<RoutingPreference>.ToWire(request.RoutingPreference),
            DepartureTime = CommonMapper.ToTimestamp(request.DepartureTime),
            ArrivalTime = CommonMapper.ToTimestamp(request.ArrivalTime),
            Units = WireEnum<Units>.ToWire(request.Units),
            TrafficModel = WireEnum<TrafficModel>.ToWire(request.TrafficModel),
            TransitPreferences = CommonMapper.ToTransitPreferencesDto(request.TransitPreferences)
        };

    // -------------------- INNER METHS --------------------
    private static FrozenDictionary<string, string> CreateHeaders(IEnumerable<string> fields)
        => new Dictionary<string, string> { { RoutesConstants.FieldMaskHeader, string.Join(',', fields) } }
            .ToFrozenDictionary();
}
