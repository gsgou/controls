using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Kiosk.Docking;

/// <summary>
/// Resolves a panel's render fragment by panel-type ID. Registered via
/// <see cref="DockingServiceCollectionExtensions.AddDockPanel{TComponent}"/>.
/// </summary>
public interface IDockableContentFactory
{
    string PanelTypeId { get; }

    /// <summary>Human-readable tab title; defaults to the panel type id.</summary>
    string DisplayName => PanelTypeId;

    Task<RenderFragment> CreateAsync(string instanceId, CancellationToken ct = default);
}
