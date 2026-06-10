using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Kiosk.Docking;

public static class DockingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the docking infrastructure for Blazor. Call once during Program.cs,
    /// then register each panel with <see cref="AddDockPanel{TComponent}"/>.
    /// </summary>
    public static IServiceCollection AddShinyDocking(this IServiceCollection services)
    {
        services.TryAddSingleton<DockableContentRegistry>();
        return services;
    }

    /// <summary>
    /// Registers a Razor component as a dock panel under <paramref name="panelTypeId"/>.
    /// </summary>
    public static IServiceCollection AddDockPanel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        this IServiceCollection services, string panelTypeId, string? displayName = null, string? icon = null)
        where TComponent : ComponentBase
    {
        services.AddSingleton<IDockableContentFactory>(_ => new ComponentPanelFactory<TComponent>(panelTypeId, displayName, icon));
        return services;
    }

    sealed class ComponentPanelFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>
        : IDockableContentFactory where TComponent : ComponentBase
    {
        public ComponentPanelFactory(string panelTypeId, string? displayName, string? icon)
        {
            PanelTypeId = panelTypeId;
            DisplayName = displayName ?? panelTypeId;
            Icon = icon;
        }

        public string PanelTypeId { get; }
        public string DisplayName { get; }
        public string? Icon { get; }

        public Task<RenderFragment> CreateAsync(string instanceId, CancellationToken ct = default)
        {
            RenderFragment fragment = builder =>
            {
                builder.OpenComponent<TComponent>(0);
                builder.CloseComponent();
            };
            return Task.FromResult(fragment);
        }
    }
}
