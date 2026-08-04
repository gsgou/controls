using AVFoundation;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;

namespace Shiny.Maui.Controls.Camera;

// Single sample-buffer delegate for the AVCaptureVideoDataOutput. Does two jobs per frame:
//   1. when an effect chain is set, render the filtered frame into the overlay UIImageView;
//   2. when frames are wanted, hand a managed AppleCameraFrame to the analysis pipeline.
sealed class VideoFrameDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
{
    // GPU-backed where we can get a Metal device: the default CIContext can fall back to a CPU renderer, which
    // is survivable for a colour matrix and hopeless for a convolution like the comic/sketch looks.
    readonly CIContext context = CreateContext();
    readonly WeakReference<UIImageView> filterTarget;

    static CIContext CreateContext()
    {
        try
        {
            if (Metal.MTLDevice.SystemDefault is { } device)
                return CIContext.FromMetalDevice(device);
        }
        catch (Exception)
        {
            // no Metal device (some simulators) — fall through to the default renderer
        }

        return new CIContext();
    }

    public VideoFrameDelegate(UIImageView filterTarget)
        => this.filterTarget = new WeakReference<UIImageView>(filterTarget);

    // The whole chain is swapped as one array reference so a frame never renders through a half-updated
    // chain — volatile gives us the publication barrier without locking the capture queue.
    volatile CIFilter[] filters = [];

    public CIFilter[] Filters
    {
        get => this.filters;
        set => this.filters = value ?? [];
    }

    /// <summary>Returns true while frames should be wrapped and pushed to the pipeline.</summary>
    public Func<bool>? WantFrames;

    /// <summary>Receives each wrapped frame (off the capture queue). The callee owns/disposes it.</summary>
    public Action<AppleCameraFrame>? OnFrame;

    /// <summary>Invoked (off the capture queue) when a frame raises an exception, so it can be surfaced.</summary>
    public Action<Exception>? OnError;

    /// <summary>Set by the handler so wrapped frames carry mirroring metadata.</summary>
    public volatile bool Mirrored;

    /// <summary>When set, each frame is composited with the overlay and appended to the burn-in recording.</summary>
    public volatile AppleVideoOverlayRecorder? Recorder;

    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
            if (pixelBuffer == null)
                return;

            var chain = this.filters;
            if (chain.Length > 0 && this.filterTarget.TryGetTarget(out var view))
                this.RenderFiltered(chain, pixelBuffer, view);

            if (this.OnFrame != null && this.WantFrames?.Invoke() == true)
            {
                var frame = new AppleCameraFrame(pixelBuffer, rotation: 0, mirrored: this.Mirrored);
                this.OnFrame(frame);
            }

            // composite + encode LAST, so analyzers/preview see the clean frame and only the file gets the overlay
            this.Recorder?.AppendVideo(sampleBuffer);
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

    // Reused across frames so a steady-state render allocates nothing here.
    readonly List<CIImage> produced = [];

    void RenderFiltered(CIFilter[] chain, CVPixelBuffer pixelBuffer, UIImageView view)
    {
        using var input = new CIImage(pixelBuffer);

        // One render for the whole chain: Core Image concatenates the recipe and evaluates it once below, so
        // stacking N effects does not cost N passes over the frame.
        this.produced.Clear();
        var output = AppleCameraFilters.Apply(input, chain, this.produced);

        try
        {
            if (output == null)
                return;

            // Render at the SOURCE extent, not the filter's own. Several Core Image filters report an
            // infinite or grown extent — CIPixellate is defined over the whole plane, CIGaussianBlur bleeds
            // outward — and asking for that either fails outright (a null CGImage, so the preview shows
            // nothing) or hands back an image that no longer matches the frame. A preview always wants
            // exactly the frame it was given.
            var cg = this.context.CreateCGImage(output, input.Extent);
            if (cg == null)
                return;

            var image = UIImage.FromImage(cg);
            cg.Dispose();
            this.Publish(image, view);
        }
        finally
        {
            // only now that the render has actually evaluated the recipe
            foreach (var image in this.produced)
                image.Dispose();

            this.produced.Clear();
        }
    }

    UIImage? pending;
    int updateQueued;

    // Hand the newest frame to the UI without flooding the main thread.
    //
    // Posting a block per delivered frame is fine while the filter is cheap, but a heavy one (comic, sketch)
    // renders slower than the capture interval, and the main thread then falls behind a queue that only grows
    // — the whole UI, preview included, appears to lock up. Instead we keep exactly one pending image and one
    // queued update: late frames replace the pending image rather than adding to a backlog, so the main thread
    // always does at most one assignment per turn and naturally renders the most recent frame it can.
    void Publish(UIImage image, UIImageView view)
    {
        // the superseded image was never handed to UIKit, so we still own it
        Interlocked.Exchange(ref this.pending, image)?.Dispose();

        if (Interlocked.Exchange(ref this.updateQueued, 1) == 1)
            return;

        view.BeginInvokeOnMainThread(() =>
        {
            Interlocked.Exchange(ref this.updateQueued, 0);

            var next = Interlocked.Exchange(ref this.pending, null);
            if (next is not null)
                view.Image = next; // UIKit retains it and releases whatever it replaced
        });
    }
}
