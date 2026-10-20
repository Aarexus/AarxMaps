namespace AarxMaps.Places.Services.Autocomplete;

/// <summary>
/// Represents a predicted place suggestion returned from an autocomplete query.
/// </summary>
/// <remarks>
/// Suggestions are typically used to guide user input before performing a full search.
/// </remarks>
public sealed class PlaceSuggestion {
    /// <summary>
    /// Gets the unique identifier of the suggested place.
    /// </summary>
    public string PlaceId { get; internal set; } = null!;

    /// <summary>
    /// Gets the full suggestion text.
    /// </summary>
    public string Text { get; internal set; } = null!;

    /// <summary>
    /// Gets the offset where the first match of the input ends within <see cref="Text"/>,
    /// or <c>null</c> when Google reports no match.
    /// </summary>
    /// <remarks>
    /// This can be used for highlighting matched substrings in UI scenarios.
    /// <para><b>Units:</b> Google counts offsets in Unicode code points, while .NET strings index
    /// UTF-16 code units. They differ only when the text contains characters outside the Basic
    /// Multilingual Plane (for example emoji).</para>
    /// </remarks>
    public int? EndOffset { get; internal set; }

    /// <summary>
    /// Gets the list of place types associated with the suggestion.
    /// </summary>
    public IReadOnlyList<string> Types { get; internal set; } = [];
}
