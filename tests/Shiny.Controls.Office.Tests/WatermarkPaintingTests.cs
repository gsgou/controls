using Shiny.Controls.Office.Skia;
using Shiny.Controls.Office.Theming;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// That the mark is actually drawn, and drawn faintly.
/// </summary>
/// <remarks>
/// Rendered to a bitmap and sampled, because everything else about a watermark is wiring: the property
/// reaches the paint request, the paint request reaches the painter, and none of that proves a single
/// pixel changed. The failure this catches is the one that looks like nothing happening at all.
/// </remarks>
public class WatermarkPaintingTests
{
    /// <summary>A solid red square, encoded, so any pixel it touches is unmistakable.</summary>
    static byte[] RedSquare()
    {
        using var bitmap = new SKBitmap(64, 64);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(new SKColor(255, 0, 0));

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    static (SKBitmap Bitmap, SKCanvas Canvas) Surface()
    {
        var bitmap = new SKBitmap(200, 200);
        var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        return (bitmap, canvas);
    }

    [Fact]
    public void TheMarkChangesThePixelsUnderIt()
    {
        var (bitmap, canvas) = Surface();
        using var _ = bitmap;
        using var __ = canvas;

        WatermarkPainter.Draw(
            canvas,
            new SKRect(0, 0, 200, 200),
            new OfficeWatermark { Image = RedSquare() });

        bitmap.GetPixel(100, 100).ShouldNotBe(SKColors.White);
    }

    [Fact]
    public void ItIsAWashRatherThanAPicture()
    {
        // A watermark sits behind text that still has to be read. Drawn at full strength it makes the
        // page unusable, which is the mistake the default opacity exists to prevent.
        var (bitmap, canvas) = Surface();
        using var _ = bitmap;
        using var __ = canvas;

        WatermarkPainter.Draw(
            canvas,
            new SKRect(0, 0, 200, 200),
            new OfficeWatermark { Image = RedSquare() });

        var centre = bitmap.GetPixel(100, 100);

        centre.Red.ShouldBe((byte)255);
        centre.Green.ShouldBeGreaterThan((byte)180, "a wash keeps most of the paper showing through");
    }

    [Fact]
    public void NothingIsDrawnForAnEmptyMark()
    {
        var (bitmap, canvas) = Surface();
        using var _ = bitmap;
        using var __ = canvas;

        WatermarkPainter.Draw(canvas, new SKRect(0, 0, 200, 200), new OfficeWatermark { Image = [] });
        WatermarkPainter.Draw(canvas, new SKRect(0, 0, 200, 200), null);

        bitmap.GetPixel(100, 100).ShouldBe(SKColors.White);
    }

    [Fact]
    public void APictureThatWillNotDecodeIsNotACrash()
    {
        var (bitmap, canvas) = Surface();
        using var _ = bitmap;
        using var __ = canvas;

        Should.NotThrow(() => WatermarkPainter.Draw(
            canvas,
            new SKRect(0, 0, 200, 200),
            new OfficeWatermark { Image = [1, 2, 3, 4] }));

        bitmap.GetPixel(100, 100).ShouldBe(SKColors.White);
    }

    [Fact]
    public void TheMarkStaysInsideTheBoundsItWasGiven()
    {
        // Clipping is what stops a document's mark running onto the surround between pages, and a
        // rotated one scaled to the page is wider than the page across its diagonal.
        var (bitmap, canvas) = Surface();
        using var _ = bitmap;
        using var __ = canvas;

        WatermarkPainter.Draw(
            canvas,
            new SKRect(0, 0, 100, 100),
            new OfficeWatermark { Image = RedSquare(), Scale = 1.0, RotationDegrees = 315, Opacity = 1 });

        bitmap.GetPixel(150, 150).ShouldBe(SKColors.White, "outside the bounds must be untouched");
        bitmap.GetPixel(50, 50).ShouldNotBe(SKColors.White);
    }

    [Fact]
    public void ContainKeepsThePictureSquareRatherThanStretchingIt()
    {
        var (bitmap, canvas) = Surface();
        using var _ = bitmap;
        using var __ = canvas;

        // A square mark on a wide surface must stay square: stretching a logo to the page is the other
        // way a watermark goes visibly wrong.
        WatermarkPainter.Draw(
            canvas,
            new SKRect(0, 0, 200, 100),
            new OfficeWatermark { Image = RedSquare(), Scale = 1.0, Opacity = 1 });

        // Scale 1.0 of the shorter side is 100, centred on a 200x100 box: x 50..150, y 0..100.
        bitmap.GetPixel(100, 50).ShouldNotBe(SKColors.White);
        bitmap.GetPixel(20, 50).ShouldBe(SKColors.White, "a square mark must not be stretched to the width");
    }
}
