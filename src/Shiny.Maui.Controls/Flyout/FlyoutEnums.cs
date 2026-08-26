namespace Shiny.Maui.Controls.Flyout;

/// <summary>
/// Which edge a <see cref="FlyoutPanel"/> slides from. Named for reading order rather than for the
/// screen, so a right-to-left <c>FlowDirection</c> mirrors both panels without the markup changing.
/// </summary>
public enum FlyoutSide
{
    /// <summary>Left in a left-to-right layout, right in a right-to-left one.</summary>
    Start,

    /// <summary>Right in a left-to-right layout, left in a right-to-left one.</summary>
    End
}


/// <summary>How much of a <see cref="FlyoutPanel"/> is showing.</summary>
public enum FlyoutPanelState
{
    /// <summary>Entirely off screen. Takes no space and nothing inside it is reachable.</summary>
    Hidden,

    /// <summary>
    /// The rail: a narrow strip <see cref="FlyoutPanel.CollapsedWidth"/> wide showing
    /// <see cref="FlyoutPanel.RailContent"/>. A rail always insets the content — it is chrome, not
    /// a drawer — on both presentations.
    /// </summary>
    Collapsed,

    /// <summary>The full panel, <see cref="FlyoutPanel.ExpandedWidth"/> wide.</summary>
    Expanded
}


/// <summary>What an <see cref="FlyoutPanelState.Expanded"/> panel does to the content beside it.</summary>
public enum FlyoutPresentation
{
    /// <summary>Floats over the content with a scrim. The content does not move.</summary>
    Overlay,

    /// <summary>
    /// Moves the content aside. How it moves is <see cref="FlyoutView.PushMode"/>: shifted whole by
    /// default, or genuinely narrowed into the space that is left.
    /// </summary>
    Push,

    /// <summary>
    /// <see cref="Push"/> when the host is at least <see cref="FlyoutPanel.CompactWidth"/> wide,
    /// <see cref="Overlay"/> below that. Measured against the <see cref="FlyoutView"/>, not the
    /// window, so a flyout nested in a pane reacts to the pane.
    /// </summary>
    Auto
}


/// <summary>What a pushing <see cref="FlyoutPanel"/> does to the content beside it.</summary>
public enum FlyoutPushMode
{
    /// <summary>
    /// The content keeps its full width and is simply translated aside, so its far edge slides out
    /// of view. Nothing inside it re-lays out: text does not rewrap, columns do not collapse, and a
    /// list does not re-measure a single row. The default.
    /// </summary>
    Shift,

    /// <summary>
    /// The content is genuinely narrowed into the space that is left, and everything inside it
    /// re-lays out to fit. Right for a responsive layout that should reflow beside an open
    /// panel — a master/detail that becomes one column — and wrong for anything else, because it
    /// re-measures the entire content tree on every frame of the animation.
    /// </summary>
    Resize
}
