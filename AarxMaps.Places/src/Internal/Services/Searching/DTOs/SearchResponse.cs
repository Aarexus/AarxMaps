namespace AarxMaps.Places.Internal.Services.Searching.DTOs;

// MAIN MODEL
// Shared by Text Search and Nearby Search; Nearby never returns a page token
internal sealed class SearchResponse {
    public List<PlaceDto> Places { get; set; } = [];
    public string? NextPageToken { get; set; }
}

// ASIDE MODELS
internal sealed class PlaceDto {
    public string? Id { get; set; }
    public LocalizedTextDto? DisplayName { get; set; }
    public List<string> Types { get; set; } = [];
}

internal sealed class LocalizedTextDto {
    public string? Text { get; set; }
}
