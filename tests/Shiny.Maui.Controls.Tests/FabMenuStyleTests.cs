using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Same bug as <see cref="AutoCompleteEntryStyleTests"/>, second worked example: an implicit
/// Style is applied from StyleableElement's constructor, before FabMenu's constructor body
/// runs, and its callbacks forward to the inner Fab which does not exist yet.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class FabMenuStyleTests
{
    static Style BuildStyle() => new(typeof(FabMenu))
    {
        Setters =
        {
            new Setter { Property = FabMenu.TextProperty, Value = "Actions" },
            new Setter { Property = FabMenu.FabBackgroundColorProperty, Value = Colors.Red },
            new Setter { Property = FabMenu.TextColorProperty, Value = Colors.White },
            new Setter { Property = FabMenu.BorderColorProperty, Value = Colors.Black },
            new Setter { Property = FabMenu.BorderThicknessProperty, Value = 2d },
            new Setter { Property = FabMenu.FabSizeProperty, Value = 64d },
            new Setter { Property = FabMenu.HasShadowProperty, Value = false },
            new Setter { Property = FabMenu.BackdropColorProperty, Value = Colors.Blue }
        }
    };

    [Fact]
    public void ImplicitAppStyle_DoesNotThrowDuringConstruction()
    {
        var app = new Application();
        app.Resources.Add(BuildStyle());

        var menu = Should.NotThrow(() => new FabMenu());

        // Applied, not merely survived.
        menu.Text.ShouldBe("Actions");
        menu.FabBackgroundColor.ShouldBe(Colors.Red);
        menu.TextColor.ShouldBe(Colors.White);
        menu.BorderThickness.ShouldBe(2d);
        menu.FabSize.ShouldBe(64d);
        menu.HasShadow.ShouldBeFalse();
        menu.BackdropColor.ShouldBe(Colors.Blue);
    }

    [Fact]
    public void NoStyle_StillConstructsWithDefaults()
    {
        new Application();

        var menu = new FabMenu();

        menu.Items.ShouldNotBeNull();
        menu.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void ExplicitStyle_AppliedAfterConstruction_TakesEffect()
    {
        new Application();

        var menu = new FabMenu();
        menu.Style = BuildStyle();

        menu.Text.ShouldBe("Actions");
        menu.FabBackgroundColor.ShouldBe(Colors.Red);
    }
}
