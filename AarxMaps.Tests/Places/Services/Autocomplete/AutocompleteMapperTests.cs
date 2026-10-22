using AarxMaps.Core.Internal.Json;
using AarxMaps.Places.Internal.Services.Autocomplete;
using AarxMaps.Places.Internal.Services.Autocomplete.DTOs;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Autocomplete;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Autocomplete")]
[Trait("Type", "AutocompleteMapper")]
public class AutocompleteMapperTests {
    private const string ValidEntry = """
        { "placePrediction": { "placeId": "valid", "text": { "text": "Valid place" } } }
        """;

    // Through the real serializer, so the DTOs are also checked against Google's camelCase shape
    private static IReadOnlyList<PlaceSuggestion> Map(string json)
        => AutocompleteMapper.ToPlaceSuggestions(MapsJsonSerializer.Deserialize<AutocompleteResponse>(json)!);

    private static IReadOnlyList<PlaceSuggestion> MapEntries(params string[] entries)
        => Map($$"""{ "suggestions": [ {{string.Join(",", entries)}} ] }""");

    // ===================== mapping =====================
    [Fact]
    public void ToPlaceSuggestions_FullPrediction_MapsEveryField() {
        var suggestion = Map(PlacesFixtures.AutocompleteSingle).Should().ContainSingle().Subject;

        suggestion.PlaceId.Should().Be("ChIJ-place-1");
        suggestion.Text.Should().Be("Bogota, Colombia");
        suggestion.EndOffset.Should().Be(6);
        suggestion.Types.Should().Equal("locality", "political");
    }

    [Fact]
    public void ToPlaceSuggestions_SeveralPredictions_KeepsGoogleOrder() {
        var suggestions = MapEntries(
            """{ "placePrediction": { "placeId": "a", "text": { "text": "A" } } }""",
            """{ "placePrediction": { "placeId": "b", "text": { "text": "B" } } }""",
            """{ "placePrediction": { "placeId": "c", "text": { "text": "C" } } }""");

        suggestions.Select(s => s.PlaceId).Should().Equal("a", "b", "c");
    }

    [Fact]
    public void ToPlaceSuggestions_SeveralMatches_UsesTheFirstEndOffset()
        => MapEntries("""
            { "placePrediction": { "placeId": "p", "text": {
                "text": "Pizza Palace", "matches": [ { "endOffset": 5 }, { "startOffset": 6, "endOffset": 9 } ] } } }
            """).Single().EndOffset.Should().Be(5);

    [Theory]
    [InlineData("""{ "placePrediction": { "placeId": "p", "text": { "text": "P" } } }""")]
    [InlineData("""{ "placePrediction": { "placeId": "p", "text": { "text": "P", "matches": [] } } }""")]
    [InlineData("""{ "placePrediction": { "placeId": "p", "text": { "text": "P", "matches": [ { "startOffset": 0 } ] } } }""")]
    public void ToPlaceSuggestions_NoUsableMatch_EndOffsetIsNull(string entry)
        => MapEntries(entry).Single().EndOffset.Should().BeNull();

    [Fact]
    public void ToPlaceSuggestions_TypesMissing_IsEmpty()
        => MapEntries(ValidEntry).Single().Types.Should().BeEmpty();

    [Fact]
    public void ToPlaceSuggestions_Types_AreACopyOfTheDtoList() {
        // Cached DTOs are shared between callers, so the public model must not alias their lists
        var dtoTypes = new List<string> { "restaurant" };
        var response = new AutocompleteResponse {
            Suggestions = [ new SuggestionDto {
                PlacePrediction = new PlacePredictionDto {
                    PlaceId = "p", Text = new FormattableTextDto { Text = "P" }, Types = dtoTypes
                }
            } ]
        };

        var types = AutocompleteMapper.ToPlaceSuggestions(response).Single().Types;
        dtoTypes.Add("cafe");

        types.Should().NotBeSameAs(dtoTypes);
        types.Should().Equal("restaurant");
    }

    // ===================== skipped entries =====================
    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "queryPrediction": { "text": { "text": "pizza near me" } } }""")]
    [InlineData("""{ "placePrediction": { "text": { "text": "No id" } } }""")]
    [InlineData("""{ "placePrediction": { "placeId": "  ", "text": { "text": "Blank id" } } }""")]
    [InlineData("""{ "placePrediction": { "placeId": "p" } }""")]
    [InlineData("""{ "placePrediction": { "placeId": "p", "text": {} } }""")]
    [InlineData("""{ "placePrediction": { "placeId": "p", "text": { "text": " " } } }""")]
    public void ToPlaceSuggestions_IncompleteOrQueryEntry_IsSkippedAndTheRestAreKept(string entry)
        => MapEntries(entry, ValidEntry).Should().ContainSingle().Which.PlaceId.Should().Be("valid");

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "suggestions": [] }""")]
    public void ToPlaceSuggestions_NoSuggestions_ReturnsAnEmptyList(string json)
        => Map(json).Should().BeEmpty();
}
