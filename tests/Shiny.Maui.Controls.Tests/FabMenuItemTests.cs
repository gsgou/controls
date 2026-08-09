using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The pill layout: items take their icon-chip side and their scale anchor from the owning menu's
/// <see cref="FabMenu.MenuAlignment"/>, so that every chip centre lands on the main FAB's axis.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class FabMenuItemTests
{
    [Fact]
    public void Defaults_AreThePillDefaults()
    {
        new Application();

        var item = new FabMenuItem();

        item.Size.ShouldBe(44d);
        item.IconSize.ShouldBe(20d);
        // A hairline outline is part of the look - 0 is the opt-out, not the default.
        item.BorderThickness.ShouldBe(1d);
        item.HasShadow.ShouldBeTrue();
    }

    [Fact]
    public void Items_AnchorToTheTrailingEdgeByDefault()
    {
        new Application();

        var item = new FabMenuItem { Text = "Share" };
        var menu = new FabMenu();
        menu.Items.Add(item);

        item.HorizontalOptions.ShouldBe(LayoutOptions.End);
        // Scale-in grows out of the main FAB, which sits on the trailing edge.
        item.AnchorX.ShouldBe(1d);
    }

    [Fact]
    public void StartAlignment_FlipsTheItemsToTheLeadingEdge()
    {
        new Application();

        var item = new FabMenuItem { Text = "Share" };
        var menu = new FabMenu { MenuAlignment = LayoutOptions.Start };
        menu.Items.Add(item);

        item.HorizontalOptions.ShouldBe(LayoutOptions.Start);
        item.AnchorX.ShouldBe(0d);
    }

    [Fact]
    public void AlignmentChange_AfterItemsAreAdded_RefreshesThem()
    {
        new Application();

        var item = new FabMenuItem { Text = "Share" };
        var menu = new FabMenu();
        menu.Items.Add(item);

        menu.MenuAlignment = LayoutOptions.Start;
        item.HorizontalOptions.ShouldBe(LayoutOptions.Start);

        menu.MenuAlignment = LayoutOptions.End;
        item.HorizontalOptions.ShouldBe(LayoutOptions.End);
    }

    [Fact]
    public void FabSizeChange_DoesNotThrowWithItemsPresent()
    {
        new Application();

        var menu = new FabMenu();
        menu.Items.Add(new FabMenuItem { Text = "Share" });
        menu.Items.Add(new FabMenuItem());

        Should.NotThrow(() => menu.FabSize = 72);
    }

    [Fact]
    public void TextlessItem_StillConstructs()
    {
        new Application();

        // No Text collapses the pill to a plain circle - a different layout branch.
        var item = Should.NotThrow(() => new FabMenuItem { Size = 56 });
        item.Text.ShouldBeNull();
    }
}
