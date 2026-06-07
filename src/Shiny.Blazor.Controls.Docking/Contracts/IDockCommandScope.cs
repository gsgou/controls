namespace Shiny.Blazor.Controls.Docking;

public interface IDockCommandScope
{
    bool IsInScope { get; }
    string? ActiveGroupId { get; }
    string? ActivePanelInstanceId { get; }
}
