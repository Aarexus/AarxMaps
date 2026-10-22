using AarxMaps.Core.Exceptions;

namespace AarxMaps.Places.Services.Autocomplete;

/// <summary>
/// Provides autocomplete functionality for retrieving place suggestions based on partial user input.
/// </summary>
/// <remarks>
/// This service wraps the Google Places Autocomplete (New) API and returns
/// strongly typed suggestions that can be used to guide user input
/// in search scenarios.
/// <para>
/// <b>Registration:</b> available after calling <c>AddAarxMaps()</c> followed by <c>AddAarxMapsPlaces()</c>.
/// </para>
/// </remarks>
public interface IAutocompleteService {
    /// <summary>
    /// Retrieves place suggestions for the specified autocomplete request.
    /// </summary>
    /// <param name="autocompleteRequest">
    /// The request containing the input text and optional parameters,
    /// built with <see cref="AutocompleteRequestBuilder"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A read-only list of <see cref="PlaceSuggestion"/> representing predicted places.
    /// Empty when Google returns no place predictions.
    /// </returns>
    /// <remarks>
    /// <para><b>Caching:</b> when a cache layer is registered, identical requests (same input, area,
    /// types and session token) are served from the cache. Returned instances may be shared and
    /// must not be mutated.</para>
    /// <para><b>Billing:</b> without a session token, Google bills every call as a separate request.
    /// See <see cref="AutocompleteRequestBuilder.WithSessionToken(string)"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="autocompleteRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">Google rejected the request (400).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<IReadOnlyList<PlaceSuggestion>> SuggestPlacesAsync(
        AutocompleteRequest autocompleteRequest,
        CancellationToken ct = default
    );
}
