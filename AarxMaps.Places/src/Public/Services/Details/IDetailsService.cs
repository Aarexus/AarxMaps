using System.Text.Json;
using AarxMaps.Core.Exceptions;

namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Provides access to detailed information about places, including metadata,
/// photos, and reviews.
/// </summary>
/// <remarks>
/// This service wraps the Google Places Details (New) and Place Photos (New) APIs and exposes both
/// strongly typed responses and raw JSON access for advanced scenarios.
/// <para><b>Registration:</b> available after calling <c>AddAarxMaps()</c> followed by <c>AddAarxMapsPlaces()</c>.</para>
/// <para><b>Sessions:</b> every method is a Place Details call, which ends an autocomplete session.
/// Set the session token (<see cref="DetailsRequestBuilder.WithSessionToken(string)"/>) on exactly one call.</para>
/// </remarks>
public interface IDetailsService {
    /// <summary>
    /// Retrieves detailed information for a place.
    /// </summary>
    /// <param name="detRequest">
    /// The place to query, built with <see cref="DetailsRequestBuilder"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="PlaceDetails"/> object containing structured information about the place.
    /// </returns>
    /// <remarks>
    /// <para><b>Billing:</b> the request asks for a fixed set of fields that includes contact, rating,
    /// opening hours and amenity data, so it is billed as Place Details Enterprise + Atmosphere.
    /// Use <see cref="GetDetailsRawAsync"/> with a narrower field list to pay a lower tier.</para>
    /// <para><b>Caching:</b> when a cache layer is registered, identical requests are served from
    /// the cache. Returned instances may be shared and must not be mutated.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="detRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">Google rejected the request, for example a malformed place ID (400).</exception>
    /// <exception cref="MapsNotFoundException">No place exists with that ID (404).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<PlaceDetails> GetDetailsAsync(DetailsRequest detRequest, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the raw JSON of a place, limited to the specified fields.
    /// </summary>
    /// <param name="detRequest">
    /// The place to query, built with <see cref="DetailsRequestBuilder"/>.
    /// </param>
    /// <param name="fields">
    /// The Place fields to return (e.g., "displayName", "rating"), as used in Google's field mask.
    /// Values are trimmed and duplicates removed. <c>"*"</c> returns every field.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="JsonDocument"/> containing the raw API response.
    /// </returns>
    /// <remarks>
    /// Useful for fields not mapped by the SDK, or to pay only for the fields you need.
    /// <para><b>Ownership:</b> the caller owns the returned document and must dispose it
    /// (<c>using var json = await ...</c>). Every call returns a new document, even from the cache.</para>
    /// <para><b>Caching:</b> responses are cached like other requests, except when the fields include
    /// photos (<c>"photos"</c>, <c>"photos.*"</c> or <c>"*"</c>), since photo names expire.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="detRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">
    /// <paramref name="fields"/> is null, empty or contains an empty value, or Google rejected the
    /// request, for example an unknown field (400).
    /// </exception>
    /// <exception cref="MapsNotFoundException">No place exists with that ID (404).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<JsonDocument> GetDetailsRawAsync(
        DetailsRequest detRequest, 
        IEnumerable<string> fields, 
        CancellationToken ct = default
    );

    /// <summary>
    /// Retrieves photos of a place.
    /// </summary>
    /// <param name="detRequest">
    /// The place to query, built with <see cref="DetailsRequestBuilder"/>.
    /// </param>
    /// <param name="photoOptions">
    /// The number and size of photos, built with <see cref="PhotoOptionsBuilder"/>.
    /// <c>null</c> uses the defaults (10 photos, up to 480x480 pixels).
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="PlacePhotos"/> object with up to the requested number of photos.
    /// </returns>
    /// <remarks>
    /// <para><b>Requests:</b> one Place Details call for the photo names, then one Place Photos call
    /// per photo, sent in parallel and each billed separately. If any photo call fails, the
    /// remaining ones are cancelled and the exception is rethrown.</para>
    /// <para><b>Caching:</b> never cached, since Google doesn't allow caching photo names and they expire.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="detRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">Google rejected the request, for example a malformed place ID (400).</exception>
    /// <exception cref="MapsNotFoundException">No place exists with that ID, or a photo expired (404).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<PlacePhotos> GetPhotosAsync(
        DetailsRequest detRequest, 
        PhotoOptions? photoOptions = null, 
        CancellationToken ct = default
    );

    /// <summary>
    /// Retrieves the reviews of a place and their AI-generated summary.
    /// </summary>
    /// <param name="detRequest">
    /// The place to query, built with <see cref="DetailsRequestBuilder"/>.
    /// </param>
    /// <param name="ct">
    /// A token to cancel the request.
    /// </param>
    /// <returns>
    /// A <see cref="PlaceReviews"/> object with up to 5 reviews (Google's limit) and the summary, when available.
    /// </returns>
    /// <remarks>
    /// <para><b>Billing:</b> reviews are billed as Place Details Enterprise + Atmosphere.</para>
    /// <para><b>Caching:</b> when a cache layer is registered, identical requests are served from
    /// the cache. Returned instances may be shared and must not be mutated.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="detRequest"/> is <c>null</c>.</exception>
    /// <exception cref="MapsApiAuthException">The API key is missing, invalid or not authorized (401/403).</exception>
    /// <exception cref="MapsInvalidRequestException">Google rejected the request, for example a malformed place ID (400).</exception>
    /// <exception cref="MapsNotFoundException">No place exists with that ID (404).</exception>
    /// <exception cref="MapsRateLimitException">The quota or rate limit was exceeded (429).</exception>
    /// <exception cref="MapsApiException">
    /// Any other non-success status, a network failure, a timeout or an unreadable response.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    Task<PlaceReviews> GetReviewsAsync(DetailsRequest detRequest, CancellationToken ct = default);
}
