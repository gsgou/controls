using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Under <c>table-layout: auto</c> a browser treats <c>width</c> on a cell as a suggestion and
/// compresses every column to fit its container. A grid asking for 1320px of columns inside an 810px
/// scroller therefore rendered at 810px, never overflowed, and its frozen columns had nothing to stay
/// put against - the pinning looked broken when the real fault was that nothing scrolled.
/// </summary>
public class DataGridColumnWidthTests
{
    class Row
    {
        public string Name { get; set; } = "";
    }

    static string? StyleFor(string? width, string? min = null, string? max = null)
    {
        var grid = new DataGrid<Row>();
        var column = new TemplateColumn<Row> { Title = "col", Width = width, MinWidth = min, MaxWidth = max };
        return grid.ColumnWidthStyle(column);
    }

    static DataGrid<Row> GridWith(double gridMin = 48, double? gridMax = null)
        => new() { MinColumnWidth = gridMin, MaxColumnWidth = gridMax };

    static TemplateColumn<Row> Column(string? min = null, string? max = null, bool? resizable = null)
        => new() { Title = "col", MinWidth = min, MaxWidth = max, Resizable = resizable };

    [Fact]
    public void ADeclaredPixelWidthCarriesAMinWidthSoTheTableCannotCompressIt()
        => StyleFor("160px").ShouldBe("width:160px;min-width:160px;");

    [Fact]
    public void OtherAbsoluteUnitsAreHeldTheSameWay()
        => StyleFor("12rem").ShouldBe("width:12rem;min-width:12rem;");

    /// <summary>
    /// A percentage is asking to be relative to the container, which is exactly what shrinking is -
    /// pinning a min-width onto it would fight the thing it asked for.
    /// </summary>
    [Fact]
    public void APercentageWidthIsLeftFreeToShrink()
        => StyleFor("20%").ShouldBe("width:20%;");

    [Fact]
    public void AColumnWithNoWidthDeclaresNothing()
        => StyleFor(null).ShouldBeNull();

    /// <summary>
    /// An explicit MinWidth replaces the floor a declared Width implies rather than stacking on top of
    /// it: "160 wide, may shrink to 80" is asking for exactly the compression that implied floor exists
    /// to prevent, and it gets to have it.
    /// </summary>
    [Fact]
    public void AnExplicitMinWidthReplacesTheFloorImpliedByTheWidth()
        => StyleFor("160px", min: "80px").ShouldBe("width:160px;min-width:80px;");

    [Fact]
    public void AMaxWidthIsEmittedAlongsideTheWidth()
        => StyleFor("160px", max: "400px").ShouldBe("width:160px;min-width:160px;max-width:400px;");

    [Fact]
    public void BoundsStandOnTheirOwnWithoutADeclaredWidth()
        => StyleFor(null, min: "80px", max: "400px").ShouldBe("min-width:80px;max-width:400px;");

    /// <summary>
    /// A percentage width still gets an explicit MinWidth - what it must not get is the *implied* one,
    /// which would pin it to a size it asked to be free of.
    /// </summary>
    [Fact]
    public void APercentageWidthKeepsAnExplicitMinWidth()
        => StyleFor("20%", min: "120px").ShouldBe("width:20%;min-width:120px;");

    /// <summary>
    /// The grid-level defaults bound a resize drag, not the layout. Emitting them here would pin a 48px
    /// floor onto every cell of every grid, overriding the percentage widths that asked to shrink.
    /// </summary>
    [Fact]
    public void TheGridLevelDefaultsAreNotEmittedAsCss()
    {
        var grid = GridWith(gridMin: 48, gridMax: 300);
        grid.ColumnWidthStyle(new TemplateColumn<Row> { Title = "col" }).ShouldBeNull();
    }

    // ---- resize clamping ----

    [Fact]
    public void ADragBelowTheGridDefaultIsHeldAtIt()
        => GridWith().ClampColumnWidth(Column(), 10).ShouldBe(48);

    [Fact]
    public void AColumnMinWidthOverridesTheGridDefault()
        => GridWith().ClampColumnWidth(Column(min: "120px"), 10).ShouldBe(120);

    [Fact]
    public void AColumnMaxWidthCapsTheDrag()
        => GridWith().ClampColumnWidth(Column(max: "200px"), 900).ShouldBe(200);

    [Fact]
    public void TheGridMaxAppliesToAColumnThatDeclaresNone()
        => GridWith(gridMax: 300).ClampColumnWidth(Column(), 900).ShouldBe(300);

    [Fact]
    public void AColumnMaxWidthOverridesTheGridMax()
        => GridWith(gridMax: 300).ClampColumnWidth(Column(max: "500px"), 900).ShouldBe(500);

    [Fact]
    public void AWidthInsideTheBoundsIsLeftAlone()
        => GridWith().ClampColumnWidth(Column(min: "80px", max: "400px"), 250).ShouldBe(250);

