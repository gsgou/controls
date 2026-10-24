using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.Desktop.Docking;
using Shiny.Maui.Controls.Desktop.FileDrop;
using Shiny.Maui.Controls.Desktop.Notch;
using Shiny.Maui.Controls.Desktop.QuickEntry;
using Shiny.Maui.Controls.Desktop.TrayIcon;
using Shiny.Maui.Controls.QuickEntry;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Desktop.Tests;

/// <summary>
/// <c>UseShinyDesktop</c> replaced six calls, and forgetting one of them failed silently - keyboard
/// shortcuts that never fired on macOS, a dock that loaded nothing. So everything inert is registered by
/// the one call, the two features that change what the app does stay opt-in, and the call is safe to
/// make twice: presenters and startup hooks are resolved as a list, where a duplicate is not shadowed
/// but runs.
/// </summary>
public class DesktopRegistrationIdempotencyTests
{
    static IServiceCollection Register(Action<MauiAppBuilder> register, int times = 1)
    {
        var builder = MauiApp.CreateBuilder();
        for (var i = 0; i < times; i++)
            register(builder);
        return builder.Services;
    }


    // By implementation: the builder registers several initializers of MAUI's own.
    static int Initializers<T>(IServiceCollection services)
        => services.Count(x => x.ServiceType == typeof(IMauiInitializeService) && x.ImplementationType == typeof(T));


    static bool Has<T>(IServiceCollection services) => services.Any(x => x.ServiceType == typeof(T));


    [Fact]
    public void TheInertFeaturesNeedNoOptIn()
    {
        var services = Register(b => b.UseShinyDesktop());

        Has<ITrayIconFactory>(services).ShouldBeTrue();
        Has<IGlobalHotKeyService>(services).ShouldBeTrue();
        Has<DockableContentRegistry>(services).ShouldBeTrue();
        Has<INotchWindowService>(services).ShouldBeTrue();
    }


    [Fact]
    public void TheQuickEntryWindowAndFileDropAreOptIn()
    {
        var services = Register(b => b.UseShinyDesktop());

        Has<IQuickEntryPresenter>(services).ShouldBeFalse();
        Has<IFileDropService>(services).ShouldBeFalse();
        Initializers<DesktopQuickEntryInitializer>(services).ShouldBe(0);
        Initializers<FileDropInitializer>(services).ShouldBe(0);
    }


    [Fact]
    public void NotchOptionsReachTheService()
    {
        var services = Register(b => b.UseShinyDesktop(cfg => cfg.ConfigureNotch(o => o.MorphDuration = 123)));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<NotchWindowOptions>().MorphDuration.ShouldBe(123d);
    }


    [Fact]
    public void CallingItTwiceRegistersEverythingOnce()
    {
        var services = Register(b => b.UseShinyDesktop(cfg => cfg.UseQuickEntryWindow().UseFileDrop()), times: 2);

        services.Count(x => x.ServiceType == typeof(IQuickEntryPresenter)).ShouldBe(1);
        services.Count(x => x.ServiceType == typeof(IScreenGlowPresenter)).ShouldBe(1);
        services.Count(x => x.ServiceType == typeof(ITrayIconFactory)).ShouldBe(1);
        Initializers<DesktopQuickEntryInitializer>(services).ShouldBe(1);
        Initializers<FileDropInitializer>(services).ShouldBe(1);
    }
}
