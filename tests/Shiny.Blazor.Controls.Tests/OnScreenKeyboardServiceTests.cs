using Shiny.Blazor.Controls.OnScreenKeyboard;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// The host re-renders on every <c>VisibilityChanged</c>, and the browser reports focus in and out
/// far more often than the keyboard actually changes state — a field-to-field tab is two events for
/// one unchanged keyboard. So the service must only raise on a real transition.
/// </summary>
public class OnScreenKeyboardServiceTests
{
    [Fact]
    public void StartsHidden()
        => new OnScreenKeyboardService(new()).IsVisible.ShouldBeFalse();

    [Fact]
    public void ShowingRaisesOnceAndShowingAgainIsSilent()
    {
        var (service, raised) = Track();

        service.Show();
        service.Show();
        service.Show();

        service.IsVisible.ShouldBeTrue();
        raised.ShouldHaveSingleItem().ShouldBeTrue();
    }

    [Fact]
    public void HidingWhileAlreadyHiddenIsSilent()
    {
        var (service, raised) = Track();

        service.Hide();

        raised.ShouldBeEmpty();
    }

    [Fact]
    public void ToggleReportsEachTransition()
    {
        var (service, raised) = Track();

        service.Toggle();
        service.Toggle();

        service.IsVisible.ShouldBeFalse();
        raised.ShouldBe(new[] { true, false });
    }

    [Fact]
    public void TheOptionsInstanceIsTheOneHandedIn()
    {
        // The host reads HeightPx and the auto-show policy off this object on every render, which is
        // what lets an app change them at runtime. A copy would silently freeze them at startup.
        var options = new OnScreenKeyboardOptions { HeightPx = 320 };
        var service = new OnScreenKeyboardService(options);

        options.HeightPx = 200;

        service.Options.ShouldBeSameAs(options);
        service.Options.HeightPx.ShouldBe(200);
    }

    static (OnScreenKeyboardService Service, List<bool> Raised) Track()
    {
        var service = new OnScreenKeyboardService(new());
        var raised = new List<bool>();
        service.VisibilityChanged += (_, visible) => raised.Add(visible);
        return (service, raised);
    }
}
