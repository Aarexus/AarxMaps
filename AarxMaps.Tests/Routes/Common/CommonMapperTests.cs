using AarxMaps.Core.Internal.Json;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Common;
using AarxMaps.Routes.Internal.Common.DTOs;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Common;

[Trait("Package", "Routes")]
[Trait("Module", "Common")]
[Trait("Type", "CommonMapper")]
public class CommonMapperTests {
    // Through the real serializer, so the DTOs are also checked against Google's camelCase shape
    private static T Parse<T>(string json) => MapsJsonSerializer.Deserialize<T>(json)!;

    // ===================== durations =====================
    [Theory]
    [InlineData("0s", 0L)]
    [InlineData("300s", 3_000_000_000L)]
    [InlineData("160.5s", 1_605_000_000L)]
    [InlineData("1.000000001s", 10_000_000L)] // nanoseconds below a tick are dropped
    public void ToDuration_GoogleDuration_IsParsed(string duration, long expectedTicks)
        => CommonMapper.ToDuration(duration).Should().Be(TimeSpan.FromTicks(expectedTicks));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("300")]
    [InlineData("abcs")]
    public void ToDuration_MissingOrMalformed_IsZero(string? duration)
        => CommonMapper.ToDuration(duration).Should().Be(TimeSpan.Zero);

    // ===================== timestamps =====================
    [Fact]
    public void ToTimestamp_OffsetWithFraction_IsSentInUtcWithWholeSeconds()
        => CommonMapper.ToTimestamp(new DateTimeOffset(2026, 10, 24, 10, 30, 15, 987, TimeSpan.FromHours(-5)))
            .Should().Be("2026-10-24T15:30:15Z");

    [Fact]
    public void ToTimestamp_NullValue_IsNull() => CommonMapper.ToTimestamp((DateTimeOffset?)null).Should().BeNull();

    [Theory]
    [InlineData("2026-10-24T15:00:00Z")]
    [InlineData("2026-10-24T15:00:00.123456789Z")]
    [InlineData("2026-10-24T10:00:00-05:00")]
    public void ToTimestamp_GoogleTimestamp_IsParsedToUtc(string timestamp) {
        var parsed = CommonMapper.ToTimestamp(timestamp);

        parsed.Should().NotBeNull();
        parsed!.Value.Offset.Should().Be(TimeSpan.Zero);
        parsed.Value.UtcDateTime.Should().BeCloseTo(new DateTime(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a date")]
    public void ToTimestamp_MissingOrMalformed_IsNull(string? timestamp)
        => CommonMapper.ToTimestamp(timestamp).Should().BeNull();

    // ===================== request DTOs =====================
    [Fact]
    public void ToWaypointDto_Location_CopiesCoordinatesHeadingAndFlags() {
        var waypoint = new WaypointBuilder().FromLocation(RoutesSamples.Bogota, 45).AsVehicleStopover().WithSideOfRoad().Build();

        var dto = CommonMapper.ToWaypointDto(waypoint);

        dto.Location!.LatLng!.Latitude.Should().Be(4.6097);
        dto.Location.LatLng.Longitude.Should().Be(-74.0817);
        dto.Location.Heading.Should().Be(45);
        dto.VehicleStopover.Should().BeTrue();
        dto.SideOfRoad.Should().BeTrue();
        dto.Via.Should().BeFalse();
        dto.PlaceId.Should().BeNull();
        dto.Address.Should().BeNull();
    }

    [Fact]
    public void ToRouteModifiersDto_EmissionType_IsWrappedInVehicleInfo() {
        var dto = CommonMapper.ToRouteModifiersDto(new RouteModifiers(avoidTolls: true, emissionType: VehicleEmissionType.Hybrid))!;

        dto.AvoidTolls.Should().BeTrue();
        dto.VehicleInfo!.EmissionType.Should().Be("HYBRID");
    }

    [Fact]
    public void ToRouteModifiersDto_NoEmissionType_HasNoVehicleInfo()
        => CommonMapper.ToRouteModifiersDto(new RouteModifiers(avoidFerries: true))!.VehicleInfo.Should().BeNull();

    [Fact]
    public void ToTransitPreferencesDto_NoModes_SendsNoModeList() {
        var dto = CommonMapper.ToTransitPreferencesDto(new TransitPreferences(routingPreference: TransitRoutingPreference.FewerTransfers))!;

        dto.AllowedTravelModes.Should().BeNull("an empty list would be noise; Google allows every mode by default");
        dto.RoutingPreference.Should().Be("FEWER_TRANSFERS");
    }

    // ===================== response DTOs =====================
    [Fact]
    public void ToTravelAdvisory_TransitFare_CombinesUnitsAndNanos() {
        var advisory = CommonMapper.ToTravelAdvisory(Parse<RouteTravelAdvisoryDto>(
            """{ "transitFare": { "currencyCode": "COP", "units": "3200", "nanos": 500000000 } }"""))!;

        advisory.TransitFare!.CurrencyCode.Should().Be("COP");
        advisory.TransitFare.Amount.Should().Be(3200.5m);
        advisory.RouteRestrictionsPartiallyIgnored.Should().BeFalse("Google omits false values");
    }

    [Fact]
    public void ToTravelAdvisory_NullDto_IsNull() => CommonMapper.ToTravelAdvisory(null).Should().BeNull();

    [Fact]
    public void ToFallbackInfo_KnownAndUnknownValues_AreMapped() {
        var fallback = CommonMapper.ToFallbackInfo(Parse<FallbackInfoDto>(
            """{ "routingMode": "FALLBACK_TRAFFIC_AWARE", "reason": "SOME_FUTURE_REASON" }"""))!;

        fallback.RoutingMode.Should().Be(FallbackRoutingMode.FallbackTrafficAware);
        fallback.Reason.Should().Be(FallbackReason.Unknown);
    }

    [Fact]
    public void ToLocation_NoLatLng_IsNull() => CommonMapper.ToLocation(Parse<LocationDto>("""{ "heading": 10 }""")).Should().BeNull();

    [Fact]
    public void ToLocation_OmittedZeroCoordinate_IsZero() {
        var location = CommonMapper.ToLocation(Parse<LocationDto>("""{ "latLng": { "longitude": -74.08 } }"""))!;

        location.LatLng.Latitude.Should().Be(0);
        location.LatLng.Longitude.Should().Be(-74.08);
        location.Heading.Should().BeNull();
    }

    [Theory]
    [InlineData("https://www.transmilenio.gov.co/", true)]
    [InlineData("http://example.com", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("/relative/path", false)]
    [InlineData(null, false)]
    public void ToUri_OnlyHttpAndHttps_AreKept(string? value, bool kept)
        => (CommonMapper.ToUri(value) is not null).Should().Be(kept);
}
