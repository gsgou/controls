using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

public class AxisMetricsTests
{
    static AxisMetrics Axis(double size = 20, int count = 1000) => new(size, count);

    [Fact]
    public void OffsetsAccumulateAtTheDefaultSize()
    {
        var axis = Axis();
        axis.OffsetOf(0).ShouldBe(0);
        axis.OffsetOf(5).ShouldBe(100);
    }

    [Fact]
    public void OverridesShiftEverythingAfterThem()
    {
        var axis = Axis();
        axis.SetSize(2, 50);

        axis.OffsetOf(2).ShouldBe(40);
        axis.OffsetOf(3).ShouldBe(90, "index 2 is 50 wide, not 20");
        axis.OffsetOf(5).ShouldBe(130);
    }

    [Fact]
    public void HiddenEntriesTakeNoSpace()
    {
        var axis = Axis();
        axis.SetHidden(1, true);

        axis.SizeOf(1).ShouldBe(0);
        axis.OffsetOf(2).ShouldBe(20);
        axis.OffsetOf(3).ShouldBe(40);
    }

    [Fact]
    public void UnhidingRestoresTheSize()
    {
        var axis = Axis();
        axis.SetHidden(1, true);
        axis.SetHidden(1, false);
        axis.OffsetOf(2).ShouldBe(40);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(19.9, 0)]
    [InlineData(20, 1)]
    [InlineData(59, 2)]
    [InlineData(60, 3)]
    public void IndexAtInvertsOffsetOf(double offset, int expected)
        => Axis().IndexAt(offset).ShouldBe(expected);

    [Fact]
    public void IndexAtHandlesOverrides()
    {
        var axis = Axis();
        axis.SetSize(0, 100);

        axis.IndexAt(50).ShouldBe(0);
        axis.IndexAt(100).ShouldBe(1);
        axis.IndexAt(119).ShouldBe(1);
        axis.IndexAt(120).ShouldBe(2);
    }

    [Fact]
    public void LargeAxesResolveWithoutAllocatingPerEntry()
    {
        // A real sheet has over a million rows; this must be a binary search, not a walk.
        var axis = new AxisMetrics(20, CellRef.MaxRow + 1);
        axis.SetSize(500_000, 100);

        axis.OffsetOf(1_000_000).ShouldBe(1_000_000d * 20 + 80);
        axis.IndexAt(axis.OffsetOf(999_999)).ShouldBe(999_999);
    }

    [Fact]
    public void SettingASizeBackToTheDefaultDropsTheOverride()
    {
        var axis = Axis();
        axis.SetSize(3, 50);
        axis.OverrideCount.ShouldBe(1);

        axis.SetSize(3, 20);
        axis.OverrideCount.ShouldBe(0);
    }

    [Fact]
    public void RangeSizeIsTheDifferenceOfOffsets()
    {
        var axis = Axis();
        axis.SetSize(1, 40);
        axis.SizeOfRange(0, 3).ShouldBe(80);
    }
}

public class GridMetricsTests
{
    [Fact]
    public void ColumnWidthConvertsThroughExcelsCharacterUnits()
    {
        // 8.43 characters is Excel's default and lands on 64px for Calibri 11.
        GridMetrics.WidthToPixels(GridMetrics.DefaultColumnWidthCharacters).ShouldBe(64, 0.5);
    }

    [Fact]
    public void RowHeightConvertsPointsToPixels()
        => GridMetrics.PointsToPixels(15).ShouldBe(20);

    [Fact]
    public void CellBoundsUseTheAxisGeometry()
    {
        var metrics = new GridMetrics();
        metrics.Columns.SetSize(0, 100);
        metrics.Rows.SetSize(0, 30);

        var bounds = metrics.CellBounds(CellRef.Parse("B2"));
        bounds.X.ShouldBe(100);
        bounds.Y.ShouldBe(30);
    }

    [Fact]
    public void RangeBoundsSpanTheWholeRectangle()
    {
        var metrics = new GridMetrics();
        var bounds = metrics.RangeBounds(CellRange.Parse("A1:C2"));

        bounds.Width.ShouldBe(metrics.Columns.SizeOfRange(0, 3));
        bounds.Height.ShouldBe(metrics.Rows.SizeOfRange(0, 2));
    }
}

