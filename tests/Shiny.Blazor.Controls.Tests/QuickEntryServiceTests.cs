using Microsoft.Extensions.DependencyInjection;
using Shiny.Blazor.Controls;
using Shiny.Blazor.Controls.QuickEntry;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// The quick entry service's state machine — open/close, the glow triggers, and the lifetime it is
/// registered with. Rendering is <c>QuickEntryHost</c>'s job and is not exercised here.
/// </summary>
public class QuickEntryServiceTests
{
    static QuickEntryService Build(Action<QuickEntryOptions>? configure = null)
    {
        var options = new QuickEntryOptions();
        configure?.Invoke(options);
        return new QuickEntryService(options);
    }

    [Fact]
    public void Toggle_flips_open_state_and_raises_both_events()
    {
        var service = Build();
        var opened = 0;
        var closed = 0;
        service.Opened += (_, _) => opened++;
        service.Closed += (_, _) => closed++;

        service.Toggle();
        service.IsOpen.ShouldBeTrue();

        service.Toggle();
        service.IsOpen.ShouldBeFalse();

        opened.ShouldBe(1);
        closed.ShouldBe(1);
    }

    [Fact]
    public void Opening_twice_raises_Opened_once()
    {
        var service = Build();
        var opened = 0;
        service.Opened += (_, _) => opened++;

        service.Show();
        service.Show();

        opened.ShouldBe(1);
    }

    [Fact]
    public void WhileOpen_lights_the_glow_with_the_popup_and_puts_it_out_again()
    {
        var service = Build(o => o.ScreenGlow = ScreenGlowTrigger.WhileOpen);

        service.Show();
        service.IsGlowVisible.ShouldBeTrue();

        service.Hide();
        service.IsGlowVisible.ShouldBeFalse();
    }

    [Fact]
    public void WhileBusy_lights_the_glow_from_the_prompt_rather_than_the_popup()
    {
        var service = Build(o => o.ScreenGlow = ScreenGlowTrigger.WhileBusy);

        service.Show();
        service.IsGlowVisible.ShouldBeFalse("the popup being open is not the trigger");

        service.Prompt.IsBusy = true;
        service.IsGlowVisible.ShouldBeTrue();

        service.Prompt.IsBusy = false;
        service.IsGlowVisible.ShouldBeFalse();
    }

    [Fact]
    public void Busy_while_closed_does_not_light_the_glow()
    {
        var service = Build(o => o.ScreenGlow = ScreenGlowTrigger.WhileBusy);
        service.Prompt.IsBusy = true;
        service.IsGlowVisible.ShouldBeFalse();
    }

    [Fact]
    public void Closing_puts_the_glow_out_however_it_was_lit()
    {
        var service = Build(o => o.ScreenGlow = ScreenGlowTrigger.WhileBusy);
        service.Show();
        service.Prompt.IsBusy = true;

        service.Hide();

        // A glow left burning with nothing on screen would be inexplicable.
        service.IsGlowVisible.ShouldBeFalse();
    }

    [Fact]
    public void The_glow_can_be_driven_with_no_popup_at_all()
    {
        var service = Build();
        service.ShowGlow();

        service.IsGlowVisible.ShouldBeTrue();
        service.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void Prompt_changes_notify_the_host_to_re_render()
    {
        var service = Build();
        var changes = 0;
        service.Changed += (_, _) => changes++;

        service.Prompt.Text = "hello";
        service.Prompt.IsBusy = true;

        changes.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Setting_the_same_value_does_not_notify()
    {
        var service = Build();
        service.Prompt.Text = "hello";

        var changes = 0;
        service.Changed += (_, _) => changes++;
        service.Prompt.Text = "hello";

        changes.ShouldBe(0);
    }

    [Fact]
    public void Registered_scoped_so_one_user_cannot_see_another_users_popup()
    {
        var services = new ServiceCollection().AddShinyQuickEntry();

        // Blazor Server runs one scope per circuit; a singleton here would put one user's popup on
        // every connected user's screen.
        foreach (var type in new[] { typeof(QuickEntryOptions), typeof(QuickEntryService), typeof(IQuickEntryService) })
        {
            services
                .Single(d => d.ServiceType == type)
                .Lifetime
                .ShouldBe(ServiceLifetime.Scoped, type.Name);
        }
    }

    [Fact]
    public void AddShinyControls_covers_quick_entry()
    {
        var services = new ServiceCollection().AddShinyControls();
        services.ShouldContain(d => d.ServiceType == typeof(IQuickEntryService));
    }
}
