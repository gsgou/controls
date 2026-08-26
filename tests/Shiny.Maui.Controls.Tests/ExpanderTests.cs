using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

[Collection(ApplicationResourcesCollection.Name)]
public class ExpanderTests
{
    public ExpanderTests() => _ = new Application();


    [Fact]
    public void TogglesBetweenOpenAndClosed()
    {
        var expander = new Expander { HeaderText = "Shipping" };

        expander.IsExpanded.ShouldBeFalse();

        expander.Toggle();
        expander.IsExpanded.ShouldBeTrue();

        expander.Toggle();
        expander.IsExpanded.ShouldBeFalse();
    }


    [Fact]
    public void RaisesTheStateEventsInOrder()
    {
        var expander = new Expander();
        var log = new List<string>();

        expander.Expanding += (_, _) => log.Add("expanding");
        expander.Expanded += (_, _) => log.Add("expanded");
        expander.Collapsing += (_, _) => log.Add("collapsing");
        expander.Collapsed += (_, _) => log.Add("collapsed");
        expander.ExpandedChanged += (_, e) => log.Add("changed:" + e.IsExpanded);

        expander.Expand();
        expander.Collapse();

        log.ShouldBe(["expanding", "expanded", "changed:True", "collapsing", "collapsed", "changed:False"]);
    }


    [Fact]
    public void CancellingExpandingLeavesItClosed()
    {
        var expander = new Expander();
        var expanded = 0;

        expander.Expanding += (_, e) => e.Cancel = true;
        expander.Expanded += (_, _) => expanded++;

        expander.Expand();

        expander.IsExpanded.ShouldBeFalse();
        expanded.ShouldBe(0);
    }


    [Fact]
    public void CancellingCollapsingLeavesItOpen()
    {
        var expander = new Expander { IsExpanded = true };
        expander.Collapsing += (_, e) => e.Cancel = true;

        expander.Collapse();

        expander.IsExpanded.ShouldBeTrue();
    }


    [Fact]
    public void TheCommandRunsWithTheNewState()
    {
        object? parameter = null;
        var expander = new Expander
        {
            ExpandedChangedCommand = new Command(p => parameter = p)
        };

        expander.Expand();

        parameter.ShouldBe(true);
    }


    [Fact]
    public void LazyContentIsNotBuiltUntilTheFirstExpand()
    {
        var built = 0;
        var expander = new Expander
        {
            LoadContentOnDemand = true,
            ContentTemplate = new DataTemplate(() =>
            {
                built++;
                return new Label { Text = "late" };
            })
        };

        built.ShouldBe(0);

        expander.Expand();
        built.ShouldBe(1);

        // Closing and reopening reuses what was built rather than building it again.
        expander.Collapse();
        expander.Expand();
        built.ShouldBe(1);
    }


    [Fact]
    public void EagerContentIsBuiltUpFront()
    {
        var built = 0;
        _ = new Expander
        {
            ContentTemplate = new DataTemplate(() =>
            {
                built++;
                return new Label();
            })
        };

        built.ShouldBe(1);
    }


    [Fact]
    public void ImplicitAppStyleDoesNotThrowDuringConstruction()
    {
        // StyleGuard cover: MAUI applies an implicit style from StyleableElement's own constructor,
        // before Expander's constructor has built the children those callbacks touch.
        var app = new Application();
        app.Resources.Add(new Style(typeof(Expander))
        {
            Setters =
            {
                new Setter { Property = Expander.HeaderTextProperty, Value = "Styled" },
                new Setter { Property = Expander.BorderColorProperty, Value = Colors.Red },
                new Setter { Property = Expander.CornerRadiusProperty, Value = 20d },
                new Setter { Property = Expander.AnimationProperty, Value = ExpanderAnimation.Fade },
                new Setter { Property = Expander.IndicatorModeProperty, Value = ExpanderIndicatorMode.Swap }
            }
        });

        var expander = Should.NotThrow(() => new Expander());

        // Applied, not merely survived.
        expander.HeaderText.ShouldBe("Styled");
        expander.BorderColor.ShouldBe(Colors.Red);
        expander.CornerRadius.ShouldBe(20d);
        expander.Animation.ShouldBe(ExpanderAnimation.Fade);
        expander.IndicatorMode.ShouldBe(ExpanderIndicatorMode.Swap);
    }

    [Fact]
    public void TheHeaderCarriesItsOwnAutomationId()
    {
        // The tap gesture lives on the header row, not on the expander, so automation driving the
        // expander's own id finds nothing to tap. This is the id that opens it.
        var expander = new Expander { AutomationId = "BasicExpander" };

        var header = FindHeader(expander);
        header.AutomationId.ShouldBe("BasicExpander" + Expander.HeaderAutomationIdSuffix);
    }


    [Fact]
    public void AnExpanderWithNoAutomationIdLeavesTheHeaderUnnamed()
    {
        var header = FindHeader(new Expander());

        header.AutomationId.ShouldBeNullOrEmpty();
    }


    [Fact]
    public void TheDefaultGlyphsAskForTextPresentation()
    {
        // Without U+FE0E, iOS draws U+25B6 as the glossy blue play-button emoji instead of a triangle.
        var expander = new Expander();

        expander.CollapsedIcon.ShouldBe("\u25B6\uFE0E");
        expander.ExpandedIcon.ShouldBe("\u25BC\uFE0E");
    }


    /// <summary>The header row is the one element in the tree carrying a tap gesture.</summary>
    static View FindHeader(Expander expander)
        => expander
            .GetVisualTreeDescendants()
            .OfType<View>()
            .First(x => x.GestureRecognizers.OfType<TapGestureRecognizer>().Any());
}
