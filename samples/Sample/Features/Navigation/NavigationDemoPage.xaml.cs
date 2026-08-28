using Shiny.Maui.Controls;

namespace Sample.Features.Navigation;

public partial class NavigationDemoPage : ContentPage
{
    public NavigationDemoPage()
    {
        this.InitializeComponent();
        SampleSourceCode.Attach(this);
    }

    // Shell is not a host for a NavigationPage, so the demo stack is presented modally - which is
    // also the shape a real app uses for a self-contained flow with its own chrome.
    void OnOpen(object? sender, EventArgs e)
        => _ = this.Navigation.PushModalAsync(new ShinyNavigationPage(new NavInboxPage()));

    void OnOpenLarge(object? sender, EventArgs e)
        => _ = this.Navigation.PushModalAsync(new ShinyNavigationPage(new NavInboxPage())
        {
            LargeTitleDisplay = LargeTitleDisplay.Collapsing
        });
}
