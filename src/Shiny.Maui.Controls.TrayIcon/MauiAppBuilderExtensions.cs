using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.TrayIcon;

namespace Shiny;

public static class TrayIconMauiAppBuilderExtensions
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
}

sealed class UnsupportedTrayIconFactory : ITrayIconFactory
{
    public ITrayIcon Create()
        => throw new PlatformNotSupportedException("System tray icons are not supported on this platform.");
}
