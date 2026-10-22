using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Services.Details;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Details;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Details")]
[Trait("Type", "DetailsRequestBuilder")]
public class DetailsRequestBuilderTests {
    [Fact]
    public void Build_WithoutPlaceId_ThrowsMapsInvalidRequestException() {
        var act = () => new DetailsRequestBuilder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("PlaceId is required*");
    }

    [Fact]
    public void Build_PlaceIdOnly_LeavesTheSessionTokenNull() {
        var request = new DetailsRequestBuilder().WithPlaceId("ChIJ-museo").Build();

        request.PlaceId.Should().Be("ChIJ-museo");
        request.SessionToken.Should().BeNull();
    }

    [Fact]
    public void WithPlaceId_PaddedId_IsTrimmed()
        => new DetailsRequestBuilder().WithPlaceId("  ChIJ-museo ").Build().PlaceId.Should().Be("ChIJ-museo");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithPlaceId_NullOrBlank_ThrowsMapsInvalidRequestException(string? placeId) {
        var act = () => new DetailsRequestBuilder().WithPlaceId(placeId!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*PlaceId*");
    }

    [Fact]
    public void WithSessionToken_ValidToken_IsSet()
        => new DetailsRequestBuilder().WithPlaceId("ChIJ-museo").WithSessionToken("tok-1").Build()
            .SessionToken.Should().Be("tok-1");

    [Theory]
    [InlineData("")]
    [InlineData("not a token")]
    public void WithSessionToken_InvalidToken_ThrowsMapsInvalidRequestException(string token) {
        var act = () => new DetailsRequestBuilder().WithSessionToken(token);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("SessionToken*");
    }
}
