using AndroidX.Camera.Video;
using AndroidX.Core.Util;

namespace Shiny.Maui.Controls.Camera;

// Listens to CameraX VideoRecordEvents and completes a Task when recording finalizes.
sealed class VideoRecordListener : Java.Lang.Object, IConsumer
{
    readonly TaskCompletionSource<CameraVideo> tcs = new();
    readonly string path;

    public VideoRecordListener(string path) => this.path = path;

    public Task<CameraVideo> Task => this.tcs.Task;

    public void Accept(Java.Lang.Object? value)
    {
        if (value is not VideoRecordEvent.Finalize fin)
            return;

        if (fin.HasError)
            this.tcs.TrySetException(new InvalidOperationException($"Video recording failed (error {fin.Error})"));
        else
            this.tcs.TrySetResult(new CameraVideo(this.path));
    }
}
