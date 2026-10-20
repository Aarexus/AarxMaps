using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Places.Internal.Services.Autocomplete.DTOs;

namespace AarxMaps.Places.Internal.Services.Autocomplete;

internal static class AutocompleteMapper {
    public static IReadOnlyList<PlaceSuggestion> ToPlaceSuggestions(AutocompleteResponse response) {
        var suggestions = new List<PlaceSuggestion>(response.Suggestions.Count);

        foreach (var suggestion in response.Suggestions) {
            var prediction = suggestion.PlacePrediction;

            // Query predictions and incomplete entries have no place to suggest
            if (string.IsNullOrWhiteSpace(prediction?.PlaceId) || string.IsNullOrWhiteSpace(prediction.Text?.Text))
                continue;

            suggestions.Add(new PlaceSuggestion() {
                PlaceId = prediction.PlaceId,
                Text = prediction.Text.Text,
                EndOffset = prediction.Text.Matches.Count > 0 ? prediction.Text.Matches[0].EndOffset : null,
                Types = [.. prediction.Types]
            });
        }

        return suggestions;
    }
}
