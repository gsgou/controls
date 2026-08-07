namespace Shiny.Controls.Keyframe;

/// <summary>
/// Blends between two values of the same type. Implementations must be pure and stateless —
/// the whole timeline model depends on being able to evaluate any point in any order, which is
/// what makes seeking, scrubbing and reverse playback correct rather than approximated.
/// </summary>
/// <typeparam name="T">The value type being animated.</typeparam>
public interface IInterpolator<T>
{
    /// <summary>Blends <paramref name="from"/> toward <paramref name="to"/>.</summary>
    /// <param name="from">Value at the start of the segment.</param>
    /// <param name="to">Value at the end of the segment.</param>
    /// <param name="progress">Eased progress through the segment. May fall outside [0,1] when an
    /// overshooting easing curve is in play, so implementations should extrapolate rather than clamp
    /// unless the type genuinely cannot represent out-of-range values.</param>
    T Interpolate(T from, T to, double progress);
}
