using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Models.Geometry;
using AarxMaps.Places.Internal.Constants;
using AarxMaps.Places.Internal.Factories;
using AarxMaps.Places.Internal.Validation;

namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Provides a fluent API for constructing <see cref="NearbyRequest"/> instances.
/// </summary>
/// <remarks>
/// This builder is used to configure nearby place searches by specifying
/// the search area, place types, and result limits.
/// </remarks>
public sealed class NearbyRequestBuilder {
    // -------------------- INIT --------------------
    private const int MaxIncludedTypes = 50;

    private LocationRestriction? _location;
    private int _maxResults = PlacesConstants.DefaultPageSize;
    private IReadOnlyList<string>? _includedTypes;

    // -------------------- BUILD --------------------
    /// <summary>
    /// Builds a new <see cref="NearbyRequest"/> instance.
    /// </summary>
    /// <returns>
    /// A fully configured <see cref="NearbyRequest"/>.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the search area is not defined.
    /// </exception>
    public NearbyRequest Build() {
        if (_location is null)
            throw new MapsInvalidRequestException("LocationRestriction is required. Call WithLocationRestriction() before Build().");

        return new NearbyRequest() {
            IncludedTypes = _includedTypes,
            MaxResultCount = _maxResults,
            LocationRestriction = _location
        };
    }

    // -------------------- CONFIG --------------------
    /// <summary>
    /// Restricts the search to a circular area.
    /// </summary>
    /// <param name="center">
    /// The center point of the search area.
    /// </param>
    /// <param name="radius">
    /// The radius in meters, greater than 0 and at most 50,000.
    /// </param>
    /// <returns>
    /// The current <see cref="NearbyRequestBuilder"/> instance.
    /// </returns>
    /// <remarks>
    /// Required. Only places inside the area are returned.
    /// </remarks>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the center is null or the radius is out of range.
    /// </exception>
    public NearbyRequestBuilder WithLocationRestriction(LatLng center, double radius) {
        _location = GeoFactory.CreateLocationRestriction(center, radius);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of results to return.
    /// </summary>
    /// <param name="maxResults">
    /// The maximum number of results, from 1 to 20. Defaults to 20.
    /// </param>
    /// <returns>
    /// The current <see cref="NearbyRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the value is outside the 1 to 20 range.
    /// </exception>
    public NearbyRequestBuilder WithMaxResults(int maxResults) {
        _maxResults = maxResults is > 0 and <= PlacesConstants.MaxPageSize ? maxResults
            : throw new MapsInvalidRequestException($"MaxResultCount must be between 1 and {PlacesConstants.MaxPageSize}.");
        return this;
    }

    /// <summary>
    /// Limits the results to places matching at least one of the specified types.
    /// </summary>
    /// <param name="includedTypes">
    /// Between 1 and 50 place types (e.g., "restaurant", "cafe"). The values are copied and trimmed.
    /// </param>
    /// <returns>
    /// The current <see cref="NearbyRequestBuilder"/> instance.
    /// </returns>
    /// <exception cref="MapsInvalidRequestException">
    /// Thrown when the collection is null, empty, has more than 50 items, or contains an empty item.
    /// </exception>
    public NearbyRequestBuilder WithIncludedTypes(IEnumerable<string> includedTypes) {
        _includedTypes = RequestValidator.ValidateAndTrimList(includedTypes, "IncludedTypes", MaxIncludedTypes);
        return this;
    }
}
