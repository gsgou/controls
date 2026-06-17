namespace Shiny.Maui.Controls.Dialogs;

/// <summary>
/// Entry/exit animation used when a dialog is shown or dismissed. These are owned, cross-platform
/// animations — the native platform alert/prompt is never used.
/// </summary>
public enum DialogAnimation
{
    /// <summary>No animation — the dialog appears/disappears instantly.</summary>
    None,

    /// <summary>Fade opacity in/out.</summary>
    Fade,

    /// <summary>Slide down from the top (+ fade).</summary>
    SlideTop,

    /// <summary>Slide up from the bottom (+ fade).</summary>
    SlideBottom,

    /// <summary>Slide in from the left (+ fade).</summary>
    SlideLeft,

    /// <summary>Slide in from the right (+ fade).</summary>
    SlideRight,

    /// <summary>Scale up from slightly smaller (+ fade).</summary>
    Zoom,

    /// <summary>Scale up with a spring/overshoot (+ fade).</summary>
    Pop
}
