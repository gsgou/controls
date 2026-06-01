using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using Sample.Features.Diagrams;
using Sample.Features.FloatingPanel;
using Sample.Features.Scheduler;
using Sample.Features.TableView;
using Shiny;
using Shiny.Maui.Controls.Scheduler;
#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .AddAudio()
            .UseShinyControls(cfg =>
            {
                cfg.SetCustomFeedback<MyCustomFeedbackService>(); // haptic is installed by default, but we want more fun
                cfg.AddDefaultMauiControlFeedback();
            })
            .UseTrayIcon()
            .UseShinyShell(x => x
                .AddGeneratedMaps()
                .Add<MinimizedSheetStandalonePage, MinimizedSheetViewModel>(registerRoute: false)
            )
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if IOS
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<Shell, Sample.Platforms.iOS.SolidTabBarRenderer>();
        });
#endif
        builder.Services.AddSpeechServices();
        builder.Services.AddSingleton<AppSettings>();

        builder.Services.AddTransient<MusicBrowsePage>();
        builder.Services.AddTransient<MusicLibraryPage>();
        builder.Services.AddTransient<StylingPage>();
        builder.Services.AddTransient<BasicFlowchartPage>();
        builder.Services.AddTransient<DirectionsPage>();
        builder.Services.AddTransient<ThemesPage>();
        builder.Services.AddTransient<SubgraphsPage>();
        builder.Services.AddTransient<InteractiveEditorPage>();
        builder.Services.AddSingleton<ISchedulerEventProvider, SampleSchedulerProvider>();

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
        builder.Logging.AddDebug();
        builder.AddMauiDevFlowAgent();
#endif

        var app = builder.Build();
        return app;
    }
}
