using AppKit;
using AVFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

// macOS equivalent of VideoFrameDelegate: filters into an NSImageView overlay and feeds the pipeline.
sealed class MacVideoFrameDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
{
    readonly CIContext context = new();
    readonly WeakReference<NSImageView> filterTarget;

    public MacVideoFrameDelegate(NSImageView filterTarget)
        => this.filterTarget = new WeakReference<NSImageView>(filterTarget);

    public volatile CIFilter? Filter;
    public Func<bool>? WantFrames;
    public Action<AppleCameraFrame>? OnFrame;
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
                this.OnFrame(new AppleCameraFrame(pixelBuffer, rotation: 0, mirrored: this.Mirrored));
        }
        finally
        {
            sampleBuffer.Dispose();
        }
    }

    void RenderFiltered(CIFilter filter, CVPixelBuffer pixelBuffer, NSImageView view)
    {
        using var input = new CIImage(pixelBuffer);
        filter.SetValueForKey(input, new NSString("inputImage"));

        using var output = filter.OutputImage;
        if (output == null)
            return;

        var cg = this.context.CreateCGImage(output, output.Extent);
        if (cg == null)
            return;

        var image = new NSImage(cg, new CGSize(cg.Width, cg.Height));
        cg.Dispose();
        view.BeginInvokeOnMainThread(() => view.Image = image);
    }
}
