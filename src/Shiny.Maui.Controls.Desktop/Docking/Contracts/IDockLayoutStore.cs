namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Persistence contract for dock layouts. Consumer-provided — no default
/// implementation ships with this package.
/// </summary>
public interface IDockLayoutStore
{
    Task<DockRoot?> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(DockRoot root, CancellationToken ct = default);

    /// <summary>
    /// Milliseconds to debounce saves triggered by layout changes. Zero disables debouncing.
    /// </summary>
    int SaveDebounceMs { get; }
}
