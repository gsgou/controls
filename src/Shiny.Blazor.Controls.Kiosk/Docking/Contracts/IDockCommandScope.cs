namespace Shiny.Blazor.Controls.Kiosk.Docking;

public interface IDockCommandScope
{
    bool IsInScope { get; }
    string? ActiveGroupId { get; }
    string? ActivePanelInstanceId { get; }
}
