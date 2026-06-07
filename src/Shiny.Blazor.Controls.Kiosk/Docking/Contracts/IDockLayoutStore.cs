namespace Shiny.Blazor.Controls.Kiosk.Docking;

public interface IDockLayoutStore
{
    Task<DockRoot?> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(DockRoot root, CancellationToken ct = default);
    int SaveDebounceMs { get; }
}
