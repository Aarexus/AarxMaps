using AarxMaps.Places.Common.Models.Geometry;

namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Represents a request for searching places near a specific geographic location.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="NearbyRequestBuilder"/>
/// to enforce required parameters such as the search area. It is serialized as the request body.
/// </remarks>
public sealed class NearbyRequest {
    internal NearbyRequest() {}

    /// <summary>
    /// Gets the circular area the results must be located in.
    /// </summary>
    public LocationRestriction LocationRestriction { get; internal set; } = null!;

    /// <summary>
    /// Gets the maximum number of results to return, from 1 to 20.
    /// </summary>
    /// <remarks>
    /// Defaults to 20 if not explicitly configured.
    /// </remarks>
    public int MaxResultCount { get; internal set; }

    /// <summary>
    /// Gets the place types the results are limited to, or <c>null</c> for no type filter.
    /// </summary>
    /// <remarks>
    /// Examples include "restaurant", "cafe", or "store".
    /// </remarks>
    public IReadOnlyList<string>? IncludedTypes { get; internal set; }
}
