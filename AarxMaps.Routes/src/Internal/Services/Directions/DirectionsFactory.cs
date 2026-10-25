using System.Collections.Frozen;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Common;
using AarxMaps.Routes.Internal.Constants;
using AarxMaps.Routes.Internal.Services.Directions.DTOs;
using AarxMaps.Routes.Services.Directions;

namespace AarxMaps.Routes.Internal.Services.Directions;

internal static class DirectionsFactory {
    // -------------------- INIT --------------------
    // optimizedIntermediateWaypointIndex is in every level: Google rejects waypoint optimization without it
    private static readonly string[] _summaryFields = [
        "fallbackInfo", "routes.routeLabels", "routes.distanceMeters", "routes.duration", "routes.staticDuration",
        "routes.description", "routes.warnings", "routes.travelAdvisory", "routes.localizedValues",
        "routes.optimizedIntermediateWaypointIndex"
    ];
    private static readonly FrozenDictionary<string, string> _summaryHeaders = CreateHeaders(_summaryFields);
    private static readonly FrozenDictionary<string, string> _standardHeaders = CreateHeaders([
        .. _summaryFields, "routes.polyline.encodedPolyline", "routes.viewport",
        "routes.legs.distanceMeters", "routes.legs.duration", "routes.legs.staticDuration",
        "routes.legs.startLocation", "routes.legs.endLocation", "routes.legs.localizedValues"
    ]);
    private static readonly FrozenDictionary<string, string> _fullHeaders = CreateHeaders([
        .. _summaryFields, "routes.polyline.encodedPolyline", "routes.viewport", "routes.routeToken", "routes.legs"
    ]);

    // -------------------- METHS --------------------
    internal static MapsApiRequest CreateRoutesRequest(RouteRequest request, RouteDetailLevel detailLevel)
        => CreateRequest(request, detailLevel switch {
            RouteDetailLevel.Summary => _summaryHeaders,
            RouteDetailLevel.Standard => _standardHeaders,
            RouteDetailLevel.Full => _fullHeaders,
            _ => throw new MapsInvalidRequestException($"{detailLevel} is not a valid RouteDetailLevel.")
        });

    internal static MapsApiRequest CreateRawRoutesRequest(RouteRequest request, IEnumerable<string> fields)
        => CreateRequest(request, CreateHeaders(NormalizeFields(fields)));

    internal static ComputeRoutesPayload ToPayload(RouteRequest request)
        => new () {
            Origin = CommonMapper.ToWaypointDto(request.Origin),
            Destination = CommonMapper.ToWaypointDto(request.Destination),
            Intermediates = request.Intermediates.Count == 0 ? null : [.. request.Intermediates.Select(CommonMapper.ToWaypointDto)],
            TravelMode = WireEnum<TravelMode>.ToWire(request.TravelMode),
            RoutingPreference = WireEnum<RoutingPreference>.ToWire(request.RoutingPreference),
            PolylineQuality = WireEnum<PolylineQuality>.ToWire(request.PolylineQuality),
            DepartureTime = CommonMapper.ToTimestamp(request.DepartureTime),
            ArrivalTime = CommonMapper.ToTimestamp(request.ArrivalTime),
            ComputeAlternativeRoutes = request.ComputeAlternativeRoutes,
            RouteModifiers = CommonMapper.ToRouteModifiersDto(request.RouteModifiers),
            Units = WireEnum<Units>.ToWire(request.Units),
            OptimizeWaypointOrder = request.OptimizeWaypointOrder,
            TrafficModel = WireEnum<TrafficModel>.ToWire(request.TrafficModel),
            TransitPreferences = CommonMapper.ToTransitPreferencesDto(request.TransitPreferences)
        };

    // -------------------- INNER METHS --------------------
    private static MapsApiRequest CreateRequest(RouteRequest request, IDictionary<string, string> headers)
        => new () {
            Method = HttpMethod.Post,
            BaseUrl = RoutesEndpoints.BaseUrl,
            Endpoint = RoutesEndpoints.ComputeRoutes,
            Headers = headers,
            Payload = ToPayload(request)
        };

    private static FrozenDictionary<string, string> CreateHeaders(IEnumerable<string> fields)
        => new Dictionary<string, string> { { RoutesConstants.FieldMaskHeader, string.Join(',', fields) } }
            .ToFrozenDictionary();

    private static List<string> NormalizeFields(IEnumerable<string> fields) {
        if (fields is null)
            throw new MapsInvalidRequestException("Fields cannot be null.");

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields) {
            var trimmed = field?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new MapsInvalidRequestException("Empty fields are not allowed.");
            if (seen.Add(trimmed))
                normalized.Add(trimmed);
        }

        if (normalized.Count == 0)
            throw new MapsInvalidRequestException("At least one field is required.");

        return normalized;
    }
}