public class GridViewportTests
{
    static GridViewport Viewport(int frozenColumns = 0, int frozenRows = 0)
    {
        var metrics = new GridMetrics { RowHeaderWidth = 40, ColumnHeaderHeight = 20 };
        metrics.Columns.SetDefaultSize(50);
        metrics.Rows.SetDefaultSize(20);
        metrics.FrozenPane = new CellRef(frozenColumns, frozenRows);

        return new GridViewport(metrics) { Width = 400, Height = 200 };
    }

    [Fact]
    public void CellRectAccountsForHeadersAndScroll()
    {
        var viewport = Viewport();
        viewport.CellRect(CellRef.Parse("A1")).X.ShouldBe(40);
        viewport.CellRect(CellRef.Parse("A1")).Y.ShouldBe(20);

        viewport.ScrollTo(100, 40);
        viewport.CellRect(CellRef.Parse("A1")).X.ShouldBe(-60);
        viewport.CellRect(CellRef.Parse("A1")).Y.ShouldBe(-20);
    }

    [Fact]
    public void HitTestRoundTripsWithCellRect()
    {
        var viewport = Viewport();
        viewport.ScrollTo(120, 60);

        var cell = CellRef.Parse("F8");
        var rect = viewport.CellRect(cell);
        var hit = viewport.HitTest(rect.X + 1, rect.Y + 1);

        hit.IsCell.ShouldBeTrue();
        hit.Cell.ShouldBe(cell);
    }

    [Fact]
    public void FrozenColumnsDoNotScroll()
    {
        var viewport = Viewport(frozenColumns: 2);
        var pinned = viewport.CellRect(CellRef.Parse("A1"));

        viewport.ScrollTo(500, 0);

        viewport.CellRect(CellRef.Parse("A1")).X.ShouldBe(pinned.X, "a frozen column must stay put");
    }

    [Fact]
    public void ScrollableColumnsStartAfterTheFrozenBand()
    {
        var viewport = Viewport(frozenColumns: 2);

        // Headers (40) plus two frozen 50px columns.
        viewport.ContentOriginX.ShouldBe(140);
        viewport.FirstVisibleColumn.ShouldBe(2);
    }

    [Fact]
    public void HitTestInsideTheFrozenBandResolvesToTheFrozenCell()
    {
        var viewport = Viewport(frozenColumns: 2, frozenRows: 1);
        viewport.ScrollTo(1000, 1000);

        var hit = viewport.HitTest(50, 25);

        hit.IsCell.ShouldBeTrue();
        hit.Cell.ShouldBe(CellRef.Parse("A1"));
        hit.Pane.ShouldBe(PaneKind.Corner);
    }

    [Fact]
    public void HeadersAreHitTestable()
    {
        var viewport = Viewport();

        viewport.HitTest(5, 5).Target.ShouldBe(HitTarget.SelectAllCorner);
        viewport.HitTest(60, 5).Target.ShouldBe(HitTarget.ColumnHeader);
        viewport.HitTest(5, 40).Target.ShouldBe(HitTarget.RowHeader);
    }

    [Fact]
    public void TheEdgeOfAColumnHeaderIsAResizeGrip()
    {
        var viewport = Viewport();
        var rect = viewport.CellRect(CellRef.Parse("A1"));

        viewport.HitTest(rect.Right - 1, 5).Target.ShouldBe(HitTarget.ColumnResize);
        viewport.HitTest(rect.X + 20, 5).Target.ShouldBe(HitTarget.ColumnHeader);
    }

    [Fact]
    public void ScrollIntoViewMovesTheMinimumDistance()
    {
        var viewport = Viewport();
        viewport.ScrollIntoView(CellRef.Parse("A1"));
        viewport.ScrollX.ShouldBe(0, "an already-visible cell must not move the viewport");

        // Content width is 400-40 = 360, i.e. 7.2 columns of 50px.
        viewport.ScrollIntoView(CellRef.Parse("J1"));
        viewport.ScrollX.ShouldBe(9 * 50 + 50 - 360);
    }

    [Fact]
    public void ScrollIntoViewIgnoresCellsInsideAFrozenBand()
    {
        var viewport = Viewport(frozenColumns: 2);
        viewport.ScrollTo(300, 0);

        viewport.ScrollIntoView(CellRef.Parse("A1"));

        viewport.ScrollX.ShouldBe(300, "a frozen cell is always visible, so nothing should scroll");
    }

