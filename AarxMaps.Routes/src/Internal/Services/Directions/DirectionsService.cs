using System.Text.Json;
using AarxMaps.Core.Abstractions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.Directions.DTOs;
using AarxMaps.Routes.Services.Directions;

namespace AarxMaps.Routes.Internal.Services.Directions;

internal sealed class DirectionsService : IDirectionsService {
    // -------------------- INIT --------------------
    private readonly IMapsApiClient _apiClient;
    public DirectionsService(IMapsApiClient apiClient) => _apiClient = apiClient;

    // -------------------- METHS --------------------
    public async Task<RouteResult> GetRoutesAsync(
        RouteRequest routeRequest, RouteDetailLevel detailLevel = RouteDetailLevel.Standard, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(routeRequest);

        // REQUEST
        var request = DirectionsFactory.CreateRoutesRequest(routeRequest, detailLevel);
        var response = await _apiClient.SendAsync<ComputeRoutesResponse>(request, ct)
            .ConfigureAwait(false);

        return DirectionsMapper.ToRouteResult(response);
    }

    public async Task<JsonDocument> GetRoutesRawAsync(
        RouteRequest routeRequest, IEnumerable<string> fields, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(routeRequest);

        // REQUEST
        var request = DirectionsFactory.CreateRawRoutesRequest(routeRequest, fields);
        return await _apiClient.SendAsync<JsonDocument>(request, ct)
            .ConfigureAwait(false);
    }
}
