namespace Shiny.Controls.Camera;

/// <summary>
/// An immutable, ordered snapshot of the effects to apply to one frame — taken on the UI thread when
/// <c>CameraView.Effects</c> or <c>CameraView.Filter</c> changes, then read from capture/encoder threads
/// without locking.
/// </summary>
/// <remarks>
/// Snapshotting is the point: the effect list is app-mutable at any moment, but a frame must be rendered
/// against one consistent set of effects. Backends hold onto a chain instance and swap it wholesale.
/// </remarks>
public sealed class CameraEffectChain
{
    /// <summary>An empty chain — nothing to apply.</summary>
    public static CameraEffectChain Empty { get; } = new([]);

    CameraEffectChain(IReadOnlyList<ICameraEffect> effects)
    {
        this.Effects = effects;
        this.DrawEffects = [.. effects.OfType<IDrawEffect>()];
        this.CaptureEffects = [.. effects.OfType<ICaptureEffect>()];
    }

    /// <summary>The enabled effects, in application order.</summary>
    public IReadOnlyList<ICameraEffect> Effects { get; }

    /// <summary>The subset that composites over the frame, in order.</summary>
    public IReadOnlyList<IDrawEffect> DrawEffects { get; }

    /// <summary>The subset applied asynchronously to captured stills, in order.</summary>
    public IReadOnlyList<ICaptureEffect> CaptureEffects { get; }

    /// <summary><c>true</c> when there is nothing at all to apply.</summary>
    public bool IsEmpty => this.Effects.Count == 0;

    /// <summary><c>true</c> when at least one effect changes pixels before compositing.</summary>
    public bool HasPixelEffects => this.Effects.Any(e => e is IColorEffect or INativeEffect);

    /// <summary>
    /// Build a chain from the control's current state. <paramref name="filter"/> is materialized as the
    /// <b>first</b> effect, so setting both <c>Filter</c> and <c>Effects</c> has a defined order rather than
    /// depending on which was assigned last.
    /// </summary>
    /// <param name="filter">The legacy <c>CameraView.Filter</c> value.</param>
    /// <param name="effects">The <c>CameraView.Effects</c> collection, or <c>null</c>.</param>
    public static CameraEffectChain Create(CameraFilter filter, IEnumerable<ICameraEffect>? effects)
    {
        var list = new List<ICameraEffect>();

        if (CameraEffects.For(filter) is { } builtIn)
            list.Add(builtIn);

        if (effects is not null)
            list.AddRange(effects.Where(e => e is not null && e.IsEnabled));

        return list.Count == 0 ? Empty : new CameraEffectChain(list);
    }

    /// <summary>
    /// Resolve the chain into an ordered render plan for one backend.
    /// </summary>
    /// <param name="isHandledNatively">
    /// Returns <c>true</c> when this backend has a native program for the effect (a Core Image filter, an AGSL
    /// shader, an SVG filter, a managed pass — whichever this backend consumes).
    /// </param>
    /// <returns>
    /// Steps in application order. Consecutive effects that fall back to a colour matrix are <b>collapsed into
    /// a single matrix</b>, so a stack of five colour looks costs one pass, not five.
    /// </returns>
    public IReadOnlyList<EffectStep> Plan(Func<ICameraEffect, bool> isHandledNatively)
    {
        ArgumentNullException.ThrowIfNull(isHandledNatively);

        var steps = new List<EffectStep>();
        ColorMatrix4x5? pending = null;

        foreach (var effect in this.Effects)
        {
            if (effect is IDrawEffect or ICaptureEffect && effect is not (IColorEffect or INativeEffect))
                continue; // composited/post-processed elsewhere, not part of the pixel plan

            if (isHandledNatively(effect))
            {
                if (pending is not null)
                {
                    steps.Add(EffectStep.ForColor(pending));
                    pending = null;
                }
                steps.Add(EffectStep.ForNative(effect));
                continue;
            }

            // no native program here — degrade to the colour matrix if the effect has one, else skip it
            if (effect is IColorEffect { ColorMatrix: { } matrix } && !matrix.IsIdentity)
                pending = pending is null ? matrix : pending.Then(matrix);
        }

        if (pending is not null)
            steps.Add(EffectStep.ForColor(pending));

        return steps;
    }

    /// <summary>
    /// How much of <paramref name="effect"/> this backend will honour, given whether it has a native program
    /// for it and whether this platform filters the live preview at all.
    /// </summary>
    /// <param name="effect">The effect to report on.</param>
    /// <param name="hasNativeProgram">Whether the backend can run the effect's native descriptor.</param>
    /// <param name="filtersPreview">Whether this platform applies pixel effects to the live preview.</param>
    /// <param name="hasStillFallback">
    /// Whether this platform can still apply the effect to a captured photo when it has no preview program —
    /// true wherever the managed pixel pass is available. Without this a spatial effect on, say, an Android 31
    /// device would report <c>Unsupported</c> when in fact the photo comes back filtered.
    /// </param>
    public static EffectSupport ResolveSupport(
        ICameraEffect effect,
        bool hasNativeProgram,
        bool filtersPreview,
        bool hasStillFallback = false)
    {
        ArgumentNullException.ThrowIfNull(effect);

        // draw + capture effects work off a canvas / encoded bytes, so they are never platform-degraded
        if (effect is IDrawEffect or ICaptureEffect)
            return EffectSupport.Full;

        if (hasNativeProgram)
            return filtersPreview ? EffectSupport.Full : EffectSupport.StillOnly;

        var hasMatrix = effect is IColorEffect { ColorMatrix.IsIdentity: false };
        if (!hasMatrix)
            return hasStillFallback ? EffectSupport.StillOnly : EffectSupport.Unsupported;

        if (!filtersPreview)
            return EffectSupport.StillOnly;

        // a matrix stand-in for something authored as a native program is a real downgrade; say so
        return effect is INativeEffect { Descriptor.HasSpatialProgram: true }
            ? EffectSupport.ColorOnly
            : EffectSupport.Full;
    }
}


/// <summary>
/// One step of a resolved render plan: either a native program to run, or a collapsed colour matrix.
/// Exactly one of <see cref="Native"/> and <see cref="Color"/> is non-null.
/// </summary>
/// <param name="Native">The effect whose native program this backend should run.</param>
/// <param name="Color">A colour matrix, possibly the product of several consecutive effects.</param>
public readonly record struct EffectStep(ICameraEffect? Native, ColorMatrix4x5? Color)
{
    /// <summary>A step that runs an effect's native program.</summary>
    public static EffectStep ForNative(ICameraEffect effect) => new(effect, null);

    /// <summary>A step that applies a colour matrix.</summary>
    public static EffectStep ForColor(ColorMatrix4x5 matrix) => new(null, matrix);

    /// <summary>The native descriptor for this step, or <c>null</c> when it is a colour step.</summary>
    public NativeEffectDescriptor? Descriptor => (this.Native as INativeEffect)?.Descriptor;
}
