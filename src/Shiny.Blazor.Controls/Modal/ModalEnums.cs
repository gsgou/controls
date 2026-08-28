namespace Shiny.Blazor.Controls;

/// <summary>How wide a <see cref="ModalView"/> is allowed to grow.</summary>
/// <remarks>
/// Each size is a max-width the panel takes up to; narrow viewports always win, so a Large modal on a
/// phone is a full-width one. Set <see cref="ModalView.Width"/> for a size that is not on this list.
/// </remarks>
public enum ModalSize
{
    /// <summary>Up to 360px. Confirmations and single-field forms.</summary>
    Small,

    /// <summary>Up to 520px. The default.</summary>
    Medium,

    /// <summary>Up to 760px.</summary>
    Large,

    /// <summary>Up to 1080px.</summary>
    ExtraLarge,

    /// <summary>The whole viewport, edge to edge, with no corner rounding.</summary>
    Full
}


/// <summary>Where a <see cref="ModalView"/> sits in the viewport.</summary>
public enum ModalPlacement
{
    /// <summary>Centred both ways. The default.</summary>
    Center,

    /// <summary>Pinned near the top, which keeps a tall modal from jumping as its content grows.</summary>
    Top,

    /// <summary>Pinned to the bottom edge, the phone-style sheet position.</summary>
    Bottom
}


/// <summary>Entry and exit motion for a <see cref="ModalView"/>.</summary>
public enum ModalAnimation
{
    /// <summary>Appears and disappears instantly.</summary>
    None,

    /// <summary>Opacity only.</summary>
    Fade,

    /// <summary>Scales up from slightly smaller, with a fade.</summary>
    Zoom,

    /// <summary>Scales up with a small overshoot, with a fade. The default.</summary>
    Pop,

    /// <summary>Drops in from above, with a fade.</summary>
    SlideTop,

    /// <summary>Rises from below, with a fade.</summary>
    SlideBottom
}


/// <summary>What closed a <see cref="ModalView"/>.</summary>
/// <remarks>
/// Handed to <see cref="ModalView.Closing"/> and <see cref="ModalView.Closed"/> so a form can tell
/// "the user pressed Save" from "the user pressed Escape" without tracking that itself.
/// </remarks>
public enum ModalCloseReason
{
    /// <summary>Code called <see cref="ModalView.CloseAsync"/>, or bound <c>IsOpen</c> to false.</summary>
    Programmatic,

    /// <summary>The header's close button.</summary>
    CloseButton,

    /// <summary>A click on the backdrop.</summary>
    Backdrop,

    /// <summary>The Escape key.</summary>
    Escape,

    /// <summary>A footer button with <see cref="ModalButton.ClosesModal"/> set.</summary>
    Button
}
