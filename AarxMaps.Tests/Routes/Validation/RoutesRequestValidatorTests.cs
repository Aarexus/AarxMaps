using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Internal.Validation;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Validation;

[Trait("Package", "Routes")]
[Trait("Module", "Validation")]
[Trait("Type", "RoutesRequestValidator")]
public class RoutesRequestValidatorTests {
    private static readonly DateTimeOffset _time = new(2026, 10, 24, 15, 0, 0, TimeSpan.Zero);

    private static Action Options(
        TravelMode? mode = null, RoutingPreference? preference = null, TrafficModel? model = null,
        TransitPreferences? transit = null, DateTimeOffset? departure = null, DateTimeOffset? arrival = null
    ) => () => RoutesRequestValidator.ValidateModeOptions(mode, preference, model, transit, departure, arrival);

    // ===================== travel mode =====================
    [Fact]
    public void ValidateTravelMode_Unknown_ThrowsMapsInvalidRequestException() {
        var act = () => RoutesRequestValidator.ValidateTravelMode(TravelMode.Unknown);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("TravelMode.Unknown*");
    }

    [Fact]
    public void ValidateTravelMode_KnownValue_IsReturned()
        => RoutesRequestValidator.ValidateTravelMode(TravelMode.Walk).Should().Be(TravelMode.Walk);

    // ===================== times =====================
    [Fact]
    public void ValidateModeOptions_NothingSet_Passes() => Options().Should().NotThrow();

    [Fact]
    public void ValidateModeOptions_DepartureAndArrival_Throws()
        => Options(TravelMode.Transit, departure: _time, arrival: _time)
            .Should().Throw<MapsInvalidRequestException>().WithMessage("Only one of DepartureTime or ArrivalTime can be set.");

    [Fact]
    public void ValidateModeOptions_DepartureWithAnyMode_Passes() {
        foreach (var mode in new TravelMode?[] { null, TravelMode.Drive, TravelMode.Walk, TravelMode.Transit })
            Options(mode, departure: _time).Should().NotThrow($"departure time is valid for {mode}");
    }

    [Fact]
    public void ValidateModeOptions_ArrivalWithTransit_Passes() => Options(TravelMode.Transit, arrival: _time).Should().NotThrow();

    [Theory]
    [InlineData(null)]
    [InlineData(TravelMode.Drive)]
    [InlineData(TravelMode.Bicycle)]
    [InlineData(TravelMode.Walk)]
    [InlineData(TravelMode.TwoWheeler)]
    public void ValidateModeOptions_ArrivalWithoutTransit_Throws(TravelMode? mode)
        => Options(mode, arrival: _time)
            .Should().Throw<MapsInvalidRequestException>().WithMessage("ArrivalTime can only be used when TravelMode is Transit.");

    // ===================== routing preference =====================
    [Theory]
    [InlineData(null)]
    [InlineData(TravelMode.Drive)]
    [InlineData(TravelMode.TwoWheeler)]
    public void ValidateModeOptions_RoutingPreferenceWithDriveLikeMode_Passes(TravelMode? mode)
        => Options(mode, RoutingPreference.TrafficAware).Should().NotThrow();

    [Theory]
    [InlineData(TravelMode.Bicycle)]
    [InlineData(TravelMode.Walk)]
    [InlineData(TravelMode.Transit)]
    public void ValidateModeOptions_RoutingPreferenceWithOtherMode_Throws(TravelMode mode)
        => Options(mode, RoutingPreference.TrafficUnaware)
            .Should().Throw<MapsInvalidRequestException>().WithMessage("RoutingPreference can only be used when TravelMode is Drive or TwoWheeler.");

    // ===================== traffic model =====================
    [Theory]
    [InlineData(null)]
    [InlineData(TravelMode.Drive)]
    public void ValidateModeOptions_TrafficModelWithDriveAndOptimal_Passes(TravelMode? mode)
        => Options(mode, RoutingPreference.TrafficAwareOptimal, TrafficModel.Pessimistic).Should().NotThrow();

    [Theory]
    [InlineData(TravelMode.Drive, null)]
    [InlineData(TravelMode.Drive, RoutingPreference.TrafficAware)]
    [InlineData(TravelMode.TwoWheeler, RoutingPreference.TrafficAwareOptimal)]
    public void ValidateModeOptions_TrafficModelOtherwise_Throws(TravelMode mode, RoutingPreference? preference)
        => Options(mode, preference, TrafficModel.BestGuess)
            .Should().Throw<MapsInvalidRequestException>().WithMessage("TrafficModel can only be used when*Drive*TrafficAwareOptimal.");

    // ===================== transit preferences =====================
    [Fact]
    public void ValidateModeOptions_TransitPreferencesWithTransit_Passes()
        => Options(TravelMode.Transit, transit: new TransitPreferences()).Should().NotThrow();

    [Theory]
    [InlineData(null)]
    [InlineData(TravelMode.Drive)]
    [InlineData(TravelMode.Walk)]
    public void ValidateModeOptions_TransitPreferencesWithoutTransit_Throws(TravelMode? mode)
        => Options(mode, transit: new TransitPreferences())
            .Should().Throw<MapsInvalidRequestException>().WithMessage("TransitPreferences can only be used when TravelMode is Transit.");

    // ===================== waypoints =====================
    public static TheoryData<string> DriveOnlyWaypoints => new() { "heading", "stopover", "sideOfRoad" };

    private static Waypoint DriveOnly(string kind) => kind switch {
        "heading" => RoutesSamples.At(RoutesSamples.Bogota, heading: 90),
        "stopover" => RoutesSamples.Stopover(RoutesSamples.Bogota),
        "sideOfRoad" => RoutesSamples.SideOfRoad(RoutesSamples.Bogota),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    [Theory]
    [MemberData(nameof(DriveOnlyWaypoints))]
    public void ValidateWaypointForMode_DriveOnlyOptionWithDriveLikeMode_Passes(string kind) {
        foreach (var mode in new TravelMode?[] { null, TravelMode.Drive, TravelMode.TwoWheeler })
            FluentActions.Invoking(() => RoutesRequestValidator.ValidateWaypointForMode(DriveOnly(kind), mode, "Origin"))
                .Should().NotThrow($"{kind} is valid for {mode}");
    }

    [Theory]
    [MemberData(nameof(DriveOnlyWaypoints))]
    public void ValidateWaypointForMode_DriveOnlyOptionWithWalk_ThrowsNamingTheWaypoint(string kind) {
        var act = () => RoutesRequestValidator.ValidateWaypointForMode(DriveOnly(kind), TravelMode.Walk, "Destination");
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Destination: Heading, VehicleStopover and SideOfRoad*Drive or TwoWheeler.");
    }

    [Fact]
    public void ValidateWaypointForMode_PlainWaypointWithTransit_Passes() {
        var act = () => RoutesRequestValidator.ValidateWaypointForMode(RoutesSamples.Place("ChIJ-place"), TravelMode.Transit, "Origin");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTerminalWaypoint_Via_ThrowsNamingTheWaypoint() {
        var act = () => RoutesRequestValidator.ValidateTerminalWaypoint(RoutesSamples.Via(RoutesSamples.Bogota), null, "Origin");
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origin cannot be a via waypoint.*");
    }

    [Fact]
    public void ValidateTerminalWaypoint_DriveOnlyOptionWithWalk_AlsoChecksTheMode() {
        var act = () => RoutesRequestValidator.ValidateTerminalWaypoint(DriveOnly("heading"), TravelMode.Walk, "Origin");
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origin: Heading*");
    }
}
