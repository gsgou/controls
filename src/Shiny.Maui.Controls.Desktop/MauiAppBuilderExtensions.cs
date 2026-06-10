using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.Desktop.Docking;
using Shiny.Maui.Controls.Desktop.TrayIcon;

namespace Shiny;

public static class DesktopMauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ITrayIconFactory"/> for the current platform. On unsupported
    /// platforms (Android, iOS, tvOS) this is a no-op factory that throws on use, so
    /// guard your tray code with platform checks.
    /// </summary>
    public static MauiAppBuilder UseTrayIcon(this MauiAppBuilder builder)
    {
#if WINDOWS
        builder.Services.TryAddSingleton<ITrayIconFactory, WindowsTrayIconFactory>();
#elif MACCATALYST
        builder.Services.TryAddSingleton<ITrayIconFactory, MacCatalystTrayIconFactory>();
#elif MACOS
        builder.Services.TryAddSingleton<ITrayIconFactory, MacTrayIconFactory>();
#else
        // net10.0 fallback — Linux at runtime, otherwise unsupported.
        builder.Services.TryAddSingleton<ITrayIconFactory>(_ =>
        {
            if (OperatingSystem.IsLinux())
                return new LinuxTrayIconFactory();
            return new UnsupportedTrayIconFactory();
        });
#endif
        return builder;
    }

    /// <summary>
    /// Registers the docking infrastructure. Call once during MAUI app startup, then
    /// register each panel with <see cref="AddDockPanel{TView}"/>.
    /// </summary>
    public static MauiAppBuilder UseShinyDocking(this MauiAppBuilder builder)
    {
        builder.Services.TryAddSingleton<DockableContentRegistry>();
        return builder;
    }

    /// <summary>
    /// Registers a panel type so dock layouts referencing <paramref name="panelTypeId"/>
    /// can resolve and instantiate the View. The View is resolved through DI per request.
    /// </summary>
    /// <typeparam name="TView">The panel content view type.</typeparam>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="panelTypeId">Stable string ID stored in persisted layouts.</param>
    public static MauiAppBuilder AddDockPanel<TView>(this MauiAppBuilder builder, string panelTypeId, string? displayName = null, string? icon = null)
        where TView : View
    {
        builder.Services.AddTransient<TView>();
        builder.Services.AddSingleton<IDockableContentFactory>(sp =>
            new ServiceProviderPanelFactory<TView>(panelTypeId, displayName, icon, sp));
        return builder;
    }

    sealed class ServiceProviderPanelFactory<TView> : IDockableContentFactory where TView : View
    {
        readonly IServiceProvider sp;

        public ServiceProviderPanelFactory(string panelTypeId, string? displayName, string? icon, IServiceProvider sp)
        {
            PanelTypeId = panelTypeId;
            DisplayName = displayName ?? panelTypeId;
            Icon = icon;
            this.sp = sp;
        }

        public string PanelTypeId { get; }
        public string DisplayName { get; }
        public string? Icon { get; }

        public Task<View> CreateAsync(string instanceId, CancellationToken ct = default)
            => Task.FromResult<View>(sp.GetRequiredService<TView>());
    }
}

sealed class UnsupportedTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create()
        => throw new PlatformNotSupportedException("System tray icons are not supported on this platform.");
}
