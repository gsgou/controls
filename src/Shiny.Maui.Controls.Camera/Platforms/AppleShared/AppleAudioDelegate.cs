using AVFoundation;
using CoreMedia;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

// Forwards audio CMSampleBuffers from an AVCaptureAudioDataOutput to the overlay recorder's AVAssetWriter.
// Only used on the burn-in recording path (VideoRecordingOptions.Overlay set); the raw-feed path lets
// AVCaptureMovieFileOutput handle audio itself.
sealed class AppleAudioDelegate : AVCaptureAudioDataOutputSampleBufferDelegate
{
    public volatile AppleVideoOverlayRecorder? Recorder;

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            this.Recorder?.AppendAudio(sampleBuffer);
        }
        catch
        {
            // native callback — never let a managed exception escape into ObjC
        }
        finally
        {
            sampleBuffer.Dispose();
        }
    }
}
