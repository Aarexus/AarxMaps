using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Routes.Services.Directions;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Services.Directions;

[Trait("Package", "Routes")]
[Trait("Module", "Services")]
[Trait("Feature", "Directions")]
[Trait("Type", "RouteRequestBuilder")]
public class RouteRequestBuilderTests {
    private static readonly DateTimeOffset _time = new(2026, 10, 24, 15, 0, 0, TimeSpan.Zero);

    private static RouteRequestBuilder WithEnds()
        => new RouteRequestBuilder().From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen));

    private static IEnumerable<Waypoint> Intermediates(int count)
        => Enumerable.Range(0, count).Select(_ => RoutesSamples.At(RoutesSamples.Chapinero));

    // ===================== required =====================
    [Fact]
    public void Build_WithoutOrigin_ThrowsMapsInvalidRequestException() {
        var act = () => new RouteRequestBuilder().To(RoutesSamples.At(RoutesSamples.Usaquen)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origin is required. Call From() before Build().");
    }

    [Fact]
    public void Build_WithoutDestination_ThrowsMapsInvalidRequestException() {
        var act = () => new RouteRequestBuilder().From(RoutesSamples.At(RoutesSamples.Bogota)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Destination is required. Call To() before Build().");
    }

    [Fact]
    public void Build_EndsOnly_LeavesEveryOptionAtGooglesDefault() {
        var request = WithEnds().Build();

        request.Intermediates.Should().BeEmpty();
        request.Should().BeEquivalentTo(new {
            TravelMode = (TravelMode?)null, RoutingPreference = (RoutingPreference?)null, PolylineQuality = (PolylineQuality?)null,
            DepartureTime = (DateTimeOffset?)null, ArrivalTime = (DateTimeOffset?)null,
            ComputeAlternativeRoutes = false, OptimizeWaypointOrder = false,
            RouteModifiers = (RouteModifiers?)null, Units = (Units?)null, TrafficModel = (TrafficModel?)null,
            TransitPreferences = (TransitPreferences?)null
        });
    }

    [Fact]
    public void Build_EveryOption_IsCopiedToTheRequest() {
        var origin = RoutesSamples.At(RoutesSamples.Bogota);
        var destination = RoutesSamples.Place("ChIJ-usaquen");
        var intermediate = RoutesSamples.Address("Chapinero, Bogota");
        var modifiers = new RouteModifiers(avoidTolls: true);

        var request = new RouteRequestBuilder()
            .From(origin).To(destination).AddIntermediate(intermediate)
            .WithTravelMode(TravelMode.Drive)
            .WithRoutingPreference(RoutingPreference.TrafficAwareOptimal)
            .WithTrafficModel(TrafficModel.Optimistic)
            .WithPolylineQuality(PolylineQuality.HighQuality)
            .WithDepartureTime(_time)
            .WithAlternativeRoutes()
            .WithOptimizedWaypointOrder()
            .WithRouteModifiers(modifiers)
            .WithUnits(Units.Imperial)
            .Build();

        request.Origin.Should().BeSameAs(origin);
        request.Destination.Should().BeSameAs(destination);
        request.Intermediates.Should().ContainSingle().Which.Should().BeSameAs(intermediate);
        request.RouteModifiers.Should().BeSameAs(modifiers);
        request.Should().BeEquivalentTo(new {
            TravelMode = TravelMode.Drive, RoutingPreference = RoutingPreference.TrafficAwareOptimal,
            TrafficModel = TrafficModel.Optimistic, PolylineQuality = PolylineQuality.HighQuality,
            DepartureTime = _time, ComputeAlternativeRoutes = true, OptimizeWaypointOrder = true, Units = Units.Imperial
        });
    }

    [Fact]
    public void Build_TransitOptions_AreCopiedToTheRequest() {
        var transit = new TransitPreferences([TransitTravelMode.Bus], TransitRoutingPreference.LessWalking);

        var request = WithEnds().WithTravelMode(TravelMode.Transit).WithArrivalTime(_time).WithTransitPreferences(transit).Build();

        request.ArrivalTime.Should().Be(_time);
        request.TransitPreferences.Should().BeSameAs(transit);
    }

    // ===================== intermediates =====================
    [Fact]
    public void WithIntermediates_TwentyFive_IsAccepted()
        => WithEnds().WithIntermediates(Intermediates(25)).Build().Intermediates.Should().HaveCount(25);

    [Fact]
    public void WithIntermediates_TwentySix_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().WithIntermediates(Intermediates(26));
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At most 25 Intermediates can be specified.");
    }

    [Fact]
    public void AddIntermediate_TwentySixth_ThrowsMapsInvalidRequestException() {
        var builder = WithEnds().WithIntermediates(Intermediates(25));

        var act = () => builder.AddIntermediate(RoutesSamples.At(RoutesSamples.Chapinero));

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("At most 25 Intermediates can be specified.");
    }

    [Fact]
    public void WithIntermediates_Null_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().WithIntermediates(null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Intermediates cannot be null.");
    }

    [Fact]
    public void WithIntermediates_NullValue_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().WithIntermediates([RoutesSamples.At(RoutesSamples.Chapinero), null!]);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Intermediates cannot contain null values.");
    }

    [Fact]
    public void AddIntermediate_Null_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().AddIntermediate(null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Intermediate cannot be null.");
    }

    [Fact]
    public void WithIntermediates_ThenAddIntermediate_AppendsInOrder() {
        var first = RoutesSamples.At(RoutesSamples.Chapinero);
        var second = RoutesSamples.Place("ChIJ-second");

        var request = WithEnds().WithIntermediates([first]).AddIntermediate(second).Build();

        request.Intermediates.Should().Equal(first, second);
    }

    [Fact]
    public void WithIntermediates_CallerListChangedAfterwards_DoesNotChangeTheRequest() {
        var list = new List<Waypoint> { RoutesSamples.At(RoutesSamples.Chapinero) };
        var builder = WithEnds().WithIntermediates(list);

        list.Add(RoutesSamples.At(RoutesSamples.Chapinero));

        builder.Build().Intermediates.Should().HaveCount(1);
    }

    [Fact]
    public void Build_CalledTwice_RequestsDoNotShareTheIntermediatesList() {
        var builder = WithEnds().AddIntermediate(RoutesSamples.At(RoutesSamples.Chapinero));
        var first = builder.Build();

        builder.AddIntermediate(RoutesSamples.At(RoutesSamples.Chapinero));

        first.Intermediates.Should().HaveCount(1);
    }

    // ===================== via & optimization =====================
    [Fact]
    public void Build_ViaIntermediate_IsAccepted()
        => WithEnds().AddIntermediate(RoutesSamples.Via(RoutesSamples.Chapinero)).Build().Intermediates.Single().Via.Should().BeTrue();

    [Fact]
    public void Build_ViaOrigin_ThrowsMapsInvalidRequestException() {
        var act = () => new RouteRequestBuilder().From(RoutesSamples.Via(RoutesSamples.Bogota)).To(RoutesSamples.At(RoutesSamples.Usaquen)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Origin cannot be a via waypoint.*");
    }

    [Fact]
    public void Build_ViaDestination_ThrowsMapsInvalidRequestException() {
        var act = () => new RouteRequestBuilder().From(RoutesSamples.At(RoutesSamples.Bogota)).To(RoutesSamples.Via(RoutesSamples.Usaquen)).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Destination cannot be a via waypoint.*");
    }

    [Fact]
    public void Build_ViaIntermediateWithOptimization_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().AddIntermediate(RoutesSamples.Via(RoutesSamples.Chapinero)).WithOptimizedWaypointOrder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Via intermediates cannot be used with OptimizeWaypointOrder.");
    }

    [Fact]
    public void Build_OptimizationWithoutIntermediates_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().WithOptimizedWaypointOrder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("OptimizeWaypointOrder requires at least one intermediate.");
    }

    // ===================== shared rules wiring =====================
    // The rules live in RoutesRequestValidatorTests; these only prove the builder applies them
    [Fact]
    public void WithTravelMode_Unknown_ThrowsMapsInvalidRequestException() {
        var act = () => WithEnds().WithTravelMode(TravelMode.Unknown);
        act.Should().Throw<MapsInvalidRequestException>();
    }

    [Fact]
    public void Build_ModeOptionsRule_IsApplied() {
        var act = () => WithEnds().WithArrivalTime(_time).Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("ArrivalTime can only be used when TravelMode is Transit.");
    }

    [Fact]
    public void Build_HeadingWithUnsetTravelMode_IsAcceptedBecauseGoogleDefaultsToDrive() {
        var act = () => new RouteRequestBuilder()
            .From(RoutesSamples.At(RoutesSamples.Bogota, heading: 90)).To(RoutesSamples.At(RoutesSamples.Usaquen)).Build();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("origin", "Origin")]
    [InlineData("destination", "Destination")]
    [InlineData("intermediate", "Intermediate")]
    public void Build_WaypointModeRule_IsAppliedToEveryWaypoint(string position, string name) {
        var drive = RoutesSamples.At(RoutesSamples.Bogota, heading: 90);
        var plain = RoutesSamples.At(RoutesSamples.Usaquen);
        var builder = new RouteRequestBuilder()
            .From(position == "origin" ? drive : plain)
            .To(position == "destination" ? drive : plain)
            .WithTravelMode(TravelMode.Walk);
        if (position == "intermediate") builder.AddIntermediate(drive);

        var act = () => builder.Build();

        act.Should().Throw<MapsInvalidRequestException>().WithMessage($"{name}: Heading*");
    }
}
