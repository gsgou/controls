namespace Shiny.Maui.Controls.Desktop.Docking;

public sealed class DockWindowState
{
    public DockRect? Bounds { get; set; }
    public bool IsMaximized { get; set; }
    public bool IsFullScreen { get; set; }

    /// <summary>
    /// Structurally distinct document well. Always present, even if empty.
    /// Tab/focus behavior in this subtree differs from tool areas.
    /// </summary>
    public DockNode DocumentArea { get; set; } = new DockEmpty();

    public DockNode? LeftRail { get; set; }
    public DockNode? TopRail { get; set; }
    public DockNode? RightRail { get; set; }
    public DockNode? BottomRail { get; set; }

    /// <summary>Panel instance ID to restore focus to on load.</summary>
    public string? ActivePanelId { get; set; }
}
