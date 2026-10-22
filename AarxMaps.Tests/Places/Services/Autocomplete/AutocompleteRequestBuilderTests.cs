using AarxMaps.Core.Exceptions;
using AarxMaps.Core.Models.Geometry;
using AarxMaps.Places.Services.Autocomplete;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Autocomplete;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Autocomplete")]
[Trait("Type", "AutocompleteRequestBuilder")]
public class AutocompleteRequestBuilderTests {
    private static readonly LatLng _bogota = new(4.6097, -74.0817);

    private static AutocompleteRequestBuilder WithInput() => new AutocompleteRequestBuilder().WithInput("Bogota");

    // ===================== input =====================
    [Fact]
    public void Build_WithoutInput_ThrowsMapsInvalidRequestException() {
        var act = () => new AutocompleteRequestBuilder().Build();
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Input is required*");
    }

    [Fact]
    public void WithInput_PaddedText_IsTrimmed()
        => new AutocompleteRequestBuilder().WithInput("  pizza  ").Build().Input.Should().Be("pizza");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithInput_NullOrBlank_ThrowsMapsInvalidRequestException(string? input) {
        var act = () => new AutocompleteRequestBuilder().WithInput(input!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*Input*");
    }

    [Fact]
    public void Build_InputOnly_LeavesEveryOptionalFieldNull() {
        var request = WithInput().Build();

        request.Input.Should().Be("Bogota");
        request.IncludedPrimaryTypes.Should().BeNull();
        request.LocationBias.Should().BeNull();
        request.LocationRestriction.Should().BeNull();
        request.SessionToken.Should().BeNull();
    }

    // ===================== area =====================
    [Fact]
    public void WithLocationBias_ValidCircle_SetsTheBiasCircleWithCoordinatesInOrder() {
        var request = WithInput().WithLocationBias(_bogota, 1500).Build();

        request.LocationBias!.Circle!.Center.Latitude.Should().Be(4.6097);
        request.LocationBias.Circle.Center.Longitude.Should().Be(-74.0817);
        request.LocationBias.Circle.Radius.Should().Be(1500);
        request.LocationBias.Rectangle.Should().BeNull();
        request.LocationRestriction.Should().BeNull();
    }

    [Fact]
    public void WithLocationRestriction_ValidCircle_SetsTheRestrictionCircleWithCoordinatesInOrder() {
        var request = WithInput().WithLocationRestriction(_bogota, 1500).Build();

        request.LocationRestriction!.Circle!.Center.Latitude.Should().Be(4.6097);
        request.LocationRestriction.Circle.Center.Longitude.Should().Be(-74.0817);
        request.LocationRestriction.Circle.Radius.Should().Be(1500);
        request.LocationRestriction.Rectangle.Should().BeNull();
        request.LocationBias.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Build_BiasAndRestrictionTogether_ThrowsMapsInvalidRequestException(bool biasFirst) {
        var builder = WithInput();
        if (biasFirst) builder.WithLocationBias(_bogota, 1500).WithLocationRestriction(_bogota, 1500);
        else builder.WithLocationRestriction(_bogota, 1500).WithLocationBias(_bogota, 1500);

        var act = () => builder.Build();

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*not both*");
    }

    [Fact]
    public void WithLocationBias_InvalidRadius_ThrowsMapsInvalidRequestException() {
        var act = () => WithInput().WithLocationBias(_bogota, 0);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Radius*");
    }

    [Fact]
    public void WithLocationRestriction_NullCenter_ThrowsMapsInvalidRequestException() {
        var act = () => WithInput().WithLocationRestriction(null!, 1500);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*center*");
    }

    // ===================== primary types =====================
    // List rules live in RequestValidatorTests; these only prove the builder wires them with its own limit
    [Fact]
    public void WithIncludedPrimaryTypes_ValidValues_AreSetTrimmed()
        => WithInput().WithIncludedPrimaryTypes([" restaurant ", "cafe"]).Build()
            .IncludedPrimaryTypes.Should().Equal("restaurant", "cafe");

    [Fact]
    public void WithIncludedPrimaryTypes_FiveValues_IsAccepted() {
        var act = () => WithInput().WithIncludedPrimaryTypes(Enumerable.Range(0, 5).Select(i => $"type_{i}"));
        act.Should().NotThrow();
    }

    [Fact]
    public void WithIncludedPrimaryTypes_SixValues_ThrowsMapsInvalidRequestExceptionNamingTheField() {
        var act = () => WithInput().WithIncludedPrimaryTypes(Enumerable.Range(0, 6).Select(i => $"type_{i}"));
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Between 1 and 5 IncludedPrimaryTypes can be specified.");
    }

    // ===================== session token =====================
    [Fact]
    public void WithSessionToken_ValidToken_IsSet()
        => WithInput().WithSessionToken("3f2c1a9e-7b4d-4e8a-9c6f-1d2e3f4a5b6c").Build()
            .SessionToken.Should().Be("3f2c1a9e-7b4d-4e8a-9c6f-1d2e3f4a5b6c");

    [Theory]
    [InlineData("")]
    [InlineData("not a token")]
    public void WithSessionToken_InvalidToken_ThrowsMapsInvalidRequestException(string token) {
        var act = () => WithInput().WithSessionToken(token);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("SessionToken*");
    }
}