    [Fact]
    public void AMaxBelowTheMinLosesToTheMin()
        => GridWith().ClampColumnWidth(Column(min: "200px", max: "100px"), 150).ShouldBe(200);

    [Fact]
    public void NoGridMaxMeansUnbounded()
        => GridWith().ClampColumnWidth(Column(), 5000).ShouldBe(5000);

    /// <summary>
    /// A drag works in pixels; only the browser knows what a % or em is worth. Such a bound is still
    /// emitted as CSS (see above) but cannot clamp the drag, which falls back to the grid default.
    /// </summary>
    [Fact]
    public void ANonPixelBoundFallsBackToTheGridDefaultForTheDrag()
    {
        GridWith(gridMin: 48).ClampColumnWidth(Column(min: "20%"), 10).ShouldBe(48);
        GridWith(gridMax: 300).ClampColumnWidth(Column(max: "50%"), 900).ShouldBe(300);
    }

    // ---- enable / disable ----

    [Fact]
    public void NoHandleIsOfferedWhenTheGridDoesNotAllowResizing()
    {
        var grid = new DataGrid<Row> { ColumnResizeMode = DataGridColumnResizeMode.None };
        grid.CanResize(Column()).ShouldBeFalse();
    }

    [Fact]
    public void AColumnCanOptOutOfResizingOnAGridThatAllowsIt()
    {
        var grid = new DataGrid<Row> { ColumnResizeMode = DataGridColumnResizeMode.Column };
        grid.CanResize(Column(resizable: false)).ShouldBeFalse();
        grid.CanResize(Column(resizable: true)).ShouldBeTrue();
        grid.CanResize(Column()).ShouldBeTrue();
    }

    // ---- Container mode: the drag moves a boundary, so the pair's total holds ----

    static (double Width, double Neighbour) Container(
        DataGrid<Row> grid, TemplateColumn<Row> col, double start,
        TemplateColumn<Row> neighbour, double neighbourStart, double target)
        => grid.ResolveContainerResize(col, start, neighbour, neighbourStart, target);

    [Fact]
    public void WhatOneColumnGainsTheNeighbourGivesUp()
    {
        var (width, neighbour) = Container(GridWith(), Column(), 160, Column(), 300, 220);
        width.ShouldBe(220);
        neighbour.ShouldBe(240);
        (width + neighbour).ShouldBe(160 + 300);
    }

    /// <summary>
    /// A neighbour that will not shrink any further hands the refusal back, so the dragged column
    /// stops with it rather than growing into space the neighbour never released.
    /// </summary>
    [Fact]
    public void ANeighbourAtItsMinimumStopsTheDrag()
    {
        var (width, neighbour) = Container(
            GridWith(), Column(), 160, Column(min: "200px"), 240, 400);

        neighbour.ShouldBe(200);
        width.ShouldBe(200);
        (width + neighbour).ShouldBe(160 + 240);
    }

    /// <summary>
    /// The neighbour gives up at most what the drag asked for. Without this the sample's auto-sized
    /// 788px column, over a grid-level 420px max, handed a 120px drag a 368px jump - and every column
    /// in the grid visibly lurched.
    /// </summary>
    [Fact]
    public void ANeighbourAlreadyOutsideItsBoundsIsNotYankedIntoThemByTheDrag()
    {
        var (width, neighbour) = Container(
            GridWith(gridMax: 420), Column(), 160, Column(), 788, 280);

        width.ShouldBe(280);
        neighbour.ShouldBe(668);
        (width + neighbour).ShouldBe(160 + 788);
    }

    [Fact]
    public void ShrinkingAColumnGivesTheSpaceToTheNeighbour()
    {
        var (width, neighbour) = Container(GridWith(), Column(), 300, Column(), 200, 240);
        width.ShouldBe(240);
        neighbour.ShouldBe(260);
        (width + neighbour).ShouldBe(300 + 200);
    }

    // ---- percentage widths ----

    /// <summary>
    /// Percentages are the portable way to size columns across both hosts, so they need to survive
    /// the whole style builder rather than only the no-bounds case above.
    /// </summary>
    [Fact]
    public void APercentageWidthPairsWithPixelBounds()
        => StyleFor("25%", min: "120px", max: "400px")
            .ShouldBe("width:25%;min-width:120px;max-width:400px;");

    [Fact]
    public void PercentageBoundsAreEmittedVerbatim()
        => StyleFor(null, min: "10%", max: "40%").ShouldBe("min-width:10%;max-width:40%;");

    /// <summary>
    /// A percentage width cannot be turned into a pixel offset in C#, so a frozen percentage column
    /// falls through to the script that measures the rendered table.
    /// </summary>
    [Fact]
    public void APercentageWidthReportsNoPixelWidth()
        => new DataGrid<Row>().PxWidth(new TemplateColumn<Row> { Title = "col", Width = "25%" })
            .ShouldBeNull();
}
