using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Services.Details;
using AarxMaps.Places.Services.Searching;

namespace AarxMaps.Places.Services;

/// <summary>
/// Provides a unified entry point for accessing all Places-related services.
/// </summary>
/// <remarks>
/// This interface aggregates the different domain services of the Places module,
/// allowing consumers to access autocomplete, search, and details functionality
/// through a single abstraction.
/// </remarks>
public interface IPlacesService {
    /// <summary>
    /// Gets the autocomplete service used to retrieve place suggestions.
    /// </summary>
    IAutocompleteService Autocomplete { get; }
    
    /// <summary>
    /// Gets the details service used to retrieve detailed place information.
    /// </summary>
    IDetailsService Details { get; }
    
    /// <summary>
    /// Gets the search service used to perform place queries.
    /// </summary>
    ISearchService Search { get; }
}