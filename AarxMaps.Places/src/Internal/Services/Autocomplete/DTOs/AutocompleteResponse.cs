namespace AarxMaps.Places.Internal.Services.Autocomplete.DTOs;

// MAIN MODEL
internal sealed class AutocompleteResponse {
    public List<SuggestionDto> Suggestions { get; set; } = [];
}

// ASIDE MODELS
internal sealed class SuggestionDto {
    public PlacePredictionDto? PlacePrediction { get; set; }
}

internal sealed class PlacePredictionDto {
    public string? PlaceId { get; set; }
    public FormattableTextDto? Text { get; set; }
    public List<string> Types { get; set; } = [];
}

internal sealed class FormattableTextDto {
    public string? Text { get; set; }
    public List<TextMatchDto> Matches { get; set; } = [];
}

internal sealed class TextMatchDto {
    public int? EndOffset { get; set; }
}
