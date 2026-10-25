using AarxMaps.Core.Exceptions;
using AarxMaps.Routes.Common.Models;
using AarxMaps.Tests.Routes.Helpers;
using FluentAssertions;

namespace AarxMaps.Tests.Routes.Common;

[Trait("Package", "Routes")]
[Trait("Module", "Common")]
[Trait("Type", "WaypointBuilder")]
public class WaypointBuilderTests {
    // ===================== sources =====================
    [Fact]
    public void Build_NoSource_ThrowsMapsInvalidRequestExceptionNamingTheThreeMethods() {
        var act = () => new WaypointBuilder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*FromPlaceId()*FromLocation()*FromAddress()*");
    }

    [Fact]
    public void FromPlaceId_Trimmed_IsTheOnlySource() {
        var waypoint = new WaypointBuilder().FromPlaceId("  ChIJ-place  ").Build();

        waypoint.PlaceId.Should().Be("ChIJ-place");
        waypoint.Location.Should().BeNull();
        waypoint.Address.Should().BeNull();
    }

    [Fact]
    public void FromAddress_Trimmed_IsTheOnlySource() {
        var waypoint = new WaypointBuilder().FromAddress(" Cra. 7 #40-62, Bogota ").Build();

        waypoint.Address.Should().Be("Cra. 7 #40-62, Bogota");
        waypoint.PlaceId.Should().BeNull();
        waypoint.Location.Should().BeNull();
    }

    [Fact]
    public void FromLocation_WithHeading_KeepsTheCoordinatesAndHeading() {
        var waypoint = new WaypointBuilder().FromLocation(RoutesSamples.Bogota, 90).Build();

        waypoint.Location!.LatLng.Should().BeSameAs(RoutesSamples.Bogota);
        waypoint.Location.Heading.Should().Be(90);
        waypoint.PlaceId.Should().BeNull();
        waypoint.Address.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromPlaceId_Empty_ThrowsMapsInvalidRequestException(string? placeId) {
        var act = () => new WaypointBuilder().FromPlaceId(placeId!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Empty PlaceId is not allowed.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromAddress_Empty_ThrowsMapsInvalidRequestException(string? address) {
        var act = () => new WaypointBuilder().FromAddress(address!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Empty Address is not allowed.");
    }

    [Fact]
    public void FromLocation_NullLocation_ThrowsMapsInvalidRequestException() {
        var act = () => new WaypointBuilder().FromLocation(null!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Location cannot be null.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(360)]
    public void FromLocation_HeadingAtTheBounds_IsAccepted(int heading)
        => new WaypointBuilder().FromLocation(RoutesSamples.Bogota, heading).Build().Location!.Heading.Should().Be(heading);

    [Theory]
    [InlineData(-1)]
    [InlineData(361)]
    public void FromLocation_HeadingOutOfRange_ThrowsMapsInvalidRequestException(int heading) {
        var act = () => new WaypointBuilder().FromLocation(RoutesSamples.Bogota, heading);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Heading must be between 0 and 360.");
    }

    [Fact]
    public void Build_TwoSources_ThrowsMapsInvalidRequestException() {
        var act = () => new WaypointBuilder().FromPlaceId("ChIJ-place").FromAddress("Bogota").Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Only one of PlaceId, Location or Address can be set.");
    }

    [Fact]
    public void FromPlaceId_CalledTwice_LastValueWins()
        => new WaypointBuilder().FromPlaceId("first").FromPlaceId("second").Build().PlaceId.Should().Be("second");

    // ===================== flags =====================
    [Fact]
    public void Build_NoFlags_AllFlagsAreFalse() {
        var waypoint = RoutesSamples.At(RoutesSamples.Bogota);

        waypoint.Via.Should().BeFalse();
        waypoint.VehicleStopover.Should().BeFalse();
        waypoint.SideOfRoad.Should().BeFalse();
    }

    [Fact]
    public void Flags_AllSet_AreKept() {
        var waypoint = new WaypointBuilder().FromLocation(RoutesSamples.Bogota).AsVia().AsVehicleStopover().WithSideOfRoad().Build();

        waypoint.Via.Should().BeTrue();
        waypoint.VehicleStopover.Should().BeTrue();
        waypoint.SideOfRoad.Should().BeTrue();
    }

    [Theory]
    [InlineData("placeId")]
    [InlineData("address")]
    public void AsVehicleStopover_WithoutLocation_ThrowsMapsInvalidRequestException(string source) {
        var builder = new WaypointBuilder().AsVehicleStopover();
        _ = source == "placeId" ? builder.FromPlaceId("ChIJ-place") : builder.FromAddress("Bogota");

        var act = () => builder.Build();

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("VehicleStopover can only be used on a waypoint defined by Location.");
    }
}
