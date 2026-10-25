using AarxMaps.Core.Internal.Json;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Services.DistanceMatrix;
using AarxMaps.Routes.Internal.Services.DistanceMatrix.DTOs;
using AarxMaps.Routes.Services.DistanceMatrix;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.DistanceMatrix;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "DistanceMatrix")]
[Trait("Type", "DistanceMatrixMapper")]
public class DistanceMatrixMapperTests {
    // Through the real serializer, so the DTO is also checked against Google's camelCase shape
    private static RouteMatrixElement Map(string json)
        => DistanceMatrixMapper.ToElement(MapsJsonSerializer.Deserialize<RouteMatrixElementDto>(json)!);

    [Fact]
    public void ToElement_FullElement_MapsEveryField() {
        var element = Map("""
            {
              "originIndex": 1, "destinationIndex": 2, "status": {}, "condition": "ROUTE_EXISTS",
              "distanceMeters": 2500, "duration": "400s", "staticDuration": "380s",
              "localizedValues": { "distance": { "text": "2.5 km" }, "duration": { "text": "7 mins" }, "staticDuration": { "text": "6 mins" } },
              "travelAdvisory": { "transitFare": { "currencyCode": "USD", "units": "2", "nanos": 750000000 } },
              "fallbackInfo": { "routingMode": "FALLBACK_TRAFFIC_AWARE", "reason": "SERVER_ERROR" }
            }
            """);

        element.OriginIndex.Should().Be(1);
        element.DestinationIndex.Should().Be(2);
        element.Condition.Should().Be(RouteMatrixElementCondition.RouteExists);
        element.Status.Should().BeNull("an empty status object means success");
        element.DistanceMeters.Should().Be(2500);
        element.Duration.Should().Be(TimeSpan.FromSeconds(400));
        element.StaticDuration.Should().Be(TimeSpan.FromSeconds(380));
        element.LocalizedValues.Should().BeEquivalentTo(new { Distance = "2.5 km", Duration = "7 mins", StaticDuration = "6 mins" });
        element.TravelAdvisory!.TransitFare.Should().BeEquivalentTo(new { CurrencyCode = "USD", Amount = 2.75m });
        element.FallbackInfo.Should().BeEquivalentTo(new {
            RoutingMode = FallbackRoutingMode.FallbackTrafficAware, Reason = FallbackReason.ServerError
        });
    }

    [Fact]
    public void ToElement_OmittedZeroValues_DefaultToZero() {
        // Google's sample sends zero indices, but proto3 JSON may omit any zero value
        var element = Map("""{ "status": {}, "condition": "ROUTE_EXISTS" }""");

        element.OriginIndex.Should().Be(0);
        element.DestinationIndex.Should().Be(0);
        element.DistanceMeters.Should().Be(0);
        element.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ToElement_FailedElement_ExposesTheErrorAndNoCondition() {
        var element = Map("""{ "originIndex": 1, "destinationIndex": 0, "status": { "code": 3, "message": "Invalid waypoint." } }""");

        element.Status.Should().BeEquivalentTo(new { Code = 3, Message = "Invalid waypoint." });
        element.Condition.Should().Be(RouteMatrixElementCondition.Unknown);
    }

    [Fact]
    public void ToElement_NoStatusAtAll_HasNoError()
        => Map("""{ "condition": "ROUTE_NOT_FOUND" }""").Status.Should().BeNull();

    [Fact]
    public void ToElement_RouteNotFound_HasTheConditionAndNoOptionalObjects() {
        var element = Map("""{ "originIndex": 0, "destinationIndex": 1, "status": {}, "condition": "ROUTE_NOT_FOUND" }""");

        element.Condition.Should().Be(RouteMatrixElementCondition.RouteNotFound);
        element.TravelAdvisory.Should().BeNull();
        element.FallbackInfo.Should().BeNull();
        element.LocalizedValues.Should().BeNull();
    }
}
