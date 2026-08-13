namespace Shiny.Blazor.Controls;

/// <summary>
/// Cross-axis alignment for <see cref="VStack"/> / <see cref="HStack"/> (CSS align-items).
/// </summary>
public enum StackAlign
{
    Start,
    Center,
    End,
    Stretch,
    Baseline
}

/// <summary>
/// Main-axis distribution for <see cref="VStack"/> / <see cref="HStack"/> (CSS justify-content).
/// </summary>
public enum StackJustify
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly
}

/// <summary>
/// Which edge of an <see cref="AppLayout"/> an <see cref="AppLayoutPanel"/> docks to.
/// </summary>
public enum PanelSide
{
    Left,
    Right
}

/// <summary>
/// How much of an <see cref="AppLayoutPanel"/> is showing.
/// </summary>
public enum PanelState
{
    /// <summary>Collapsed to nothing.</summary>
    Hidden,

    /// <summary>Collapsed to a narrow rail rendering <c>ToolbarContent</c>.</summary>
    Toolbar,

    /// <summary>Fully expanded to the panel's <c>Size</c>.</summary>
    Shown
}

/// <summary>
/// Whether the header / footer of an <see cref="AppLayout"/> runs the full width of the
/// layout or is inset between the left and right panels.
/// </summary>
public enum LayoutSpan
{
    /// <summary>Spans the whole layout; the panels sit between the header and footer.</summary>
    Full,

    /// <summary>Spans only the content column; the panels run the full height.</summary>
    Content
}
