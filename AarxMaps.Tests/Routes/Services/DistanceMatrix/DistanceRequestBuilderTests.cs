using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.DistanceMatrix;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.DistanceMatrix;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "DistanceMatrix")]
[Trait("Type", "DistanceRequestBuilder")]
public class DistanceRequestBuilderTests {
    private static readonly DateTimeOffset _time = new(2026, 10, 24, 15, 0, 0, TimeSpan.Zero);

    private static IEnumerable<Waypoint> Points(int count) => Enumerable.Range(0, count).Select(_ => RoutesSamples.At(RoutesSamples.Bogota));
    private static IEnumerable<Waypoint> Places(int count) => Enumerable.Range(0, count).Select(i => RoutesSamples.Place($"ChIJ-{i}"));

    private static DistanceRequestBuilder OneByOne()
        => new DistanceRequestBuilder().AddOrigin(RoutesSamples.At(RoutesSamples.Bogota)).AddDestination(RoutesSamples.At(RoutesSamples.Usaquen));

    // ===================== required =====================
    [Fact]
    public void Build_WithoutOrigins_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().AddDestination(RoutesSamples.At(RoutesSamples.Usaquen)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At least one origin is required.*");
    }

    [Fact]
    public void Build_WithoutDestinations_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().AddOrigin(RoutesSamples.At(RoutesSamples.Bogota)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At least one destination is required.*");
    }

    [Fact]
    public void Build_OneByOne_LeavesEveryOptionAtGooglesDefault() {
        var request = OneByOne().Build();

        request.Origins.Should().ContainSingle().Which.RouteModifiers.Should().BeNull();
        request.Destinations.Should().ContainSingle();
        request.Should().BeEquivalentTo(new {
            TravelMode = (TravelMode?)null, RoutingPreference = (RoutingPreference?)null,
            DepartureTime = (DateTimeOffset?)null, ArrivalTime = (DateTimeOffset?)null,
            Units = (Units?)null, TrafficModel = (TrafficModel?)null, TransitPreferences = (TransitPreferences?)null
        });
    }

    [Fact]
    public void Build_EveryOption_IsCopiedToTheRequest() {
        var request = OneByOne()
            .WithTravelMode(TravelMode.Drive)
            .WithRoutingPreference(RoutingPreference.TrafficAwareOptimal)
            .WithTrafficModel(TrafficModel.BestGuess)
            .WithDepartureTime(_time)
            .WithUnits(Units.Imperial)
            .Build();

        request.Should().BeEquivalentTo(new {
            TravelMode = TravelMode.Drive, RoutingPreference = RoutingPreference.TrafficAwareOptimal,
            TrafficModel = TrafficModel.BestGuess, DepartureTime = _time, Units = Units.Imperial
        });
    }

    // ===================== origins & destinations =====================
    [Fact]
    public void WithOrigins_SharedModifiers_AreAppliedToEveryOriginInOrder() {
        var first = RoutesSamples.At(RoutesSamples.Bogota);
        var second = RoutesSamples.Place("ChIJ-second");
        var modifiers = new RouteModifiers(avoidTolls: true);

        var request = new DistanceRequestBuilder().WithOrigins([first, second], modifiers)
            .AddDestination(RoutesSamples.At(RoutesSamples.Usaquen)).Build();

        request.Origins.Select(o => o.Waypoint).Should().Equal(first, second);
        request.Origins.Should().AllSatisfy(o => o.RouteModifiers.Should().BeSameAs(modifiers));
    }

    [Fact]
    public void AddOrigin_PerOriginModifiers_AreKeptPerRow() {
        var modifiers = new RouteModifiers(avoidHighways: true);

        var request = new DistanceRequestBuilder()
            .AddOrigin(RoutesSamples.At(RoutesSamples.Bogota), modifiers)
            .AddOrigin(RoutesSamples.At(RoutesSamples.Chapinero))
            .AddDestination(RoutesSamples.At(RoutesSamples.Usaquen)).Build();

        request.Origins.Select(o => o.RouteModifiers).Should().Equal(modifiers, null);
    }

    [Fact]
    public void WithDestinations_ThenAddDestination_AppendsInOrder() {
        var first = RoutesSamples.At(RoutesSamples.Usaquen);
        var second = RoutesSamples.Address("Chapinero, Bogota");

        var request = new DistanceRequestBuilder().AddOrigin(RoutesSamples.At(RoutesSamples.Bogota))
            .WithDestinations([first]).AddDestination(second).Build();

        request.Destinations.Should().Equal(first, second);
    }

    [Fact]
    public void WithOrigins_Null_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().WithOrigins(null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origins cannot be null.");
    }

