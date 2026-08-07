namespace Shiny.Controls.Keyframe;

/// <summary>Which way each iteration of a timeline runs.</summary>
public enum PlaybackDirection
{
    /// <summary>Every iteration runs forwards.</summary>
    Normal,

    /// <summary>Every iteration runs backwards.</summary>
    Reverse,

    /// <summary>Even iterations run forwards, odd ones backwards. Ping-pong.</summary>
    Alternate,

    /// <summary>Even iterations run backwards, odd ones forwards.</summary>
    AlternateReverse
}

/// <summary>
/// What a timeline does to its targets outside its active window — before the start delay has
/// elapsed and after the final iteration finishes.
/// </summary>
[Flags]
public enum FillMode
{
    /// <summary>Targets are left alone outside the active window.</summary>
    None = 0,

    /// <summary>The final value is held after the timeline finishes.</summary>
    Forwards = 1,

    /// <summary>The initial value is applied during the start delay, before playback begins.</summary>
    Backwards = 2,

    /// <summary>Both <see cref="Backwards"/> and <see cref="Forwards"/>.</summary>
    Both = Forwards | Backwards
}

/// <summary>The lifecycle state of a running timeline.</summary>
public enum PlaybackState
{
    /// <summary>Never started, or stopped and reset.</summary>
    Idle,

    /// <summary>Advancing with the clock.</summary>
    Running,

    /// <summary>Holding position; the clock is ignored until resumed.</summary>
    Paused,

    /// <summary>Reached the end of its active duration.</summary>
    Finished
}
