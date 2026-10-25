using AarxMaps.Core.Internal.Json;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.Directions;
using AarxMaps.Routes.Internal.Services.Directions.DTOs;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.Directions;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "Directions")]
[Trait("Type", "DirectionsMapper")]
public class DirectionsMapperTests {
    // Through the real serializer, so the DTOs are also checked against Google's camelCase shape
    private static ComputeRoutesResponse Parse(string json) => MapsJsonSerializer.Deserialize<ComputeRoutesResponse>(json)!;
    private static RouteResult Map(string json) => DirectionsMapper.ToRouteResult(Parse(json));

    // ===================== route =====================
    [Fact]
    public void ToRouteResult_DriveResponse_MapsEveryRouteField() {
        var route = Map(RoutesFixtures.RoutesDrive).Routes[0];

        route.Labels.Should().Equal(RouteLabel.DefaultRoute);
        route.DistanceMeters.Should().Be(1200);
        route.Duration.Should().Be(TimeSpan.FromSeconds(300));
        route.StaticDuration.Should().Be(TimeSpan.FromSeconds(280));
        route.Description.Should().Be("Cra. 7");
        route.Warnings.Should().Equal("This route has restricted usage roads.");
        route.EncodedPolyline.Should().Be("route_poly");
        route.OptimizedWaypointOrder.Should().Equal(1, 0);
        route.RouteToken.Should().Be("route-token-1");
        route.TravelAdvisory!.RouteRestrictionsPartiallyIgnored.Should().BeTrue();
        route.TravelAdvisory.TransitFare.Should().BeNull();
        route.LocalizedValues.Should().BeEquivalentTo(new {
            Distance = "1.2 km", Duration = "5 mins", StaticDuration = "5 mins", TransitFare = (string?)null
        });
    }

    [Fact]
    public void ToRouteResult_DriveResponse_MapsTheViewportCornersInOrder() {
        var viewport = Map(RoutesFixtures.RoutesDrive).Routes[0].Viewport!;

        viewport.Low.Latitude.Should().Be(4.6);
        viewport.Low.Longitude.Should().Be(-74.09);
        viewport.High.Latitude.Should().Be(4.66);
        viewport.High.Longitude.Should().Be(-74.06);
    }

    [Fact]
    public void ToRouteResult_AlternativeWithUnknownLabel_KeepsTheKnownLabelAndMapsTheRestAsUnknown() {
        var alternative = Map(RoutesFixtures.RoutesDrive).Routes[1];

        alternative.Labels.Should().Equal(RouteLabel.DefaultRouteAlternate, RouteLabel.Unknown);
        alternative.DistanceMeters.Should().Be(1500);
        alternative.Duration.Should().Be(TimeSpan.FromSeconds(420));
    }

    [Fact]
    public void ToRouteResult_BareRoute_HasEmptyCollectionsAndNullObjects() {
        var alternative = Map(RoutesFixtures.RoutesDrive).Routes[1];

        alternative.Legs.Should().BeEmpty();
        alternative.Warnings.Should().BeEmpty();
        alternative.OptimizedWaypointOrder.Should().BeEmpty();
        alternative.Viewport.Should().BeNull();
        alternative.EncodedPolyline.Should().BeNull();
        alternative.TravelAdvisory.Should().BeNull();
        alternative.LocalizedValues.Should().BeNull();
    }

    [Fact]
    public void ToRouteResult_Fallback_IsMapped() {
        var fallback = Map(RoutesFixtures.RoutesDrive).FallbackInfo!;

        fallback.RoutingMode.Should().Be(FallbackRoutingMode.FallbackTrafficUnaware);
        fallback.Reason.Should().Be(FallbackReason.LatencyExceeded);
    }

    [Fact]
    public void ToRouteResult_NoRouteFound_HasNoRoutesAndNoFallback() {
        var result = Map(RoutesFixtures.RoutesEmpty);

        result.Routes.Should().BeEmpty();
        result.FallbackInfo.Should().BeNull();
    }

    [Fact]
    public void ToRouteResult_ViewportWithoutACorner_IsNull()
        => Map("""{ "routes": [ { "viewport": { "low": { "latitude": 1, "longitude": 2 } } } ] }""")
            .Routes[0].Viewport.Should().BeNull();

    // ===================== leg & steps =====================
    [Fact]
    public void ToRouteResult_DriveResponse_MapsTheLeg() {
        var leg = Map(RoutesFixtures.RoutesDrive).Routes[0].Legs.Should().ContainSingle().Subject;

        leg.DistanceMeters.Should().Be(1200);
        leg.Duration.Should().Be(TimeSpan.FromSeconds(300));
        leg.StaticDuration.Should().Be(TimeSpan.FromSeconds(280));
        leg.EncodedPolyline.Should().Be("leg_poly");
        leg.StartLocation!.LatLng.Latitude.Should().Be(4.6097);
        leg.StartLocation.LatLng.Longitude.Should().Be(-74.0817);
        leg.EndLocation!.LatLng.Latitude.Should().Be(4.6533);
        leg.Segments.Should().BeEmpty("steps overview is only sent for transit");
        leg.LocalizedValues.Should().BeEquivalentTo(new { Distance = "1.2 km", Duration = "5 mins", StaticDuration = "5 mins" });
    }

