using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Docking;

public static class DockingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the docking infrastructure for Blazor. Call once during Program.cs,
    /// then register each panel with <see cref="AddDockPanel{TComponent}"/>. Also covered by
    /// <c>AddShinyControls()</c> — calling both is safe.
    /// </summary>
    /// <remarks>
    /// Singleton on purpose, unlike the other control services. <see cref="DockableContentRegistry"/>
    /// is an immutable lookup built once from the registered factories and holds no per-user state —
    /// the live layout lives in <c>DockHost</c>, which is a component and therefore already per-user.
    /// Do not "fix" this to scoped for consistency.
    /// </remarks>
    public static IServiceCollection AddShinyDocking(this IServiceCollection services)
    {
        services.TryAddSingleton<DockableContentRegistry>();
        return services;
    }

    /// <summary>
    /// Registers a Razor component as a dock panel under <paramref name="panelTypeId"/>.
    /// </summary>
    /// <param name="canClose">
    /// Whether the user may close the panel. Pass false for one the surface cannot do without -
    /// closing it would otherwise leave a layout with no way back to it.
    /// </param>
    public static IServiceCollection AddDockPanel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        this IServiceCollection services, string panelTypeId, string? displayName = null, string? icon = null, bool canClose = true)
        where TComponent : ComponentBase
    {
        services.AddSingleton<IDockableContentFactory>(_ => new ComponentPanelFactory<TComponent>(panelTypeId, displayName, icon, canClose));
        return services;
    }

    sealed class ComponentPanelFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>
        : IDockableContentFactory where TComponent : ComponentBase
    {
        public ComponentPanelFactory(string panelTypeId, string? displayName, string? icon, bool canClose)
        {
            PanelTypeId = panelTypeId;
            DisplayName = displayName ?? panelTypeId;
            Icon = icon;
            CanClose = canClose;
        }

        public string PanelTypeId { get; }
        public string DisplayName { get; }
        public string? Icon { get; }
        public bool CanClose { get; }

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
