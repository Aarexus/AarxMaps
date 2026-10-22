using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Searching;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Searching;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Searching")]
[Trait("Type", "NearbyRequestBuilder")]
public class NearbyRequestBuilderTests {
    private static readonly LatLng _bogota = new(4.6097, -74.0817);

    private static NearbyRequestBuilder WithArea() => new NearbyRequestBuilder().WithLocationRestriction(_bogota, 1500);

    // ===================== location restriction =====================
    [Fact]
    public void Build_WithoutLocationRestriction_ThrowsMapsInvalidRequestException() {
        var act = () => new NearbyRequestBuilder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("LocationRestriction is required*");
    }

    [Fact]
    public void WithLocationRestriction_Circle_SetsTheRestrictionCircleWithCoordinatesInOrder() {
        var restriction = WithArea().Build().LocationRestriction;

        restriction.Circle!.Center.Latitude.Should().Be(4.6097);
        restriction.Circle.Center.Longitude.Should().Be(-74.0817);
        restriction.Circle.Radius.Should().Be(1500);
        restriction.Rectangle.Should().BeNull();
    }

    [Fact]
    public void WithLocationRestriction_InvalidRadius_ThrowsMapsInvalidRequestException() {
        var act = () => new NearbyRequestBuilder().WithLocationRestriction(_bogota, -1);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Radius*");
    }

    [Fact]
    public void Build_RestrictionOnly_UsesMaxResultCount20AndNoIncludedTypes() {
        var request = WithArea().Build();

        request.MaxResultCount.Should().Be(20);
        request.IncludedTypes.Should().BeNull();
    }

    // ===================== max results =====================
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void WithMaxResults_OneToTwenty_IsSet(int maxResults)
        => WithArea().WithMaxResults(maxResults).Build().MaxResultCount.Should().Be(maxResults);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void WithMaxResults_OutOfRange_ThrowsMapsInvalidRequestException(int maxResults) {
        var act = () => WithArea().WithMaxResults(maxResults);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("MaxResultCount must be between 1 and 20.");
    }

    // ===================== included types =====================
    // List rules live in RequestValidatorTests; these only prove the builder wires them with its own limit
    [Fact]
    public void WithIncludedTypes_ValidValues_AreSetTrimmed()
        => WithArea().WithIncludedTypes([" cafe ", "bakery"]).Build().IncludedTypes.Should().Equal("cafe", "bakery");

    [Fact]
    public void WithIncludedTypes_FiftyValues_IsAccepted() {
        var act = () => WithArea().WithIncludedTypes(Enumerable.Range(0, 50).Select(i => $"type_{i}"));
        act.Should().NotThrow();
    }

    [Fact]
    public void WithIncludedTypes_FiftyOneValues_ThrowsMapsInvalidRequestExceptionNamingTheField() {
        var act = () => WithArea().WithIncludedTypes(Enumerable.Range(0, 51).Select(i => $"type_{i}"));
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Between 1 and 50 IncludedTypes can be specified.");
    }
}
