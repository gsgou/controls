using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platforms.MacOS.Essentials;
using Microsoft.Maui.Platforms.MacOS.Hosting;
using Shiny;
using Shiny.Maui.Controls.QuickEntry;
#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace Sample.MacOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppMacOS<App>()
            .AddMacOSEssentials()
            .UseShinyControls(cfg => cfg
                .AddDefaultMauiControlFeedback()
                .ConfigureQuickEntry(o =>
                {
                    o.HotKey = OperatingSystem.IsMacOS() ? "Cmd+Opt+Space" : "Ctrl+Alt+Space";
                    o.Placement = QuickEntryPlacement.TopCenter;
                    o.ScreenGlow = ScreenGlowTrigger.WhileBusy;
                }))
            .UseShinyShell(x => x.AddGeneratedMaps())
            .UseTrayIcon()
            .UseDesktopQuickEntry()
            .UseShinyMediaElement()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

            });

        builder.Services.AddSingleton<global::Sample.AppSettings>();

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddDebug();
        builder.AddMauiDevFlowAgent();
#endif

        return builder.Build();
    }
}
