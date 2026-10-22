using AarxMaps.Places.Services;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Services.Details;
using AarxMaps.Places.Services.Searching;

namespace AarxMaps.Places.Internal.Services;

internal sealed class PlacesService : IPlacesService {
    // -------------------- INIT --------------------
    public IAutocompleteService Autocomplete { get; }
    public IDetailsService Details { get; }
    public ISearchService Search { get; }

    public PlacesService(
        IAutocompleteService autocompleteService, 
        IDetailsService detailsService, 
        ISearchService searchService
    ) {
        Autocomplete = autocompleteService;
        Details = detailsService;
        Search = searchService;
    }
}
