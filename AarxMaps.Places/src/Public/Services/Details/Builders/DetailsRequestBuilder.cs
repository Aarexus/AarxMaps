using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Internal.Validation;

namespace AarxMaps.Places.Services.Details;

/// <summary>
/// Provides a fluent API for constructing <see cref="DetailsRequest"/> instances.
/// </summary>
public sealed class DetailsRequestBuilder {
    // -------------------- INIT --------------------
    private string? _placeId;
    private string? _sessionToken;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="DetailsRequest"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="DetailsRequest"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the place ID is not specified.
    /// </exception>
    public DetailsRequest Build() {
        if (_placeId is null)
            throw new MapsInvalidRequestException("PlaceId is required. Call WithPlaceId() before Build().");

        return new DetailsRequest() {
            PlaceId = _placeId,
            SessionToken = _sessionToken
        };
    }

    // -------------------- CONFIG --------------------
    /// <summary>
    /// Sets the place to query.
    /// </summary>
    /// <param name="placeId">
    /// The place ID, as returned by search or autocomplete. Leading and trailing whitespace is removed.
    /// </param>
    /// <returns>
    /// The current <see cref="DetailsRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the place ID is null, empty, or whitespace.
    /// </exception>
    public DetailsRequestBuilder WithPlaceId(string placeId) {
        _placeId = RequestValidator.ValidateAndTrimRequiredText(placeId, "PlaceId");
        return this;
    }

    /// <summary>
    /// Ends an autocomplete session with this call.
    /// </summary>
    /// <param name="sessionToken">
    /// The token used for the autocomplete requests of the session.
    /// </param>
    /// <returns>
    /// The current <see cref="DetailsRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// <para><b>Billing:</b> a Details call ends the session, so use the token on exactly one call.
    /// A reused token is billed as if no session token was provided.</para>
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the token is empty, longer than 36 characters, or contains characters
    /// other than <c>A-Z</c>, <c>a-z</c>, <c>0-9</c>, <c>-</c> and <c>_</c>.
    /// </exception>
    public DetailsRequestBuilder WithSessionToken(string sessionToken) {
        _sessionToken = RequestValidator.ValidateSessionToken(sessionToken);
        return this;
    }
}
