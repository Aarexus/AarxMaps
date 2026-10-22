namespace AarxMaps.Places.Services.Searching;

/// <summary>
/// Represents the result of a nearby search.
/// </summary>
public sealed class NearbySearchResult {
    /// <summary>
    /// Gets the places found in the search area. Empty when nothing matches.
    /// </summary>
    public IReadOnlyList<PlaceSearchInfo> Places { get; internal set; } = [];
}
