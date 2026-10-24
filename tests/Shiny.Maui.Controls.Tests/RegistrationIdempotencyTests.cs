using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.QuickEntry;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Calling a registration twice - from a shared module and from the app, say - must not register its
/// presenters and startup hooks twice. These services are resolved as a list, so a duplicate is not
/// shadowed the way a second single registration is: it runs. Two feedback integrators fire every haptic
/// twice; two in-app quick entry presenters show two popups.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class RegistrationIdempotencyTests
{
    static IServiceCollection Twice(Action<MauiAppBuilder> register)
    {
        var builder = MauiApp.CreateBuilder();
        register(builder);
        register(builder);
        return builder.Services;
    }


    static int Count<TService, TImplementation>(IServiceCollection services)
        => services.Count(x => x.ServiceType == typeof(TService) && x.ImplementationType == typeof(TImplementation));


    [Fact]
    public void UseShinyControlsTwiceRegistersEachPresenterOnce()
    {
        using var _ = ThemeManagerState.Preserve();
        var services = Twice(b => b.UseShinyControls());

        Count<IQuickEntryPresenter, InAppQuickEntryPresenter>(services).ShouldBe(1);
        Count<IScreenGlowPresenter, InAppScreenGlowPresenter>(services).ShouldBe(1);
        Count<IMauiInitializeService, KeyboardShortcutInitializer>(services).ShouldBe(1);
    }


    [Fact]
    public void ControlFeedbackTwiceRegistersOneIntegrator()
    {
        using var _ = ThemeManagerState.Preserve();
        var services = Twice(b => b.UseShinyControls(cfg => cfg.AddDefaultMauiControlFeedback()));

        Count<IMauiInitializeService, MauiControlFeedbackIntegrator>(services).ShouldBe(1);
    }
}
