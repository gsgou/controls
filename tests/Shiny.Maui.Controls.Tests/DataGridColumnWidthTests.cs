using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.DataGrid;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Column min/max is what stops a resize drag from collapsing a column to an unusable sliver or
/// dragging it off the edge of the grid. The clamp is deliberately split in two: a drag falls back to
/// the grid-level defaults, while a *declared* width is bounded only by the column's own min/max -
/// otherwise the 48 default would quietly widen a narrow icon column nobody ever tried to resize.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class DataGridColumnWidthTests
{
    static (DataGrid.DataGrid Grid, DataGridColumn Column) Build(
        double colMin = 0, double colMax = 0, double gridMin = 48, double gridMax = 0)
    {
        var column = new DataGridColumn { Title = "col", MinWidth = colMin, MaxWidth = colMax };
        var grid = new DataGrid.DataGrid { MinColumnWidth = gridMin, MaxColumnWidth = gridMax };
        grid.Columns.Add(column);
        return (grid, column);
    }

    [Fact]
    public void ADragBelowTheGridDefaultIsHeldAtIt()
    {
        var (grid, column) = Build();
        grid.ClampColumnWidth(column, 10).ShouldBe(48);
    }

    [Fact]
    public void AColumnMinWidthOverridesTheGridDefault()
    {
        var (grid, column) = Build(colMin: 120);
        grid.ClampColumnWidth(column, 10).ShouldBe(120);
    }

    [Fact]
    public void AColumnMaxWidthCapsTheDrag()
    {
        var (grid, column) = Build(colMax: 200);
        grid.ClampColumnWidth(column, 900).ShouldBe(200);
    }

    [Fact]
    public void TheGridMaxAppliesToAColumnThatDeclaresNone()
    {
        var (grid, column) = Build(gridMax: 300);
        grid.ClampColumnWidth(column, 900).ShouldBe(300);
    }

    [Fact]
    public void AColumnMaxWidthOverridesTheGridMax()
    {
        var (grid, column) = Build(colMax: 500, gridMax: 300);
        grid.ClampColumnWidth(column, 900).ShouldBe(500);
    }

    [Fact]
    public void AWidthInsideTheBoundsIsLeftAlone()
    {
        var (grid, column) = Build(colMin: 80, colMax: 400);
        grid.ClampColumnWidth(column, 250).ShouldBe(250);
    }

    /// <summary>
    /// A max below the min is a misconfiguration either way, but honouring the floor leaves a column
    /// the user can still see and drag; honouring the ceiling leaves a sliver they cannot.
    /// </summary>
    [Fact]
    public void AMaxBelowTheMinLosesToTheMin()
    {
        var (grid, column) = Build(colMin: 200, colMax: 100);
        grid.ClampColumnWidth(column, 150).ShouldBe(200);
    }

    [Fact]
    public void ZeroGridMaxMeansUnbounded()
    {
        var (grid, column) = Build();
        grid.ClampColumnWidth(column, 5000).ShouldBe(5000);
    }

    [Fact]
    public void TheGridMinNeverFallsBelowOne()
    {
        var (grid, column) = Build(gridMin: 0);
        grid.ClampColumnWidth(column, 0).ShouldBe(1);
    }

    [Fact]
    public void EffectiveMaxWidthIsNullWhenNothingCapsTheColumn()
    {
        var (grid, column) = Build();
        grid.EffectiveMaxWidth(column).ShouldBeNull();
    }

    /// <summary>
    /// The grid-level default must not reach a declared width. A Width="40" icon column stays 40 wide,
    /// even though a drag on it would stop at 48.
    /// </summary>
    [Fact]
    public void ADeclaredWidthIgnoresTheGridLevelDefaults()
    {
        var (grid, column) = Build(gridMin: 48, gridMax: 300);
        column.Width = new GridLength(40);
        grid.ResolveWidth(column).Value.ShouldBe(40);

        column.Width = new GridLength(900);
        grid.ResolveWidth(column).Value.ShouldBe(900);
    }

    [Fact]
    public void ADeclaredWidthIsStillHeldInsideTheColumnsOwnBounds()
    {
        var (grid, column) = Build(colMin: 100, colMax: 200);
        column.Width = new GridLength(40);
        grid.ResolveWidth(column).Value.ShouldBe(100);

        column.Width = new GridLength(900);
        grid.ResolveWidth(column).Value.ShouldBe(200);
    }

    /// <summary>
    /// A star column outside HorizontalScroll has to stay a star - clamping it would mean silently
    /// turning it absolute and killing the proportional layout the caller asked for.
    /// </summary>
    [Fact]
    public void AStarWidthSurvivesTheClampUnchanged()
    {
        var (grid, column) = Build(colMin: 100, colMax: 200);
        column.Width = GridLength.Star;
        grid.ResolveWidth(column).IsStar.ShouldBeTrue();
    }

    /// <summary>Under HorizontalScroll a star resolves to a number, and the number is bounded.</summary>
    [Fact]
    public void AResolvedStarWidthIsBoundedByTheColumn()
    {
        var (grid, column) = Build(colMax: 90);
        grid.HorizontalScroll = true;
        grid.DefaultColumnWidth = 150;
        column.Width = GridLength.Star;
        grid.ResolveWidth(column).Value.ShouldBe(90);
    }

    // ---- percentage widths ----

    /// <summary>
    /// A star factor <i>is</i> a percentage: the Grid divides the available width in the ratio of the
    /// factors, so 30/70 columns get exactly 30% and 70% of it.
    /// </summary>
    [Fact]
    public void APercentageBecomesAStarOfTheSameFactor()
    {
        var (grid, column) = Build();
        column.WidthPercent = 30;

        var resolved = grid.ResolveWidth(column);
        resolved.IsStar.ShouldBeTrue();
        resolved.Value.ShouldBe(30);
    }

    [Fact]
    public void APercentageWinsOverAnAbsoluteWidth()
    {
        var (grid, column) = Build();
        column.Width = new GridLength(400);
        column.WidthPercent = 25;

        grid.ResolveWidth(column).IsStar.ShouldBeTrue();
    }

    /// <summary>
    /// Under HorizontalScroll there is no available width to share - the columns are meant to overflow
    /// it - so a percentage resolves against the scroller's own width instead.
    /// </summary>
    [Fact]
    public void UnderHorizontalScrollAPercentageResolvesAgainstTheViewport()
    {
        var (grid, column) = Build();
        grid.HorizontalScroll = true;
        grid.WidthRequest = 1000;
        grid.Layout(new Rect(0, 0, 1000, 400));
        column.WidthPercent = 25;

        grid.ResolveWidth(column).Value.ShouldBe(250);
    }

    [Fact]
    public void AResolvedPercentageIsStillHeldInsideTheColumnsBounds()
    {
        var (grid, column) = Build(colMax: 120);
        grid.HorizontalScroll = true;
        grid.Layout(new Rect(0, 0, 1000, 400));
        column.WidthPercent = 50;

        grid.ResolveWidth(column).Value.ShouldBe(120);
    }

    /// <summary>
    /// Before the first layout there is no viewport to take a percentage of; DefaultColumnWidth is the
    /// same stand-in a star gets in this mode, and the SizeChanged hook re-resolves once it arrives.
    /// </summary>
    [Fact]
    public void WithNoMeasuredViewportAPercentageFallsBackToTheDefaultColumnWidth()
    {
        var (grid, column) = Build();
        grid.HorizontalScroll = true;
        grid.DefaultColumnWidth = 140;
        column.WidthPercent = 25;

        grid.ResolveWidth(column).Value.ShouldBe(140);
    }

    [Fact]
    public void ZeroPercentMeansUnsetAndLeavesTheDeclaredWidthAlone()
    {
        var (grid, column) = Build();
        column.Width = new GridLength(220);

        grid.ResolveWidth(column).Value.ShouldBe(220);
    }
}
