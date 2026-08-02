using AVFoundation;
using AudioToolbox;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace Shiny.Maui.Controls.Camera;

// Owns an AVAssetWriter that burns an IVideoOverlayRenderer into every recorded frame. Shared by the
// iOS/MacCatalyst and macOS handlers when VideoRecordingOptions.Overlay is set; the raw-feed fast path uses
// AVCaptureMovieFileOutput instead. Frames/audio arrive off the capture queues, so all state is guarded.
//
// The incoming BGRA CVPixelBuffer is composited in place — we wrap it in a CGBitmapContext, flip to the
// UIKit top-left origin MAUI Graphics expects, invoke DrawOverlay, then append the (now-composited) sample
// buffer straight to the video input (no separate pixel-buffer pool needed). Frames arrive already upright
// and front-mirror-corrected via the handler's OrientConnections, so overlay text renders the right way up.
sealed class AppleVideoOverlayRecorder
{
    readonly IVideoOverlayRenderer overlay;
    readonly CameraFacing facing;
    readonly string path;
    readonly bool includeAudio;

    /// <summary>
    /// Target video bitrate, or null for AVFoundation's default for the dimensions.
    /// </summary>
    /// <remarks>
    /// This path has to be given the bitrate explicitly because it does not go through
    /// <c>AVCaptureMovieFileOutput</c> — it owns its own <c>AVAssetWriter</c>. Leaving it unset was a real
    /// inconsistency rather than a gap: with an overlay attached the encode silently changed codec *and*
    /// bitrate compared to the same recording without one, so toggling a burn-in overlay changed the size and
    /// fidelity of the output for reasons nothing in the API hinted at.
    /// </remarks>
    readonly int? videoBitrate;

    readonly object gate = new();
    readonly TaskCompletionSource<CameraVideo> tcs = new();

    AVAssetWriter? writer;
    AVAssetWriterInput? videoInput;
    AVAssetWriterInput? audioInput;
    CGColorSpace? colorSpace;
    CMTime startPts = CMTime.Invalid;
    CMTime lastPts = CMTime.Invalid;
    long frameIndex;
    bool finished;

    public AppleVideoOverlayRecorder(
        string path,
        bool includeAudio,
        CameraFacing facing,
        IVideoOverlayRenderer overlay,
        int? videoBitrate = null
    )
    {
        this.path = path;
        this.includeAudio = includeAudio;
        this.facing = facing;
        this.overlay = overlay;
        this.videoBitrate = videoBitrate;
    }

    public Task<CameraVideo> Task => this.tcs.Task;

    // Called on the video capture queue for every frame while recording. Lazily starts the writer on the
    // first frame (dimensions are read from the buffer), then composites + appends.
    public void AppendVideo(CMSampleBuffer sampleBuffer)
    {
        if (Volatile.Read(ref this.finished))
            return;

        using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
        if (pixelBuffer == null)
            return;

        var pts = sampleBuffer.PresentationTimeStamp;
        lock (this.gate)
        {
            if (this.finished)
                return;
            if (this.writer == null && !this.TryStart((int)pixelBuffer.Width, (int)pixelBuffer.Height, pts))
                return;
            if (this.videoInput is not { ReadyForMoreMediaData: true })
                return; // encoder backpressure — drop this frame (lowers fps rather than desyncs)

            this.Composite(pixelBuffer, pts);
            this.videoInput.AppendSampleBuffer(sampleBuffer);
            this.lastPts = pts;
            this.frameIndex++;
        }
    }

    // Called on the audio capture queue. Dropped until the writer session has started off a video frame.
    public void AppendAudio(CMSampleBuffer sampleBuffer)
    {
        lock (this.gate)
        {
            if (this.finished || this.writer == null || this.startPts == CMTime.Invalid)
                return;
            if (this.audioInput is { ReadyForMoreMediaData: true })
                this.audioInput.AppendSampleBuffer(sampleBuffer);
        }
    }

