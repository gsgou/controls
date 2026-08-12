namespace Shiny.Controls.MotionIcons;

/// <summary>
/// Motion that can be applied to any icon, including artwork the library has never seen.
/// </summary>
public enum MotionPreset
{
    /// <summary>
    /// The motion authored for this icon, falling back to <see cref="Pulse"/> for artwork that has
    /// none. This is the default, and the reason a bell rings rather than merely pulsing.
    /// </summary>
    Default,

    /// <summary>Nothing moves. Useful for binding the icon into a static state.</summary>
    None,

    /// <summary>Swells and settles.</summary>
    Pulse,

    /// <summary>A double thump, on the rhythm of a heartbeat.</summary>
    Beat,

    /// <summary>One full turn.</summary>
    Spin,

    /// <summary>Shakes side to side.</summary>
    Shake,

    /// <summary>Rocks back and forth, damping out.</summary>
    Wobble,

    /// <summary>Hops, landing with a bounce.</summary>
    Bounce,

    /// <summary>Drifts gently up and down.</summary>
    Float,

    /// <summary>Dips, then springs past its resting size and settles.</summary>
    Pop,

    /// <summary>Grows while wiggling — the "look at me" one.</summary>
    Tada,

    /// <summary>Flips horizontally, as though turning a card over.</summary>
    Flip,

    /// <summary>Swings from its top edge, like something hanging.</summary>
    Swing,

    /// <summary>Fades out and back.</summary>
    Blink,

    /// <summary>Draws each stroke on in turn, then holds. The signature motion-icon effect.</summary>
    Draw,

    /// <summary>Slides right and returns — a directional hint.</summary>
    Nudge,

    /// <summary>Jitters in short, sharp rotations.</summary>
    Jiggle
}
