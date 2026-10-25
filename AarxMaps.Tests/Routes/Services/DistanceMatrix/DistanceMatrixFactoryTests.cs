using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.DistanceMatrix;
using AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;
using AarxMaps.Routes.Services.DistanceMatrix;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.DistanceMatrix;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "DistanceMatrix")]
[Trait("Type", "DistanceMatrixFactory")]
public class DistanceMatrixFactoryTests {
    private const string SummaryMask = "originIndex,destinationIndex,status,condition,distanceMeters,duration,fallbackInfo";

    private static readonly DistanceRequest _request = new DistanceRequestBuilder()
        .AddOrigin(RoutesSamples.At(RoutesSamples.Bogota)).AddDestination(RoutesSamples.At(RoutesSamples.Usaquen)).Build();

    private static string FieldMask(RouteDetailLevel level)
        => DistanceMatrixFactory.CreateMatrixRequest(_request, level).Headers!["X-Goog-FieldMask"];

    [Fact]
    public void CreateMatrixRequest_Always_PostsToComputeRouteMatrixWithAPayload() {
        var request = DistanceMatrixFactory.CreateMatrixRequest(_request, RouteDetailLevel.Standard);

        request.Method.Should().Be(HttpMethod.Post);
        request.BaseUrl.Should().Be(new Uri("https://routes.googleapis.com/"));
        request.Endpoint.Should().Be("distanceMatrix/v2:computeRouteMatrix");
        request.Payload.Should().BeOfType<ComputeRouteMatrixPayload>();
    }

    [Fact]
    public void CreateMatrixRequest_Summary_RequestsIndicesStatusAndTotals()
        => FieldMask(RouteDetailLevel.Summary).Should().Be(SummaryMask);

    [Fact]
    public void CreateMatrixRequest_Standard_AddsStaticDurationAndLocalizedValues()
        => FieldMask(RouteDetailLevel.Standard).Should().Be(SummaryMask + ",staticDuration,localizedValues");

    [Fact]
    public void CreateMatrixRequest_Full_AddsTheTravelAdvisory()
        => FieldMask(RouteDetailLevel.Full).Should().Be(SummaryMask + ",staticDuration,localizedValues,travelAdvisory");

    [Fact]
    public void CreateMatrixRequest_UndefinedLevel_ThrowsMapsInvalidRequestException() {
        var act = () => DistanceMatrixFactory.CreateMatrixRequest(_request, (RouteDetailLevel)42);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("42 is not a valid RouteDetailLevel.");
    }

    [Fact]
    public void ToPayload_OneByOne_LeavesEveryOptionUnset() {
        var payload = DistanceMatrixFactory.ToPayload(_request);

        payload.Origins.Should().ContainSingle().Which.RouteModifiers.Should().BeNull();
        payload.Destinations.Should().ContainSingle();
        payload.Should().BeEquivalentTo(new {
            TravelMode = (string?)null, RoutingPreference = (string?)null, DepartureTime = (string?)null,
            ArrivalTime = (string?)null, Units = (string?)null, TrafficModel = (string?)null
        });
        payload.TransitPreferences.Should().BeNull();
    }
}
