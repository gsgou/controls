using AndroidX.Camera.Core;

namespace Shiny.Maui.Controls.Camera;

// Bridges CameraX ImageCapture's in-memory callback to a Task<CameraPhoto>. ImageCapture emits a
// JPEG-format ImageProxy whose single plane holds the encoded bytes.
sealed class ImageCapturedCallback : ImageCapture.OnImageCapturedCallback
{
    readonly TaskCompletionSource<CameraPhoto> tcs = new();

    public Task<CameraPhoto> Task => this.tcs.Task;

    public override void OnCaptureSuccess(IImageProxy image)
    {
        try
        {
            var buffer = image.GetPlanes()![0].Buffer!;
            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            this.tcs.TrySetResult(new CameraPhoto(bytes, image.Width, image.Height));
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
}
