namespace Shiny.Blazor.Controls.Docking;

public sealed class DockWindowState
{
    public DockRect? Bounds { get; set; }
    public bool IsMaximized { get; set; }
    public bool IsFullScreen { get; set; }

    public DockNode DocumentArea { get; set; } = new DockEmpty();
    public DockNode? LeftRail { get; set; }
    public DockNode? TopRail { get; set; }
    public DockNode? RightRail { get; set; }
    public DockNode? BottomRail { get; set; }

    public string? ActivePanelId { get; set; }
}
