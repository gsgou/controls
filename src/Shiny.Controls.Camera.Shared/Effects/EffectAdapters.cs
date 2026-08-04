using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Camera;

/// <summary>
/// A custom effect built from a colour matrix — the simplest way to ship your own look, and the only kind
/// guaranteed to be honoured on every platform and every surface.
/// </summary>
/// <example>
/// <code>
/// // knock the greens back and lift the shadows
/// camera.Effects.Add(new ColorEffect("my.look", new ColorMatrix4x5([
///     1.0f, 0,    0,    0, 0.02f,
///     0,    0.9f, 0,    0, 0.02f,
///     0,    0,    1.0f, 0, 0.02f,
///     0,    0,    0,    1, 0
/// ])));
/// </code>
/// </example>
public sealed class ColorEffect : IColorEffect
{
    /// <param name="id">Stable identifier for this effect.</param>
    /// <param name="matrix">The matrix applied to every pixel.</param>
    public ColorEffect(string id, ColorMatrix4x5 matrix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(matrix);

        this.Id = id;
        this.ColorMatrix = matrix;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public ColorMatrix4x5 ColorMatrix { get; }
}


/// <summary>Adapts an inline draw delegate to <see cref="IDrawEffect"/>.</summary>
/// <remarks>The delegate runs off the UI thread once per frame — see <see cref="IDrawEffect"/>.</remarks>
public sealed class DelegateDrawEffect : IDrawEffect
{
    readonly Action<ICanvas, RectF, CameraEffectContext> draw;

    /// <param name="id">Stable identifier for this effect.</param>
    /// <param name="draw">Invoked per frame to paint the effect.</param>
    public DelegateDrawEffect(string id, Action<ICanvas, RectF, CameraEffectContext> draw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(draw);

        this.Id = id;
        this.draw = draw;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public void Draw(ICanvas canvas, RectF frame, CameraEffectContext context)
        => this.draw(canvas, frame, context);
}


/// <summary>
/// Adapts an existing <see cref="IDrawable"/> (e.g. a <c>CameraOverlayDrawable</c>) into an effect. The
/// drawable is drawn across the full frame rect each frame.
/// </summary>
/// <remarks>The drawable is invoked off the UI thread once per frame — see <see cref="IDrawEffect"/>.</remarks>
public sealed class DrawableEffect : IDrawEffect
{
    readonly IDrawable drawable;

    /// <param name="id">Stable identifier for this effect.</param>
    /// <param name="drawable">The drawable to render into every frame.</param>
    public DrawableEffect(string id, IDrawable drawable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(drawable);

        this.Id = id;
        this.drawable = drawable;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public void Draw(ICanvas canvas, RectF frame, CameraEffectContext context)
        => this.drawable.Draw(canvas, frame);
}


/// <summary>
/// Adapts a legacy <see cref="IVideoOverlayRenderer"/> (recording-only burn-in) into an
/// <see cref="IDrawEffect"/>, so overlay code written against the old interface can be dropped into
/// <c>CameraView.Effects</c> and start drawing on the preview and stills too.
/// </summary>
public sealed class VideoOverlayEffect : IDrawEffect
{
    readonly IVideoOverlayRenderer overlay;

    /// <param name="id">Stable identifier for this effect.</param>
    /// <param name="overlay">The renderer to adapt.</param>
    public VideoOverlayEffect(string id, IVideoOverlayRenderer overlay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(overlay);

        this.Id = id;
        this.overlay = overlay;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public void Draw(ICanvas canvas, RectF frame, CameraEffectContext context)
        => this.overlay.DrawOverlay(canvas, frame, new VideoOverlayContext(
            context.Elapsed, context.FrameIndex, context.Width, context.Height, context.Facing));
}
