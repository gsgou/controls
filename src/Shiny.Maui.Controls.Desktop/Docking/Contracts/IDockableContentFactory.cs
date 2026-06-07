using Microsoft.Maui.Controls;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Resolves a panel View by its panel-type ID. Registered via
/// <see cref="DockingMauiAppBuilderExtensions.AddDockPanel{T}"/>.
/// </summary>
public interface IDockableContentFactory
{
    string PanelTypeId { get; }
    Task<View> CreateAsync(string instanceId, CancellationToken ct = default);
}
