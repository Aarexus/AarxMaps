using AarxMaps.Core.Exceptions;
using AarxMaps.Places.Services.Details;
using FluentAssertions;

namespace AarxMaps.Tests.Places.Services.Details;

[Trait("Package", "Places")]
[Trait("Module", "Services")]
[Trait("Feature", "Details")]
[Trait("Type", "PhotoOptionsBuilder")]
public class PhotoOptionsBuilderTests {
    [Fact]
    public void Build_Defaults_AreTenPhotosAt480By480() {
        var options = new PhotoOptionsBuilder().Build();

        options.MaxPhotos.Should().Be(10);
        options.MaxHeightPx.Should().Be(480);
        options.MaxWidthPx.Should().Be(480);
    }

    // ===================== max photos =====================
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void WithMaxPhotos_OneToTen_IsSet(int maxPhotos)
        => new PhotoOptionsBuilder().WithMaxPhotos(maxPhotos).Build().MaxPhotos.Should().Be(maxPhotos);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void WithMaxPhotos_OutOfRange_ThrowsMapsInvalidRequestException(int maxPhotos) {
        var act = () => new PhotoOptionsBuilder().WithMaxPhotos(maxPhotos);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("MaxPhotos must be between 1 and 10.");
    }

    // ===================== resolution =====================
    [Theory]
    [InlineData(1, 1)]
    [InlineData(4800, 4800)]
    [InlineData(1200, 800)]
    public void WithResolution_WithinBounds_SetsHeightAndWidthInOrder(int height, int width) {
        var options = new PhotoOptionsBuilder().WithResolution(height, width).Build();

        options.MaxHeightPx.Should().Be(height);
        options.MaxWidthPx.Should().Be(width);
    }

    [Theory]
    [InlineData(0, 480)]
    [InlineData(-1, 480)]
    [InlineData(4801, 480)]
    [InlineData(480, 0)]
    [InlineData(480, -1)]
    [InlineData(480, 4801)]
    public void WithResolution_EitherDimensionOutOfRange_ThrowsMapsInvalidRequestException(int height, int width) {
        var act = () => new PhotoOptionsBuilder().WithResolution(height, width);
        act.Should().Throw<MapsInvalidRequestException>().WithMessage("Resolution must be between 1 and 4800 pixels.");
    }

    [Fact]
    public void WithResolution_Rejected_KeepsThePreviousResolution() {
        var builder = new PhotoOptionsBuilder().WithResolution(1200, 800);

        var act = () => builder.WithResolution(1200, 0);

        act.Should().Throw<MapsInvalidRequestException>();
        builder.Build().Should().BeEquivalentTo(new { MaxHeightPx = 1200, MaxWidthPx = 800 });
    }
}
