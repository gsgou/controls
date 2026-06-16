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
