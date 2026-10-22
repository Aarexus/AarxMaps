using System.Net;
using System.Text.Json.Nodes;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Autocomplete;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Places.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AarxMaps.Tests.Places.Services.Autocomplete;

/// <summary>Real MapsApiClient over MockHttp: asserts what Google actually receives.</summary>
[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Autocomplete")]
[Trait("Type", "AutocompleteWire")]
public class AutocompleteWireTests {
    private const string Url = "https://places.googleapis.com/v1/places:autocomplete";
    private static readonly LatLng _bogota = new(4.6097, -74.0817);

    private static async Task<(IReadOnlyList<PlaceSuggestion> Result, RecordedRequest Sent)> SendAsync(
        AutocompleteRequest request, string response = PlacesFixtures.AutocompleteSingle
    ) {
        var http = new RecordingHttp().Respond(HttpMethod.Post, Url, response);
        using var provider = PlacesTestServices.BuildWire(http.Mock);

        var result = await provider.GetRequiredService<IAutocompleteService>().SuggestPlacesAsync(request);

        return (result, http.Requests.Should().ContainSingle().Subject);
    }

    private static void BodyShouldBe(RecordedRequest sent, string expectedJson)
        => JsonNode.DeepEquals(JsonNode.Parse(sent.Body!), JsonNode.Parse(expectedJson))
            .Should().BeTrue($"the body sent was {sent.Body}");

    // ===================== body =====================
    [Fact]
    public async Task Body_InputOnly_SendsOnlyInputWithNoNullFields() {
        var (_, sent) = await SendAsync(new AutocompleteRequestBuilder().WithInput("Bogota").Build());

        BodyShouldBe(sent, """{ "input": "Bogota" }""");
    }

    [Fact]
    public async Task Body_EveryFieldWithBias_MatchesGooglesSchema() {
        var request = new AutocompleteRequestBuilder()
            .WithInput("pizza")
            .WithIncludedPrimaryTypes(["restaurant", "cafe"])
            .WithLocationBias(_bogota, 1500)
            .WithSessionToken("tok-1")
            .Build();

        var (_, sent) = await SendAsync(request);

        BodyShouldBe(sent, """
            {
              "input": "pizza",
              "includedPrimaryTypes": [ "restaurant", "cafe" ],
              "locationBias": { "circle": { "center": { "latitude": 4.6097, "longitude": -74.0817 }, "radius": 1500 } },
              "sessionToken": "tok-1"
            }
            """);
    }

    [Fact]
    public async Task Body_Restriction_IsSentAsLocationRestrictionCircle() {
        var request = new AutocompleteRequestBuilder().WithInput("pizza").WithLocationRestriction(_bogota, 800).Build();

        var (_, sent) = await SendAsync(request);

        BodyShouldBe(sent, """
            {
              "input": "pizza",
              "locationRestriction": { "circle": { "center": { "latitude": 4.6097, "longitude": -74.0817 }, "radius": 800 } }
            }
            """);
    }

    // ===================== transport =====================
    [Fact]
    public async Task Request_Always_PostsWithFieldMaskAndApiKeyHeaderAndNoQuery() {
        var (_, sent) = await SendAsync(new AutocompleteRequestBuilder().WithInput("Bogota").Build());

        sent.Method.Should().Be(HttpMethod.Post);
        sent.Uri.Should().Be(new Uri(Url));
        sent.Uri.Query.Should().BeEmpty("the API key must never land in the URL");
        sent.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
        sent.Headers.Should().ContainKey("X-Goog-FieldMask")
            .WhoseValue.Should().Be("suggestions.placePrediction.placeId,suggestions.placePrediction.text,suggestions.placePrediction.types");
    }

    // ===================== response =====================
    [Fact]
    public async Task Response_GoogleFixture_IsMappedToSuggestions() {
        var (result, _) = await SendAsync(new AutocompleteRequestBuilder().WithInput("Bogota").Build());

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new {
            PlaceId = "ChIJ-place-1",
            Text = "Bogota, Colombia",
            EndOffset = 6,
            Types = new[] { "locality", "political" }
        });
    }

    [Fact]
    public async Task Response_NotFound_ThrowsMapsNotFoundException() {
        var http = new RecordingHttp().Respond(HttpMethod.Post, Url,
            """{ "error": { "code": 404, "message": "Not found.", "status": "NOT_FOUND" } }""", HttpStatusCode.NotFound);
        using var provider = PlacesTestServices.BuildWire(http.Mock);

        var act = () => provider.GetRequiredService<IAutocompleteService>()
            .SuggestPlacesAsync(new AutocompleteRequestBuilder().WithInput("Bogota").Build());

        (await act.Should().ThrowAsync<MapsNotFoundException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
