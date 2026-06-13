using AVFoundation;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

// Bridges AVCapturePhotoOutput's delegate callback to a Task<CameraPhoto>.
sealed class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
{
    readonly TaskCompletionSource<CameraPhoto> tcs = new();

    public Task<CameraPhoto> Task => this.tcs.Task;

    public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
    {
        if (error != null)
        {
            this.tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
            return;
        }

        using var data = photo.FileDataRepresentation;
        if (data == null)
        {
            this.tcs.TrySetException(new InvalidOperationException("No photo data was produced"));
            return;
        }

        var bytes = data.ToArray();
        var dims = photo.ResolvedSettings.PhotoDimensions;
        this.tcs.TrySetResult(new CameraPhoto(bytes, dims.Width, dims.Height));
    }
}