    [Fact]
    public void WithDestinations_NullValue_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().WithDestinations([RoutesSamples.At(RoutesSamples.Bogota), null!]);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Destinations cannot contain null values.");
    }

    [Fact]
    public void AddOrigin_Null_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().AddOrigin(null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origin cannot be null.");
    }

    [Fact]
    public void AddDestination_Null_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().AddDestination(null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Destination cannot be null.");
    }

    [Fact]
    public void Build_CalledTwice_RequestsDoNotShareTheLists() {
        var builder = OneByOne();
        var first = builder.Build();

        builder.AddOrigin(RoutesSamples.At(RoutesSamples.Chapinero)).AddDestination(RoutesSamples.At(RoutesSamples.Chapinero));

        first.Origins.Should().HaveCount(1);
        first.Destinations.Should().HaveCount(1);
    }

    // ===================== limits =====================
    [Fact]
    public void Build_625Elements_IsAccepted() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Points(25)).WithDestinations(Points(25)).Build();
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_626Elements_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Points(2)).WithDestinations(Points(313)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origins x destinations must be at most 625*");
    }

    [Fact]
    public void Build_101ElementsWithTrafficAwareOptimal_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Points(1)).WithDestinations(Points(101))
            .WithRoutingPreference(RoutingPreference.TrafficAwareOptimal).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origins x destinations must be at most 100*");
    }

    [Fact]
    public void Build_101ElementsWithTransit_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Points(1)).WithDestinations(Points(101))
            .WithTravelMode(TravelMode.Transit).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origins x destinations must be at most 100*");
    }

    [Fact]
    public void Build_100ElementsWithTransit_IsAccepted() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Points(10)).WithDestinations(Points(10))
            .WithTravelMode(TravelMode.Transit).Build();
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_101ElementsWithTrafficAware_IsAccepted() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Points(1)).WithDestinations(Points(101))
            .WithRoutingPreference(RoutingPreference.TrafficAware).Build();
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_FiftyPlaceIdOrAddressWaypoints_IsAccepted() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Places(1))
            .WithDestinations([.. Places(48), RoutesSamples.Address("Bogota"), RoutesSamples.At(RoutesSamples.Usaquen)]).Build();
        act.Should().NotThrow("coordinates do not count toward the limit");
    }

    [Fact]
    public void Build_FiftyOnePlaceIdOrAddressWaypoints_ThrowsMapsInvalidRequestException() {
        var act = () => new DistanceRequestBuilder().WithOrigins(Places(1))
            .WithDestinations([.. Places(49), RoutesSamples.Address("Bogota")]).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At most 50 origins and destinations*PlaceId or an Address.");
    }

    // ===================== shared rules wiring =====================
    // The rules live in RoutesRequestValidatorTests; these only prove the builder applies them
    [Fact]
    public void WithTravelMode_Unknown_ThrowsMapsInvalidRequestException() {
        var act = () => OneByOne().WithTravelMode(TravelMode.Unknown);
        act.Should().Throw<MapsInvalidRequestException>();
    }

    [Fact]
    public void Build_ModeOptionsRule_IsApplied() {
        var act = () => OneByOne().WithTransitPreferences(new TransitPreferences()).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("TransitPreferences can only be used when TravelMode is Transit.");
    }

    [Fact]
    public void Build_TransitOptions_AreAccepted() {
        var transit = new TransitPreferences([TransitTravelMode.Subway]);

        var request = OneByOne().WithTravelMode(TravelMode.Transit).WithArrivalTime(_time).WithTransitPreferences(transit).Build();

        request.ArrivalTime.Should().Be(_time);
        request.TransitPreferences.Should().BeSameAs(transit);
    }

    [Theory]
    [InlineData("origin", "Origin")]
    [InlineData("destination", "Destination")]
    public void Build_ViaWaypoint_ThrowsMapsInvalidRequestExceptionNamingTheSide(string side, string name) {
        var via = RoutesSamples.Via(RoutesSamples.Bogota);
        var plain = RoutesSamples.At(RoutesSamples.Usaquen);

        var act = () => new DistanceRequestBuilder()
            .AddOrigin(side == "origin" ? via : plain).AddDestination(side == "destination" ? via : plain).Build();

        act.Should().Throw<MapsInvalidRequestException>().WithMessage($"{name} cannot be a via waypoint.*");
    }

    [Fact]
    public void Build_WaypointModeRule_IsApplied() {
        var act = () => new DistanceRequestBuilder().AddOrigin(RoutesSamples.SideOfRoad(RoutesSamples.Bogota))
            .AddDestination(RoutesSamples.At(RoutesSamples.Usaquen)).WithTravelMode(TravelMode.Bicycle).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origin: Heading, VehicleStopover and SideOfRoad*");
    }
}
