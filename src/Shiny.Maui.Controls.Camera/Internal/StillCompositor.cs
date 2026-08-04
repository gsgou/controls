#if ANDROID || IOS || MACCATALYST || WINDOWS || MACOS
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Composites the chain's <see cref="IDrawEffect"/>s into a captured still, so a mask or watermark that is
/// visible on the preview also ends up in the saved photo.
/// </summary>
/// <remarks>
/// Written once against MAUI Graphics rather than per platform: <c>PlatformImage</c> and
/// <c>PlatformBitmapExportService</c> already wrap CoreGraphics / android.graphics / Win2D, and a still is a
/// one-shot operation where the abstraction costs nothing. The pixel effects are <b>not</b> applied here —
/// those are already baked in by the platform capture path, which can do them on the GPU.
/// </remarks>
static class StillCompositor
{
    public static CameraPhoto Composite(CameraPhoto photo, CameraEffectChain chain, CameraFacing facing)
    {
        if (chain.DrawEffects.Count == 0)
            return photo;

        try
        {
            using var input = new MemoryStream(photo.Data, false);
            var image = PlatformImage.FromStream(input);
            if (image is null)
                return photo;

            var width = (int)image.Width;
            var height = (int)image.Height;
            if (width <= 0 || height <= 0)
                return photo;

            using var context = new PlatformBitmapExportService().CreateContext(width, height);
            var canvas = context.Canvas;
            canvas.DrawImage(image, 0, 0, width, height);

            var bounds = new RectF(0, 0, width, height);
            var effectContext = new CameraEffectContext(
                TimeSpan.Zero, 0, width, height, facing, CameraSurface.Photo, [], null);

            foreach (var effect in chain.DrawEffects)
            {
                canvas.SaveState();
                try
                {
                    effect.Draw(canvas, bounds, effectContext);
                }
                catch (Exception)
                {
                    // one bad effect must not cost the user their photo
                }
                finally
                {
                    canvas.RestoreState();
                }
            }

            using var output = new MemoryStream();
            context.Image.Save(output, ImageFormat.Jpeg, 0.95f);
            var bytes = output.ToArray();

            return bytes.Length == 0 ? photo : new CameraPhoto(bytes, width, height);
        }
        catch (Exception)
        {
            // Compositing is a best-effort enhancement of a photo that already exists and is already correct.
            // Losing the overlay is a far better outcome than losing the capture.
            return photo;
        }
    }


    /// <summary>
    /// Run the chain's <see cref="ICaptureEffect"/>s over an encoded still, in order. Each effect is expected
    /// to return the input unchanged on failure; one that throws is skipped rather than failing the capture.
    /// </summary>
    public static async Task<CameraPhoto> ApplyCaptureEffectsAsync(
        CameraPhoto photo, CameraEffectChain chain, CancellationToken ct)
    {
        if (chain.CaptureEffects.Count == 0)
            return photo;

        var data = photo.Data;
        foreach (var effect in chain.CaptureEffects)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var next = await effect.ApplyAsync(data, ct).ConfigureAwait(false);
                if (next is { Length: > 0 })
                    data = next;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // keep the photo we have and move on to the next effect
            }
        }

        if (ReferenceEquals(data, photo.Data))
            return photo;

        // A capture effect may hand back a differently-sized image (an image-generation model very often
        // does), so re-read the dimensions rather than trusting the originals.
        var (width, height) = ReadSize(data);
        return new CameraPhoto(data, width > 0 ? width : photo.Width, height > 0 ? height : photo.Height);
    }

    static (int Width, int Height) ReadSize(byte[] encoded)
    {
        try
        {
            using var ms = new MemoryStream(encoded, false);
            var image = PlatformImage.FromStream(ms);
            return image is null ? (0, 0) : ((int)image.Width, (int)image.Height);
        }
        catch (Exception)
        {
            return (0, 0);
        }
    }
}
#endif
