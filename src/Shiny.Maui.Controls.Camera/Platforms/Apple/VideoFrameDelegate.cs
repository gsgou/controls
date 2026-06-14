using AVFoundation;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;

namespace Shiny.Maui.Controls.Camera;

// Single sample-buffer delegate for the AVCaptureVideoDataOutput. Does two jobs per frame:
//   1. when a CIFilter is set, render the filtered frame into the overlay UIImageView;
//   2. when frames are wanted, hand a managed AppleCameraFrame to the analysis pipeline.
sealed class VideoFrameDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
{
    readonly CIContext context = new();
    readonly WeakReference<UIImageView> filterTarget;

    public VideoFrameDelegate(UIImageView filterTarget)
        => this.filterTarget = new WeakReference<UIImageView>(filterTarget);

    public volatile CIFilter? Filter;

    /// <summary>Returns true while frames should be wrapped and pushed to the pipeline.</summary>
    public Func<bool>? WantFrames;

    /// <summary>Receives each wrapped frame (off the capture queue). The callee owns/disposes it.</summary>
    public Action<AppleCameraFrame>? OnFrame;

    /// <summary>Invoked (off the capture queue) when a frame raises an exception, so it can be surfaced.</summary>
    public Action<Exception>? OnError;

    /// <summary>Set by the handler so wrapped frames carry mirroring metadata.</summary>
    public volatile bool Mirrored;

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
            if (pixelBuffer == null)
                return;

            var filter = this.Filter;
            if (filter != null && this.filterTarget.TryGetTarget(out var view))
                this.RenderFiltered(filter, pixelBuffer, view);

            if (this.OnFrame != null && this.WantFrames?.Invoke() == true)
            {
                var frame = new AppleCameraFrame(pixelBuffer, rotation: 0, mirrored: this.Mirrored);
                this.OnFrame(frame);
            }
        }
        catch (Exception ex)
        {
            // This is a native AVFoundation callback — a managed exception must never escape into ObjC or
            // the app hard-crashes. Swallow per-frame failures (report the first) and keep the camera alive.
            this.OnError?.Invoke(ex);
        }
        finally
        {
            sampleBuffer.Dispose();
        }
    }

    void RenderFiltered(CIFilter filter, CVPixelBuffer pixelBuffer, UIImageView view)
    {
        using var input = new CIImage(pixelBuffer);
        filter.SetValueForKey(input, new NSString("inputImage"));

        using var output = filter.OutputImage;
        if (output == null)
            return;

        var cg = this.context.CreateCGImage(output, output.Extent);
        if (cg == null)
            return;

        var image = UIImage.FromImage(cg);
        cg.Dispose();
        view.BeginInvokeOnMainThread(() => view.Image = image);
    }
}
