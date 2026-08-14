namespace Shiny.Maui.Controls;

/// <summary>How a walkthrough step presents its text.</summary>
public enum WalkthroughDisplay
{
    /// <summary>
    /// A compact bubble with a tail, no chrome and no buttons. For a short label on an obvious
    /// control — pair it with a step <c>Duration</c> or backdrop-tap advance.
    /// </summary>
    Tooltip,

    /// <summary>
    /// The full card: title, text, "2 of 5", and Back/Next/Skip. Tailed, so it stays visibly attached
    /// to its target. The default, and the right answer for most tours.
    /// </summary>
    Popover,

    /// <summary>
    /// The same card without a tail, sitting next to the target rather than pointing at it. Reads as
    /// calmer, and avoids a tail that would have to point at something enormous.
    /// </summary>
    Inline,

    /// <summary>
    /// No card at all — the text sits directly on the dimmed backdrop and the cut-out does the
    /// pointing. Needs <see cref="Walkthrough.UseOverlay"/>, and falls back to
    /// <see cref="Popover"/> without it, since bare text over live content is unreadable.
    /// </summary>
    Spotlight
}


/// <summary>How a step's callout enters or leaves.</summary>
public enum WalkthroughAnimation
{
    /// <summary>Appear outright.</summary>
    None,

    /// <summary>Fade.</summary>
    Fade,

    /// <summary>Fade while sliding in from the target's side.</summary>
    Slide,

    /// <summary>Fade while growing from 85%, anchored at the tail.</summary>
    Zoom,

    /// <summary>Overshoot past full size and settle — the one that reads as "look here".</summary>
    Pop
}


/// <summary>The shape of the hole cut in the backdrop.</summary>
public enum WalkthroughHighlight
{
    /// <summary>A rounded rectangle. Matches most controls.</summary>
    RoundedRectangle,

    /// <summary>Square corners.</summary>
    Rectangle,

    /// <summary>A circle around the target's centre, sized to cover it. For icon buttons and avatars.</summary>
    Circle,

    /// <summary>An ellipse inscribed in the target's bounds.</summary>
    Ellipse,

    /// <summary>Do not cut a hole — dim everything, and let the callout carry the step on its own.</summary>
    None
}


/// <summary>Why a walkthrough stopped.</summary>
public enum WalkthroughEndReason
{
    /// <summary>The user reached the end.</summary>
    Completed,

    /// <summary>The user took Skip.</summary>
    Skipped,

    /// <summary>Something called <c>Stop()</c>, or the page went away underneath it.</summary>
    Stopped
}
