using AarxMaps.Core.Abstractions;
using AarxMaps.Places.Internal.Services.Searching.DTOs;
using AarxMaps.Places.Services.Searching;

namespace AarxMaps.Places.Internal.Services.Searching;

internal sealed class SearchService : ISearchService {
    // -------------------- INIT --------------------
    private readonly IMapsApiClient _apiClient;
    public SearchService(IMapsApiClient apiClient) => _apiClient = apiClient;

    // -------------------- METHS --------------------
    public async Task<TextSearchResult> SearchByTextAsync(
        TextRequest textRequest, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(textRequest);

        // REQUEST
        var request = SearchFactory.CreateTextRequest(textRequest);
        var response = await _apiClient.SendAsync<SearchResponse>(request, ct)
            .ConfigureAwait(false);

        return SearchMapper.ToTextResult(response);
    }

    public async Task<NearbySearchResult> SearchNearbyAsync(
        NearbyRequest nearbyRequest, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(nearbyRequest);

        // REQUEST
        var request = SearchFactory.CreateNearbyRequest(nearbyRequest);
        var response = await _apiClient.SendAsync<SearchResponse>(request, ct)
            .ConfigureAwait(false);

        return SearchMapper.ToNearbyResult(response);
    }
}
