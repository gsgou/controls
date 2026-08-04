using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Camera;

/// <summary>
/// Per-frame metadata handed to an <see cref="IDrawEffect"/>, including whatever the active analyzer last
/// produced — which is what lets a draw effect anchor itself to something the camera is tracking (a face, a
/// barcode, a document edge) rather than to a fixed position.
/// </summary>
/// <param name="Elapsed">
/// Time since the surface started. For a recording this is measured from the first recorded frame; for the
/// live preview, from when the effect chain was attached.
/// </param>
/// <param name="FrameIndex">Zero-based index of this frame within the current surface.</param>
/// <param name="Width">Frame width in pixels.</param>
/// <param name="Height">Frame height in pixels.</param>
/// <param name="Facing">Which camera produced the frame (front frames arrive already un-mirrored).</param>
/// <param name="Surface">Which surface is being drawn — the same effect runs on all three.</param>
/// <param name="Overlays">
/// The active analyzer's most recent boxes, in normalized upright image space. Empty when no analyzer is
/// assigned. These persist across dropped frames, so they are safe to read every frame.
/// </param>
/// <param name="AnalyzerResult">
/// The active analyzer's most recent typed result, or <c>null</c>. Effects that need richer data than a
/// bounding box downcast this — e.g. a face effect to <c>IReadOnlyList&lt;DetectedFace&gt;</c>. Because the
/// analyzer is chosen by the app, an effect that requires a particular one should say so plainly rather than
/// silently drawing nothing.
/// </param>
public readonly record struct CameraEffectContext(
    TimeSpan Elapsed,
    long FrameIndex,
    int Width,
    int Height,
    CameraFacing Facing,
    CameraSurface Surface,
    IReadOnlyList<OverlayBox> Overlays,
    object? AnalyzerResult
)
{
    /// <summary>The frame bounds in pixels — <c>(0, 0, Width, Height)</c>.</summary>
    public RectF Bounds => new(0, 0, this.Width, this.Height);

    /// <summary>Scale a normalized rect (analyzer space) into this frame's pixel space.</summary>
    public RectF ToPixels(RectF normalized) => new(
        normalized.X * this.Width,
        normalized.Y * this.Height,
        normalized.Width * this.Width,
        normalized.Height * this.Height
    );

    /// <summary>Scale a normalized point (analyzer space) into this frame's pixel space.</summary>
    public PointF ToPixels(PointF normalized) => new(normalized.X * this.Width, normalized.Y * this.Height);
}


/// <summary>Which output an effect is currently being applied to.</summary>
public enum CameraSurface
{
    /// <summary>The live on-screen preview.</summary>
    Preview,

    /// <summary>A still captured via <c>CapturePhotoAsync</c>.</summary>
    Photo,

    /// <summary>A frame being encoded into a video recording.</summary>
    Video
}
