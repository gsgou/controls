namespace Shiny.Blazor.Controls.Docking;

public interface IDockHost
{
    bool IsLocked { get; set; }
    IDockEvents Events { get; }
    IDockCommandScope CommandScope { get; }

    Task LoadAsync(DockRoot root, CancellationToken ct = default);
    DockRoot Snapshot();

    Task ShowPanelAsync(string panelTypeId, DockArea preferredArea = DockArea.Left, CancellationToken ct = default);
    Task HidePanelAsync(string panelInstanceId, CancellationToken ct = default);
    Task ActivatePanelAsync(string panelInstanceId, CancellationToken ct = default);
    Task ResetLayoutAsync(CancellationToken ct = default);

    /// <summary>Collapse a rail to a slim edge bar of panel titles, or expand it back.</summary>
    Task SetRailCollapsedAsync(DockArea area, bool collapsed, CancellationToken ct = default);
}
