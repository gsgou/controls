namespace Shiny.Controls.MotionIcons;

/// <summary>
/// The easing curves a motion track can use.
/// </summary>
/// <remarks>
/// A closed enum rather than a delegate, because a motion spec has to survive being compiled into
/// CSS — and a lambda cannot be. Every member here has an exact definition in
/// <see cref="MotionEasings"/> that both hosts evaluate, so the curve is the same whether it is
/// being sampled by the MAUI timeline or written into an <c>@keyframes</c> block.
/// </remarks>
public enum MotionEase
{
    /// <summary>No easing.</summary>
    Linear,

    /// <summary>CSS <c>ease</c>. The default.</summary>
    Ease,

    /// <summary>CSS <c>ease-in</c>.</summary>
    EaseIn,

    /// <summary>CSS <c>ease-out</c>.</summary>
    EaseOut,

    /// <summary>CSS <c>ease-in-out</c>.</summary>
    EaseInOut,

    /// <summary>Accelerating quadratic.</summary>
    QuadIn,

    /// <summary>Decelerating quadratic.</summary>
    QuadOut,

    /// <summary>Quadratic at both ends.</summary>
    QuadInOut,

    /// <summary>Accelerating cubic.</summary>
    CubicIn,

    /// <summary>Decelerating cubic.</summary>
    CubicOut,

    /// <summary>Cubic at both ends.</summary>
    CubicInOut,

    /// <summary>Sharply decelerating quartic.</summary>
    QuartOut,

    /// <summary>Very sharply decelerating quintic.</summary>
    QuintOut,

    /// <summary>Sinusoidal ease in.</summary>
    SinIn,

    /// <summary>Sinusoidal ease out.</summary>
    SinOut,

    /// <summary>Sinusoidal at both ends — the natural curve for an oscillation.</summary>
    SinInOut,

    /// <summary>Exponential ease in.</summary>
    ExpoIn,

    /// <summary>Exponential ease out.</summary>
    ExpoOut,

    /// <summary>Circular ease out.</summary>
    CircOut,

    /// <summary>Anticipates by pulling back before moving forward.</summary>
    BackIn,

    /// <summary>Overshoots the target then settles back.</summary>
    BackOut,

    /// <summary>Anticipates, then overshoots.</summary>
    BackInOut,

    /// <summary>Springs past the target and oscillates to rest.</summary>
    ElasticOut,

    /// <summary>Bounces to rest, like a dropped ball.</summary>
    BounceOut,

    /// <summary>Holds the starting value, then jumps at the end of the segment.</summary>
    StepEnd
}
