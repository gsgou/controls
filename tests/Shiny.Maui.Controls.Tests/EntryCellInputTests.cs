using Microsoft.Maui.Controls;
using EntryCell = Shiny.Maui.Controls.Cells.EntryCell;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// EntryCell borrows TextEntry's masking and keyboard accessory, but keeps its own chrome. These
/// cover the borrowed halves: that the raw/formatted split matches TextEntry's contract, and that the
/// accessory's field navigation treats the cell as the input inside it rather than the cell itself.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class EntryCellInputTests
{
    [Fact]
    public void Mask_KeepsValueTextRawAndFormatsSeparately()
    {
        new Application();

        var cell = new EntryCell { Mask = "(###) ###-####" };
        cell.ValueText = "5551234567";

        // The binding target stays raw; the mask only decides what is displayed.
        cell.ValueText.ShouldBe("5551234567");
        cell.FormattedValueText.ShouldBe("(555) 123-4567");
    }

    [Fact]
    public void Mask_ClipsInputToTheNumberOfSlots()
    {
        new Application();

        var cell = new EntryCell { Mask = "##/####" };
        cell.ValueText = "1220267777";

        cell.FormattedValueText.ShouldBe("12/2026");
    }

    [Fact]
    public void Mask_SetBeforeValue_StillFormats()
    {
        new Application();

        // Order matters: a mask applied after the value has to reformat what is already there.
        var cell = new EntryCell();
        cell.ValueText = "5551234567";
        cell.Mask = "(###) ###-####";

        cell.FormattedValueText.ShouldBe("(555) 123-4567");
    }

    [Fact]
    public void ClearingTheMask_ReturnsTheFieldToTheRawValue()
    {
        new Application();

        var cell = new EntryCell { Mask = "(###) ###-####" };
        cell.ValueText = "5551234567";
        cell.Mask = null;

        cell.FormattedValueText.ShouldBe(string.Empty);
        cell.ValueText.ShouldBe("5551234567");
    }

    [Fact]
    public void Navigation_CollectsTheInputInsideTheCell_NotTheCell()
    {
        new Application();

        var cell = new EntryCell();
        var layout = new VerticalStackLayout();
        layout.Children.Add(cell);
        _ = new ContentPage { Content = layout };

        var host = (IKeyboardAccessoryHost)cell;
        var fields = KeyboardFieldNavigator.Collect(host.NavigationElement);

        // A cell is a ContentView, so the walk descends into it and stops on the entry.
        fields.Count.ShouldBe(1);
        fields[0].ShouldBeSameAs(host.NavigationElement);
        host.NavigationElement.ShouldNotBeSameAs(cell);
    }

    [Fact]
    public void FieldGroup_ScopesNavigationToTheGroup()
    {
        new Application();

        var grouped1 = new EntryCell { FieldGroup = "payment" };
        var ungrouped = new EntryCell();
        var grouped2 = new EntryCell { FieldGroup = "payment" };

        var layout = new VerticalStackLayout();
        layout.Children.Add(grouped1);
        layout.Children.Add(ungrouped);
        layout.Children.Add(grouped2);
        _ = new ContentPage { Content = layout };

        var current = ((IKeyboardAccessoryHost)grouped1).NavigationElement;
        var fields = KeyboardFieldNavigator.Collect(current);

        fields.Count.ShouldBe(2);
        fields.ShouldNotContain(((IKeyboardAccessoryHost)ungrouped).NavigationElement);

        // First in its group: nowhere back, somewhere forward.
        KeyboardFieldNavigator.CanMove(current, KeyboardNavigationDirection.Previous).ShouldBeFalse();
        KeyboardFieldNavigator.CanMove(current, KeyboardNavigationDirection.Next).ShouldBeTrue();
    }

    [Fact]
    public void TextEntryAndEntryCell_ShareOneNavigationRun()
    {
        new Application();

        var entry = new TextEntry();
        var cell = new EntryCell();
        var layout = new VerticalStackLayout();
        layout.Children.Add(entry);
        layout.Children.Add(cell);
        _ = new ContentPage { Content = layout };

        var fields = KeyboardFieldNavigator.Collect(entry);

        // The TextEntry is collected as its wrapper, the cell as its inner input - both are stops.
        fields.Count.ShouldBe(2);
        fields[0].ShouldBeSameAs(entry);
        fields[1].ShouldBeSameAs(((IKeyboardAccessoryHost)cell).NavigationElement);
    }
}
