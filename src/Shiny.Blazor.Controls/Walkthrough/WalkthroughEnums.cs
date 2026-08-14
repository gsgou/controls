namespace Shiny.Blazor.Controls;

/// <summary>How a walkthrough step presents its text.</summary>
public enum WalkthroughDisplay
{
    /// <summary>A compact bubble with a tail and no buttons. Pair it with a step Duration.</summary>
    Tooltip,

    /// <summary>The full card: title, text, "2 of 5", Back/Next/Skip. Tailed. The default.</summary>
    Popover,

    /// <summary>The same card without a tail, sitting beside the target rather than pointing at it.</summary>
    Inline,

    /// <summary>
    /// No card — the text sits directly on the dimmed backdrop and the cut-out does the pointing.
    /// Needs <c>UseOverlay</c>, and falls back to <see cref="Popover"/> without it.
    /// </summary>
    Spotlight
}


/// <summary>How a step's callout enters or leaves.</summary>
public enum WalkthroughAnimation
{
    None,
    Fade,
    Slide,
    Zoom,
    Pop
}


/// <summary>The shape of the hole cut in the backdrop.</summary>
public enum WalkthroughHighlight
{
    RoundedRectangle,
    Rectangle,

    /// <summary>A circle around the target's centre, sized to cover it.</summary>
    Circle,

    /// <summary>An ellipse inscribed in the target's bounds.</summary>
    Ellipse,

    /// <summary>Dim everything and cut nothing.</summary>
    None
}


/// <summary>Why a walkthrough stopped.</summary>
public enum WalkthroughEndReason
{
    Completed,
    Skipped,
    Stopped
}
