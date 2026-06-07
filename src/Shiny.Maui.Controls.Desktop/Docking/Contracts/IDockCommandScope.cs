namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Indicates whether keyboard focus is currently inside a dock surface, so
/// global accelerators (Ctrl+W close tab, Ctrl+Tab MRU, Ctrl+Alt+PgUp/Dn group nav)
/// only fire when the dock system owns input.
/// </summary>
public interface IDockCommandScope
{
    bool IsInScope { get; }
    string? ActiveGroupId { get; }
    string? ActivePanelInstanceId { get; }
}
