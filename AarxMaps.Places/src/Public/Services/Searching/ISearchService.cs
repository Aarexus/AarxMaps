using AarxMaps.Core.Exceptions;

namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Provides functionality for searching places using text queries
/// or geographic constraints.
/// </summary>
/// <remarks>
/// This service wraps the Google Places Text Search (New) and Nearby Search (New) APIs
/// and returns strongly typed results for common search scenarios.
/// <para>
/// <b>Registration:</b> available after calling <c>AddAarxMaps()</c> followed by <c>AddAarxMapsPlaces()</c>.
/// </para>
/// </remarks>
public interface ISearchService {
    /// <summary>
    /// Searches for places matching a text query.
    /// </summary>
    /// <param name="textRequest">
    /// The request containing the text query and optional parameters,
    /// built with <see cref="TextRequestBuilder"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="TextSearchResult"/> with the matching places and, when more results exist,
    /// the token for the next page.
    /// </returns>
    /// <remarks>
    /// <para><b>Pagination:</b> to fetch the next page, build a new request with the same parameters
    /// and <see cref="TextRequestBuilder.WithPageToken(string)"/> set to
    /// <see cref="TextSearchResult.NextPageToken"/>. Only the page token and page size may change
    /// between pages; any other difference makes Google reject the request.</para>
    /// <para><b>Caching:</b> when a cache layer is registered, identical requests are served from
    /// the cache. Returned instances may be shared and must not be mutated.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="textRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">Google rejected the request (400).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<TextSearchResult> SearchByTextAsync(
        TextRequest textRequest,
        CancellationToken ct = default
    );

    /// <summary>
    /// Searches for places inside a circular area.
    /// </summary>
    /// <param name="nearbyRequest">
    /// The request containing the search area and optional filters,
    /// built with <see cref="NearbyRequestBuilder"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="NearbySearchResult"/> with the places found in the area.
    /// </returns>
    /// <remarks>
    /// <para><b>Pagination:</b> Nearby Search returns a single page of up to 20 places.</para>
    /// <para><b>Caching:</b> when a cache layer is registered, identical requests are served from
    /// the cache. Returned instances may be shared and must not be mutated.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="nearbyRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">Google rejected the request (400).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<NearbySearchResult> SearchNearbyAsync(
        NearbyRequest nearbyRequest,
        CancellationToken ct = default
    );
}
