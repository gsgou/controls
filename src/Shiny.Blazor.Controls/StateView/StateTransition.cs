namespace Shiny.Blazor.Controls;

/// <summary>
/// How <see cref="StateView"/> animates the incoming state in.
/// </summary>
public enum StateTransition
{
    /// <summary>Swap instantly.</summary>
    None,

    /// <summary>Fade in.</summary>
    Fade,

    /// <summary>
    /// Slide horizontally, with the direction taken from the move: a state later in the markup
    /// enters from the right, an earlier one from the left.
    /// </summary>
    Slide,

    /// <summary>Slide in from the right, whichever way the move is going.</summary>
    SlideLeft,

    /// <summary>Slide in from the left, whichever way the move is going.</summary>
    SlideRight,

    /// <summary>Slide in from the bottom.</summary>
    SlideUp,

    /// <summary>Slide in from the top.</summary>
    SlideDown,

    /// <summary>Fade while growing into place.</summary>
    Scale
}
