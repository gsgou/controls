using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Docking;

public partial class DockHost : ComponentBase, IDockHost
{
    [Parameter] public DockRoot? InitialLayout { get; set; }
    [Parameter] public bool IsLocked { get; set; }

    [Parameter] public string? BackgroundColor { get; set; }

    string HostStyle => string.IsNullOrEmpty(BackgroundColor)
        ? string.Empty
        : $"--shiny-dock-host-bg: {BackgroundColor};";

    public IDockEvents Events { get; } = new DockEventsImpl();
    public IDockCommandScope CommandScope { get; } = new DockCommandScopeImpl();

    public Task LoadAsync(DockRoot root, CancellationToken ct = default)
    {
        InitialLayout = root;
        StateHasChanged();
        return Task.CompletedTask;
    }

    public DockRoot Snapshot() => InitialLayout ?? new DockRoot();

    public Task ShowPanelAsync(string panelTypeId, DockArea preferredArea = DockArea.Left, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task HidePanelAsync(string panelInstanceId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ActivatePanelAsync(string panelInstanceId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ResetLayoutAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    sealed class DockEventsImpl : IDockEvents
    {
        public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;
        public event EventHandler<PanelActivatedEventArgs>? PanelActivated;
        public event EventHandler<DockDragEventArgs>? DragStarted;
        public event EventHandler<DockDragEventArgs>? DragCompleted;
        public event EventHandler<DockDragEventArgs>? DragCancelled;

        internal void RaiseLayoutChanged(LayoutChangedEventArgs e) => LayoutChanged?.Invoke(this, e);
        internal void RaisePanelActivated(PanelActivatedEventArgs e) => PanelActivated?.Invoke(this, e);
        internal void RaiseDragStarted(DockDragEventArgs e) => DragStarted?.Invoke(this, e);
        internal void RaiseDragCompleted(DockDragEventArgs e) => DragCompleted?.Invoke(this, e);
        internal void RaiseDragCancelled(DockDragEventArgs e) => DragCancelled?.Invoke(this, e);
    }

    sealed class DockCommandScopeImpl : IDockCommandScope
    {
        public bool IsInScope { get; internal set; }
        public string? ActiveGroupId { get; internal set; }
        public string? ActivePanelInstanceId { get; internal set; }
    }
}
