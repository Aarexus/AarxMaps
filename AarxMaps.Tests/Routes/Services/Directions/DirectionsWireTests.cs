using System.Net;
using System.Text.Json.Nodes;
using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Tests.Core.Helpers;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AarxMaps.Tests.Routes.Services.Directions;

/// <summary>Real MapsApiClient over MockHttp: asserts what Google actually receives.</summary>
[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "Directions")]
[Trait("Type", "DirectionsWire")]
public class DirectionsWireTests {
    private const string RoutesUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";

    // Builders are mutable, so every test gets its own
    private static RouteRequestBuilder Ends() => new RouteRequestBuilder()
        .From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen));

    private static async Task<(RouteResult Result, RecordedRequest Sent)> GetRoutesAsync(
        RouteRequest request, string response = RoutesFixtures.RoutesDrive
    ) {
        var http = new RecordingHttp().Respond(HttpMethod.Post, RoutesUrl, response);
        using var provider = RoutesTestServices.BuildWire(http.Mock);

        var result = await provider.GetRequiredService<IDirectionsService>().GetRoutesAsync(request);

        return (result, http.Requests.Should().ContainSingle().Subject);
    }

    private static void BodyShouldBe(RecordedRequest sent, string expectedJson)
        => JsonNode.DeepEquals(JsonNode.Parse(sent.Body!), JsonNode.Parse(expectedJson))
            .Should().BeTrue($"the body sent was {sent.Body}");

    // ===================== body =====================
    [Fact]
    public async Task Body_EndsOnly_SendsOnlyTheWaypointsWithNoNullsOrFalseFlags() {
        var (_, sent) = await GetRoutesAsync(Ends().Build());

        BodyShouldBe(sent, """
            {
              "origin": { "location": { "latLng": { "latitude": 4.6097, "longitude": -74.0817 } } },
              "destination": { "location": { "latLng": { "latitude": 4.6951, "longitude": -74.0305 } } }
            }
            """);
    }

    [Fact]
    public async Task Body_EveryDriveOption_MatchesGooglesSchema() {
        var request = new RouteRequestBuilder()
            .From(new WaypointBuilder().FromLocation(RoutesSamples.Bogota, 90).AsVehicleStopover().WithSideOfRoad().Build())
            .To(RoutesSamples.Place("ChIJ-usaquen"))
            .AddIntermediate(RoutesSamples.Address("Chapinero, Bogota"))
            .AddIntermediate(RoutesSamples.Via(RoutesSamples.Chapinero))
            .WithTravelMode(TravelMode.Drive)
            .WithRoutingPreference(RoutingPreference.TrafficAwareOptimal)
            .WithTrafficModel(TrafficModel.Pessimistic)
            .WithPolylineQuality(PolylineQuality.HighQuality)
            .WithDepartureTime(new DateTimeOffset(2026, 10, 24, 10, 30, 0, 500, TimeSpan.FromHours(-5)))
            .WithAlternativeRoutes()
            .WithRouteModifiers(new RouteModifiers(avoidTolls: true, avoidHighways: true, avoidFerries: true,
                emissionType: VehicleEmissionType.Electric))
            .WithUnits(Units.Metric)
            .Build();

        var (_, sent) = await GetRoutesAsync(request);

        BodyShouldBe(sent, """
            {
              "origin": {
                "location": { "latLng": { "latitude": 4.6097, "longitude": -74.0817 }, "heading": 90 },
                "vehicleStopover": true,
                "sideOfRoad": true
              },
              "destination": { "placeId": "ChIJ-usaquen" },
              "intermediates": [
                { "address": "Chapinero, Bogota" },
                { "location": { "latLng": { "latitude": 4.6533, "longitude": -74.0636 } }, "via": true }
              ],
              "travelMode": "DRIVE",
              "routingPreference": "TRAFFIC_AWARE_OPTIMAL",
              "polylineQuality": "HIGH_QUALITY",
              "departureTime": "2026-10-24T15:30:00Z",
              "computeAlternativeRoutes": true,
              "routeModifiers": {
                "avoidTolls": true, "avoidHighways": true, "avoidFerries": true,
                "vehicleInfo": { "emissionType": "ELECTRIC" }
              },
              "units": "METRIC",
              "trafficModel": "PESSIMISTIC"
            }
            """);
    }

    [Fact]
    public async Task Body_TransitOptions_MatchGooglesSchema() {
        var request = new RouteRequestBuilder()
            .From(RoutesSamples.Place("ChIJ-museo")).To(RoutesSamples.Place("ChIJ-calle72"))
            .WithTravelMode(TravelMode.Transit)
            .WithArrivalTime(new DateTimeOffset(2026, 10, 24, 15, 15, 0, TimeSpan.Zero))
            .WithTransitPreferences(new TransitPreferences([TransitTravelMode.Bus, TransitTravelMode.LightRail],
                TransitRoutingPreference.LessWalking))
            .Build();

        var (_, sent) = await GetRoutesAsync(request, RoutesFixtures.RoutesTransit);

        BodyShouldBe(sent, """
            {
              "origin": { "placeId": "ChIJ-museo" },
              "destination": { "placeId": "ChIJ-calle72" },
              "travelMode": "TRANSIT",
              "arrivalTime": "2026-10-24T15:15:00Z",
              "transitPreferences": { "allowedTravelModes": [ "BUS", "LIGHT_RAIL" ], "routingPreference": "LESS_WALKING" }
            }
            """);
    }

    [Fact]
    public async Task Body_WaypointOptimization_SendsTheFlag() {
        var (_, sent) = await GetRoutesAsync(Ends().AddIntermediate(RoutesSamples.At(RoutesSamples.Chapinero))
            .WithOptimizedWaypointOrder().Build());

        JsonNode.Parse(sent.Body!)!["optimizeWaypointOrder"]!.GetValue<bool>().Should().BeTrue();
    }

    // ===================== transport =====================
    [Fact]
    public async Task Request_Always_PostsWithFieldMaskAndApiKeyHeaderAndNoQuery() {
        var (_, sent) = await GetRoutesAsync(Ends().Build());

        sent.Method.Should().Be(HttpMethod.Post);
        sent.Uri.Should().Be(new Uri(RoutesUrl));
        sent.Uri.Query.Should().BeEmpty("the API key must never land in the URL");
        sent.Headers.Should().Contain("X-Goog-Api-Key", TestRequests.DefaultApiKey);
        sent.Headers.Should().ContainKey("X-Goog-FieldMask").WhoseValue.Should().StartWith("fallbackInfo,routes.routeLabels");
    }

    // ===================== response =====================
    [Fact]
    public async Task Response_GoogleFixture_IsMapped() {
        var (result, _) = await GetRoutesAsync(Ends().Build());

        result.Routes.Should().HaveCount(2);
        result.Routes[0].Duration.Should().Be(TimeSpan.FromSeconds(300));
        result.Routes[0].Legs[0].Steps[1].NavigationInstruction!.Maneuver.Should().Be(Maneuver.TurnSlightLeft);
    }

    [Fact]
    public async Task Response_BadRequest_ThrowsMapsInvalidRequestException() {
        // e.g. an address Google cannot geocode
        var http = new RecordingHttp().Respond(HttpMethod.Post, RoutesUrl,
            """{ "error": { "code": 400, "message": "Address could not be geocoded.", "status": "INVALID_ARGUMENT" } }""",
            HttpStatusCode.BadRequest);
        using var provider = RoutesTestServices.BuildWire(http.Mock);

        var act = () => provider.GetRequiredService<IDirectionsService>().GetRoutesAsync(new RouteRequestBuilder()
            .From(RoutesSamples.Address("nowhere at all")).To(RoutesSamples.At(RoutesSamples.Usaquen)).Build());

        (await act.Should().ThrowAsync<MapsInvalidRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RawResponse_Fields_AreSentAsTheMaskAndTheJsonIsReturned() {
        var http = new RecordingHttp().Respond(HttpMethod.Post, RoutesUrl, RoutesFixtures.RoutesDrive);
        using var provider = RoutesTestServices.BuildWire(http.Mock);

        using var json = await provider.GetRequiredService<IDirectionsService>().GetRoutesRawAsync(Ends().Build(), ["routes.routeToken"]);

        http.Requests.Single().Headers["X-Goog-FieldMask"].Should().Be("routes.routeToken");
        json.RootElement.GetProperty("routes")[0].GetProperty("routeToken").GetString().Should().Be("route-token-1");
    }
}
