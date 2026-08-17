namespace Shiny.Controls.Camera;

/// <summary>
/// A camera frame handed to analyzers. Native analyzers downcast to the platform-specific subclass
/// (e.g. <c>AppleCameraFrame.SampleBuffer</c>, <c>AndroidCameraFrame.ImageProxy</c>,
/// <c>WindowsCameraFrame.SoftwareBitmap</c>) to consume the native buffer with zero copies. Luminance-based
/// analyzers (motion) call <see cref="GetLuminance"/> to get a lazily materialized, cached 8-bit grayscale
/// plane in native (un-rotated) image space.
/// </summary>
/// <remarks>
/// The frame is only valid for the duration of <see cref="IFrameAnalyzer.AnalyzeAsync"/>; the pipeline
/// disposes it (releasing the pooled native buffer) immediately afterwards. Do not retain it.
/// </remarks>
public abstract class CameraFrame : IDisposable
{
    /// <summary>Buffer width in pixels (before rotation is applied).</summary>
    public abstract int Width { get; }

    /// <summary>Buffer height in pixels (before rotation is applied).</summary>
    public abstract int Height { get; }

    /// <summary>Clockwise degrees (0/90/180/270) to rotate the buffer so the scene is upright.</summary>
    public abstract int Rotation { get; }

    /// <summary><c>true</c> when the image is horizontally mirrored (front camera).</summary>
    public abstract bool IsMirrored { get; }

    /// <summary>The native pixel layout of the underlying buffer.</summary>
    public abstract CameraFrameFormat Format { get; }

    byte[]? luminance;
    int refCount = 1;

    /// <summary>
    /// Returns an 8-bit luminance plane for the frame in native (un-rotated) image space, sized
    /// <see cref="Width"/> x <see cref="Height"/>. Materialized once and cached for the frame's lifetime.
    /// </summary>
    public ReadOnlySpan<byte> GetLuminance()
    {
        this.luminance ??= this.MaterializeLuminance();
        return this.luminance;
    }

    /// <summary>Produce the luminance plane from the native buffer. Called at most once per frame.</summary>
    protected abstract byte[] MaterializeLuminance();

    /// <summary>
    /// Fill <paramref name="destination"/> with an evenly spaced <paramref name="columns"/> x
    /// <paramref name="rows"/> grid of luminance samples across the whole frame, row-major.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For consumers that want a <i>statistic</i> rather than an image — an exposure or ambient-light
    /// average, say. <see cref="GetLuminance"/> materializes the entire plane, which at 1080p is a 2 MB
    /// array on the Large Object Heap converted pixel by pixel; reading a 32x32 grid off it to produce one
    /// number pays two million reads for a thousand. Platforms override this to sample the native buffer
    /// directly and allocate nothing.
    /// </para>
    /// <para>
    /// The base implementation goes through <see cref="GetLuminance"/>, so a platform that has not
    /// overridden it is correct, just not cheap.
    /// </para>
    /// </remarks>
    public virtual void SampleLuminance(Span<byte> destination, int columns, int rows)
    {
        var plane = this.GetLuminance();
        for (var r = 0; r < rows; r++)
        {
            var y = SampleCoordinate(r, rows, this.Height);
            for (var c = 0; c < columns; c++)
                destination[r * columns + c] = plane[y * this.Width + SampleCoordinate(c, columns, this.Width)];
        }
    }

    /// <summary>
    /// Where the <paramref name="index"/>th of <paramref name="count"/> samples lands across
    /// <paramref name="extent"/> pixels.
    /// </summary>
    /// <remarks>
    /// Half a step in, so the grid is centred rather than hugging the top-left edge, and clamped so the
    /// last sample cannot run off the end. Defined here so every platform override grids identically —
    /// two platforms sampling different pixels would answer differently for the same scene.
    /// </remarks>
    protected static int SampleCoordinate(int index, int count, int extent)
        => Math.Min((int)((index + 0.5) / count * extent), extent - 1);

    /// <summary>
    /// Increment the reference count so the frame survives while another consumer holds it. The pipeline
    /// retains once per analyzer that accepts the frame; each balances with a <see cref="Dispose"/>.
    /// </summary>
    internal CameraFrame Retain()
    {
        Interlocked.Increment(ref this.refCount);
        return this;
    }

    /// <summary>Release a reference. When the last reference is released the native buffer is freed.</summary>
    public void Dispose()
    {
        if (Interlocked.Decrement(ref this.refCount) > 0)
            return;

        this.luminance = null;
        this.ReleaseNative();
        GC.SuppressFinalize(this);
    }

    /// <summary>Free the underlying native buffer (close the ImageProxy, dispose the sample buffer, …).</summary>
    protected virtual void ReleaseNative() { }
}
