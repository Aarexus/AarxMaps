using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Models.Geometry;
using AarxMaps.Places.Internal.Constants;
using AarxMaps.Places.Internal.Factories;
using AarxMaps.Places.Internal.Validation;

namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Provides a fluent API for constructing <see cref="TextRequest"/> instances.
/// </summary>
/// <remarks>
/// This builder is used to configure text searches by specifying the query,
/// an optional type filter, a search area, and pagination.
/// </remarks>
public sealed class TextRequestBuilder {
    // -------------------- INIT --------------------
    private string? _textQuery;
    private int _pageSize = PlacesConstants.DefaultPageSize;
    private string? _pageToken;
    private string? _includedType;
    private LocationBias? _locBias;
    private LocationRestriction? _locRestriction;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="TextRequest"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="TextRequest"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the text query is not specified, or when both a location bias and a
    /// location restriction are specified.
    /// </exception>
    public TextRequest Build() {
        if (_textQuery is null)
            throw new MapsInvalidRequestException("TextQuery is required. Call WithTextQuery() before Build().");
        if (_locBias is not null && _locRestriction is not null)
            throw new MapsInvalidRequestException("Either LocationBias or LocationRestriction can be specified to define the search area, not both.");

        return new TextRequest() {
            TextQuery = _textQuery,
            PageSize = _pageSize,
            PageToken = _pageToken,
            IncludedType = _includedType,
            LocationBias = _locBias,
            LocationRestriction = _locRestriction
        };
    }

    // -------------------- CONFIG --------------------
    /// <summary>
    /// Sets the text query to search for.
    /// </summary>
    /// <param name="textQuery">
    /// The query (e.g., "pizza in New York"). Leading and trailing whitespace is removed.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the query is null, empty, or whitespace.
    /// </exception>
    public TextRequestBuilder WithTextQuery(string textQuery) {
        _textQuery = RequestValidator.ValidateAndTrimRequiredText(textQuery, "TextQuery");
        return this;
    }

    /// <summary>
    /// Limits the results to places of the specified type.
    /// </summary>
    /// <param name="includedType">
    /// A place type from Google's place types table (e.g., "restaurant"). Leading and trailing
    /// whitespace is removed.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the type is null, empty, or whitespace.
    /// </exception>
    public TextRequestBuilder WithIncludedType(string includedType) {
        _includedType = RequestValidator.ValidateAndTrimRequiredText(includedType, "IncludedType");
        return this;
    }

    /// <summary>
    /// Sets the maximum number of results per page.
    /// </summary>
    /// <param name="pageSize">
    /// The page size, from 1 to 20. Defaults to 20.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the value is outside the 1 to 20 range.
    /// </exception>
    public TextRequestBuilder WithPageSize(int pageSize) {
        _pageSize = pageSize is > 0 and <= PlacesConstants.MaxPageSize ? pageSize
            : throw new MapsInvalidRequestException($"PageSize must be between 1 and {PlacesConstants.MaxPageSize}.");
        return this;
    }

    /// <summary>
    /// Requests the page identified by a token from a previous search.
    /// </summary>
    /// <param name="pageToken">
    /// The <see cref="TextSearchResult.NextPageToken"/> returned by the previous page.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// Every other parameter except the page size must match the request that returned the
    /// token; otherwise Google rejects the request.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the token is null, empty, or whitespace.
    /// </exception>
    public TextRequestBuilder WithPageToken(string pageToken) {
        _pageToken = RequestValidator.ValidateAndTrimRequiredText(pageToken, "PageToken");
        return this;
    }

    /// <summary>
    /// Biases the results toward a circular area.
    /// </summary>
    /// <param name="center">
    /// The center point of the bias circle.
    /// </param>
    /// <param name="radius">
    /// The radius in meters, greater than 0 and at most 50,000.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// This does not strictly restrict results but influences ranking toward the specified area.
    /// Cannot be combined with <see cref="WithLocationRestriction(LatLng, LatLng)"/>.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the center is null or the radius is out of range.
    /// </exception>
    public TextRequestBuilder WithLocationBias(LatLng center, double radius) {
        _locBias = GeoFactory.CreateLocationBias(center, radius);
        return this;
    }

    /// <summary>
    /// Biases the results toward a rectangular area.
    /// </summary>
    /// <param name="southWest">
    /// The south-west corner of the rectangle.
    /// </param>
    /// <param name="northEast">
    /// The north-east corner of the rectangle. Its longitude may be lower than the south-west
    /// longitude, for a rectangle crossing the 180th meridian.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// This does not strictly restrict results but influences ranking toward the specified area.
    /// Cannot be combined with <see cref="WithLocationRestriction(LatLng, LatLng)"/>.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when a corner is null or the south-west latitude is greater than the north-east latitude.
    /// </exception>
    public TextRequestBuilder WithLocationBias(LatLng southWest, LatLng northEast) {
        _locBias = GeoFactory.CreateLocationBias(southWest, northEast);
        return this;
    }

    /// <summary>
    /// Restricts the results to a rectangular area.
    /// </summary>
    /// <param name="southWest">
    /// The south-west corner of the rectangle.
    /// </param>
    /// <param name="northEast">
    /// The north-east corner of the rectangle. Its longitude may be lower than the south-west
    /// longitude, for a rectangle crossing the 180th meridian.
    /// </param>
    /// <returns>
    /// The current <see cref="TextRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// Only places inside the area are returned. Text Search only accepts a rectangle as a
    /// restriction. Cannot be combined with <see cref="WithLocationBias(LatLng, double)"/>
    /// or <see cref="WithLocationBias(LatLng, LatLng)"/>.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when a corner is null or the south-west latitude is greater than the north-east latitude.
    /// </exception>
    public TextRequestBuilder WithLocationRestriction(LatLng southWest, LatLng northEast) {
        _locRestriction = GeoFactory.CreateLocationRestriction(southWest, northEast);
        return this;
    }
}
