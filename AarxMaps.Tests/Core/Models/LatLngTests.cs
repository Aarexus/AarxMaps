using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using FluentAssertions;

namespace AarxMaps.Tests.Core.Models;

[Trait("Package", "Core")]
[Trait("Module", "Models")]
[Trait("Type", "LatLng")]
public class LatLngTests {
    [Fact]
    public void Constructor_UnequalCoordinates_KeepsLatitudeAndLongitudeInOrder() {
        var latLng = new LatLng(4.6097, -74.0817);

        latLng.Latitude.Should().Be(4.6097);
        latLng.Longitude.Should().Be(-74.0817);
    }

    [Theory]
    [InlineData(-90.0, -180.0)]
    [InlineData(90.0, 180.0)]
    [InlineData(0.0, 0.0)]
    public void Constructor_BoundaryCoordinates_AreAccepted(double latitude, double longitude) {
        var act = () => new LatLng(latitude, longitude);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_InvalidLatitude_ThrowsMapsInvalidRequestException(double latitude) {
        var act = () => new LatLng(latitude, 0);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Latitude*");
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_InvalidLongitude_ThrowsMapsInvalidRequestException(double longitude) {
        var act = () => new LatLng(0, longitude);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Longitude*");
    }
}