    [Fact]
    public void ToRouteResult_DriveResponse_MapsTheStepsInOrder() {
        var steps = Map(RoutesFixtures.RoutesDrive).Routes[0].Legs[0].Steps;

        steps.Should().HaveCount(2);
        steps[0].DistanceMeters.Should().Be(500);
        steps[0].StaticDuration.Should().Be(TimeSpan.FromSeconds(120));
        steps[0].EncodedPolyline.Should().Be("step_poly_1");
        steps[0].EndLocation!.LatLng.Latitude.Should().Be(4.62);
        steps[0].NavigationInstruction.Should().BeEquivalentTo(new { Maneuver = Maneuver.Depart, Instructions = "Head north on Cra. 7" });
        steps[0].LocalizedValues.Should().BeEquivalentTo(new { Distance = "0.5 km", StaticDuration = "2 mins" });
        steps[0].TravelMode.Should().Be(TravelMode.Drive);
        steps[0].TransitDetails.Should().BeNull();

        steps[1].StaticDuration.Should().Be(TimeSpan.FromSeconds(160.5));
        steps[1].NavigationInstruction!.Maneuver.Should().Be(Maneuver.TurnSlightLeft);
        steps[1].EncodedPolyline.Should().BeNull();
        steps[1].LocalizedValues.Should().BeNull();
    }

    // ===================== transit =====================
    [Fact]
    public void ToRouteResult_TransitResponse_MapsTheStepModesAndSegmentsWithOmittedZeroIndex() {
        var leg = Map(RoutesFixtures.RoutesTransit).Routes[0].Legs[0];

        leg.Steps.Select(s => s.TravelMode).Should().Equal(TravelMode.Walk, TravelMode.Transit);
        leg.Segments.Should().BeEquivalentTo(new[] {
            new { StepStartIndex = 0, StepEndIndex = 0, TravelMode = TravelMode.Walk, NavigationInstruction = new { Instructions = "Walk to Museo del Oro" } },
            new { StepStartIndex = 1, StepEndIndex = 1, TravelMode = TravelMode.Transit, NavigationInstruction = new { Instructions = "Bus towards Portal Norte" } }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void ToRouteResult_TransitResponse_MapsTheTransitDetails() {
        var details = Map(RoutesFixtures.RoutesTransit).Routes[0].Legs[0].Steps[1].TransitDetails!;

        details.DepartureStop!.Name.Should().Be("Museo del Oro");
        details.DepartureStop.Location!.Latitude.Should().Be(4.602);
        details.ArrivalStop!.Name.Should().Be("Calle 72");
        details.ArrivalStop.Location!.Longitude.Should().Be(-74.063);
        details.DepartureTime.Should().BeCloseTo(new DateTimeOffset(2026, 10, 24, 15, 0, 0, TimeSpan.Zero), TimeSpan.FromSeconds(1));
        details.ArrivalTime.Should().Be(new DateTimeOffset(2026, 10, 24, 15, 15, 0, TimeSpan.Zero));
        details.DepartureTimeText.Should().Be("10:00 AM");
        details.ArrivalTimeText.Should().Be("10:15 AM");
        details.Headsign.Should().Be("Portal Norte");
        details.StopCount.Should().Be(6);
    }

    [Fact]
    public void ToRouteResult_TransitResponse_MapsTheLineAndDropsNonWebAgencyLinks() {
        var line = Map(RoutesFixtures.RoutesTransit).Routes[0].Legs[0].Steps[1].TransitDetails!.Line!;

        line.Should().BeEquivalentTo(new {
            Name = "Troncal Caracas", ShortName = "B74", Color = "#d30000", TextColor = "#ffffff",
            VehicleType = TransitVehicleType.Bus, VehicleName = "Bus"
        });
        line.Agencies.Should().BeEquivalentTo(new[] {
            new { Name = "TransMilenio", Uri = (Uri?)new Uri("https://www.transmilenio.gov.co/") },
            new { Name = "Shady Agency", Uri = (Uri?)null }
        }, o => o.WithStrictOrdering());
    }

    [Fact]
    public void ToRouteResult_TransitResponse_MapsTheFare() {
        var route = Map(RoutesFixtures.RoutesTransit).Routes[0];

        route.TravelAdvisory!.TransitFare.Should().BeEquivalentTo(new { CurrencyCode = "COP", Amount = 3200.5m });
        route.LocalizedValues!.TransitFare.Should().Be("COP 3,200.50");
    }

    // ===================== copies =====================
    [Fact]
    public void ToRouteResult_SameDtoTwice_BuildsIndependentModels() {
        // Cached DTOs are shared, so every mapping must produce new instances and lists
        var dto = Parse(RoutesFixtures.RoutesDrive);

        var first = DirectionsMapper.ToRouteResult(dto);
        var second = DirectionsMapper.ToRouteResult(dto);

        second.Routes.Should().NotBeSameAs(first.Routes);
        second.Routes[0].Should().NotBeSameAs(first.Routes[0]);
        second.Routes[0].Warnings.Should().NotBeSameAs(first.Routes[0].Warnings);
        second.Routes[0].Legs[0].Steps.Should().NotBeSameAs(first.Routes[0].Legs[0].Steps);
        second.Should().BeEquivalentTo(first);
    }
}
