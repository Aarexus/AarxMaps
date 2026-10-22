using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Internal.Validation;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Validation;

[Trait("Package", "Places")]
[Trait("Module", "Validation")]
[Trait("Type", "RequestValidator")]
public class RequestValidatorTests {
    // ===================== required text =====================
    [Theory]
    [InlineData("pizza", "pizza")]
    [InlineData("  pizza  ", "pizza")]
    [InlineData("\tpizza near me\n", "pizza near me")]
    public void ValidateAndTrimRequiredText_NonBlank_ReturnsTheTrimmedText(string text, string expected)
        => RequestValidator.ValidateAndTrimRequiredText(text, "TextQuery").Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void ValidateAndTrimRequiredText_NullOrBlank_ThrowsMapsInvalidRequestExceptionNamingTheField(string? text) {
        var act = () => RequestValidator.ValidateAndTrimRequiredText(text!, "TextQuery");
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*TextQuery*");
    }

    // ===================== list =====================
    private const int MaxValues = 3;

    [Theory]
    [InlineData(1)]
    [InlineData(MaxValues)]
    public void ValidateAndTrimList_OneToMaxValues_ReturnsThemInOrder(int count) {
        var values = Enumerable.Range(0, count).Select(i => $"type_{i}").ToList();

        RequestValidator.ValidateAndTrimList(values, "IncludedTypes", MaxValues).Should().Equal(values);
    }

    [Fact]
    public void ValidateAndTrimList_PaddedValues_AreTrimmed()
        => RequestValidator.ValidateAndTrimList([" restaurant ", "\tcafe\n"], "IncludedTypes", MaxValues)
            .Should().Equal("restaurant", "cafe");

    [Fact]
    public void ValidateAndTrimList_CallerArray_IsNeitherReturnedNorTrimmedInPlace() {
        string[] values = [" restaurant ", "cafe"];

        var result = RequestValidator.ValidateAndTrimList(values, "IncludedTypes", MaxValues);

        result.Should().NotBeSameAs(values);
        values.Should().Equal(" restaurant ", "cafe");
    }

    [Fact]
    public void ValidateAndTrimList_CallerMutatesTheirListAfterwards_ResultIsUnaffected() {
        var values = new List<string> { "restaurant", "cafe" };

        var result = RequestValidator.ValidateAndTrimList(values, "IncludedTypes", MaxValues);
        values[0] = "bar";
        values.Add("bakery");

        result.Should().Equal("restaurant", "cafe");
    }

    [Fact]
    public void ValidateAndTrimList_Null_ThrowsMapsInvalidRequestExceptionNamingTheField() {
        var act = () => RequestValidator.ValidateAndTrimList(null!, "IncludedTypes", MaxValues);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("IncludedTypes cannot be null.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MaxValues + 1)]
    public void ValidateAndTrimList_ZeroOrMoreThanMax_ThrowsMapsInvalidRequestExceptionWithTheLimit(int count) {
        var values = Enumerable.Range(0, count).Select(i => $"type_{i}");

        var act = () => RequestValidator.ValidateAndTrimList(values, "IncludedTypes", MaxValues);

        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Between 1 and 3 IncludedTypes can be specified.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndTrimList_NullOrBlankValue_ThrowsMapsInvalidRequestExceptionNamingTheField(string? value) {
        var act = () => RequestValidator.ValidateAndTrimList(["restaurant", value!], "IncludedTypes", MaxValues);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Empty IncludedTypes values are not allowed.");
    }

    // ===================== session token =====================
    [Theory]
    [InlineData("a")]
    [InlineData("abc-DEF_123")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")] // 36 chars
    [InlineData("3f2c1a9e-7b4d-4e8a-9c6f-1d2e3f4a5b6c")] // a GUID, the typical token
    public void ValidateSessionToken_ValidToken_IsReturnedUnchanged(string token)
        => RequestValidator.ValidateSessionToken(token).Should().Be(token);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789a")] // 37 chars
    public void ValidateSessionToken_WrongLength_ThrowsMapsInvalidRequestException(string? token) {
        var act = () => RequestValidator.ValidateSessionToken(token!);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*between 1 and 36*");
    }

    [Theory]
    [InlineData("abc def")]
    [InlineData(" abc")]
    [InlineData("abc+def")]
    [InlineData("abc/def")]
    [InlineData("abc=")]
    [InlineData("abc.def")]
    [InlineData("año")]
    [InlineData("café")]
    public void ValidateSessionToken_CharacterOutsideUrlSafeBase64_ThrowsMapsInvalidRequestException(string token) {
        var act = () => RequestValidator.ValidateSessionToken(token);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("*base64*");
    }
}
