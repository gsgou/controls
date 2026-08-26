namespace Shiny.Blazor.Controls;

/// <summary>Which edge the <see cref="ProgressLine"/> runs along.</summary>
public enum ProgressLinePosition
{
    Top,
    Bottom
}


/// <summary>What the <see cref="ProgressLine"/> pins itself to.</summary>
public enum ProgressLineAnchor
{
    /// <summary>
    /// The browser viewport, via <c>position: fixed</c> — the line stays on the window edge however
    /// the page scrolls. The default, and what "across the top of the window" means.
    /// </summary>
    Viewport,

    /// <summary>
    /// The nearest positioned ancestor, via <c>position: absolute</c>. Use this to run the line along
    /// the edge of a panel or under an <c>AppLayout</c> header rather than the window.
    /// </summary>
    Container
}
