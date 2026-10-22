using AarxMaps.Places.Common.Models.Geometry;

namespace AarxMaps.Places.Services.Autocomplete;

/// <summary>
/// Represents a request for retrieving place autocomplete suggestions.
/// </summary>
/// <remarks>
/// This model is constructed using <see cref="AutocompleteRequestBuilder"/>
/// to ensure all required fields are properly set. It is serialized as the request body.
/// </remarks>
public sealed class AutocompleteRequest {
    internal AutocompleteRequest() {}

    /// <summary>
    /// Gets the input text used to generate autocomplete suggestions.
    /// </summary>
    /// <remarks>
    /// This value represents partial user input (e.g., "coffee", "pizza near").
    /// </remarks>
    public string Input { get; internal set; } = null!;

    /// <summary>
    /// Gets the primary place types the results are limited to, or <c>null</c> for no type filter.
    /// </summary>
    /// <remarks>
    /// Up to 5 types from Google's place types table (e.g., "restaurant", "cafe").
    /// </remarks>
    public IReadOnlyList<string>? IncludedPrimaryTypes { get; internal set; }

    /// <summary>
    /// Gets the optional geographic bias applied to the autocomplete results.
    /// </summary>
    /// <remarks>
    /// When provided, results are biased toward the specified area but not strictly restricted.
    /// Mutually exclusive with <see cref="LocationRestriction"/>.
    /// </remarks>
    public LocationBias? LocationBias { get; internal set; }

    /// <summary>
    /// Gets the optional geographic restriction applied to the autocomplete results.
    /// </summary>
    /// <remarks>
    /// When provided, only results inside the specified area are returned.
    /// Mutually exclusive with <see cref="LocationBias"/>.
    /// </remarks>
    public LocationRestriction? LocationRestriction { get; internal set; }

    /// <summary>
    /// Gets the session token that groups this call into an autocomplete session for billing,
    /// or <c>null</c> when no session is used.
    /// </summary>
    public string? SessionToken { get; internal set; }
}
