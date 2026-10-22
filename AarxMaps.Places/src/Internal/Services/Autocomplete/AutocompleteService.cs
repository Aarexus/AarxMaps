using AarxMaps.Core.Abstractions;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Internal.Services.Autocomplete.DTOs;

namespace AarxMaps.Places.Internal.Services.Autocomplete;

internal sealed class AutocompleteService : IAutocompleteService {
    // -------------------- INIT --------------------
    private readonly IMapsApiClient _apiClient;
    public AutocompleteService(IMapsApiClient apiClient) => _apiClient = apiClient;

    // -------------------- METHS --------------------
    public async Task<IReadOnlyList<PlaceSuggestion>> SuggestPlacesAsync(
        AutocompleteRequest autocompleteRequest, CancellationToken ct = default
    ) {
        // VALIDATION
        ArgumentNullException.ThrowIfNull(autocompleteRequest);

        // REQUEST
        var request = AutocompleteFactory.CreateRequest(autocompleteRequest);
        var response = await _apiClient.SendAsync<AutocompleteResponse>(request, ct)
            .ConfigureAwait(false);

        return AutocompleteMapper.ToPlaceSuggestions(response);
    }
}
