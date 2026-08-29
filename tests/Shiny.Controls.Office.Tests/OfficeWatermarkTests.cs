using Shiny.Controls.Office.Theming;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// The watermark model, and the one rule the painters lean on: when there is nothing to draw.
/// </summary>
/// <remarks>
/// <see cref="OfficeWatermark.IsEmpty"/> is what every painter checks before doing any work, so a mark
/// that reports itself present with nothing usable in it costs a decode attempt on every frame of a
/// scroll — and one that reports itself empty when it is not simply never appears.
/// </remarks>
public class OfficeWatermarkTests
{
    static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void AMarkWithAPictureIsNotEmpty()
        => new OfficeWatermark { Image = Png }.IsEmpty.ShouldBeFalse();

    [Fact]
    public void NoBytesMeansNothingToDraw()
        => new OfficeWatermark { Image = [] }.IsEmpty.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void TransparentMeansNothingToDraw(double opacity)
        => new OfficeWatermark { Image = Png, Opacity = opacity }.IsEmpty.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ZeroScaleMeansNothingToDraw(double scale)
        => new OfficeWatermark { Image = Png, Scale = scale }.IsEmpty.ShouldBeTrue();

    [Fact]
    public void TheDefaultIsFaintEnoughToReadThrough()
    {
        // The failure people actually hit is a mark drawn at full strength that makes the page
        // unusable, so the default has to be a wash rather than a picture.
        var mark = new OfficeWatermark { Image = Png };

        mark.Opacity.ShouldBeLessThan(0.3);
        mark.Opacity.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ItIsAValueSoAHostCanCompareBeforeRepainting()
    {
        var a = new OfficeWatermark { Image = Png, RotationDegrees = 315 };
        var b = new OfficeWatermark { Image = Png, RotationDegrees = 315 };

        a.ShouldBe(b);
        a.ShouldNotBe(a with { RotationDegrees = 0 });
    }
}
