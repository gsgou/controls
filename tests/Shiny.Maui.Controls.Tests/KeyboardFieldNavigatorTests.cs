using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The accessory bar's prev/next arrows are only as good as the run of fields behind them. This is the
/// one part of the keyboard accessory with real logic and no platform in it, so it is the part that
/// gets tested.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class KeyboardFieldNavigatorTests
{
    static ContentPage BuildPage(params View[] children)
    {
        new Application();

        var layout = new VerticalStackLayout();
        foreach (var child in children)
            layout.Children.Add(child);

        return new ContentPage { Content = new ScrollView { Content = layout } };
    }

    [Fact]
    public void Collect_ReturnsFieldsInVisualTreeOrder()
    {
        var first = new TextEntry { Placeholder = "First" };
        var second = new TextEntry { Placeholder = "Second" };
        var third = new TextEntry { Placeholder = "Third" };
        BuildPage(first, second, third);

        var fields = KeyboardFieldNavigator.Collect(second);

        fields.Count.ShouldBe(3);
        fields[0].ShouldBeSameAs(first);
        fields[1].ShouldBeSameAs(second);
        fields[2].ShouldBeSameAs(third);
    }

    [Fact]
    public void Collect_IncludesPlainMauiInputs()
    {
        var shiny = new TextEntry();
        var plain = new Entry();
        BuildPage(shiny, plain);

        KeyboardFieldNavigator.Collect(shiny).Count.ShouldBe(2);
    }

    [Fact]
    public void Collect_StopsAtTheWrapper_NotTheEntryInsideIt()
    {
        // TextEntry composes a BorderlessEntry internally. Walking into it would list every field
        // twice and make "next" a no-op that lands on the same control.
        var entry = new TextEntry();
        BuildPage(entry);

        var fields = KeyboardFieldNavigator.Collect(entry);

        fields.Count.ShouldBe(1);
        fields[0].ShouldBeSameAs(entry);
    }

    [Fact]
    public void Collect_SkipsDisabledInvisibleAndReadOnlyFields()
    {
        var current = new TextEntry();
        var disabled = new TextEntry { IsEnabled = false };
        var hidden = new TextEntry { IsVisible = false };
        var readOnly = new TextEntry { IsReadOnly = true };
        BuildPage(current, disabled, hidden, readOnly);

        var fields = KeyboardFieldNavigator.Collect(current);

        fields.Count.ShouldBe(1);
        fields[0].ShouldBeSameAs(current);
    }

    [Fact]
    public void Collect_SkipsFieldsInsideAHiddenContainer()
    {
        var current = new TextEntry();
        var buried = new TextEntry();
        var hiddenGroup = new VerticalStackLayout { IsVisible = false };
        hiddenGroup.Children.Add(buried);

        BuildPage(current, hiddenGroup);

        KeyboardFieldNavigator.Collect(current).Count.ShouldBe(1);
    }

    [Fact]
    public void Collect_FiltersToTheCurrentFieldsGroup()
    {
        var payment1 = new TextEntry { FieldGroup = "payment" };
        var payment2 = new TextEntry { FieldGroup = "payment" };
        var unrelated = new TextEntry();
        BuildPage(payment1, unrelated, payment2);

        var fields = KeyboardFieldNavigator.Collect(payment1);

        fields.Count.ShouldBe(2);
        fields.ShouldNotContain(unrelated);
    }

    [Fact]
    public void CanMove_IsFalseAtTheEndsOfTheRun()
    {
        var first = new TextEntry();
        var last = new TextEntry();
        BuildPage(first, last);

        KeyboardFieldNavigator.CanMove(first, KeyboardNavigationDirection.Previous).ShouldBeFalse();
        KeyboardFieldNavigator.CanMove(first, KeyboardNavigationDirection.Next).ShouldBeTrue();
        KeyboardFieldNavigator.CanMove(last, KeyboardNavigationDirection.Next).ShouldBeFalse();
        KeyboardFieldNavigator.CanMove(last, KeyboardNavigationDirection.Previous).ShouldBeTrue();
    }

    [Fact]
    public void Move_ReturnsFalseWhenThereIsNowhereToGo()
    {
        var only = new TextEntry();
        BuildPage(only);

        KeyboardFieldNavigator.Move(only, KeyboardNavigationDirection.Next).ShouldBeFalse();
        KeyboardFieldNavigator.Move(only, KeyboardNavigationDirection.Previous).ShouldBeFalse();
    }

    [Fact]
    public void Collect_HandlesAFieldWithNoPage()
    {
        // A field built in code and not yet parented still has to answer, rather than throw from a
        // property-changed callback on the accessory item.
        new Application();
        var orphan = new TextEntry();

        var fields = KeyboardFieldNavigator.Collect(orphan);

        fields.Count.ShouldBe(1);
        fields[0].ShouldBeSameAs(orphan);
    }
}
