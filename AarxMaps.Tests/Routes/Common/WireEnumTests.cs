using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Common;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Routes.Services.DistanceMatrix;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Common;

/// <summary>
/// Every member against Google's literal values, so a renamed member (which would silently change
/// the wire value) fails here.
/// </summary>
[Trait("Package", "Routes")]
[Trait("Module", "Common")]
[Trait("Type", "WireEnum")]
public class WireEnumTests {
    private static void ShouldMapExactly<TEnum>(params string[] googleValues) where TEnum : struct, Enum {
        var members = Enum.GetValues<TEnum>().Where(v => v.ToString() != "Unknown").ToArray();

        members.Select(WireEnum<TEnum>.ToWire).Should().Equal(googleValues);
        googleValues.Select(WireEnum<TEnum>.FromWire).Should().Equal(members);
    }

    // ===================== request enums =====================
    [Fact]
    public void TravelMode_MatchesGoogle()
        => ShouldMapExactly<TravelMode>("DRIVE", "BICYCLE", "WALK", "TWO_WHEELER", "TRANSIT");

    [Fact]
    public void RoutingPreference_MatchesGoogle()
        => ShouldMapExactly<RoutingPreference>("TRAFFIC_UNAWARE", "TRAFFIC_AWARE", "TRAFFIC_AWARE_OPTIMAL");

    [Fact]
    public void Units_MatchesGoogle() => ShouldMapExactly<Units>("METRIC", "IMPERIAL");

    [Fact]
    public void TrafficModel_MatchesGoogle() => ShouldMapExactly<TrafficModel>("BEST_GUESS", "PESSIMISTIC", "OPTIMISTIC");

    [Fact]
    public void TransitTravelMode_MatchesGoogle()
        => ShouldMapExactly<TransitTravelMode>("BUS", "SUBWAY", "TRAIN", "LIGHT_RAIL", "RAIL");

    [Fact]
    public void TransitRoutingPreference_MatchesGoogle()
        => ShouldMapExactly<TransitRoutingPreference>("LESS_WALKING", "FEWER_TRANSFERS");

    [Fact]
    public void VehicleEmissionType_MatchesGoogle()
        => ShouldMapExactly<VehicleEmissionType>("GASOLINE", "ELECTRIC", "HYBRID", "DIESEL");

    [Fact]
    public void PolylineQuality_MatchesGoogle() => ShouldMapExactly<PolylineQuality>("HIGH_QUALITY", "OVERVIEW");

    // ===================== response enums =====================
    [Fact]
    public void RouteLabel_MatchesGoogle()
        => ShouldMapExactly<RouteLabel>("DEFAULT_ROUTE", "DEFAULT_ROUTE_ALTERNATE", "FUEL_EFFICIENT", "SHORTER_DISTANCE");

    [Fact]
    public void Maneuver_MatchesGoogle()
        => ShouldMapExactly<Maneuver>(
            "TURN_SLIGHT_LEFT", "TURN_SHARP_LEFT", "UTURN_LEFT", "TURN_LEFT", "TURN_SLIGHT_RIGHT", "TURN_SHARP_RIGHT",
            "UTURN_RIGHT", "TURN_RIGHT", "STRAIGHT", "RAMP_LEFT", "RAMP_RIGHT", "MERGE", "FORK_LEFT", "FORK_RIGHT",
            "FERRY", "FERRY_TRAIN", "ROUNDABOUT_LEFT", "ROUNDABOUT_RIGHT", "DEPART", "NAME_CHANGE", "DESTINATION");

    [Fact]
    public void TransitVehicleType_MatchesGoogle()
        => ShouldMapExactly<TransitVehicleType>(
            "BUS", "CABLE_CAR", "COMMUTER_TRAIN", "FERRY", "FUNICULAR", "GONDOLA_LIFT", "HEAVY_RAIL", "HIGH_SPEED_TRAIN",
            "INTERCITY_BUS", "LONG_DISTANCE_TRAIN", "METRO_RAIL", "MONORAIL", "OTHER", "RAIL", "SHARE_TAXI", "SUBWAY",
            "TRAM", "TROLLEYBUS");

    [Fact]
    public void FallbackRoutingMode_MatchesGoogle()
        => ShouldMapExactly<FallbackRoutingMode>("FALLBACK_TRAFFIC_UNAWARE", "FALLBACK_TRAFFIC_AWARE");

    [Fact]
    public void FallbackReason_MatchesGoogle() => ShouldMapExactly<FallbackReason>("SERVER_ERROR", "LATENCY_EXCEEDED");

    [Fact]
    public void RouteMatrixElementCondition_MatchesGoogle()
        => ShouldMapExactly<RouteMatrixElementCondition>("ROUTE_EXISTS", "ROUTE_NOT_FOUND");

    // ===================== edge cases =====================
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("MANEUVER_UNSPECIFIED")]
    [InlineData("SOME_FUTURE_VALUE")]
    [InlineData("turn_left")]
    public void FromWire_UnrecognizedValue_IsUnknown(string? value)
        => WireEnum<Maneuver>.FromWire(value).Should().Be(Maneuver.Unknown);

    [Fact]
    public void ToWire_NullValue_IsNull() => WireEnum<Units>.ToWire((Units?)null).Should().BeNull();

    [Fact]
    public void ToWire_UndefinedValue_ThrowsMapsInvalidRequestException() {
        var act = () => WireEnum<Units>.ToWire((Units)42);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("42 is not a valid Units.");
    }
}
