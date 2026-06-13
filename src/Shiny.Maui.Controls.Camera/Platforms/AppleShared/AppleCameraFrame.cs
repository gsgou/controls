using System.Runtime.InteropServices;
using CoreGraphics;
using CoreVideo;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A <see cref="CameraFrame"/> backed by a managed copy of the frame's BGRA pixels. The bytes are copied
/// synchronously from the <see cref="CVPixelBuffer"/> inside the capture callback, so the native sample
/// buffer can be released immediately and async analyzers operate on a stable buffer. Native analyzers
/// (Apple Vision) rebuild a <see cref="CGImage"/> via <see cref="ToCGImage"/>.
/// </summary>
public sealed class AppleCameraFrame : CameraFrame
{
    readonly byte[] bgra;

    public AppleCameraFrame(CVPixelBuffer pixelBuffer, int rotation, bool mirrored)
    {
        this.Width = (int)pixelBuffer.Width;
        this.Height = (int)pixelBuffer.Height;
        this.Rotation = rotation;
        this.IsMirrored = mirrored;

        pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            var bytesPerRow = (int)pixelBuffer.BytesPerRow;
            int w = this.Width, h = this.Height;
            this.bgra = new byte[w * h * 4];
            var baseAddr = pixelBuffer.BaseAddress;
            for (var row = 0; row < h; row++)
                Marshal.Copy(baseAddr + row * bytesPerRow, this.bgra, row * w * 4, w * 4);
        }
        finally
        {
            pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    public override int Width { get; }
    public override int Height { get; }
    public override int Rotation { get; }
    public override bool IsMirrored { get; }
    public override CameraFrameFormat Format => CameraFrameFormat.Bgra32;

    /// <summary>The raw BGRA pixels (4 bytes/px, row-packed at <see cref="CameraFrame.Width"/>).</summary>
    public byte[] Bgra => this.bgra;

    /// <summary>Build a <see cref="CGImage"/> from the BGRA buffer (for Vision / Core Image analyzers).</summary>
    public CGImage? ToCGImage()
    {
        using var cs = CGColorSpace.CreateDeviceRGB();
        using var ctx = new CGBitmapContext(
            this.bgra, this.Width, this.Height, 8, this.Width * 4, cs,
            (CGBitmapFlags)CGImageAlphaInfo.NoneSkipFirst | CGBitmapFlags.ByteOrder32Little);
        return ctx.ToImage();
    }

    protected override byte[] MaterializeLuminance()
    {
        int w = this.Width, h = this.Height;
        var lum = new byte[w * h];
        var src = this.bgra;
        // BGRA -> Rec.601 luma
        for (var i = 0; i < w * h; i++)
        {
            var b = src[i * 4];
            var g = src[i * 4 + 1];
            var r = src[i * 4 + 2];
            lum[i] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
        }
        return lum;
    }
}
