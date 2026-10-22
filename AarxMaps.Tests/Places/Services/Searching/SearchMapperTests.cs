using AarxMaps.Core.Internal.Json;
using AarxMaps.Places.Internal.Services.Searching;
using AarxMaps.Places.Internal.Services.Searching.DTOs;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Searching;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Searching")]
[Trait("Type", "SearchMapper")]
public class SearchMapperTests {
    private const string ValidPlace = """{ "id": "valid", "displayName": { "text": "Valid place" } }""";

    // Through the real serializer, so the DTOs are also checked against Google's camelCase shape
    private static SearchResponse Parse(string json) => MapsJsonSerializer.Deserialize<SearchResponse>(json)!;

    private static SearchResponse ParsePlaces(params string[] places)
        => Parse($$"""{ "places": [ {{string.Join(",", places)}} ] }""");

    // ===================== text =====================
    [Fact]
    public void ToTextResult_GooglePage_MapsEveryPlaceAndTheNextPageToken() {
        var result = SearchMapper.ToTextResult(Parse(PlacesFixtures.SearchTextPage));

        result.NextPageToken.Should().Be("page-2-token");
        result.Places.Should().HaveCount(2);
        result.Places[0].PlaceId.Should().Be("ChIJ-pizza-1");
        result.Places[0].DisplayName.Should().Be("Pizza Uno");
        result.Places[0].Types.Should().Equal("pizza_restaurant", "restaurant");
        result.Places[1].PlaceId.Should().Be("ChIJ-pizza-2");
    }

    [Theory]
    [InlineData("""{ "places": [] }""")]
    [InlineData("""{ "places": [], "nextPageToken": "" }""")]
    public void ToTextResult_NoTokenOrEmptyToken_NextPageTokenIsNull(string json)
        => SearchMapper.ToTextResult(Parse(json)).NextPageToken.Should().BeNull();

    [Fact]
    public void ToTextResult_EmptyResponse_ReturnsNoPlacesAndNoToken() {
        var result = SearchMapper.ToTextResult(Parse("{}"));

        result.Places.Should().BeEmpty();
        result.NextPageToken.Should().BeNull();
    }

    // ===================== nearby =====================
    [Fact]
    public void ToNearbyResult_GooglePage_MapsEveryPlace() {
        var place = SearchMapper.ToNearbyResult(Parse(PlacesFixtures.SearchNearbyPage)).Places.Should().ContainSingle().Subject;

        place.PlaceId.Should().Be("ChIJ-cafe-1");
        place.DisplayName.Should().Be("Cafe Uno");
        place.Types.Should().Equal("cafe", "food");
    }

    [Fact]
    public void ToNearbyResult_EmptyResponse_ReturnsNoPlaces()
        => SearchMapper.ToNearbyResult(Parse("{}")).Places.Should().BeEmpty();

    // ===================== shared place mapping =====================
    [Fact]
    public void Places_SeveralEntries_KeepGoogleOrder()
        => SearchMapper.ToNearbyResult(ParsePlaces(
                """{ "id": "a", "displayName": { "text": "A" } }""",
                """{ "id": "b", "displayName": { "text": "B" } }""",
                """{ "id": "c", "displayName": { "text": "C" } }"""))
            .Places.Select(p => p.PlaceId).Should().Equal("a", "b", "c");

    [Fact]
    public void Places_TypesMissing_IsEmpty()
        => SearchMapper.ToNearbyResult(ParsePlaces(ValidPlace)).Places.Single().Types.Should().BeEmpty();

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "displayName": { "text": "No id" } }""")]
    [InlineData("""{ "id": " ", "displayName": { "text": "Blank id" } }""")]
    [InlineData("""{ "id": "p" }""")]
    [InlineData("""{ "id": "p", "displayName": {} }""")]
    [InlineData("""{ "id": "p", "displayName": { "text": "  " } }""")]
    public void Places_IncompleteEntry_IsSkippedAndTheRestAreKept(string place) {
        var response = ParsePlaces(place, ValidPlace);

        SearchMapper.ToTextResult(response).Places.Should().ContainSingle().Which.PlaceId.Should().Be("valid");
        SearchMapper.ToNearbyResult(response).Places.Should().ContainSingle().Which.PlaceId.Should().Be("valid");
    }

    [Fact]
    public void Places_Types_AreACopyOfTheDtoList() {
        // Cached DTOs are shared between callers, so the public model must not alias their lists
        var dtoTypes = new List<string> { "restaurant" };
        var response = new SearchResponse {
            Places = [ new PlaceDto { Id = "p", DisplayName = new LocalizedTextDto { Text = "P" }, Types = dtoTypes } ]
        };

        var types = SearchMapper.ToTextResult(response).Places.Single().Types;
        dtoTypes.Add("cafe");

        types.Should().NotBeSameAs(dtoTypes);
        types.Should().Equal("restaurant");
    }
}
