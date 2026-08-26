using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The decision that stops the line hiding behind the chrome it is reporting on. The rule under test
/// is that a bar earns an inset exactly when it is painted inside the same coordinate space the line
/// is positioned in — every other arrangement already starts past it.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ProgressLineInsetTests
{
    public ProgressLineInsetTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    /// <summary>A page whose content is an overlay root, as every Shiny overlay leaves it.</summary>
    static ContentPage PageWithRoot(out PageOverlay.ShinyOverlayRoot root)
    {
        var page = new ContentPage { Content = new VerticalStackLayout() };
        root = PageOverlay.GetOrCreateRoot(page);
        return page;
    }


    [Fact]
    public void ATabBarInsideTheOverlayRootPushesTheLineUp()
    {
        var page = PageWithRoot(out var root);
        var layer = PageOverlay.GetOrCreateLayer<PageOverlay.TabBarLayer>(root, PageOverlay.Layers.TabBar);
        var bar = new ShinyTabBar { BarHeight = 62 };
        layer.Children.Add(bar);

        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Bottom)
            .ShouldBe(62);
    }


    /// <summary>
    /// The <c>ShinyTabbedPage</c> / native <c>TabbedPage</c> shape: the bar is a sibling of the whole
    /// overlay root, so the root's own space already ends above it and an inset would be a gap.
    /// </summary>
    [Fact]
    public void ATabBarOutsideTheOverlayRootEarnsNoInset()
    {
        var page = new ContentPage();
        var root = new PageOverlay.ShinyOverlayRoot();
        var host = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
        };
        var bar = new ShinyTabBar { BarHeight = 62 };

        host.Children.Add(root);
        host.Children.Add(bar);
        page.Content = host;

        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Bottom)
            .ShouldBe(0);
    }


    [Fact]
    public void ANavBarInsideTheOverlayRootPushesTheLineDown()
    {
        var page = PageWithRoot(out var root);
        var bar = new ShinyNavBar { BarHeight = 56 };
        root.Children.Add(bar);

        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Top)
            .ShouldBe(56);
    }


    /// <summary>
    /// The <c>ShinyNavigationPage</c> shape: the bar sits in row 0 of a host grid wrapping the root,
    /// so the root already begins below it.
    /// </summary>
    [Fact]
    public void ANavBarWrappingTheOverlayRootEarnsNoInset()
    {
        var page = new ContentPage();
        var root = new PageOverlay.ShinyOverlayRoot();
        var host = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }
        };
        var bar = new ShinyNavBar { BarHeight = 56 };

        Grid.SetRow(bar, 0);
        Grid.SetRow(root, 1);
        host.Children.Add(root);
        host.Children.Add(bar);
        page.Content = host;

        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Top)
            .ShouldBe(0);
    }


    /// <summary>
    /// A hidden bar occupies no space, so insetting for it would leave the line floating away from
    /// the edge for no visible reason.
    /// </summary>
    [Fact]
    public void AHiddenBarIsIgnored()
    {
        var page = PageWithRoot(out var root);
        root.Children.Add(new ShinyNavBar { BarHeight = 56, IsVisible = false });

        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Top)
            .ShouldBe(0);
    }


    /// <summary>
    /// MAUI hands a page inside a <c>NavigationPage</c> a content area that already excludes the
    /// native bar and the status bar behind it.
    /// </summary>
    [Fact]
    public void ANativeNavigationBarEarnsNoInset()
    {
        var page = new ContentPage { Content = new VerticalStackLayout() };
        var root = PageOverlay.GetOrCreateRoot(page);
        _ = new NavigationPage(page);

        NavigationPage.SetHasNavigationBar(page, true);

        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Top)
            .ShouldBe(0);
    }


    /// <summary>
    /// A <c>NavigationPage</c> whose bar the page turned off leaves the top edge to the line — and on
    /// a head with a notch, the safe area is then the line's problem rather than the bar's.
    /// </summary>
    [Fact]
    public void ANavigationPageWithItsBarOffFallsBackToTheSafeArea()
    {
        var page = new ContentPage { Content = new VerticalStackLayout() };
        var root = PageOverlay.GetOrCreateRoot(page);
        _ = new NavigationPage(page);

        NavigationPage.SetHasNavigationBar(page, false);

        // Zero on this TFM: only Apple's heads report an inset, and the tests do not run there.
        ProgressLineInsets
            .Resolve(page, root, ProgressLinePosition.Top)
            .ShouldBe(0);
    }
}
