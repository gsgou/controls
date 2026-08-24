using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Docking;

/// <summary>
/// Resolves a panel's render fragment by panel-type ID. Registered via
/// <see cref="DockingServiceCollectionExtensions.AddDockPanel{TComponent}"/>.
/// </summary>
public interface IDockableContentFactory
{
    string PanelTypeId { get; }

    /// <summary>Human-readable tab title; defaults to the panel type id.</summary>
    string DisplayName => PanelTypeId;

    /// <summary>Optional glyph (emoji/unicode) shown in the tab and in collapsed edge bars.</summary>
    string? Icon => null;

    /// <summary>
    /// Whether the user may close this panel. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Set false for a panel the surface cannot function without - a file explorer's folder tree,
    /// an editor's document area. Closing one of those leaves a layout with no way to get it back
    /// unless the app has built its own "reopen panel" affordance, which most have not.
    /// <para>
    /// This hides the tab's close button <em>and</em> makes <see cref="IDockHost.HidePanelAsync"/>
    /// refuse the panel: a hidden button is not a rule, and layout state that only the UI enforces
    /// is state a stray call can still break.
    /// </para>
    /// </remarks>
    bool CanClose => true;

    Task<RenderFragment> CreateAsync(string instanceId, CancellationToken ct = default);
}
