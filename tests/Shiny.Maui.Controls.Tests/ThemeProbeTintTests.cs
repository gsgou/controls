using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Infrastructure;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// <see cref="ThemeProbe.Tint"/> is the "leave it unset and it follows the theme" seam every Shiny
/// control shares, so the round trip through an explicit colour and back has to actually round-trip.
/// It did not: <c>SetValue</c> writes a <em>local</em> value, a local value outranks a dynamic
/// resource, and so the return leg set a resource that was silently outranked by the colour still in
/// the local slot. Nothing threw — the control just kept the previous colour, which is how a
/// walkthrough callout following a spotlight step (transparent fill) rendered its card-less text on
/// the dim instead of coming back as a card.
/// </summary>
public class ThemeProbeTintTests
{
    const string Token = "TintTestColor";

    static (Application App, BoxView Probe) Host(Color seed)
    {
        // Dynamic resources only resolve for elements in the application's element tree.
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { { Token, seed } });

        var probe = new BoxView();
        var page = new ContentPage { Content = new VerticalStackLayout { Children = { probe } } };
        page.Parent = app;

        return (app, probe);
    }


    [Fact]
    public void UnsetFollowsTheToken()
    {
        var (_, probe) = Host(Colors.BlueViolet);

        ThemeProbe.Tint(probe, BoxView.ColorProperty, null, Token);

        probe.Color.ShouldBe(Colors.BlueViolet);
    }


    [Fact]
    public void ExplicitColourWins()
    {
        var (_, probe) = Host(Colors.BlueViolet);

        ThemeProbe.Tint(probe, BoxView.ColorProperty, Colors.Orange, Token);

        probe.Color.ShouldBe(Colors.Orange);
    }


    [Fact]
    public void GoingBackToTheTokenDropsTheExplicitColour()
    {
        var (_, probe) = Host(Colors.BlueViolet);

        ThemeProbe.Tint(probe, BoxView.ColorProperty, Colors.Transparent, Token);
        probe.Color.ShouldBe(Colors.Transparent);

        ThemeProbe.Tint(probe, BoxView.ColorProperty, null, Token);

        probe.Color.ShouldBe(Colors.BlueViolet);
    }


    [Fact]
    public void AfterTheRoundTripItStillTracksAThemeSwap()
    {
        var (app, probe) = Host(Colors.BlueViolet);

        ThemeProbe.Tint(probe, BoxView.ColorProperty, Colors.Transparent, Token);
        ThemeProbe.Tint(probe, BoxView.ColorProperty, null, Token);

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { { Token, Colors.SeaGreen } });

        probe.Color.ShouldBe(Colors.SeaGreen);
    }
}
