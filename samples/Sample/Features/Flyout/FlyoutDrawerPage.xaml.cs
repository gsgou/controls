using Microsoft.Extensions.DependencyInjection;
using Shiny.Maui.Controls.Flyout;

namespace Sample.Features.Flyout;

public partial class FlyoutDrawerPage : ContentPage
{
    public FlyoutDrawerPage()
    {
        InitializeComponent();
    }

    // Driven through the service rather than a named panel: with the declare-once install there is
    // nothing in this page's markup to name, and this is what a view model would do (inject
    // IFlyoutService there rather than resolving it, as here, from a page).
    void OnToggle(object? sender, EventArgs e)
    {
        var flyouts = this.Handler?.MauiContext?.Services.GetService<IFlyoutService>()
            ?? IPlatformApplication.Current?.Services.GetService<IFlyoutService>();

        _ = flyouts?.ToggleAsync();
    }
}
