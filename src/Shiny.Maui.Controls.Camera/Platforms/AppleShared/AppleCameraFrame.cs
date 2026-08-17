using System.Runtime.InteropServices;
using CoreGraphics;
using CoreMedia;
using CoreVideo;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A <see cref="CameraFrame"/> over an <c>AVCaptureVideoDataOutput</c> buffer, in one of two modes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Borrowed</b> (<see cref="Borrow"/>) holds the capture's own <see cref="CMSampleBuffer"/> open and
/// reads its pixels in place — no copy at all, which is what <c>AndroidCameraFrame</c> does with its
/// <c>ImageProxy</c>. <b>Copied</b> (<see cref="Copy"/>) takes a managed BGRA snapshot the way this class
/// always did.
/// </para>
/// <para>
/// ⚠️ <b>The mode is not a preference, it is a correctness question, and getting it wrong corrupts an
/// analysis rather than slowing it.</b> <c>AppleVideoOverlayRecorder.Composite</c> renders pixel effects
/// and draws the burn-in overlay <i>back into the very buffer the capture delivered</i>. A borrowed frame
/// handed to an async analyzer would then be read on one thread while the encoder mutates it on another —
/// the analyzer would see the HUD burned across the scene it is reading, torn halfway. So a frame is
/// borrowed only while <b>nothing is going to write to that buffer</b>; with a recorder attached it must be
/// copied. <c>VideoFrameDelegate</c> owns that decision because it is the only thing that knows.
/// </para>
/// <para>
/// <b>Even the copy is now lazy.</b> Every in-tree Apple analyzer (OCR, barcode, face, documents) consumes
/// <see cref="ToCGImage"/>, which builds straight off the pixels — so on the common path nothing
/// materializes <see cref="Bgra"/> at all, in either mode.
/// </para>
/// <para>
/// Holding a capture buffer open is bounded and deliberate: the pipeline runs one analysis at a time and
/// <see cref="Internal.CameraPipeline.WantsFrame"/> refuses a frame while a pass is in flight, so at most
/// one buffer is ever out of the pool. <c>AVCaptureVideoDataOutput</c> drops late frames rather than
/// queueing them, which is the behaviour we want if a pass ever overruns.
/// </para>
/// </remarks>
public sealed class AppleCameraFrame : CameraFrame
{
    readonly CMSampleBuffer? owned;
    readonly CVPixelBuffer pixelBuffer;
    byte[]? bgra;

    AppleCameraFrame(CMSampleBuffer? owned, CVPixelBuffer pixelBuffer, byte[]? bgra, int rotation, bool mirrored)
    {
        this.owned = owned;
        this.pixelBuffer = pixelBuffer;
        this.bgra = bgra;
        this.Width = (int)pixelBuffer.Width;
        this.Height = (int)pixelBuffer.Height;
        this.Rotation = rotation;
        this.IsMirrored = mirrored;
    }

    /// <summary>
    /// Hold the capture buffer open and read it in place. The frame takes ownership of
    /// <paramref name="sampleBuffer"/> and disposes it when the last reference is released — the caller
    /// must not.
    /// </summary>
    /// <remarks>
    /// ⚠️ Only safe while nothing will write to this buffer for the frame's lifetime. See the class remarks.
    /// </remarks>
    public static AppleCameraFrame Borrow(CMSampleBuffer sampleBuffer, CVPixelBuffer pixelBuffer, int rotation, bool mirrored)
        => new(sampleBuffer, pixelBuffer, bgra: null, rotation, mirrored);

    /// <summary>
    /// Take a managed BGRA snapshot, so the frame outlives the capture buffer and is unaffected by anything
    /// written to it afterwards. The caller keeps ownership of its buffers.
    /// </summary>
    public static AppleCameraFrame Copy(CVPixelBuffer pixelBuffer, int rotation, bool mirrored)
        => new(owned: null, pixelBuffer, CopyBgra(pixelBuffer), rotation, mirrored);

    public override int Width { get; }
    public override int Height { get; }
    public override int Rotation { get; }
    public override bool IsMirrored { get; }
    public override CameraFrameFormat Format => CameraFrameFormat.Bgra32;

    /// <summary>
    /// The capture buffer itself, for analyzers that can consume one directly. Valid until the frame is
    /// disposed.
    /// </summary>
    public CVPixelBuffer PixelBuffer => this.pixelBuffer;

    /// <summary>
    /// The raw BGRA pixels (4 bytes/px, row-packed at <see cref="CameraFrame.Width"/>).
    /// </summary>
    /// <remarks>
    /// <b>Materialized on first read, not up front.</b> At 1080p this is an 8.3 MB array and every one of
    /// them lands on the Large Object Heap, so it is worth not allocating for the analyzers that never ask.
    /// Prefer <see cref="ToCGImage"/> or <see cref="PixelBuffer"/>.
    /// </remarks>
    public byte[] Bgra => this.bgra ??= CopyBgra(this.pixelBuffer);

