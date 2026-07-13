using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Camera;

/// <summary>
/// Draws an overlay that is composited (burned) into every recorded video frame. Unlike the on-screen
/// <c>CameraOverlayView</c> — which only paints the live preview — whatever this renderer draws ends up in
/// the saved <c>.mov</c>/<c>.mp4</c> file (watermark, timestamp, telemetry, reticles, etc.).
/// </summary>
/// <remarks>
/// <para><b>Threading:</b> <see cref="DrawOverlay"/> is invoked once per encoded frame on a capture/encoder
/// thread — <b>never</b> the UI thread. A renderer that reflects mutable UI state must read it through a
/// <c>volatile</c> field or an immutable snapshot; do not touch UI objects from inside it.</para>
/// <para><b>Coordinate space:</b> draw in the frame's pixel space — the origin is the top-left of the frame
/// and the drawable area is <c>(0,0)..(context.Width, context.Height)</c>. Frames are delivered upright and,
/// for the front camera, already un-mirrored, so text/logos render the right way round. When mapping
/// normalized (0..1) analyzer geometry, multiply by <c>context.Width</c>/<c>context.Height</c>.</para>
/// </remarks>
public interface IVideoOverlayRenderer
{
    /// <summary>Paint the overlay for one encoded frame.</summary>
    /// <param name="canvas">Canvas over the encoded frame; draw in pixel space.</param>
    /// <param name="frame">The frame bounds in pixels (<c>0,0,Width,Height</c>).</param>
    /// <param name="context">Timing / size / facing metadata for this frame.</param>
    void DrawOverlay(ICanvas canvas, RectF frame, VideoOverlayContext context);
}


/// <summary>Per-frame metadata passed to <see cref="IVideoOverlayRenderer.DrawOverlay"/>.</summary>
/// <param name="Elapsed">Time since recording started, from the frame's presentation timestamp.</param>
/// <param name="FrameIndex">Zero-based index of this frame within the recording.</param>
/// <param name="Width">Encoded frame width in pixels.</param>
/// <param name="Height">Encoded frame height in pixels.</param>
/// <param name="Facing">Which camera produced the frame (front frames arrive already un-mirrored).</param>
public readonly record struct VideoOverlayContext(
    TimeSpan Elapsed,
    long FrameIndex,
    int Width,
    int Height,
    CameraFacing Facing
);


/// <summary>Adapts an inline draw delegate to <see cref="IVideoOverlayRenderer"/>.</summary>
/// <remarks>The delegate runs off the UI thread once per frame — see <see cref="IVideoOverlayRenderer"/>.</remarks>
public sealed class DelegateVideoOverlay : IVideoOverlayRenderer
{
    readonly Action<ICanvas, RectF, VideoOverlayContext> draw;

    /// <param name="draw">Invoked per encoded frame to paint the overlay.</param>
    public DelegateVideoOverlay(Action<ICanvas, RectF, VideoOverlayContext> draw)
        => this.draw = draw ?? throw new ArgumentNullException(nameof(draw));

    /// <inheritdoc />
    public void DrawOverlay(ICanvas canvas, RectF frame, VideoOverlayContext context)
        => this.draw(canvas, frame, context);
}


/// <summary>
/// Adapts an existing <see cref="IDrawable"/> (e.g. a <c>CameraOverlayDrawable</c>) so it can be burned into
/// the recording. The drawable is drawn across the full frame rect each frame.
/// </summary>
/// <remarks>The drawable is invoked off the UI thread once per frame — see <see cref="IVideoOverlayRenderer"/>.</remarks>
public sealed class DrawableVideoOverlay : IVideoOverlayRenderer
{
    readonly IDrawable drawable;

    /// <param name="drawable">The drawable to render into every recorded frame.</param>
    public DrawableVideoOverlay(IDrawable drawable)
        => this.drawable = drawable ?? throw new ArgumentNullException(nameof(drawable));

    /// <inheritdoc />
    public void DrawOverlay(ICanvas canvas, RectF frame, VideoOverlayContext context)
        => this.drawable.Draw(canvas, frame);
}