    bool TryStart(int width, int height, CMTime pts)
    {
        try
        {
            var url = NSUrl.FromFilename(this.path);
            this.writer = new AVAssetWriter(url, AVFileTypes.QuickTimeMovie.GetConstant()!, out var err);
            if (err != null || this.writer == null)
            {
                this.Fail(err?.LocalizedDescription ?? "Could not create AVAssetWriter");
                return false;
            }

            var videoSettings = new AVVideoSettingsCompressed
            {
                Codec = AVVideoCodec.H264,
                Width = width,
                Height = height
            };

            if (this.videoBitrate is > 0 and var bitrate)
            {
                videoSettings.CodecSettings = new AVVideoCodecSettings
                {
                    AverageBitRate = bitrate
                };
            }
            this.videoInput = new AVAssetWriterInput(AVMediaTypes.Video.GetConstant()!, videoSettings)
            {
                ExpectsMediaDataInRealTime = true
            };
            if (this.writer.CanAddInput(this.videoInput))
                this.writer.AddInput(this.videoInput);

            if (this.includeAudio)
            {
                var audioSettings = new AudioSettings
                {
                    Format = AudioFormatType.MPEG4AAC,
                    SampleRate = 44100,
                    NumberChannels = 1,
                    EncoderBitRate = 64000
                };
                this.audioInput = new AVAssetWriterInput(AVMediaTypes.Audio.GetConstant()!, audioSettings)
                {
                    ExpectsMediaDataInRealTime = true
                };
                if (this.writer.CanAddInput(this.audioInput))
                    this.writer.AddInput(this.audioInput);
            }

            if (!this.writer.StartWriting())
            {
                this.Fail(this.writer.Error?.LocalizedDescription ?? "AVAssetWriter failed to start");
                return false;
            }
            this.writer.StartSessionAtSourceTime(pts);
            this.startPts = pts;
            return true;
        }
        catch (Exception ex)
        {
            this.Fail(ex.Message);
            return false;
        }
    }

    void Composite(CVPixelBuffer pixelBuffer, CMTime pts)
    {
        // read-write lock (flags 0) so DrawOverlay writes land in the buffer we then encode
        pixelBuffer.Lock((CVPixelBufferLock)0);
        try
        {
            int w = (int)pixelBuffer.Width, h = (int)pixelBuffer.Height;
            var bytesPerRow = (int)pixelBuffer.BytesPerRow;
            this.colorSpace ??= CGColorSpace.CreateDeviceRGB();

            using var ctx = new CGBitmapContext(
                pixelBuffer.BaseAddress, w, h, 8, bytesPerRow, this.colorSpace,
                (CGBitmapFlags)CGImageAlphaInfo.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little);

            // CGBitmapContext is bottom-left origin; flip to the UIKit top-left convention MAUI Graphics assumes
            ctx.TranslateCTM(0, h);
            ctx.ScaleCTM(1, -1);

            var canvas = new PlatformCanvas(() => this.colorSpace!) { Context = ctx, DisplayScale = 1 };
            var elapsed = this.startPts == CMTime.Invalid
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds(CMTime.Subtract(pts, this.startPts).Seconds);

            this.overlay.DrawOverlay(canvas, new RectF(0, 0, w, h),
                new VideoOverlayContext(elapsed, this.frameIndex, w, h, this.facing));
        }
        finally
        {
            pixelBuffer.Unlock((CVPixelBufferLock)0);
        }
    }

    public Task<CameraVideo> FinishAsync()
    {
        AVAssetWriter? w;
        CMTime end;
        lock (this.gate)
        {
            if (this.finished)
                return this.tcs.Task;
            this.finished = true;
            w = this.writer;
            end = this.lastPts;
            this.videoInput?.MarkAsFinished();
            this.audioInput?.MarkAsFinished();
        }

        if (w == null)
        {
            this.tcs.TrySetException(new InvalidOperationException("No frames were recorded"));
            return this.tcs.Task;
        }

        if (end != CMTime.Invalid)
            w.EndSessionAtSourceTime(end);

        w.FinishWriting(() =>
        {
            if (w.Status == AVAssetWriterStatus.Completed)
            {
                var duration = this.startPts != CMTime.Invalid && end != CMTime.Invalid
                    ? TimeSpan.FromSeconds(CMTime.Subtract(end, this.startPts).Seconds)
                    : (TimeSpan?)null;
                this.tcs.TrySetResult(new CameraVideo(this.path, duration));
            }
            else
            {
                this.tcs.TrySetException(new InvalidOperationException(
                    w.Error?.LocalizedDescription ?? "AVAssetWriter did not finish"));
            }
        });
        return this.tcs.Task;
    }

    void Fail(string message)
    {
        this.finished = true;
        this.tcs.TrySetException(new InvalidOperationException(message));
    }
}