    static byte[] CopyBgra(CVPixelBuffer pixelBuffer)
    {
        pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            int w = (int)pixelBuffer.Width, h = (int)pixelBuffer.Height;
            var bytesPerRow = (int)pixelBuffer.BytesPerRow;
            var bgra = new byte[w * h * 4];
            var baseAddr = pixelBuffer.BaseAddress;
            for (var row = 0; row < h; row++)
                Marshal.Copy(baseAddr + row * bytesPerRow, bgra, row * w * 4, w * 4);

            return bgra;
        }
        finally
        {
            pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    /// <summary>Build a <see cref="CGImage"/> for Vision / Core Image analyzers.</summary>
    /// <remarks>
    /// Built straight off the capture buffer, so the common analyzer path copies the frame exactly once —
    /// into the <see cref="CGImage"/> — instead of into a managed array and then into a bitmap context
    /// wrapped around that array. A frame that has already materialized <see cref="Bgra"/> is built from
    /// it rather than re-locking the buffer.
    /// </remarks>
    public CGImage? ToCGImage()
    {
        using var cs = CGColorSpace.CreateDeviceRGB();
        const CGBitmapFlags flags = (CGBitmapFlags)CGImageAlphaInfo.NoneSkipFirst | CGBitmapFlags.ByteOrder32Little;

        if (this.bgra is { } snapshot)
        {
            using var fromSnapshot = new CGBitmapContext(
                snapshot, this.Width, this.Height, 8, this.Width * 4, cs, flags);
            return fromSnapshot.ToImage();
        }

        this.pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            using var ctx = new CGBitmapContext(
                this.pixelBuffer.BaseAddress, this.Width, this.Height, 8,
                (int)this.pixelBuffer.BytesPerRow, cs, flags);
            return ctx.ToImage();
        }
        finally
        {
            this.pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    /// <summary>
    /// Rec.601 luma, read straight from the capture buffer where the frame is borrowed.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Do not "optimize" this into a sub-sampled plane.</b> The contract is a full
    /// <see cref="CameraFrame.Width"/> x <see cref="CameraFrame.Height"/> plane and motion analyzers index
    /// into it by pixel. A consumer that only wants an average should stride over the result instead.
    /// </remarks>
    protected override byte[] MaterializeLuminance()
    {
        int w = this.Width, h = this.Height;
        var lum = new byte[w * h];

        if (this.bgra is { } snapshot)
        {
            Luma(snapshot, lum, w, h, w * 4);
            return lum;
        }

        this.pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            var bytesPerRow = (int)this.pixelBuffer.BytesPerRow;
            var row = new byte[bytesPerRow];
            var baseAddr = this.pixelBuffer.BaseAddress;

            // One row at a time rather than the whole plane: a reusable row buffer keeps this off the Large
            // Object Heap, where an 8 MB frame-sized array would land on every sample.
            for (var y = 0; y < h; y++)
            {
                Marshal.Copy(baseAddr + y * bytesPerRow, row, 0, bytesPerRow);
                Luma(row, lum, w, 1, bytesPerRow, destOffset: y * w);
            }
            return lum;
        }
        finally
        {
            this.pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    /// <summary>
    /// Reads only the sampled pixels out of the capture buffer — a thousand reads instead of two million,
    /// and no plane allocated at all.
    /// </summary>
    public override void SampleLuminance(Span<byte> destination, int columns, int rows)
    {
        if (this.bgra is { } snapshot)
        {
            SampleFrom(snapshot, destination, columns, rows, this.Width, this.Height, this.Width * 4);
            return;
        }

        this.pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            var bytesPerRow = (int)this.pixelBuffer.BytesPerRow;
            var baseAddr = this.pixelBuffer.BaseAddress;
            var px = new byte[4];

            for (var r = 0; r < rows; r++)
            {
                var y = SampleCoordinate(r, rows, this.Height);
                for (var c = 0; c < columns; c++)
                {
                    var x = SampleCoordinate(c, columns, this.Width);
                    Marshal.Copy(baseAddr + y * bytesPerRow + x * 4, px, 0, 4);
                    destination[r * columns + c] = (byte)((px[2] * 77 + px[1] * 150 + px[0] * 29) >> 8);
                }
            }
        }
        finally
        {
            this.pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    static void SampleFrom(byte[] src, Span<byte> dest, int columns, int rows, int w, int h, int stride)
    {
        for (var r = 0; r < rows; r++)
        {
            var y = SampleCoordinate(r, rows, h);
            for (var c = 0; c < columns; c++)
            {
                var i = y * stride + SampleCoordinate(c, columns, w) * 4;
                dest[r * columns + c] = (byte)((src[i + 2] * 77 + src[i + 1] * 150 + src[i] * 29) >> 8);
            }
        }
    }

    static void Luma(byte[] src, byte[] dest, int w, int h, int srcStride, int destOffset = 0)
    {
        for (var y = 0; y < h; y++)
        {
            var s = y * srcStride;
            var d = destOffset + y * w;
            for (var x = 0; x < w; x++)
            {
                var b = src[s + x * 4];
                var g = src[s + x * 4 + 1];
                var r = src[s + x * 4 + 2];
                dest[d + x] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
            }
        }
    }

    protected override void ReleaseNative()
    {
        this.bgra = null;

        // Only a borrowed frame owns anything. A copied one was handed buffers the delegate still owns and
        // disposes itself, so releasing them here would be a double free.
        if (this.owned is null)
            return;

        this.pixelBuffer.Dispose();
        this.owned.Dispose();
    }
}
