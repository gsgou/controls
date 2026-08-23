namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>When the quick entry popup drives the screen glow.</summary>
public enum ScreenGlowTrigger
{
    /// <summary>Never. The glow can still be driven by hand through <see cref="IScreenGlowService"/>.</summary>
    None,

    /// <summary>Glow the whole time the popup is open.</summary>
    WhileOpen,

    /// <summary>
    /// Glow only while the popup's content reports itself busy — the closest match to Siri, which
    /// lights the edge while it is listening and thinking rather than the whole time it is up.
    /// Works with <see cref="PromptView.IsBusy"/> out of the box.
    /// </summary>
    WhileBusy
}

/// <summary>
/// Appearance of the screen-edge glow — the animated colour wash around the display border, in the
/// style of Siri on iOS.
/// </summary>
public sealed class ScreenGlowOptions
{
    /// <summary>How far the glow reaches in from the screen edge, in device-independent pixels. Default 110.</summary>
    public double Thickness { get; set; } = 110d;

    /// <summary>
    /// Colours cycled around the edge. Sampled as a loop, so the first and last blend into each
    /// other. Defaults to a blue → violet → pink → amber → teal ramp.
    /// </summary>
    public IList<Color> Palette { get; set; } = new List<Color>
    {
        Color.FromArgb("#4F7DFF"),
        Color.FromArgb("#A24BFF"),
        Color.FromArgb("#FF4FA3"),
        Color.FromArgb("#FF9A3D"),
        Color.FromArgb("#35D6B0")
    };

    /// <summary>How many colour pools travel around the border. Default 5. More is smoother and costs more to draw.</summary>
    public int BlobCount { get; set; } = 5;

    /// <summary>
    /// Laps of the screen perimeter per second. Default 0.09 — a slow drift underneath the colour
    /// change, rather than a chase light. Raise it for visible travel.
    /// </summary>
    public double Speed { get; set; } = 0.09d;

    /// <summary>
    /// Seconds for the whole edge to work through the palette once. Default 7. This is the movement
    /// you actually notice: the colour changes in place rather than the glow racing around.
    /// </summary>
    public double ColorCycleSeconds { get; set; } = 7d;

    /// <summary>
    /// Seconds per brightness breath. Default 2.8; zero holds it steady. The pulse is what makes the
    /// glow read as alive rather than as a static coloured border.
    /// </summary>
    public double PulseSeconds { get; set; } = 2.8d;

    /// <summary>
    /// How deep the breath goes, 0–1. Default 0.35 — the glow falls to 65% of
    /// <see cref="Intensity"/> at the bottom of each pulse.
    /// </summary>
    public double PulseDepth { get; set; } = 0.35d;

    /// <summary>Overall opacity multiplier, 0–1. Default 0.9.</summary>
    public double Intensity { get; set; } = 0.9d;

    /// <summary>
    /// How many stacked passes build the inward falloff. Default 3. Each pass is clipped a little
    /// further in, so they accumulate towards the edge and fade out smoothly; 1 gives a hard inner
    /// border, 5 is very soft and noticeably more expensive on a large display.
    /// </summary>
    public int Layers { get; set; } = 3;

    /// <summary>Animation frame rate. Default 30. This is a full-screen redraw, so lowering it is the first thing to try on an older GPU.</summary>
    public int FrameRate { get; set; } = 30;

    /// <summary>
    /// Fade in / out duration. Default 600ms — long enough to read as the glow arriving rather than
    /// appearing, which is the whole character of the effect.
    /// </summary>
    public TimeSpan FadeDuration { get; set; } = TimeSpan.FromMilliseconds(600);
}
