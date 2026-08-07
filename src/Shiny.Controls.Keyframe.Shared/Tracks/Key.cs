namespace Shiny.Controls.Keyframe;

/// <summary>
/// A single keyframe: a value pinned to a normalised position within a timeline iteration.
/// </summary>
/// <typeparam name="T">The value type being animated.</typeparam>
/// <remarks>
/// <para><b>Easing semantics.</b> A key's <see cref="Easing"/> governs the segment that
/// <i>begins</i> at that key and runs to the next one — the same rule CSS uses for
/// <c>animation-timing-function</c> declared inside a keyframe block. The easing on the final
/// key is therefore never used. This is worth internalising because the alternative convention
/// (easing describes how you <i>arrive</i> at the key) is equally defensible and produces
/// visibly different motion.</para>
/// </remarks>
public readonly struct Key<T>
{
    /// <summary>Position within the iteration, 0 to 1.</summary>
    public double Offset { get; }

    /// <summary>The value at this position. Meaningless when <see cref="IsImplicit"/> is true.</summary>
    public T? Value { get; }

    /// <summary>Easing for the segment starting at this key. Null means linear.</summary>
    public EasingFunction? Easing { get; }

    /// <summary>
    /// When true this key resolves to whatever the target's value was when the timeline started,
    /// rather than to <see cref="Value"/>. This is what makes an interrupted animation pick up from
    /// where it actually is instead of snapping back to a hardcoded starting point.
    /// </summary>
    public bool IsImplicit { get; }

    /// <summary>Creates a keyframe with an explicit value.</summary>
    /// <param name="offset">Position within the iteration; must be within [0,1].</param>
    /// <param name="value">The value at this position.</param>
    /// <param name="easing">Easing for the segment starting here. Null means linear.</param>
    public Key(double offset, T value, EasingFunction? easing = null)
    {
        if (double.IsNaN(offset) || offset < 0d || offset > 1d)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Keyframe offset must be within [0,1].");

        Offset = offset;
        Value = value;
        Easing = easing;
        IsImplicit = false;
    }

    Key(double offset, EasingFunction? easing)
    {
        Offset = offset;
        Value = default;
        Easing = easing;
        IsImplicit = true;
    }

    /// <summary>
    /// Creates a keyframe that resolves to the target's value at the moment playback starts.
    /// </summary>
    /// <param name="offset">Position within the iteration; must be within [0,1].</param>
    /// <param name="easing">Easing for the segment starting here. Null means linear.</param>
    public static Key<T> Current(double offset = 0d, EasingFunction? easing = null)
    {
        if (double.IsNaN(offset) || offset < 0d || offset > 1d)
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "Keyframe offset must be within [0,1].");

        return new Key<T>(offset, easing);
    }

    /// <summary>Returns this key with its easing replaced.</summary>
    public Key<T> WithEasing(EasingFunction? easing)
        => IsImplicit ? Current(Offset, easing) : new Key<T>(Offset, Value!, easing);
}
