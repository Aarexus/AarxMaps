using System.Net;
using System.Text.Json.Nodes;
using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Extensions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.DistanceMatrix;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AarxMaps.Tests.Routes.Services.DistanceMatrix;

/// <summary>Real MapsApiClient over MockHttp: asserts what Google actually receives and the streamed read.</summary>
[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "DistanceMatrix")]
[Trait("Type", "DistanceMatrixWire")]
public class DistanceMatrixWireTests {
    private const string MatrixUrl = "https://routes.googleapis.com/distanceMatrix/v2:computeRouteMatrix";

    private static DistanceRequest OneByOne() => new DistanceRequestBuilder()
        .AddOrigin(RoutesSamples.At(RoutesSamples.Bogota)).AddDestination(RoutesSamples.At(RoutesSamples.Usaquen)).Build();

    private static async Task<(IReadOnlyList<RouteMatrixElement> Result, RecordedRequest Sent)> GetDistancesAsync(
        DistanceRequest request, Action<IServiceCollection>? configure = null
    ) {
        var http = new RecordingHttp().Respond(HttpMethod.Post, MatrixUrl, RoutesFixtures.Matrix2x2);
        using var provider = RoutesTestServices.BuildWire(http.Mock, configure);

        var result = await provider.GetRequiredService<IDistanceMatrixService>().GetDistancesAsync(request);

        return (result, http.Requests.Should().ContainSingle().Subject);
    }

    private static void BodyShouldBe(RecordedRequest sent, string expectedJson)
        => JsonNode.DeepEquals(JsonNode.Parse(sent.Body!), JsonNode.Parse(expectedJson))
            .Should().BeTrue($"the body sent was {sent.Body}");

    // ===================== body =====================
    [Fact]
    public async Task Body_OneByOne_SendsOnlyTheWaypointsWrappedAsGoogleExpects() {
        var (_, sent) = await GetDistancesAsync(OneByOne());

        BodyShouldBe(sent, """
            {
              "origins": [ { "waypoint": { "location": { "latLng": { "latitude": 4.6097, "longitude": -74.0817 } } } } ],
              "destinations": [ { "waypoint": { "location": { "latLng": { "latitude": 4.6951, "longitude": -74.0305 } } } } ]
            }
            """);
    }

    [Fact]
    public async Task Body_EveryOption_MatchesGooglesSchema() {
        var request = new DistanceRequestBuilder()
            .AddOrigin(RoutesSamples.Place("ChIJ-museo"), new RouteModifiers(avoidTolls: true, emissionType: VehicleEmissionType.Diesel))
            .AddOrigin(RoutesSamples.At(RoutesSamples.Chapinero, heading: 180))
            .WithDestinations([RoutesSamples.Address("Usaquen, Bogota"), RoutesSamples.SideOfRoad(RoutesSamples.Usaquen)])
            .WithTravelMode(TravelMode.Drive)
            .WithRoutingPreference(RoutingPreference.TrafficAwareOptimal)
            .WithTrafficModel(TrafficModel.Optimistic)
            .WithDepartureTime(new DateTimeOffset(2026, 10, 24, 15, 0, 0, TimeSpan.Zero))
            .WithUnits(Units.Imperial)
            .Build();

        var (_, sent) = await GetDistancesAsync(request);

        BodyShouldBe(sent, """
            {
              "origins": [
                { "waypoint": { "placeId": "ChIJ-museo" },
                  "routeModifiers": { "avoidTolls": true, "vehicleInfo": { "emissionType": "DIESEL" } } },
                { "waypoint": { "location": { "latLng": { "latitude": 4.6533, "longitude": -74.0636 }, "heading": 180 } } }
              ],
              "destinations": [
                { "waypoint": { "address": "Usaquen, Bogota" } },
                { "waypoint": { "location": { "latLng": { "latitude": 4.6951, "longitude": -74.0305 } }, "sideOfRoad": true } }
              ],
              "travelMode": "DRIVE",
              "routingPreference": "TRAFFIC_AWARE_OPTIMAL",
              "departureTime": "2026-10-24T15:00:00Z",
              "units": "IMPERIAL",
              "trafficModel": "OPTIMISTIC"
            }
            """);
    }

    // ===================== transport =====================
    [Fact]
    public async Task Request_Always_PostsWithFieldMaskAndApiKeyHeaderAndNoQuery() {
        var (_, sent) = await GetDistancesAsync(OneByOne());

        sent.Method.Should().Be(HttpMethod.Post);
        sent.Uri.Should().Be(new Uri(MatrixUrl));
        sent.Uri.Query.Should().BeEmpty("the API key must never land in the URL");
        sent.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
        sent.Headers.Should().ContainKey("X-Goog-FieldMask").WhoseValue.Should().StartWith("originIndex,destinationIndex,status,condition");
    }

    // ===================== response =====================
    [Fact]
    public async Task Response_StreamedArray_IsMappedAndOrdered() {
        var (result, _) = await GetDistancesAsync(OneByOne());

        result.Select(e => (e.OriginIndex, e.DestinationIndex)).Should().Equal((0, 0), (0, 1), (1, 0), (1, 1));
        result[3].LocalizedValues!.Distance.Should().Be("2.5 km");
    }

    [Fact]
    public async Task Response_WithMemoryCache_IsNeverCached() {
        var http = new RecordingHttp().Respond(HttpMethod.Post, MatrixUrl, RoutesFixtures.Matrix2x2);
        using var provider = RoutesTestServices.BuildWire(http.Mock, s => s.AddAarxMapsMemoryCache(TestServices.ValidCaching));
        var service = provider.GetRequiredService<IDistanceMatrixService>();

        await service.GetDistancesAsync(OneByOne());
        await service.GetDistancesAsync(OneByOne());

        http.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Response_BadRequest_ThrowsMapsInvalidRequestException() {
        var http = new RecordingHttp().Respond(HttpMethod.Post, MatrixUrl,
            """{ "error": { "code": 400, "message": "Too many elements.", "status": "INVALID_ARGUMENT" } }""",
            HttpStatusCode.BadRequest);
        using var provider = RoutesTestServices.BuildWire(http.Mock);

        var act = () => provider.GetRequiredService<IDistanceMatrixService>().GetDistancesAsync(OneByOne());

        (await act.Should().ThrowAsync<MapsInvalidRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
