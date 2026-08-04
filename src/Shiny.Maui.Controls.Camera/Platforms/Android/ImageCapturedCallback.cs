using Android.Graphics;
using AndroidX.Camera.Core;

namespace Shiny.Maui.Controls.Camera;

// Bridges CameraX ImageCapture's in-memory callback to a Task<CameraPhoto>. ImageCapture emits a
// JPEG-format ImageProxy whose single plane holds the encoded bytes. When effects are active the still is
// run through the same chain as the live preview before re-encoding, so photos match what the user sees —
// and, on API levels below the preview's RenderEffect requirement, the photo is filtered even though the
// preview was not.
sealed class ImageCapturedCallback : ImageCapture.OnImageCapturedCallback
{
    readonly TaskCompletionSource<CameraPhoto> tcs = new();
    readonly CameraEffectChain chain;

    public ImageCapturedCallback(CameraEffectChain chain) => this.chain = chain;

    public Task<CameraPhoto> Task => this.tcs.Task;

    public override void OnCaptureSuccess(IImageProxy image)
    {
        try
        {
            var buffer = image.GetPlanes()![0].Buffer!;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            // an empty chain keeps the raw JPEG (with its EXIF orientation) untouched
            var result = this.chain.IsEmpty
                ? new CameraPhoto(bytes, image.Width, image.Height)
                : ApplyEffects(bytes, this.chain, image.ImageInfo?.RotationDegrees ?? 0);

            this.tcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            this.tcs.TrySetException(ex);
        }
        finally
        {
            image.Close();
        }
    }

    public override void OnError(ImageCaptureException exception)
        => this.tcs.TrySetException(new InvalidOperationException(exception.Message, exception));

    static CameraPhoto ApplyEffects(byte[] jpeg, CameraEffectChain chain, int rotationDegrees)
    {
        using var src = BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length)
            ?? throw new InvalidOperationException("Could not decode the captured photo");

        // Re-encoding via Bitmap drops the JPEG's EXIF orientation, so bake the rotation into the pixels.
        var upright = src;
        if (rotationDegrees != 0)
        {
            using var transform = new Matrix();
            transform.PostRotate(rotationDegrees);
            upright = Bitmap.CreateBitmap(src, 0, 0, src.Width, src.Height, transform, true)!;
        }

        try
        {
            var plan = AndroidCameraFilters.StillPlan(chain);

            // Fast path — an all-colour plan is one draw with one ColorMatrixColorFilter, exactly as before.
            if (AndroidCameraFilters.CreateStillColorMatrix(plan) is { } matrix)
            {
                using var graded = Bitmap.CreateBitmap(upright.Width, upright.Height, Bitmap.Config.Argb8888!)!;
                using (var canvas = new Canvas(graded))
                using (var paint = new Android.Graphics.Paint())
                {
                    paint.SetColorFilter(new ColorMatrixColorFilter(matrix));
                    canvas.DrawBitmap(upright, 0, 0, paint);
                }
                return Encode(graded);
            }

            // Ordered path — a spatial effect is in the chain, so every step runs in sequence over the pixels.
            // A still is one-shot, so the managed cost is paid once rather than per frame.
            using var output = ApplyOrderedPlan(upright, plan);
            return Encode(output);
        }
        finally
        {
            if (!ReferenceEquals(upright, src))
                upright.Dispose();
        }
    }

    static CameraPhoto Encode(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 95, ms);
        return new CameraPhoto(ms.ToArray(), bitmap.Width, bitmap.Height);
    }

    static Bitmap ApplyOrderedPlan(Bitmap source, IReadOnlyList<EffectStep> plan)
    {
        var width = source.Width;
        var height = source.Height;

        var argb = new int[width * height];
        source.GetPixels(argb, 0, width, 0, 0, width, height);

        var surface = new PixelSurface(width, height, ToBgra(argb));
        foreach (var step in plan)
        {
            if (step.Color is { } matrix)
                surface.Apply(matrix);
            else if (step.Descriptor?.Managed is { } pass)
                surface = pass(surface);
        }

        var result = Bitmap.CreateBitmap(surface.Width, surface.Height, Bitmap.Config.Argb8888!)!;
        result.SetPixels(ToArgb(surface.Pixels), 0, surface.Width, 0, 0, surface.Width, surface.Height);
        return result;
    }

    static byte[] ToBgra(int[] argb)
    {
        var bgra = new byte[argb.Length * 4];
        for (var i = 0; i < argb.Length; i++)
        {
            var c = argb[i];
            var o = i * 4;
            bgra[o] = (byte)c;               // B
            bgra[o + 1] = (byte)(c >> 8);    // G
            bgra[o + 2] = (byte)(c >> 16);   // R
            bgra[o + 3] = (byte)(c >> 24);   // A
        }
        return bgra;
    }

    static int[] ToArgb(byte[] bgra)
    {
        var argb = new int[bgra.Length / 4];
        for (var i = 0; i < argb.Length; i++)
        {
            var o = i * 4;
            argb[i] = (bgra[o + 3] << 24) | (bgra[o + 2] << 16) | (bgra[o + 1] << 8) | bgra[o];
        }
        return argb;
    }
}
