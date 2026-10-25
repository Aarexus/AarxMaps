using AarxMaps.Core.Exceptions;
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
[Trait("Type", "DirectionsFactory")]
public class DirectionsFactoryTests {
    private const string SummaryMask =
        "fallbackInfo,routes.routeLabels,routes.distanceMeters,routes.duration,routes.staticDuration,"
        + "routes.description,routes.warnings,routes.travelAdvisory,routes.localizedValues,"
        + "routes.optimizedIntermediateWaypointIndex";

    private static readonly RouteRequest _request = new RouteRequestBuilder()
        .From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen)).Build();

    private static string FieldMask(RouteDetailLevel level)
        => DirectionsFactory.CreateRoutesRequest(_request, level).Headers!["X-Goog-FieldMask"];

    // ===================== request shape =====================
    [Fact]
    public void CreateRoutesRequest_Always_PostsToComputeRoutesWithAPayload() {
        var request = DirectionsFactory.CreateRoutesRequest(_request, RouteDetailLevel.Standard);

        request.Method.Should().Be(HttpMethod.Post);
        request.BaseUrl.Should().Be(new Uri("https://routes.googleapis.com/"));
        request.Endpoint.Should().Be("directions/v2:computeRoutes");
        request.Payload.Should().BeOfType<ComputeRoutesPayload>();
        request.Cacheable.Should().BeTrue();
    }

    // ===================== field masks =====================
    [Fact]
    public void CreateRoutesRequest_Summary_RequestsRouteTotalsOnly()
        => FieldMask(RouteDetailLevel.Summary).Should().Be(SummaryMask);

    [Fact]
    public void CreateRoutesRequest_Standard_AddsGeometryAndLegTotals()
        => FieldMask(RouteDetailLevel.Standard).Should().Be(SummaryMask
            + ",routes.polyline.encodedPolyline,routes.viewport,routes.legs.distanceMeters,routes.legs.duration,"
            + "routes.legs.staticDuration,routes.legs.startLocation,routes.legs.endLocation,routes.legs.localizedValues");

    [Fact]
    public void CreateRoutesRequest_Full_AddsTheRouteTokenAndCompleteLegs()
        => FieldMask(RouteDetailLevel.Full).Should().Be(SummaryMask
            + ",routes.polyline.encodedPolyline,routes.viewport,routes.routeToken,routes.legs");

    [Theory]
    [InlineData(RouteDetailLevel.Summary)]
    [InlineData(RouteDetailLevel.Standard)]
    [InlineData(RouteDetailLevel.Full)]
    public void CreateRoutesRequest_EveryLevel_IncludesTheOptimizedOrderGoogleRequiresForOptimization(RouteDetailLevel level)
        => FieldMask(level).Split(',').Should().Contain("routes.optimizedIntermediateWaypointIndex");

    [Fact]
    public void CreateRoutesRequest_UndefinedLevel_ThrowsMapsInvalidRequestException() {
        var act = () => DirectionsFactory.CreateRoutesRequest(_request, (RouteDetailLevel)42);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("42 is not a valid RouteDetailLevel.");
    }

    // ===================== raw =====================
    [Fact]
    public void CreateRawRoutesRequest_Fields_AreTrimmedAndDeduplicatedInOrder()
        => DirectionsFactory.CreateRawRoutesRequest(_request, [" routes.duration ", "routes.legs", "routes.duration"])
            .Headers!["X-Goog-FieldMask"].Should().Be("routes.duration,routes.legs");

    [Fact]
    public void CreateRawRoutesRequest_NullFields_ThrowsMapsInvalidRequestException() {
        var act = () => DirectionsFactory.CreateRawRoutesRequest(_request, null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Fields cannot be null.");
    }

    [Fact]
    public void CreateRawRoutesRequest_EmptyFields_ThrowsMapsInvalidRequestException() {
        var act = () => DirectionsFactory.CreateRawRoutesRequest(_request, []);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At least one field is required.");
    }

    [Fact]
    public void CreateRawRoutesRequest_BlankField_ThrowsMapsInvalidRequestException() {
        var act = () => DirectionsFactory.CreateRawRoutesRequest(_request, ["routes.duration", " "]);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Empty fields are not allowed.");
    }

    // ===================== payload =====================
    [Fact]
    public void ToPayload_EndsOnly_LeavesEveryOptionUnset() {
        var payload = DirectionsFactory.ToPayload(_request);

        payload.Intermediates.Should().BeNull("an empty list would be noise in the body");
        payload.Should().BeEquivalentTo(new {
            TravelMode = (string?)null, RoutingPreference = (string?)null, PolylineQuality = (string?)null,
            DepartureTime = (string?)null, ArrivalTime = (string?)null, Units = (string?)null, TrafficModel = (string?)null,
            ComputeAlternativeRoutes = false, OptimizeWaypointOrder = false
        });
        payload.RouteModifiers.Should().BeNull();
        payload.TransitPreferences.Should().BeNull();
    }

    [Fact]
    public void ToPayload_UndefinedEnumValue_ThrowsMapsInvalidRequestException() {
        var request = new RouteRequestBuilder()
            .From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen))
            .WithUnits((Units)42).Build();

        var act = () => DirectionsFactory.ToPayload(request);

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("42 is not a valid Units.");
    }
}
