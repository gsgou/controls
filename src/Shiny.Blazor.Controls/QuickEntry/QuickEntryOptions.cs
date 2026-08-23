namespace Shiny.Blazor.Controls.QuickEntry;

/// <summary>
/// Configuration for the quick entry popup — the prompt surface summoned over the page.
/// </summary>
/// <remarks>
/// The MAUI package's twin of this class also carries a <c>Presentation</c> setting choosing between
/// an in-app overlay and a real OS window. A browser has no second option, so it is absent here
/// rather than present and permanently pinned.
/// </remarks>
public class QuickEntryOptions
{
    /// <summary>Popup width in CSS pixels. Default 720, capped at the viewport on a narrow screen.</summary>
    public double Width { get; set; } = 720d;

    /// <summary>Ceiling for the popup's height in CSS pixels. Default 560; content past it scrolls.</summary>
    public double MaxHeight { get; set; } = 560d;

    /// <summary>Where the popup sits. Default <see cref="QuickEntryPlacement.TopCenter"/>.</summary>
    public QuickEntryPlacement Placement { get; set; } = QuickEntryPlacement.TopCenter;

    /// <summary>Top edge as a fraction of the viewport height for <see cref="QuickEntryPlacement.TopCenter"/>. Default 0.18.</summary>
    public double TopMarginRatio { get; set; } = 0.18d;

    /// <summary>Gap below the popup as a fraction of the viewport height for <see cref="QuickEntryPlacement.BottomCenter"/>. Default 0.12.</summary>
    public double BottomMarginRatio { get; set; } = 0.12d;

    /// <summary>Close the popup when the backdrop behind it is clicked. Default true.</summary>
    public bool DismissOnScrimTap { get; set; } = true;

    /// <summary>Close the popup on Escape. Content gets first refusal, so a prompt can clear itself first. Default true.</summary>
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Move focus into the popup when it opens. Default true.</summary>
    public bool FocusOnShow { get; set; } = true;

    /// <summary>Dim the page behind the popup. Default true.</summary>
    public bool ShowScrim { get; set; } = true;

    /// <summary>Whether opening the popup also lights the screen-edge glow. Default <see cref="ScreenGlowTrigger.None"/>.</summary>
    public ScreenGlowTrigger ScreenGlow { get; set; } = ScreenGlowTrigger.None;

    /// <summary>Appearance of the screen-edge glow.</summary>
    public ScreenGlowOptions Glow { get; } = new();
}

/// <summary>
/// Appearance of the screen-edge glow — the animated colour wash around the edge of the viewport, in
/// the style of Siri on iOS.
/// </summary>
public class ScreenGlowOptions
{
    /// <summary>How far the glow reaches in from the edge, in CSS pixels. Default 110.</summary>
    public double Thickness { get; set; } = 110d;

    /// <summary>Colours cycled around the edge. Any CSS colour. Defaults to a blue → violet → pink → amber → teal ramp.</summary>
    public IList<string> Palette { get; set; } = new List<string>
    {
        "#4F7DFF",
        "#A24BFF",
        "#FF4FA3",
        "#FF9A3D",
        "#35D6B0"
    };

    /// <summary>
    /// Seconds for one full lap of the edge. Default 14 — a slow drift underneath the colour change,
    /// rather than a chase light. Lower it for visible travel.
    /// </summary>
    public double LapSeconds { get; set; } = 14d;

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

    /// <summary>Overall opacity, 0–1. Default 0.9.</summary>
    public double Intensity { get; set; } = 0.9d;

    /// <summary>
    /// Fade in / out duration. Default 600ms — long enough to read as the glow arriving rather than
    /// appearing, which is the whole character of the effect.
    /// </summary>
    public TimeSpan FadeDuration { get; set; } = TimeSpan.FromMilliseconds(600);
}
