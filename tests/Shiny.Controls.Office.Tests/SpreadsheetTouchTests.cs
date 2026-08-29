using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.View;
using Shiny.Controls.Office.View;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// The touch interaction model: a tap selects, a drag pans, and a selection is extended by dragging a
/// handle.
/// </summary>
/// <remarks>
/// The bug these exist for had no symptom to assert against — the grid worked perfectly, and simply
/// could not be scrolled sideways by anything but a mouse wheel that no phone has. Everything here is
/// about a gesture doing one thing and not the other, which is exactly what a regression would swap
/// back.
/// </remarks>
public class SpreadsheetTouchTests
{
    static async Task<SpreadsheetController> SetupAsync()
    {
        var workbook = await Workbook.OpenAsync(new MemoryStream(WorkbookFixture.Build()));
        var controller = new SpreadsheetController(workbook, workbook["Data"]);
        controller.Resize(600, 400);
        return controller;
    }

    /// <summary>A point inside the cell, clear of the headers and of either handle.</summary>
    static (double X, double Y) Middle(SpreadsheetController controller, CellRef cell)
    {
        var rect = controller.Viewport.CellRect(cell);
        return (rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
    }

    [Fact]
    public async Task AMouseDragStillExtendsTheSelection()
    {
        // The whole point of keying off the pointer kind: nothing changes for a mouse.
        var controller = await SetupAsync();
        var from = Middle(controller, new CellRef(1, 1));
        var to = Middle(controller, new CellRef(3, 4));

        controller.PointerDown(from.X, from.Y);
        controller.PointerMove(to.X, to.Y);
        controller.PointerUp();

        controller.Selection.Range.Right.ShouldBe(3);
        controller.Selection.Range.Bottom.ShouldBe(4);
    }

    [Fact]
    public async Task ATouchDragPansInsteadOfSelecting()
    {
        var controller = await SetupAsync();
        var from = Middle(controller, new CellRef(3, 6));

        controller.PointerDown(from.X, from.Y, kind: PointerKind.Touch);
        controller.PointerMove(from.X - 120, from.Y - 90);
        controller.PointerUp();

        controller.Viewport.ScrollX.ShouldBe(120, 0.5);
        controller.Viewport.ScrollY.ShouldBe(90, 0.5);

        // and the selection was left exactly where it was
        controller.Selection.Range.IsSingleCell.ShouldBeTrue();
        controller.Selection.Active.ShouldBe(new CellRef(0, 0));
    }

    [Fact]
    public async Task ATouchThatDoesNotTravelIsATapAndSelects()
    {
        var controller = await SetupAsync();
        var (x, y) = Middle(controller, new CellRef(2, 3));

        controller.PointerDown(x, y, kind: PointerKind.Touch);
        controller.PointerUp();

        controller.Selection.Active.ShouldBe(new CellRef(2, 3));
        controller.Viewport.ScrollX.ShouldBe(0);
    }

    [Fact]
    public async Task AFingerThatWobblesOnTheWayUpIsStillATap()
    {
        // No finger lands and lifts on the same pixel. Without the slop every tap would register as a
        // one-pixel pan and select nothing at all.
        var controller = await SetupAsync();
        var (x, y) = Middle(controller, new CellRef(2, 3));

        controller.PointerDown(x, y, kind: PointerKind.Touch);
        controller.PointerMove(x + 2, y - 3);
        controller.PointerUp();

        controller.Selection.Active.ShouldBe(new CellRef(2, 3));
        controller.Viewport.ScrollX.ShouldBe(0);
    }

    [Fact]
    public async Task TheSelectionIsExtendedByDraggingItsEndHandle()
    {
        var controller = await SetupAsync();
        controller.Selection.MoveTo(new CellRef(1, 1));

        var handles = controller.Viewport.SelectionHandles(controller.Selection.Range);
        var grab = (X: handles.End.X + (handles.End.Width / 2), Y: handles.End.Y + (handles.End.Height / 2));

        controller.Viewport.SelectionHandleAt(controller.Selection.Range, grab.X, grab.Y)
            .ShouldBe(SelectionHandle.End);

        var target = Middle(controller, new CellRef(4, 5));

        controller.PointerDown(grab.X, grab.Y, kind: PointerKind.Touch);
        controller.PointerMove(target.X, target.Y);
        controller.PointerUp();

        controller.Selection.Range.Left.ShouldBe(1);
        controller.Selection.Range.Top.ShouldBe(1);
        controller.Selection.Range.Right.ShouldBe(4);
        controller.Selection.Range.Bottom.ShouldBe(5);

        // the grab must not have panned as well
        controller.Viewport.ScrollX.ShouldBe(0);
    }

    [Fact]
    public async Task DraggingTheStartHandlePastTheOtherEndFlipsTheRangeRatherThanCollapsingIt()
    {
        var controller = await SetupAsync();
        controller.Selection.SelectRange(new CellRange(new CellRef(2, 2), new CellRef(4, 4)));

        var handles = controller.Viewport.SelectionHandles(controller.Selection.Range);
        var grab = (X: handles.Start.X + (handles.Start.Width / 2), Y: handles.Start.Y + (handles.Start.Height / 2));

        var target = Middle(controller, new CellRef(7, 8));

        controller.PointerDown(grab.X, grab.Y, kind: PointerKind.Touch);
        controller.PointerMove(target.X, target.Y);
        controller.PointerUp();

        controller.Selection.Range.Left.ShouldBe(4);
        controller.Selection.Range.Right.ShouldBe(7);
        controller.Selection.Range.Bottom.ShouldBe(8);
    }

    [Fact]
    public async Task AHeaderStillSelectsUnderTouch()
    {
        // Row and column selection is what cut, copy and insert operate on. Turning a header press
        // into a pan would take those away from touch entirely.
        var controller = await SetupAsync();
        var cell = controller.Viewport.CellRect(new CellRef(2, 0));

        controller.PointerDown(cell.X + (cell.Width / 2), 4, kind: PointerKind.Touch);
        controller.PointerUp();

        controller.Selection.Range.Left.ShouldBe(2);
        controller.Selection.Range.Right.ShouldBe(2);
        controller.Selection.Range.Bottom.ShouldBe(CellRef.MaxRow);
    }

    [Fact]
    public async Task APanCannotRunOffPastTheContent()
    {
        // A wheel moves a notch at a time; a finger flings. A sheet that scrolls into an unbounded
        // field of blank cells is indistinguishable from one that has lost its data.
        var controller = await SetupAsync();
        var (x, y) = Middle(controller, new CellRef(2, 2));

        controller.PointerDown(x, y, kind: PointerKind.Touch);
        controller.PointerMove(x - 500_000, y - 500_000);
        controller.PointerUp();

        var used = controller.Sheet.UsedRange!.Value;
        var contentWidth = controller.Metrics.Columns.SizeOfRange(0, used.Right + 1);
        var contentHeight = controller.Metrics.Rows.SizeOfRange(0, used.Bottom + 1);

        // Somewhere past the data, but on the same order as it — not half a million pixels away.
        controller.Viewport.ScrollX.ShouldBeLessThan(contentWidth + controller.Viewport.Width);
        controller.Viewport.ScrollY.ShouldBeLessThan(contentHeight + controller.Viewport.Height);
    }

    [Fact]
    public async Task TheSurfaceOnlyDrawsHandlesOnceAFingerHasBeenSeen()
    {
        var controller = await SetupAsync();
        controller.UsesTouch.ShouldBeFalse();

        var (x, y) = Middle(controller, new CellRef(1, 1));
        controller.PointerDown(x, y);
        controller.PointerUp();
        controller.UsesTouch.ShouldBeFalse("a mouse has drag-to-extend, so handles would be two targets that do nothing");

        controller.PointerDown(x, y, kind: PointerKind.Touch);
        controller.PointerUp();
        controller.UsesTouch.ShouldBeTrue();
    }

    [Fact]
    public async Task AHandleIsNotGrabbableOverTheHeaders()
    {
        // A1's top-left handle sits on the corner box. Letting it be grabbed there would put a target
        // on top of the select-all corner and the header strips.
        var controller = await SetupAsync();
        controller.Selection.MoveTo(new CellRef(0, 0));

        controller.Viewport.SelectionHandleAt(controller.Selection.Range, 2, 2).ShouldBeNull();
    }
}
