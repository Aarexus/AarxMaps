using AarxMaps.Places.Common.Models.Geometry;

namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Represents a request for searching places with a text query.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="TextRequestBuilder"/>
/// to ensure all required fields are properly set. It is serialized as the request body.
/// </remarks>
public sealed class TextRequest {
    internal TextRequest() {}

    /// <summary>
    /// Gets the text query (e.g., "pizza in New York", "shoe stores near Ottawa").
    /// </summary>
    public string TextQuery { get; internal set; } = null!;

    /// <summary>
    /// Gets the maximum number of results per page, from 1 to 20.
    /// </summary>
    /// <remarks>
    /// Defaults to 20 if not explicitly configured.
    /// </remarks>
    public int PageSize { get; internal set; }

    /// <summary>
    /// Gets the token of the page to fetch, or <c>null</c> for the first page.
    /// </summary>
    public string? PageToken { get; internal set; }

    /// <summary>
    /// Gets the place type the results are limited to, or <c>null</c> for no type filter.
    /// </summary>
    public string? IncludedType { get; internal set; }

    /// <summary>
    /// Gets the optional area the results are biased toward.
    /// </summary>
    /// <remarks>
    /// Can be a circle or a rectangle. Mutually exclusive with <see cref="LocationRestriction"/>.
    /// </remarks>
    public LocationBias? LocationBias { get; internal set; }

    /// <summary>
    /// Gets the optional rectangular area the results must be located in.
    /// </summary>
    /// <remarks>
    /// Text Search only accepts a rectangle here. Mutually exclusive with <see cref="LocationBias"/>.
    /// </remarks>
    public LocationRestriction? LocationRestriction { get; internal set; }
}
