using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Presents the chain's <see cref="IDrawEffect"/>s (and an optional legacy
/// <see cref="IVideoOverlayRenderer"/>) as a single renderer, so a platform that already has burn-in overlay
/// machinery can composite effects into a recording without learning about the effect model.
/// </summary>
/// <remarks>
/// Used by the Android path, whose <c>OverlayEffect</c> takes exactly one renderer. Draw effects paint first,
/// in chain order; the per-recording overlay paints last, on top.
/// </remarks>
sealed class EffectVideoOverlay : IVideoOverlayRenderer
{
    readonly IReadOnlyList<IDrawEffect> effects;
    readonly IVideoOverlayRenderer? overlay;
    readonly Func<(IReadOnlyList<OverlayBox> Overlays, object? Result)>? snapshot;

    EffectVideoOverlay(
        IReadOnlyList<IDrawEffect> effects,
        IVideoOverlayRenderer? overlay,
        Func<(IReadOnlyList<OverlayBox>, object?)>? snapshot)
    {
        this.effects = effects;
        this.overlay = overlay;
        this.snapshot = snapshot;
    }

    /// <summary>
    /// Combine a chain and a legacy overlay into one renderer, or return <c>null</c> when there is nothing to
    /// draw — which is the signal to keep using the platform's cheap raw-feed recording path.
    /// </summary>
    public static IVideoOverlayRenderer? Create(
        CameraEffectChain chain,
        IVideoOverlayRenderer? overlay,
        Func<(IReadOnlyList<OverlayBox>, object?)>? snapshot)
    {
        if (chain.DrawEffects.Count == 0)
            return overlay;

        return new EffectVideoOverlay(chain.DrawEffects, overlay, snapshot);
    }

    public void DrawOverlay(ICanvas canvas, RectF frame, VideoOverlayContext context)
    {
        var (overlays, result) = this.snapshot?.Invoke() ?? ([], null);
        var effectContext = new CameraEffectContext(
            context.Elapsed, context.FrameIndex, context.Width, context.Height, context.Facing,
            CameraSurface.Video, overlays, result);

        foreach (var effect in this.effects)
        {
            canvas.SaveState();
            try
            {
                effect.Draw(canvas, frame, effectContext);
            }
            catch (Exception)
            {
                // a faulting effect must not abort the encode — skip it and keep recording
            }
            finally
            {
                canvas.RestoreState();
            }
        }

        this.overlay?.DrawOverlay(canvas, frame, context);
    }
}
