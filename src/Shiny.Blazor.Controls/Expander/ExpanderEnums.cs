namespace Shiny.Blazor.Controls;

/// <summary>
/// How an <see cref="Expander"/> animates its content in and out. The members combine, so the
/// common accordion look — the panel growing while its content fades up into place — is
/// <c>ExpanderAnimation.Height | ExpanderAnimation.Fade</c>.
/// </summary>
[Flags]
public enum ExpanderAnimation
{
    /// <summary>Snap open and closed with no animation.</summary>
    None = 0,

    /// <summary>Fade the content in and out.</summary>
    Fade = 1,

    /// <summary>Translate the content in from the edge named by <see cref="Expander.SlideFrom"/>.</summary>
    Slide = 2,

    /// <summary>
    /// Grow and shrink the panel between zero and the content's natural size, so the page below moves
    /// with the reveal instead of being uncovered by it.
    /// </summary>
    Height = 4
}


/// <summary>Which edge <see cref="ExpanderAnimation.Slide"/> moves the content in from.</summary>
public enum ExpanderSlideFrom
{
    /// <summary>Enters downwards, from above.</summary>
    Top,

    /// <summary>Enters upwards, from below.</summary>
    Bottom,

    /// <summary>Enters rightwards, from the left.</summary>
    Left,

    /// <summary>Enters leftwards, from the right.</summary>
    Right
}


/// <summary>Where an <see cref="Expander"/> puts its content relative to its header.</summary>
public enum ExpandDirection
{
    /// <summary>Header on top, content revealed beneath it.</summary>
    Down,

    /// <summary>Content above the header — what a panel anchored to the bottom of the page wants.</summary>
    Up
}


/// <summary>Which side of the header an <see cref="Expander"/>'s indicator sits on.</summary>
public enum ExpanderIndicatorPosition
{
    /// <summary>Trailing edge, after the header content.</summary>
    End,

    /// <summary>Leading edge, before the header content.</summary>
    Start
}


/// <summary>How an <see cref="Expander"/>'s indicator reacts to the expanded state.</summary>
public enum ExpanderIndicatorMode
{
    /// <summary>No indicator at all.</summary>
    None,

    /// <summary>One glyph that rotates a quarter turn when expanded.</summary>
    Rotate,

    /// <summary>Two glyphs, swapped between collapsed and expanded.</summary>
    Swap
}


/// <summary>How many items an <see cref="Accordion"/> allows open at once.</summary>
public enum AccordionSelectionMode
{
    /// <summary>Opening one closes the rest.</summary>
    Single,

    /// <summary>Any number can be open together.</summary>
    Multiple
}
