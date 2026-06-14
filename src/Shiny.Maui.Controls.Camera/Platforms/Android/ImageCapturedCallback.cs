using Android.Graphics;
using AndroidX.Camera.Core;
using CameraFilter = Shiny.Controls.Camera.CameraFilter;

namespace Shiny.Maui.Controls.Camera;

// Bridges CameraX ImageCapture's in-memory callback to a Task<CameraPhoto>. ImageCapture emits a
// JPEG-format ImageProxy whose single plane holds the encoded bytes. When a filter is active the still is
// run through the same CameraFilter as the live preview before re-encoding, so photos match what the user
// sees.
sealed class ImageCapturedCallback : ImageCapture.OnImageCapturedCallback
{
    readonly TaskCompletionSource<CameraPhoto> tcs = new();
    readonly CameraFilter filter;

    public ImageCapturedCallback(CameraFilter filter) => this.filter = filter;

    public Task<CameraPhoto> Task => this.tcs.Task;

    public override void OnCaptureSuccess(IImageProxy image)
    {
        try
        {
            var buffer = image.GetPlanes()![0].Buffer!;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);

            // None keeps the raw JPEG (with its EXIF orientation) untouched
            var result = this.filter == CameraFilter.None
                ? new CameraPhoto(bytes, image.Width, image.Height)
                : ApplyFilter(bytes, this.filter, image.ImageInfo?.RotationDegrees ?? 0);

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

    static CameraPhoto ApplyFilter(byte[] jpeg, CameraFilter filter, int rotationDegrees)
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
            using var output = Bitmap.CreateBitmap(upright.Width, upright.Height, Bitmap.Config.Argb8888!)!;
            using var canvas = new Canvas(output);
            using var paint = new Android.Graphics.Paint();
            if (AndroidCameraFilters.ColorMatrix(filter) is { } matrix)
                paint.SetColorFilter(new ColorMatrixColorFilter(matrix));
            canvas.DrawBitmap(upright, 0, 0, paint);

            using var ms = new MemoryStream();
            output.Compress(Bitmap.CompressFormat.Jpeg!, 95, ms);
            return new CameraPhoto(ms.ToArray(), output.Width, output.Height);
        }
        finally
        {
            if (!ReferenceEquals(upright, src))
                upright.Dispose();
        }
    }
}
