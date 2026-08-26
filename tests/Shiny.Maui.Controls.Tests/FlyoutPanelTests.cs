using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Flyout;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>What the panel itself decides: which body is showing, and what a page-wide install wraps.</summary>
[Collection(ApplicationResourcesCollection.Name)]
public class FlyoutPanelTests
{
    public FlyoutPanelTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }


    [Fact]
    public void CollapsedShowsTheRailInsteadOfTheBody()
    {
        var body = new Label { Text = "body" };
        var rail = new Label { Text = "rail" };
        var panel = new FlyoutPanel { PanelContent = body, RailContent = rail };
        _ = new FlyoutView { Start = panel, IsAnimationEnabled = false };

        panel.State = FlyoutPanelState.Collapsed;
        rail.Parent.ShouldNotBeNull();
        IsShowing(rail).ShouldBeTrue();
        IsShowing(body).ShouldBeFalse();

        panel.State = FlyoutPanelState.Expanded;
        IsShowing(rail).ShouldBeFalse();
        IsShowing(body).ShouldBeTrue();
    }


    /// <summary>
    /// Without rail content there is nothing to swap to, so the collapsed panel keeps showing the
    /// leading edge of the body rather than going blank.
    /// </summary>
    [Fact]
    public void CollapsedWithoutRailContentKeepsTheBody()
    {
        var body = new Label { Text = "body" };
        var panel = new FlyoutPanel { PanelContent = body };
        _ = new FlyoutView { Start = panel, IsAnimationEnabled = false };

        panel.State = FlyoutPanelState.Collapsed;

        IsShowing(body).ShouldBeTrue();
    }


    [Fact]
    public void AssigningToASideSetsTheSide()
    {
        var panel = new FlyoutPanel();
        var view = new FlyoutView { End = panel };

        panel.Side.ShouldBe(FlyoutSide.End);
        panel.Host.ShouldBe(view);
    }


    [Fact]
    public void ReplacingAPanelReleasesTheOldOne()
    {
        var first = new FlyoutPanel();
        var view = new FlyoutView { Start = first };

        view.Start = new FlyoutPanel();

        first.Host.ShouldBeNull();
        view.GetPanel(FlyoutSide.Start).ShouldNotBe(first);
    }


    /// <summary>Every ancestor between the view and the panel has to be visible for it to be on screen.</summary>
    static bool IsShowing(View view)
    {
        Element? current = view;
        while (current is not null)
        {
            if (current is View v && !v.IsVisible)
                return false;

            if (current is FlyoutPanel)
                return true;

            current = current.Parent;
        }
        return true;
    }
}


/// <summary>The declare-once install: what it wraps, and what it carries from page to page.</summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ShinyFlyoutInstallTests
{
    public ShinyFlyoutInstallTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }


    static DataTemplate PanelTemplate() => new(() => new FlyoutPanel
    {
        State = FlyoutPanelState.Collapsed,
        PanelContent = new Label { Text = "nav" }
    });


    [Fact]
    public void InstallingOnAPageWrapsItsContent()
    {
        var body = new Label { Text = "body" };
        var page = new ContentPage { Content = body };

        ShinyFlyout.SetStartTemplate(page, PanelTemplate());

        var view = ShinyFlyout.GetFlyoutView(page);
        view.ShouldNotBeNull();
        page.Content.ShouldBe(view);
        view.Content.ShouldBe(body);
        view.GetPanel(FlyoutSide.Start).ShouldNotBeNull();
    }


    /// <summary>
    /// A <see cref="ShinyContentPage"/> keeps its own root grid — that is where the overlay host for
    /// toasts and dialogs lives, and wrapping it would put the flyout above them.
    /// </summary>
    [Fact]
    public void InstallingOnAShinyContentPageWrapsThePageContent()
    {
        var body = new Label { Text = "body" };
        var page = new ShinyContentPage { PageContent = body };

        ShinyFlyout.SetStartTemplate(page, PanelTemplate());

        var view = ShinyFlyout.GetFlyoutView(page);
        view.ShouldNotBeNull();
        page.PageContent.ShouldBe(view);
        view.Content.ShouldBe(body);
    }


    /// <summary>
    /// XAML applies properties in document order, so a template declared above the content is set
    /// while the page still has none. Installing then would wrap nothing and be overwritten by the
    /// content assignment a line later.
    /// </summary>
    [Fact]
    public void InstallingBeforeTheContentExistsWaitsForIt()
    {
        var page = new ContentPage();

        ShinyFlyout.SetStartTemplate(page, PanelTemplate());
        ShinyFlyout.GetFlyoutView(page).ShouldBeNull();

        var body = new Label { Text = "body" };
        page.Content = body;

        var view = ShinyFlyout.GetFlyoutView(page);
        view.ShouldNotBeNull();
        page.Content.ShouldBe(view);
        view.Content.ShouldBe(body);
    }


    /// <summary>
    /// Each page builds its own panel from the template, so the thing that has to survive a
    /// navigation is the state — a drawer left open must still be open on the page you land on.
    /// </summary>
    [Fact]
    public void CarriesTheStateFromOnePageToTheNext()
    {
        var host = new NavigationPage();
        ShinyFlyout.SetStartTemplate(host, PanelTemplate());

        var first = new ContentPage { Content = new Label() };
        ShinyFlyout.InstallOn(host, first);
        ShinyFlyout.GetFlyoutView(first)!.GetPanel(FlyoutSide.Start)!.State = FlyoutPanelState.Expanded;

        var second = new ContentPage { Content = new Label() };
        ShinyFlyout.InstallOn(host, second);

        ShinyFlyout.GetFlyoutView(second)!.GetPanel(FlyoutSide.Start)!.State.ShouldBe(FlyoutPanelState.Expanded);
    }


    [Fact]
    public void APageThatBringsItsOwnFlyoutIsLeftAlone()
    {
        var page = new ShinyFlyoutPage { Detail = new Label() };

        ShinyFlyout.SetStartTemplate(page, PanelTemplate());

        ShinyFlyout.GetFlyoutView(page).ShouldBeNull();
    }


    [Fact]
    public void EachPageGetsItsOwnPanelInstance()
    {
        var template = PanelTemplate();
        var first = new ContentPage { Content = new Label() };
        var second = new ContentPage { Content = new Label() };

        ShinyFlyout.SetStartTemplate(first, template);
        ShinyFlyout.SetStartTemplate(second, template);

        var one = ShinyFlyout.GetFlyoutView(first)!.GetPanel(FlyoutSide.Start);
        var two = ShinyFlyout.GetFlyoutView(second)!.GetPanel(FlyoutSide.Start);

        one.ShouldNotBeNull();
        two.ShouldNotBeNull();
        one.ShouldNotBe(two);
    }


    [Fact]
    public void ATemplateThatDoesNotMakeAPanelSaysSo()
    {
        var page = new ContentPage { Content = new Label() };
        var template = new DataTemplate(() => new Label());

        Should.Throw<InvalidOperationException>(() => ShinyFlyout.SetStartTemplate(page, template));
    }
}
