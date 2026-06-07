using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Kiosk.Docking;

/// <summary>
/// Resolves a panel's render fragment by panel-type ID. Registered via
/// <see cref="DockingServiceCollectionExtensions.AddDockPanel{TComponent}"/>.
/// </summary>
public interface IDockableContentFactory
{
    string PanelTypeId { get; }
    Task<RenderFragment> CreateAsync(string instanceId, CancellationToken ct = default);
}
