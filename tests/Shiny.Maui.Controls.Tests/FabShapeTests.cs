using Microsoft.Maui.Controls;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// A text-only Fab used to render wider than tall: the icon Image was visible (and sized) even
/// with no Icon set, and the inner grid's column spacing applied regardless — so "+" got an
/// icon-shaped bump on its left and the button was never a circle.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class FabShapeTests
{
    static (Border Border, Grid Inner) Parts(Fab fab)
    {
        var border = (Border)fab.Content!;
        return (border, (Grid)border.Content!);
    }

    [Fact]
    public void NoIcon_IconImageIsHidden_AndNoSpacingReserved()
    {
        new Application();

        var fab = new Fab { Text = "+", Size = 60 };
        var (border, inner) = Parts(fab);

        inner.Children.OfType<Image>().Single().IsVisible.ShouldBeFalse();
        inner.ColumnSpacing.ShouldBe(0);

        // Width is left to the content so long labels extend, but the minimum keeps a short
        // label circular once measured.
        border.WidthRequest.ShouldBe(-1);
        border.HeightRequest.ShouldBe(60);
        border.MinimumWidthRequest.ShouldBe(60);
    }

    [Fact]
    public void IconOnly_IsCircle()
    {
        new Application();

        var fab = new Fab { Icon = "add.png", Size = 60 };
        var (border, inner) = Parts(fab);

        inner.Children.OfType<Image>().Single().IsVisible.ShouldBeTrue();
        inner.ColumnSpacing.ShouldBe(0);
        border.WidthRequest.ShouldBe(60);
        border.HeightRequest.ShouldBe(60);
        border.Padding.ShouldBe(new Thickness(0));
    }

    [Fact]
    public void IconAndText_SpacesTheTwo()
    {
        new Application();

        var fab = new Fab { Icon = "add.png", Text = "Add Item" };
        var (_, inner) = Parts(fab);

        inner.ColumnSpacing.ShouldBe(8);
    }

    [Fact]
    public void ClearingText_ReturnsToCircle()
    {
        new Application();

        var fab = new Fab { Text = "Add Item", Size = 56 };
        fab.Text = null;

        var (border, _) = Parts(fab);
        border.WidthRequest.ShouldBe(56);
        border.Padding.ShouldBe(new Thickness(0));
    }

    [Fact]
    public void SizeChange_KeepsCornerRadiusCircular()
    {
        new Application();

        var fab = new Fab { Icon = "add.png", Size = 72 };
        var (border, _) = Parts(fab);

        var shape = (Microsoft.Maui.Controls.Shapes.RoundRectangle)border.StrokeShape!;
        shape.CornerRadius.TopLeft.ShouldBe(36);
    }
}
