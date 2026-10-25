using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// <see cref="TabLabelPosition"/> and the header templates - where the label goes, which setting
/// wins, and what a template is handed.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class ShinyTabLabelPositionTests
{
    public ShinyTabLabelPositionTests()
    {
        TestDispatcherProvider.Install();
        _ = new Application();
    }

    static ShinyTabBar Build(params string[] titles)
    {
        var bar = new ShinyTabBar { AnimationDuration = 0 };
        foreach (var title in titles)
            bar.Items.Add(new ShinyTabItem { Title = title, Icon = "home", Route = title.ToLowerInvariant() });

        return bar;
    }

    static int LabelIndex(ShinyTabBar bar, int cell)
        => bar.LabelRowAt(cell).Children.IndexOf(bar.LabelAt(cell));


    [Theory]
    [InlineData(TabLabelPosition.Default, TabLabelPosition.Default, TabLabelPosition.Bottom)]
    [InlineData(TabLabelPosition.Default, TabLabelPosition.Top, TabLabelPosition.Top)]
    [InlineData(TabLabelPosition.End, TabLabelPosition.Top, TabLabelPosition.End)]
    [InlineData(TabLabelPosition.Hidden, TabLabelPosition.Bottom, TabLabelPosition.Hidden)]
    public void TheTabBeatsTheBarAndDefaultMeansBottom(TabLabelPosition item, TabLabelPosition bar, TabLabelPosition expected)
        => ShinyTabBar.ResolveLabelPosition(item, bar).ShouldBe(expected);


    [Fact]
    public void LabelsSitUnderTheIconByDefault()
    {
        var bar = Build("One", "Two");

        bar.LabelRowAt(0).Orientation.ShouldBe(StackOrientation.Vertical);
        LabelIndex(bar, 0).ShouldBe(1);
    }


    [Fact]
    public void TopPutsTheLabelAboveTheIcon()
    {
        var bar = Build("One", "Two");

        bar.LabelPosition = TabLabelPosition.Top;

        bar.LabelRowAt(0).Orientation.ShouldBe(StackOrientation.Vertical);
        LabelIndex(bar, 0).ShouldBe(0);
    }


    [Theory]
    [InlineData(TabLabelPosition.Start, 0)]
    [InlineData(TabLabelPosition.End, 1)]
    public void StartAndEndPutTheLabelBesideTheIcon(TabLabelPosition position, int labelIndex)
    {
        var bar = Build("One", "Two");

        bar.LabelPosition = position;

        bar.LabelRowAt(1).Orientation.ShouldBe(StackOrientation.Horizontal);
        LabelIndex(bar, 1).ShouldBe(labelIndex);
    }


    [Fact]
    public void OneTabCanOverrideTheBar()
    {
        var bar = Build("One", "Two");
        bar.LabelPosition = TabLabelPosition.End;

        bar.Items[0].LabelPosition = TabLabelPosition.Top;

        bar.LabelRowAt(0).Orientation.ShouldBe(StackOrientation.Vertical);
        LabelIndex(bar, 0).ShouldBe(0);
        bar.LabelRowAt(1).Orientation.ShouldBe(StackOrientation.Horizontal);
    }


    [Fact]
    public void HiddenBeatsLabelModeAlways()
    {
        var bar = Build("One", "Two");
        bar.LabelMode = TabLabelMode.Always;

        bar.Items[1].LabelPosition = TabLabelPosition.Hidden;

        bar.LabelAt(0).IsVisible.ShouldBeTrue();
        bar.LabelAt(1).IsVisible.ShouldBeFalse();
    }


    [Fact]
    public void LabelModeStillHidesAPositionedLabel()
    {
        var bar = Build("One", "Two");
        bar.LabelPosition = TabLabelPosition.Start;

        bar.LabelMode = TabLabelMode.SelectedOnly;

        bar.LabelAt(0).IsVisible.ShouldBeTrue();
        bar.LabelAt(1).IsVisible.ShouldBeFalse();
    }


    [Fact]
    public void ABarTemplateReplacesEveryCell()
    {
        var bar = Build("One", "Two");

        bar.TabTemplate = new DataTemplate(() => new Label());

        bar.HeaderAt(0).ShouldNotBeNull();
        bar.HeaderAt(1).ShouldNotBeNull();
        bar.IconAreaAt(0).Children.OfType<Label>().Single().BindingContext.ShouldBeSameAs(bar.HeaderAt(0));
    }


    [Fact]
    public void ATabTemplateBeatsTheBarTemplate()
    {
        var bar = Build("One", "Two");
        bar.TabTemplate = new DataTemplate(() => new Label());

        bar.Items[1].HeaderTemplate = new DataTemplate(() => new Button());

        bar.IconAreaAt(0).Children.OfType<Label>().ShouldHaveSingleItem();
        bar.IconAreaAt(1).Children.OfType<Button>().ShouldHaveSingleItem();
    }


    [Fact]
    public void ATemplateSelectorIsHandedTheTab()
    {
        var bar = Build("One", "Two");
        var selector = new RouteSelector();

        bar.TabTemplate = selector;

        selector.Seen.ShouldBe(["one", "two"]);
    }


    [Fact]
    public void TheHeaderContextFollowsSelectionAndBadges()
    {
        var bar = Build("One", "Two");
        bar.TabTemplate = new DataTemplate(() => new Label());

        bar.HeaderAt(0)!.IsSelected.ShouldBeTrue();
        bar.HeaderAt(1)!.IsSelected.ShouldBeFalse();
        bar.HeaderAt(1)!.Title.ShouldBe("Two");

        bar.SelectedIndex = 1;
        bar.Items[0].Badge = "3";

        bar.HeaderAt(0)!.IsSelected.ShouldBeFalse();
        bar.HeaderAt(1)!.IsSelected.ShouldBeTrue();
        bar.HeaderAt(0)!.Badge.ShouldBe("3");
        bar.HeaderAt(0)!.HasBadge.ShouldBeTrue();
    }


    [Fact]
    public void TheHeaderIsHandedTheBarsIcon()
    {
        var bar = Build("One", "Two");
        bar.TabTemplate = new DataTemplate(() => new Label());

        bar.HeaderAt(0)!.Icon.ShouldNotBeNull();
        bar.HeaderAt(0)!.Icon.ShouldBeSameAs(bar.IconViewAt(0));
    }


    [Fact]
    public void TheHeaderReportsTheLabelRules()
    {
        var bar = Build("One", "Two");
        bar.TabTemplate = new DataTemplate(() => new Label());

        bar.Items[1].LabelPosition = TabLabelPosition.Hidden;

        bar.HeaderAt(0)!.IsLabelVisible.ShouldBeTrue();
        bar.HeaderAt(0)!.LabelPosition.ShouldBe(TabLabelPosition.Bottom);
        bar.HeaderAt(1)!.IsLabelVisible.ShouldBeFalse();
    }


    [Fact]
    public void ATemplateThatIsNotAViewThrows()
    {
        var bar = Build("One");

        Should.Throw<InvalidOperationException>(() => bar.TabTemplate = new DataTemplate(() => new ShinyTabItem()));
    }


    sealed class RouteSelector : DataTemplateSelector
    {
        public List<string?> Seen { get; } = new();

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            this.Seen.Add(((ShinyTabItem)item).Route);
            return new DataTemplate(() => new Label());
        }
    }
}
