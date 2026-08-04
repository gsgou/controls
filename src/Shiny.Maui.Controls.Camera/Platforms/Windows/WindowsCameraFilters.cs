using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Shiny.Maui.Controls.Camera;

// Applies a CameraEffectChain to a captured still on Windows.
//
// Windows has no live-preview effect hook that is worth its cost here — MediaCapture would need an
// IBasicVideoEffect plus a Win2D pipeline — so this is the only surface where Windows applies effects, and
// GetEffectSupport reports StillOnly to match. It runs on the CPU over a managed BGRA copy, which is fine for
// a one-shot capture and would not be for a frame loop.
static class WindowsCameraFilters
{
    /// <summary>Whether this backend can run <paramref name="effect"/>'s native program.</summary>
    public static bool IsHandledNatively(ICameraEffect effect)
        => (effect as INativeEffect)?.Descriptor.Managed is not null;

    /// <summary>
    /// Apply the chain's pixel effects to <paramref name="bitmap"/> in place. Returns the same bitmap when
    /// there is nothing to do, or a new one when a managed pass changed the dimensions.
    /// </summary>
    public static SoftwareBitmap Apply(SoftwareBitmap bitmap, CameraEffectChain chain)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(chain);

        var steps = chain.Plan(IsHandledNatively);
        if (steps.Count == 0)
            return bitmap;

        var source = bitmap;
        var converted = false;
        if (source.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
        {
            source = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            converted = true;
        }

        try
        {
            var surface = Read(source);
            foreach (var step in steps)
            {
                if (step.Color is { } matrix)
                    surface.Apply(matrix);
                else if (step.Descriptor?.Managed is { } pass)
                    surface = pass(surface);
            }

            return Write(surface, source, converted);
        }
        finally
        {
            if (converted && !ReferenceEquals(source, bitmap))
                source.Dispose();
        }
    }

    static PixelSurface Read(SoftwareBitmap bitmap)
    {
        var length = bitmap.PixelWidth * bitmap.PixelHeight * 4;
        var buffer = new Windows.Storage.Streams.Buffer((uint)length);
        bitmap.CopyToBuffer(buffer);

        var bytes = new byte[length];
        using var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(bytes);

        return new PixelSurface(bitmap.PixelWidth, bitmap.PixelHeight, bytes);
    }

    static SoftwareBitmap Write(PixelSurface surface, SoftwareBitmap source, bool sourceIsOwned)
    {
        // A pass that resized needs a new bitmap; otherwise write straight back into the one we read from
        // (or a copy of it, when the caller still owns the original).
        var target = surface.Width == source.PixelWidth && surface.Height == source.PixelHeight && sourceIsOwned
            ? SoftwareBitmap.Copy(source)
            : new SoftwareBitmap(BitmapPixelFormat.Bgra8, surface.Width, surface.Height, BitmapAlphaMode.Premultiplied);

        using var writer = new DataWriter();
        writer.WriteBytes(surface.Pixels);
        target.CopyFromBuffer(writer.DetachBuffer());
        return target;
    }
}
