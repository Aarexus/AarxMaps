using AarxMaps.Core.Abstractions;
using AarxMaps.Core.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;
using AarxMaps.Routes.Services.DistanceMatrix;

namespace AarxMaps.Routes.Internal.Services.DistanceMatrix;

internal sealed class DistanceMatrixService : IDistanceMatrixService {
    // -------------------- INIT --------------------
    private readonly IMapsApiClient _apiClient;
    public DistanceMatrixService(IMapsApiClient apiClient) => _apiClient = apiClient;

    // -------------------- METHS --------------------
    public async Task<IReadOnlyList<RouteMatrixElement>> GetDistancesAsync(
        DistanceRequest distanceRequest, RouteDetailLevel detailLevel = RouteDetailLevel.Standard, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(distanceRequest);

        // REQUEST
        var request = DistanceMatrixFactory.CreateMatrixRequest(distanceRequest, detailLevel);
        var elements = new List<RouteMatrixElement>(distanceRequest.Origins.Count * distanceRequest.Destinations.Count);
        await foreach (var element in _apiClient.SendAndEnumerateAsync<RouteMatrixElementDto>(request, ct).ConfigureAwait(false))
            elements.Add(DistanceMatrixMapper.ToElement(element));

        // Google streams elements in no particular order
        elements.Sort(static (a, b) => a.OriginIndex != b.OriginIndex
            ? a.OriginIndex.CompareTo(b.OriginIndex)
            : a.DestinationIndex.CompareTo(b.DestinationIndex));
        return elements;
    }
}
