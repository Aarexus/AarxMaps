using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Searching;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Searching;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Searching")]
[Trait("Type", "TextRequestBuilder")]
public class TextRequestBuilderTests {
    private static readonly LatLng _bogota = new(4.6097, -74.0817);
    private static readonly LatLng _southWest = new(4.5, -74.2);
    private static readonly LatLng _northEast = new(4.8, -73.9);

    private static TextRequestBuilder WithQuery() => new TextRequestBuilder().WithTextQuery("pizza");

    // ===================== text query =====================
    [Fact]
    public void Build_WithoutTextQuery_ThrowsMapsInvalidRequestException() {
        var act = () => new TextRequestBuilder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("TextQuery is required*");
    }

    [Fact]
    public void WithTextQuery_PaddedText_IsTrimmed()
        => new TextRequestBuilder().WithTextQuery("  pizza near me ").Build().TextQuery.Should().Be("pizza near me");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithTextQuery_NullOrBlank_ThrowsMapsInvalidRequestException(string? query) {
        var act = () => new TextRequestBuilder().WithTextQuery(query!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*TextQuery*");
    }

    [Fact]
    public void Build_TextQueryOnly_UsesPageSize20AndLeavesEveryOptionalFieldNull() {
        var request = WithQuery().Build();

        request.TextQuery.Should().Be("pizza");
        request.PageSize.Should().Be(20);
        request.PageToken.Should().BeNull();
        request.IncludedType.Should().BeNull();
        request.LocationBias.Should().BeNull();
        request.LocationRestriction.Should().BeNull();
    }

    // ===================== paging =====================
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void WithPageSize_OneToTwenty_IsSet(int pageSize)
        => WithQuery().WithPageSize(pageSize).Build().PageSize.Should().Be(pageSize);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void WithPageSize_OutOfRange_ThrowsMapsInvalidRequestException(int pageSize) {
        var act = () => WithQuery().WithPageSize(pageSize);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("PageSize must be between 1 and 20.");
    }

    [Fact]
    public void WithPageToken_PaddedToken_IsTrimmed()
        => WithQuery().WithPageToken(" page-2-token ").Build().PageToken.Should().Be("page-2-token");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithPageToken_NullOrBlank_ThrowsMapsInvalidRequestException(string? token) {
        var act = () => WithQuery().WithPageToken(token!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*PageToken*");
    }

    // ===================== included type =====================
    [Fact]
    public void WithIncludedType_PaddedType_IsTrimmed()
        => WithQuery().WithIncludedType(" restaurant ").Build().IncludedType.Should().Be("restaurant");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithIncludedType_NullOrBlank_ThrowsMapsInvalidRequestException(string? type) {
        var act = () => WithQuery().WithIncludedType(type!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*IncludedType*");
    }

    // ===================== area =====================
    [Fact]
    public void WithLocationBias_Circle_SetsTheBiasCircleWithCoordinatesInOrder() {
        var request = WithQuery().WithLocationBias(_bogota, 1500).Build();

        request.LocationBias!.Circle!.Center.Latitude.Should().Be(4.6097);
        request.LocationBias.Circle.Center.Longitude.Should().Be(-74.0817);
        request.LocationBias.Circle.Radius.Should().Be(1500);
        request.LocationBias.Rectangle.Should().BeNull();
        request.LocationRestriction.Should().BeNull();
    }

    [Fact]
    public void WithLocationBias_Rectangle_SetsTheBiasRectangleSouthWestLowNorthEastHigh() {
        var request = WithQuery().WithLocationBias(_southWest, _northEast).Build();

        request.LocationBias!.Rectangle!.Low.Should().BeSameAs(_southWest);
        request.LocationBias.Rectangle.High.Should().BeSameAs(_northEast);
        request.LocationBias.Circle.Should().BeNull();
        request.LocationRestriction.Should().BeNull();
    }

    [Fact]
    public void WithLocationRestriction_Rectangle_SetsTheRestrictionRectangleSouthWestLowNorthEastHigh() {
        var request = WithQuery().WithLocationRestriction(_southWest, _northEast).Build();

        request.LocationRestriction!.Rectangle!.Low.Should().BeSameAs(_southWest);
        request.LocationRestriction.Rectangle.High.Should().BeSameAs(_northEast);
        request.LocationRestriction.Circle.Should().BeNull();
        request.LocationBias.Should().BeNull();
    }

    [Fact]
    public void WithLocationBias_CalledAgain_ReplacesTheEarlierBias() {
        var request = WithQuery().WithLocationBias(_bogota, 1500).WithLocationBias(_southWest, _northEast).Build();

        request.LocationBias!.Circle.Should().BeNull();
        request.LocationBias.Rectangle.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Build_BiasAndRestrictionTogether_ThrowsMapsInvalidRequestException(bool biasFirst) {
        var builder = WithQuery();
        if (biasFirst) builder.WithLocationBias(_bogota, 1500).WithLocationRestriction(_southWest, _northEast);
        else builder.WithLocationRestriction(_southWest, _northEast).WithLocationBias(_southWest, _northEast);

        var act = () => builder.Build();

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*not both*");
    }

    [Fact]
    public void WithLocationBias_InvalidRadius_ThrowsMapsInvalidRequestException() {
        var act = () => WithQuery().WithLocationBias(_bogota, 50_001);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Radius*");
    }

    [Fact]
    public void WithLocationRestriction_InvertedLatitudes_ThrowsMapsInvalidRequestException() {
        var act = () => WithQuery().WithLocationRestriction(_northEast, _southWest);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*latitude*");
    }
}
