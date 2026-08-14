namespace Shiny.Maui.Controls;

/// <summary>Which side of its target a tooltip bubble sits on.</summary>
public enum TooltipPlacement
{
    /// <summary>
    /// Pick the side with the most room, preferring below, then above, then right, then left. A
    /// tooltip on a control near the bottom of the screen flips up on its own rather than being
    /// clipped, which is the whole reason not to hard-code a side.
    /// </summary>
    Auto,

    /// <summary>Above the target, tail pointing down.</summary>
    Top,

    /// <summary>Below the target, tail pointing up.</summary>
    Bottom,

    /// <summary>Left of the target, tail pointing right.</summary>
    Left,

    /// <summary>Right of the target, tail pointing left.</summary>
    Right,

    /// <summary>Centred in the page and ignoring the target. No tail is drawn.</summary>
    Center
}


/// <summary>What opens a tooltip.</summary>
public enum TooltipTrigger
{
    /// <summary>Nothing opens it but <c>IsOpen</c> or <c>Show()</c>. The default for a bound tooltip.</summary>
    Manual,

    /// <summary>A tap on the target.</summary>
    Tap,

    /// <summary>A press-and-hold on the target — the phone idiom, since there is no pointer to hover.</summary>
    LongPress,

    /// <summary>Pointer entering the target. Desktop and mouse-attached tablets only; falls back to nothing elsewhere.</summary>
    Hover,

    /// <summary>The target taking focus, which is also what makes a tooltip reachable by keyboard.</summary>
    Focus
}


/// <summary>How a tooltip bubble enters and leaves.</summary>
public enum TooltipAnimation
{
    /// <summary>Appear and disappear outright.</summary>
    None,

    /// <summary>Fade.</summary>
    Fade,

    /// <summary>Fade while growing from 90%, anchored towards the target.</summary>
    Scale,

    /// <summary>Fade while sliding in from the target's side.</summary>
    Slide
}
