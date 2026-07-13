using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Shiny.Maui.Controls.ImageEditor;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// Cross-platform compress / resize / format-convert helper for picked photos.
/// Loads the source with <see cref="PlatformImage"/> (same primitive the ImageEditor uses),
/// optionally downsizes, and re-encodes to PNG or JPEG at the requested quality.
/// </summary>
static class MediaImageProcessor
{
    /// <param name="source">The raw picked/captured image stream.</param>
    /// <param name="format">Output encoding (Png or Jpeg; other values fall back to PNG).</param>
    /// <param name="quality">0..1 encoder quality (applies to JPEG).</param>
    /// <param name="maxDimension">If &gt; 0, the longest edge is capped to this many pixels.</param>
    public static Task<MediaPickerItem?> ProcessAsync(
        Stream source,
        ImageExportFormat format,
        float quality,
        int maxDimension = 0
    ) => Task.Run<MediaPickerItem?>(() =>
    {
        var image = PlatformImage.FromStream(source);
        if (image == null)
            return null;

        var working = image;
        if (maxDimension > 0 && (image.Width > maxDimension || image.Height > maxDimension))
            working = image.Downsize(maxDimension, disposeOriginal: true);

        using var ms = new MemoryStream();
        using (var encoded = working.AsStream(format.ToBitmapFormat(), quality))
            encoded.CopyTo(ms);

        return new MediaPickerItem(ms.ToArray(), (int)working.Width, (int)working.Height, ContentTypeFor(format));
    });

    /// <summary>Map a 1..100 compression percentage to the 0..1 encoder quality (values are clamped).</summary>
    internal static float NormalizeQuality(int percent) => Math.Clamp(percent, 1, 100) / 100f;

    /// <summary>The MIME type produced for a given output format (non-PNG encodes as JPEG).</summary>
    internal static string ContentTypeFor(ImageExportFormat format)
        => format == ImageExportFormat.Png ? "image/png" : "image/jpeg";
}
