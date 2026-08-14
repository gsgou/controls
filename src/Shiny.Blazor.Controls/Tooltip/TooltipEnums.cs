namespace Shiny.Blazor.Controls;

/// <summary>Which side of its target a tooltip bubble sits on.</summary>
public enum TooltipPlacement
{
    /// <summary>
    /// Pick the side with the most room, preferring below, then above, then right, then left. A
    /// tooltip near the bottom of the viewport flips up on its own instead of being clipped.
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

    /// <summary>Centred in the viewport, ignoring the target. No tail is drawn.</summary>
    Center
}


/// <summary>What opens a tooltip.</summary>
public enum TooltipTrigger
{
    /// <summary>Nothing but <c>IsOpen</c> or <c>ShowAsync()</c>.</summary>
    Manual,

    /// <summary>Pointer entering the target — the desktop default.</summary>
    Hover,

    /// <summary>A click on the target.</summary>
    Click,

    /// <summary>The target taking focus, which is what makes a tooltip reachable by keyboard.</summary>
    Focus,

    /// <summary>Hover and focus together. The accessible default for a hint on a control.</summary>
    HoverOrFocus,

    /// <summary>A press held on the target — the touch equivalent of hover.</summary>
    LongPress
}


/// <summary>How a tooltip bubble enters and leaves.</summary>
public enum TooltipAnimation
{
    None,
    Fade,
    Scale,
    Slide
}
