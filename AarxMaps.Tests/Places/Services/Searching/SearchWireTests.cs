using System.Net;
using System.Text.Json.Nodes;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Searching;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AarxMaps.Tests.Places.Services.Searching;

/// <summary>Real MapsApiClient over MockHttp: asserts what Google actually receives.</summary>
[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Searching")]
[Trait("Type", "SearchWire")]
public class SearchWireTests {
    private const string TextUrl = "https://places.googleapis.com/v1/places:searchText";
    private const string NearbyUrl = "https://places.googleapis.com/v1/places:searchNearby";

    private static readonly LatLng _bogota = new(4.6097, -74.0817);
    private static readonly LatLng _southWest = new(4.5, -74.2);
    private static readonly LatLng _northEast = new(4.8, -73.9);

    private static async Task<(TextSearchResult Result, RecordedRequest Sent)> SearchTextAsync(TextRequest request) {
        var http = new RecordingHttp().Respond(HttpMethod.Post, TextUrl, PlacesFixtures.SearchTextPage);
        using var provider = PlacesTestServices.BuildWire(http.Mock);

        var result = await provider.GetRequiredService<ISearchService>().SearchByTextAsync(request);

        return (result, http.Requests.Should().ContainSingle().Subject);
    }

    private static async Task<(NearbySearchResult Result, RecordedRequest Sent)> SearchNearbyAsync(NearbyRequest request) {
        var http = new RecordingHttp().Respond(HttpMethod.Post, NearbyUrl, PlacesFixtures.SearchNearbyPage);
        using var provider = PlacesTestServices.BuildWire(http.Mock);

        var result = await provider.GetRequiredService<ISearchService>().SearchNearbyAsync(request);

        return (result, http.Requests.Should().ContainSingle().Subject);
    }

    private static void BodyShouldBe(RecordedRequest sent, string expectedJson)
        => JsonNode.DeepEquals(JsonNode.Parse(sent.Body!), JsonNode.Parse(expectedJson))
            .Should().BeTrue($"the body sent was {sent.Body}");

    private static void ShouldBePostWithHeaders(RecordedRequest sent, string url, string fieldMask) {
        sent.Method.Should().Be(HttpMethod.Post);
        sent.Uri.Should().Be(new Uri(url));
        sent.Uri.Query.Should().BeEmpty("the API key must never land in the URL");
        sent.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
        sent.Headers.Should().ContainKey("X-Goog-FieldMask").WhoseValue.Should().Be(fieldMask);
    }

    // ===================== text body =====================
    [Fact]
    public async Task TextBody_QueryOnly_SendsQueryAndDefaultPageSizeWithNoNullFields() {
        var (_, sent) = await SearchTextAsync(new TextRequestBuilder().WithTextQuery("pizza").Build());

        BodyShouldBe(sent, """{ "textQuery": "pizza", "pageSize": 20 }""");
    }

    [Fact]
    public async Task TextBody_EveryFieldWithCircleBias_MatchesGooglesSchema() {
        var request = new TextRequestBuilder()
            .WithTextQuery("pizza")
            .WithPageSize(5)
            .WithPageToken("page-2-token")
            .WithIncludedType("restaurant")
            .WithLocationBias(_bogota, 1500)
            .Build();

        var (_, sent) = await SearchTextAsync(request);

        BodyShouldBe(sent, """
            {
              "textQuery": "pizza",
              "pageSize": 5,
              "pageToken": "page-2-token",
              "includedType": "restaurant",
              "locationBias": { "circle": { "center": { "latitude": 4.6097, "longitude": -74.0817 }, "radius": 1500 } }
            }
            """);
    }

    [Fact]
    public async Task TextBody_RectangleBias_IsSentAsLowHighViewport() {
        var (_, sent) = await SearchTextAsync(
            new TextRequestBuilder().WithTextQuery("pizza").WithLocationBias(_southWest, _northEast).Build());

        BodyShouldBe(sent, """
            {
              "textQuery": "pizza",
              "pageSize": 20,
              "locationBias": { "rectangle": {
                "low": { "latitude": 4.5, "longitude": -74.2 },
                "high": { "latitude": 4.8, "longitude": -73.9 } } }
            }
            """);
    }

    [Fact]
    public async Task TextBody_RectangleRestriction_IsSentAsLocationRestrictionRectangle() {
        var (_, sent) = await SearchTextAsync(
            new TextRequestBuilder().WithTextQuery("pizza").WithLocationRestriction(_southWest, _northEast).Build());

        BodyShouldBe(sent, """
            {
              "textQuery": "pizza",
              "pageSize": 20,
              "locationRestriction": { "rectangle": {
                "low": { "latitude": 4.5, "longitude": -74.2 },
                "high": { "latitude": 4.8, "longitude": -73.9 } } }
            }
            """);
    }

    // ===================== nearby body =====================
    [Fact]
    public async Task NearbyBody_RestrictionOnly_SendsCircleAndDefaultMaxResultCount() {
        var (_, sent) = await SearchNearbyAsync(new NearbyRequestBuilder().WithLocationRestriction(_bogota, 800).Build());

        BodyShouldBe(sent, """
            {
              "locationRestriction": { "circle": { "center": { "latitude": 4.6097, "longitude": -74.0817 }, "radius": 800 } },
              "maxResultCount": 20
            }
            """);
    }

    [Fact]
    public async Task NearbyBody_EveryField_MatchesGooglesSchema() {
        var request = new NearbyRequestBuilder()
            .WithLocationRestriction(_bogota, 800)
            .WithMaxResults(10)
            .WithIncludedTypes(["cafe", "bakery"])
            .Build();

        var (_, sent) = await SearchNearbyAsync(request);

        BodyShouldBe(sent, """
            {
              "locationRestriction": { "circle": { "center": { "latitude": 4.6097, "longitude": -74.0817 }, "radius": 800 } },
              "maxResultCount": 10,
              "includedTypes": [ "cafe", "bakery" ]
            }
            """);
    }

    // ===================== transport =====================
    [Fact]
    public async Task TextRequest_Always_PostsWithTextFieldMaskAndApiKeyHeaderAndNoQuery() {
        var (_, sent) = await SearchTextAsync(new TextRequestBuilder().WithTextQuery("pizza").Build());

        ShouldBePostWithHeaders(sent, TextUrl, "nextPageToken,places.id,places.displayName,places.types");
    }

    [Fact]
    public async Task NearbyRequest_Always_PostsWithNearbyFieldMaskAndApiKeyHeaderAndNoQuery() {
        var (_, sent) = await SearchNearbyAsync(new NearbyRequestBuilder().WithLocationRestriction(_bogota, 800).Build());

        ShouldBePostWithHeaders(sent, NearbyUrl, "places.id,places.displayName,places.types");
    }

    // ===================== response =====================
    [Fact]
    public async Task TextResponse_GoogleFixture_IsMappedWithTheNextPageToken() {
        var (result, _) = await SearchTextAsync(new TextRequestBuilder().WithTextQuery("pizza").Build());

        result.NextPageToken.Should().Be("page-2-token");
        result.Places.Should().BeEquivalentTo(new[] {
            new { PlaceId = "ChIJ-pizza-1", DisplayName = "Pizza Uno", Types = new[] { "pizza_restaurant", "restaurant" } },
            new { PlaceId = "ChIJ-pizza-2", DisplayName = "Pizza Dos", Types = new[] { "restaurant" } }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task NearbyResponse_GoogleFixture_IsMapped() {
        var (result, _) = await SearchNearbyAsync(new NearbyRequestBuilder().WithLocationRestriction(_bogota, 800).Build());

        result.Places.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { PlaceId = "ChIJ-cafe-1", DisplayName = "Cafe Uno", Types = new[] { "cafe", "food" } });
    }

    [Fact]
    public async Task TextResponse_BadRequest_ThrowsMapsInvalidRequestException() {
        // e.g. an expired page token, or parameters that differ from the first page
        var http = new RecordingHttp().Respond(HttpMethod.Post, TextUrl,
            """{ "error": { "code": 400, "message": "Invalid page token.", "status": "INVALID_ARGUMENT" } }""",
            HttpStatusCode.BadRequest);
        using var provider = PlacesTestServices.BuildWire(http.Mock);

        var act = () => provider.GetRequiredService<ISearchService>()
            .SearchByTextAsync(new TextRequestBuilder().WithTextQuery("pizza").WithPageToken("expired").Build());

        (await act.Should().ThrowAsync<MapsInvalidRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
