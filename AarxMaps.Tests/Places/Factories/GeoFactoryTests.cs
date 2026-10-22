using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Common.Models.Geometry;
using AarxMaps.Places.Internal.Factories;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Factories;

[Trait("Package", "Places")]
[Trait("Module", "Factories")]
[Trait("Type", "GeoFactory")]
public class GeoFactoryTests {
    private static readonly LatLng _bogota = new(4.6097, -74.0817);

    // Bias and restriction share the same rules, so every test runs against both
    public static TheoryData<string> Kinds => new() { "bias", "restriction" };

    private static (GeoCircle? Circle, GeoRectangle? Rectangle) Circle(string kind, LatLng center, double radius)
        => kind == "bias"
            ? Shape(GeoFactory.CreateLocationBias(center, radius))
            : Shape(GeoFactory.CreateLocationRestriction(center, radius));

    private static (GeoCircle? Circle, GeoRectangle? Rectangle) Rectangle(string kind, LatLng southWest, LatLng northEast)
        => kind == "bias"
            ? Shape(GeoFactory.CreateLocationBias(southWest, northEast))
            : Shape(GeoFactory.CreateLocationRestriction(southWest, northEast));

    private static (GeoCircle?, GeoRectangle?) Shape(LocationBias bias) => (bias.Circle, bias.Rectangle);
    private static (GeoCircle?, GeoRectangle?) Shape(LocationRestriction restriction) => (restriction.Circle, restriction.Rectangle);

    // ===================== circle =====================
    [Theory]
    [MemberData(nameof(Kinds))]
    public void Circle_ValidInput_KeepsCoordinatesInOrderAndSetsOnlyTheCircle(string kind) {
        var (circle, rectangle) = Circle(kind, _bogota, 1500);

        circle.Should().NotBeNull();
        circle.Center.Latitude.Should().Be(4.6097);
        circle.Center.Longitude.Should().Be(-74.0817);
        circle.Radius.Should().Be(1500);
        rectangle.Should().BeNull();
    }

    [Theory]
    [InlineData("bias", 0.1)]
    [InlineData("bias", 50_000.0)]
    [InlineData("restriction", 0.1)]
    [InlineData("restriction", 50_000.0)]
    public void Circle_RadiusAtTheBounds_IsAccepted(string kind, double radius) {
        var (circle, _) = Circle(kind, _bogota, radius);
        circle!.Radius.Should().Be(radius);
    }

    [Theory]
    [InlineData("bias", 0.0)]
    [InlineData("bias", -1.0)]
    [InlineData("bias", 50_000.001)]
    [InlineData("bias", double.PositiveInfinity)]
    [InlineData("bias", double.NegativeInfinity)]
    [InlineData("bias", double.NaN)]
    [InlineData("restriction", 0.0)]
    [InlineData("restriction", -1.0)]
    [InlineData("restriction", 50_000.001)]
    [InlineData("restriction", double.PositiveInfinity)]
    [InlineData("restriction", double.NegativeInfinity)]
    [InlineData("restriction", double.NaN)]
    public void Circle_RadiusOutOfRange_ThrowsMapsInvalidRequestException(string kind, double radius) {
        var act = () => Circle(kind, _bogota, radius);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Radius*");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Circle_NullCenter_ThrowsMapsInvalidRequestException(string kind) {
        var act = () => Circle(kind, null!, 1500);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*center*");
    }

    // ===================== rectangle =====================
    [Theory]
    [MemberData(nameof(Kinds))]
    public void Rectangle_ValidCorners_MapsSouthWestToLowAndNorthEastToHighAndSetsOnlyTheRectangle(string kind) {
        var southWest = new LatLng(4.5, -74.2);
        var northEast = new LatLng(4.8, -73.9);

        var (circle, rectangle) = Rectangle(kind, southWest, northEast);

        rectangle.Should().NotBeNull();
        rectangle.Low.Should().BeSameAs(southWest);
        rectangle.High.Should().BeSameAs(northEast);
        circle.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Rectangle_EqualLatitudes_IsAccepted(string kind) {
        var act = () => Rectangle(kind, new LatLng(4.6, -74.2), new LatLng(4.6, -73.9));
        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Rectangle_CrossingTheAntimeridian_IsAccepted(string kind) {
        // South-west longitude east of north-east longitude: Google reads it as a box over 180°
        var (_, rectangle) = Rectangle(kind, new LatLng(-20, 170), new LatLng(-10, -170));

        rectangle!.Low.Longitude.Should().Be(170);
        rectangle.High.Longitude.Should().Be(-170);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Rectangle_SouthWestLatitudeAboveNorthEast_ThrowsMapsInvalidRequestException(string kind) {
        var act = () => Rectangle(kind, new LatLng(4.8, -74.2), new LatLng(4.5, -73.9));
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*latitude*");
    }

    [Theory]
    [InlineData("bias", true)]
    [InlineData("bias", false)]
    [InlineData("restriction", true)]
    [InlineData("restriction", false)]
    public void Rectangle_NullCorner_ThrowsMapsInvalidRequestException(string kind, bool southWestIsNull) {
        var southWest = southWestIsNull ? null : new LatLng(4.5, -74.2);
        var northEast = southWestIsNull ? new LatLng(4.8, -73.9) : null;

        var act = () => Rectangle(kind, southWest!, northEast!);

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*corners*");
    }
}
