namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Represents one page of text search results.
/// </summary>
public sealed class TextSearchResult {
    /// <summary>
    /// Gets the places on this page. Empty when nothing matches.
    /// </summary>
    public IReadOnlyList<PlaceSearchInfo> Places { get; internal set; } = [];

    /// <summary>
    /// Gets the token for the next page, or <c>null</c> when this is the last page.
    /// </summary>
    /// <remarks>
    /// Pass it to <see cref="TextRequestBuilder.WithPageToken(string)"/> on a request with the
    /// same parameters as the one that produced this page.
    /// </remarks>
    public string? NextPageToken { get; internal set; }
}
