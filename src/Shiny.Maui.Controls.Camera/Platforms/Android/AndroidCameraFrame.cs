using AndroidX.Camera.Core;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A <see cref="CameraFrame"/> backed by a CameraX <see cref="IImageProxy"/> (YUV_420_888). The proxy is
/// held open until the last reference is released, so native analyzers (MLKit) can consume
/// <see cref="Proxy"/> directly with zero copies; the managed luminance path reads plane 0 (the Y plane).
/// </summary>
public sealed class AndroidCameraFrame(IImageProxy proxy, bool mirrored) : CameraFrame
{
    /// <summary>The underlying CameraX frame. Valid until the frame is disposed. Do not close it yourself.</summary>
    public IImageProxy Proxy { get; } = proxy;

    public override int Width => this.Proxy.Width;
    public override int Height => this.Proxy.Height;
    public override int Rotation => this.Proxy.ImageInfo.RotationDegrees;
    public override bool IsMirrored => mirrored;
    public override CameraFrameFormat Format => CameraFrameFormat.Yuv420;

    protected override byte[] MaterializeLuminance()
    {
        var plane = this.Proxy.GetPlanes()![0];
        var buffer = plane.Buffer!;
        var rowStride = plane.RowStride;
        int w = this.Width, h = this.Height;
        var lum = new byte[w * h];

        if (rowStride == w)
        {
            buffer.Get(lum, 0, w * h);
        }
        else
        {
            for (var row = 0; row < h; row++)
            {
                buffer.Position(row * rowStride);
                buffer.Get(lum, row * w, w);
            }
        }
        return lum;
    }

    /// <summary>
    /// Reads the sampled pixels straight out of the Y plane — no 2 MB plane materialized to answer a
    /// question a thousand bytes can.
    /// </summary>
    public override void SampleLuminance(Span<byte> destination, int columns, int rows)
    {
        var plane = this.Proxy.GetPlanes()![0];
        var buffer = plane.Buffer!;
        var rowStride = plane.RowStride;
        var pixelStride = plane.PixelStride;
        var one = new byte[1];

        for (var r = 0; r < rows; r++)
        {
            var y = SampleCoordinate(r, rows, this.Height);
            for (var c = 0; c < columns; c++)
            {
                var x = SampleCoordinate(c, columns, this.Width);
                buffer.Position(y * rowStride + x * pixelStride);
                buffer.Get(one, 0, 1);
                destination[r * columns + c] = one[0];
            }
        }
    }

    protected override void ReleaseNative() => this.Proxy.Close();
}
