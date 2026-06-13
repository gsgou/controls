using AVFoundation;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

// Bridges AVCaptureMovieFileOutput's finished-recording callback to a Task<CameraVideo>.
sealed class MovieRecordingDelegate : AVCaptureFileOutputRecordingDelegate
{
    readonly TaskCompletionSource<CameraVideo> tcs = new();

    public Task<CameraVideo> Task => this.tcs.Task;

    public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError? error)
    {
        // AVFoundation reports a non-null error even on success when recording is stopped manually;
        // it sets AVErrorRecordingSuccessfullyFinishedKey to true in that case.
        var finishedOk = error == null
            || (error.UserInfo?[new NSString("AVErrorRecordingSuccessfullyFinishedKey")] as NSNumber)?.BoolValue == true;

        if (!finishedOk)
            this.tcs.TrySetException(new InvalidOperationException(error!.LocalizedDescription));
        else
            this.tcs.TrySetResult(new CameraVideo(outputFileUrl.Path!));
    }
}
