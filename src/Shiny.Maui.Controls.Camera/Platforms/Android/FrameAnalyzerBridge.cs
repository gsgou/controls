using AndroidX.Camera.Core;

namespace Shiny.Maui.Controls.Camera;

// CameraX ImageAnalysis.Analyzer that wraps each ImageProxy into an AndroidCameraFrame and feeds the
// shared pipeline. The pipeline owns the frame and closes the proxy once all analyzers finish.
sealed class FrameAnalyzerBridge(CameraViewHandler handler) : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    public void Analyze(IImageProxy image)
    {
        if (!handler.Pipeline.HasAnalyzer)
        {
            image.Close();
            return;
        }

        var mirrored = handler.MaybeVirtualView?.Facing == CameraFacing.Front;
        handler.Pipeline.Process(new AndroidCameraFrame(image, mirrored), default);
    }
}
