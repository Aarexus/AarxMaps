using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Models.Geometry;
using AarxMaps.Places.Internal.Factories;
using AarxMaps.Places.Internal.Validation;

namespace AarxMaps.Places.Services.Autocomplete;

/// <summary>
/// Provides a fluent API for constructing <see cref="AutocompleteRequest"/> instances.
/// </summary>
/// <remarks>
/// This builder simplifies the creation of autocomplete requests by enforcing
/// required parameters and providing optional configuration such as location bias.
/// </remarks>
public sealed class AutocompleteRequestBuilder {
    // -------------------- INIT --------------------
    private const int MaxPrimaryTypes = 5;

    private string? _input;
    private IReadOnlyList<string>? _includedPrimTypes;
    private LocationBias? _locBias;
    private LocationRestriction? _locRestriction;
    private string? _sessionToken;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="AutocompleteRequest"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="AutocompleteRequest"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the input is not specified, or when both a location bias and a
    /// location restriction are specified.
    /// </exception>
    public AutocompleteRequest Build() {
        if (_input is null)
            throw new MapsInvalidRequestException("Input is required. Call WithInput() before Build().");
        if (_locBias is not null && _locRestriction is not null)
            throw new MapsInvalidRequestException("Either LocationBias or LocationRestriction can be specified to define the search area, not both.");

        return new AutocompleteRequest() {
            Input = _input, IncludedPrimaryTypes = _includedPrimTypes,
            LocationBias = _locBias, LocationRestriction = _locRestriction,
            SessionToken = _sessionToken
        };
    }

    // -------------------- CONFIG --------------------
    /// <summary>
    /// Sets the input text used to generate autocomplete suggestions.
    /// </summary>
    /// <param name="input">
    /// The partial user input (e.g., "coffee", "pizza near"). Leading and trailing whitespace is removed.
    /// </param>
    /// <returns>
    /// The current <see cref="AutocompleteRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the input is null, empty, or whitespace.
    /// </exception>
    public AutocompleteRequestBuilder WithInput(string input) {
        _input = RequestValidator.ValidateAndTrimRequiredText(input, "Input");
        return this;
    }

    /// <summary>
    /// Limits the results to places whose primary type matches one of the specified types.
    /// </summary>
    /// <param name="includedTypes">
    /// Between 1 and 5 place types from Google's place types table (e.g., "restaurant", "cafe").
    /// The values are copied and trimmed.
    /// </param>
    /// <returns>
    /// The current <see cref="AutocompleteRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the collection is null, empty, has more than 5 items, or contains an empty item.
    /// </exception>
    public AutocompleteRequestBuilder WithIncludedPrimaryTypes(IEnumerable<string> includedTypes) {
        _includedPrimTypes = RequestValidator.ValidateAndTrimList(includedTypes, "IncludedPrimaryTypes", MaxPrimaryTypes);
        return this;
    }

    /// <summary>
    /// Applies a geographic bias to the autocomplete results.
    /// </summary>
    /// <param name="center">
    /// The center point of the bias circle.
    /// </param>
    /// <param name="radius">
    /// The radius in meters, greater than 0 and at most 50,000.
    /// </param>
    /// <returns>
    /// The current <see cref="AutocompleteRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// This does not strictly restrict results but influences ranking
    /// toward the specified area. Cannot be combined with
    /// <see cref="WithLocationRestriction(LatLng, double)"/>.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the center is null or the radius is out of range.
    /// </exception>
    public AutocompleteRequestBuilder WithLocationBias(LatLng center, double radius) {
        _locBias = GeoFactory.CreateLocationBias(center, radius);
        return this;
    }

    /// <summary>
    /// Restricts the autocomplete results to a geographic area.
    /// </summary>
    /// <param name="center">
    /// The center point of the restriction circle.
    /// </param>
    /// <param name="radius">
    /// The radius in meters, greater than 0 and at most 50,000.
    /// </param>
    /// <returns>
    /// The current <see cref="AutocompleteRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// Only places inside the area are returned. Cannot be combined with
    /// <see cref="WithLocationBias(LatLng, double)"/>.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the center is null or the radius is out of range.
    /// </exception>
    public AutocompleteRequestBuilder WithLocationRestriction(LatLng center, double radius) {
        _locRestriction = GeoFactory.CreateLocationRestriction(center, radius);
        return this;
    }

    /// <summary>
    /// Groups this call into an autocomplete session for billing.
    /// </summary>
    /// <param name="sessionToken">
    /// A URL- and filename-safe base64 string of at most 36 characters. A version 4 UUID
    /// (<c>Guid.NewGuid().ToString()</c>) is the recommended format.
    /// </param>
    /// <returns>
    /// The current <see cref="AutocompleteRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// <para><b>Billing:</b> without a session token, Google bills every autocomplete call separately.
    /// Reuse the same token for every keystroke of one user search and generate a new one for
    /// the next search. A session ends with a Place Details call that uses the same token.</para>
    /// <para><b>Caching:</b> the token is part of the request, so cached responses are only reused
    /// within the same session.</para>
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the token is empty, longer than 36 characters, or contains characters
    /// other than <c>A-Z</c>, <c>a-z</c>, <c>0-9</c>, <c>-</c> and <c>_</c>.
    /// </exception>
    public AutocompleteRequestBuilder WithSessionToken(string sessionToken) {
        _sessionToken = RequestValidator.ValidateSessionToken(sessionToken);
        return this;
    }
}
