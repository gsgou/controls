namespace Shiny.Maui.Controls;

/// <summary>
/// How <see cref="StateView"/> animates from the outgoing state to the incoming one.
/// </summary>
public enum StateTransition
{
    /// <summary>Swap instantly.</summary>
    None,

    /// <summary>Cross-fade.</summary>
    Fade,

    /// <summary>
    /// Slide horizontally, with the direction taken from the move: a state later in
    /// <see cref="StateView.States"/> enters from the right, an earlier one from the left. This is
    /// what makes a wizard feel like it has a forwards and a backwards.
    /// </summary>
    Slide,

    /// <summary>Slide horizontally, always as if moving forwards (incoming enters from the right).</summary>
    SlideLeft,

    /// <summary>Slide horizontally, always as if moving backwards (incoming enters from the left).</summary>
    SlideRight,

    /// <summary>Slide vertically, incoming enters from the bottom.</summary>
    SlideUp,

    /// <summary>Slide vertically, incoming enters from the top.</summary>
    SlideDown,

    /// <summary>Cross-fade while the incoming state grows into place.</summary>
    Scale
}