    [Fact]
    public void VisibleRangeCoversTheViewport()
    {
        var viewport = Viewport();
        var (first, last) = viewport.VisibleColumns();

        first.ShouldBe(0);
        last.ShouldBeGreaterThanOrEqualTo(6);
    }
}

public class SpreadsheetSelectionTests
{
    [Fact]
    public void MoveToCollapsesTheSelection()
    {
        var selection = new SpreadsheetSelection();
        selection.MoveTo(CellRef.Parse("C3"));

        selection.Active.ShouldBe(CellRef.Parse("C3"));
        selection.Range.ShouldBe(new CellRange(CellRef.Parse("C3")));
        selection.IsSingleCell.ShouldBeTrue();
    }

    [Fact]
    public void ExtendGrowsFromTheAnchorNotTheLastExtension()
    {
        // Shift-clicking twice must both times select from the original cell.
        var selection = new SpreadsheetSelection();
        selection.MoveTo(CellRef.Parse("B2"));

        selection.ExtendTo(CellRef.Parse("D4"));
        selection.Range.ShouldBe(CellRange.Parse("B2:D4"));

        selection.ExtendTo(CellRef.Parse("C3"));
        selection.Range.ShouldBe(CellRange.Parse("B2:C3"));
    }

    [Fact]
    public void MovingClampsAtTheSheetEdge()
    {
        var selection = new SpreadsheetSelection();
        selection.MoveTo(new CellRef(0, 0));
        selection.Move(MoveDirection.Up);
        selection.Move(MoveDirection.Left);

        selection.Active.ShouldBe(new CellRef(0, 0));
    }

    [Fact]
    public void EnterWrapsInsideAMultiCellSelection()
    {
        var selection = new SpreadsheetSelection();
        selection.SelectRange(CellRange.Parse("A1:B2"));

        selection.Active.ShouldBe(CellRef.Parse("A1"));
        selection.Advance(byRow: true);
        selection.Active.ShouldBe(CellRef.Parse("A2"));

        selection.Advance(byRow: true);
        selection.Active.ShouldBe(CellRef.Parse("B1"), "past the bottom it wraps to the next column");

        selection.Range.ShouldBe(CellRange.Parse("A1:B2"), "advancing must not change the selection");
    }

    [Fact]
    public void EnterOnASingleCellJustMoves()
    {
        var selection = new SpreadsheetSelection();
        selection.MoveTo(CellRef.Parse("A1"));
        selection.Advance(byRow: true);

        selection.Active.ShouldBe(CellRef.Parse("A2"));
        selection.IsSingleCell.ShouldBeTrue();
    }

    [Fact]
    public void CtrlArrowStopsAtTheEndOfARun()
    {
        // A1:A3 populated, A4 empty, A7 populated.
        var populated = new HashSet<string> { "A1", "A2", "A3", "A7" };
        bool IsPopulated(CellRef cell) => populated.Contains(cell.Relative().ToString());

        var selection = new SpreadsheetSelection();
        selection.MoveTo(CellRef.Parse("A1"));

        selection.MoveToEdge(MoveDirection.Down, IsPopulated);
        selection.Active.ShouldBe(CellRef.Parse("A3"), "stops at the last cell before the gap");
    }

    [Fact]
    public void CtrlArrowJumpsAcrossAGapToTheNextData()
    {
        var populated = new HashSet<string> { "A1", "A2", "A3", "A7" };
        bool IsPopulated(CellRef cell) => populated.Contains(cell.Relative().ToString());

        var selection = new SpreadsheetSelection();
        selection.MoveTo(CellRef.Parse("A3"));

        selection.MoveToEdge(MoveDirection.Down, IsPopulated);
        selection.Active.ShouldBe(CellRef.Parse("A7"), "from the edge of a run it jumps to the next block");
    }

    [Fact]
    public void SelectColumnCoversEveryRow()
    {
        var selection = new SpreadsheetSelection();
        selection.SelectColumn(2);

        selection.Range.Left.ShouldBe(2);
        selection.Range.Right.ShouldBe(2);
        selection.Range.RowCount.ShouldBe(CellRef.MaxRow + 1);
    }

    [Fact]
    public void ChangedFiresOnEveryMutation()
    {
        var selection = new SpreadsheetSelection();
        var count = 0;
        selection.Changed += (_, _) => count++;

        selection.MoveTo(CellRef.Parse("B2"));
        selection.ExtendTo(CellRef.Parse("C3"));
        selection.Advance(byRow: false);

        count.ShouldBe(3);
    }
}
