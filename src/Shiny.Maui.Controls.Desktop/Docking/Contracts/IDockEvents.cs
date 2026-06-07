namespace Shiny.Maui.Controls.Desktop.Docking;

public sealed class LayoutChangedEventArgs : EventArgs
{
    public required DockRoot Snapshot { get; init; }
    public required string Reason { get; init; }
}

public sealed class PanelActivatedEventArgs : EventArgs
{
    public required string PanelInstanceId { get; init; }
    public required string PanelTypeId { get; init; }
}

public sealed class DockDragEventArgs : EventArgs
{
    public required string SourcePanelInstanceId { get; init; }
    public string? TargetGroupId { get; init; }
    public DockZone? TargetZone { get; init; }
}

public interface IDockEvents
{
    event EventHandler<LayoutChangedEventArgs>? LayoutChanged;
    event EventHandler<PanelActivatedEventArgs>? PanelActivated;
    event EventHandler<DockDragEventArgs>? DragStarted;
    event EventHandler<DockDragEventArgs>? DragCompleted;
    event EventHandler<DockDragEventArgs>? DragCancelled;
}
