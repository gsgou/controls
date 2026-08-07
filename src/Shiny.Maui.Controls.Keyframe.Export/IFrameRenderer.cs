using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;
using SkiaSharp;

namespace Shiny.Maui.Controls.Keyframe.Export;

/// <summary>
/// Rasterises canvas drawing into pixels.
/// </summary>
/// <remarks>
/// Kept as an interface because <c>Microsoft.Maui.Graphics</c> is a drawing abstraction with no
/// rasteriser of its own — the pixels have to come from somewhere platform-specific. Skia is the
/// default because it runs headless on every desktop OS, which is what makes export usable from CI.
/// </remarks>
public interface IFrameRenderer
{
    /// <summary>Renders one frame and returns its pixels.</summary>
    /// <param name="width">Output width in pixels.</param>
    /// <param name="height">Output height in pixels.</param>
    /// <param name="background">Painted first. Null leaves the frame transparent.</param>
    /// <param name="draw">Draws the frame's content.</param>
    /// <returns>Premultiplied RGBA pixels, row major, four bytes per pixel.</returns>
    byte[] Render(int width, int height, Color? background, Action<ICanvas> draw);
}

/// <summary>Rasterises frames with Skia. Works headless on Windows, macOS and Linux.</summary>
public sealed class SkiaFrameRenderer : IFrameRenderer
{
    /// <inheritdoc />
    public byte[] Render(int width, int height, Color? background, Action<ICanvas> draw)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentNullException.ThrowIfNull(draw);

        using var context = new SkiaBitmapExportContext(width, height, displayScale: 1f);
        var canvas = context.Canvas;

        if (background is not null)
        {
            canvas.FillColor = background;
            canvas.FillRectangle(0f, 0f, width, height);
        }

        draw(canvas);

        var bitmap = context.Bitmap;
        var source = bitmap.Bytes;

        // Skia's byte order depends on the platform's preferred colour type, so ask rather than
        // assume. Getting this wrong silently swaps red and blue in every exported frame — the
        // kind of bug that survives review because the output still looks like a plausible image.
        if (bitmap.ColorType is not SKColorType.Bgra8888)
            return source;

        var pixels = new byte[source.Length];

        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = source[i + 2];
            pixels[i + 1] = source[i + 1];
            pixels[i + 2] = source[i];
            pixels[i + 3] = source[i + 3];
        }

        return pixels;
    }
}
