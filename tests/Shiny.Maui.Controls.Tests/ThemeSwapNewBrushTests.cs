using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Themes;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// A theme swap has to hand a <c>Brush</c>-typed property a <em>new</em> brush. Recolouring the brush it
/// already holds is not enough on Windows, which keeps the native fill the view was first drawn with.
/// </summary>
/// <remarks>
/// That is how a walkthrough callout stayed dark: its card started in the tooltip's inverse surface,
/// switched to a light surface container, and on Windows kept the dark fill while its dark text vanished
/// on it. These brushes used to be bound to a hidden probe's colour (<c>ThemeProbe.Create</c>) or to a
/// colour token set on the brush itself (<c>ThemeTokens.TokenBrush</c>), and both recolour one brush in
/// place. Every assertion below checks the old brush was left alone and a different one took its place,
/// which only a brush token replaced along with the dictionary does.
/// </remarks>
[Collection(ApplicationResourcesCollection.Name)]
public class ThemeSwapNewBrushTests
{
    public ThemeSwapNewBrushTests()
    {
        TestDispatcherProvider.Install();
        TestDispatcherProvider.Instance.Timers.Clear();
        _ = new Application();
        Application.Current!.Resources.MergedDictionaries.Add(new BasicLightTheme());
    }


    static T OnPage<T>(T view) where T : View
    {
        var page = new ContentPage { Content = view };
        page.Parent = Application.Current;
        return view;
    }


    // Exactly what ShinyThemeManager does - remove the applied dictionary, add the next one.
    static void SwapToDark()
    {
        var app = Application.Current!;
        app.Resources.MergedDictionaries.Remove(app.Resources.MergedDictionaries.First());
        app.Resources.MergedDictionaries.Add(new BasicDarkTheme());
    }


    static Color Token(string colorKey) => (Color)Application.Current!.Resources[colorKey];


    static void ShouldSwapToANewBrush(Func<Brush?> read, string colorKey)
    {
        var light = read().ShouldBeOfType<SolidColorBrush>();
        var lightColor = Token(colorKey);
        light.Color.ShouldBe(lightColor);

        SwapToDark();

        var dark = read().ShouldBeOfType<SolidColorBrush>();
        dark.ShouldNotBeSameAs(light);
        dark.Color.ShouldBe(Token(colorKey));
        light.Color.ShouldBe(lightColor, "the brush the view already held was recoloured in place");
    }


    [Fact]
    public void TooltipCardFill()
    {
        var bubble = OnPage(new TooltipBubble());
        ShouldSwapToANewBrush(() => bubble.Card.Background, ShinyThemeKeys.Color.InverseSurface);
    }


    [Fact]
    public void TooltipCardStroke()
    {
        var bubble = OnPage(new TooltipBubble());
        ShouldSwapToANewBrush(() => bubble.Card.Stroke, ShinyThemeKeys.Color.OutlineVariant);
    }


    [Fact]
    public void WalkthroughCalloutFill()
    {
        var bubble = OnPage(new TooltipBubble());
        var tooltipFill = bubble.Card.Background;

        // The walkthrough's switch from the tooltip's surface to its own is itself a new brush.
        bubble.FillToken = ShinyThemeKeys.Color.SurfaceContainerHigh;
        bubble.Card.Background.ShouldNotBeSameAs(tooltipFill);

        ShouldSwapToANewBrush(() => bubble.Card.Background, ShinyThemeKeys.Color.SurfaceContainerHigh);
    }


    [Fact]
    public void AnExplicitCalloutColourWins_AndClearingItGoesBackToTheTheme()
    {
        var bubble = OnPage(new TooltipBubble { BubbleColor = Colors.Red });
        ((SolidColorBrush)bubble.Card.Background).Color.ShouldBe(Colors.Red);

        bubble.BubbleColor = null;
        ShouldSwapToANewBrush(() => bubble.Card.Background, ShinyThemeKeys.Color.InverseSurface);
    }


    [Fact]
    public void NavBarSurface()
    {
        var bar = OnPage(new ShinyNavBar());
        ShouldSwapToANewBrush(() => bar.Surface.Background, ShinyThemeKeys.Color.SurfaceContainer);
    }


    [Fact]
    public void NavBarSurfaceGoesBackToTheThemeAfterAnExplicitBrush()
    {
        var bar = OnPage(new ShinyNavBar { BarBackground = new SolidColorBrush(Colors.Red) });
        ((SolidColorBrush)bar.Surface.Background).Color.ShouldBe(Colors.Red);

        bar.BarBackground = null;
        ShouldSwapToANewBrush(() => bar.Surface.Background, ShinyThemeKeys.Color.SurfaceContainer);
    }


    [Fact]
    public void ExpanderStroke()
    {
        var expander = OnPage(new Expander());
        var border = (Border)expander.Children[0];
        ShouldSwapToANewBrush(() => border.Stroke, ShinyThemeKeys.Color.OutlineVariant);
    }


    [Fact]
    public void TooltipShadow()
    {
        var bubble = OnPage(new TooltipBubble());
        ShouldSwapToANewBrush(() => bubble.Card.Shadow.Brush, ShinyThemeKeys.Color.Shadow);
    }


    [Fact]
    public void ButtonShadow()
    {
        var button = OnPage(new ShinyButton { Text = "Raised", HasShadow = true });
        var border = (Border)button.Content!;
        ShouldSwapToANewBrush(() => border.Shadow.Brush, ShinyThemeKeys.Color.Shadow);
    }
}
