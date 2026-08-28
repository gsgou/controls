using Shiny.Controls.Office.Skia;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.Commands;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Guards on two defects the formatting toolbar exposed, both of which were silent.
/// </summary>
/// <remarks>
/// Neither had a test on the path that actually failed. Column widths were covered at the command
/// level while the bug was in the drag that should have raised the command, and the font defect only
/// shows on WebAssembly, where no test runs — so both are written here against the seam rather than
/// against the symptom.
/// </remarks>
public class SpreadsheetRegressionTests
{
    // ---- a resized column has to survive the save ----

    static Workbook Sheet()
    {
        var workbook = Workbook.Create("Sheet1");
        workbook.Execute(new SetCellValueCommand("Sheet1", CellRef.Parse("A1"), CellValue.FromText("x")));
        return workbook;
    }

    static SpreadsheetController Ready(Workbook workbook)
    {
        var controller = new SpreadsheetController(workbook, workbook.Sheets[0]);
        controller.Resize(600, 400);
        return controller;
    }

    /// <summary>Drags a column header's trailing edge by <paramref name="by"/> pixels.</summary>
    static void DragColumnEdge(SpreadsheetController controller, int column, double by)
    {
        var bounds = controller.Viewport.CellRect(new CellRef(column, 0));

        // Inside the column header strip, and a pixel *short* of the divider. The grip is measured
        // against whichever column the pointer is over, so a point exactly on the boundary already
        // belongs to the next column and reads as a plain header click.
        var grip = bounds.Right - 1;
        var y = controller.Metrics.ColumnHeaderHeight / 2;

        controller.PointerDown(grip, y);
        controller.PointerMove(grip + by, y);
        controller.PointerUp();
    }

    [Fact]
    public void DraggingAColumnEdge_RecordsTheWidthInTheFile()
    {
        // The drag used to move GridMetrics and nothing else, so the width the user dragged out was
        // gone the moment the workbook was saved and reopened - with no error anywhere.
        using var workbook = Sheet();
        var controller = Ready(workbook);

        var before = controller.Metrics.Columns.SizeOf(0);
        DragColumnEdge(controller, 0, 60);

        controller.Metrics.Columns.SizeOf(0).ShouldBe(before + 60, 0.01);
        workbook.Sheets[0].GetColumnWidth(0).ShouldNotBeNull();
    }

    [Fact]
    public void ADraggedWidth_ComesBackAfterAReopen()
    {
        double reopened;

        using (var workbook = Sheet())
        {
            var controller = Ready(workbook);
            DragColumnEdge(controller, 0, 60);

            using var stream = new MemoryStream(workbook.ToArray(), writable: false);
            using var round = Workbook.OpenAsync(stream).GetAwaiter().GetResult();

            reopened = GridMetrics.FromWorksheet(round.Sheets[0]).Columns.SizeOf(0);
        }

        reopened.ShouldBeGreaterThan(GridMetrics.WidthToPixels(GridMetrics.DefaultColumnWidthCharacters));
    }

    [Fact]
    public void DraggingAColumnEdge_IsUndoable()
    {
        using var workbook = Sheet();
        var controller = Ready(workbook);

        DragColumnEdge(controller, 0, 60);
        controller.Undo();

        workbook.Sheets[0].GetColumnWidth(0).ShouldBeNull();
    }

    [Fact]
    public void DraggingARowEdge_RecordsTheHeightInTheFile()
    {
        using var workbook = Sheet();
        var controller = Ready(workbook);

        var bounds = controller.Viewport.CellRect(new CellRef(0, 0));
        var grip = bounds.Bottom - 1;
        var x = controller.Metrics.RowHeaderWidth / 2;

        controller.PointerDown(x, grip);
        controller.PointerMove(x, grip + 20);
        controller.PointerUp();

        workbook.Sheets[0].GetRowHeight(0).ShouldNotBeNull();
    }

    [Fact]
    public void SelectingAColumnHeader_StillSelectsRatherThanResizing()
    {
        // The resize grip is a few pixels wide, and a commit on every pointer-up would put a no-op
        // width command on the undo stack for an ordinary header click.
        using var workbook = Sheet();
        var controller = Ready(workbook);

        var bounds = controller.Viewport.CellRect(new CellRef(1, 0));
        var y = controller.Metrics.ColumnHeaderHeight / 2;

        controller.PointerDown(bounds.X + bounds.Width / 2, y);
        controller.PointerUp();

        controller.Selection.Range.Left.ShouldBe(1);

        // By name rather than by CanUndo: the fixture writes a cell, so the stack is never empty. What
        // must not be on top of it is a width command.
        workbook.Undo.UndoName.ShouldBe("Edit Cell", "selecting a column is not an edit");
    }

    // ---- the painter has to resolve fonts through the registry ----

    const int Width = 240;
    const int Height = 120;

    static byte[] Font(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, name));

    /// <summary>Paints one cell of text in <paramref name="family"/>, optionally through a registry.</summary>
    static SKBitmap Render(string family, bool bold, OfficeFontRegistry? registry)
    {
        using var workbook = Workbook.Create("Sheet1");

        var format = ResolvedFormat.Default with { FontName = family, FontSize = 16, Bold = bold };
        var style = workbook.StyleWriter.Intern(format);

        workbook.Execute(new SetCellValueCommand("Sheet1", CellRef.Parse("A1"), CellValue.FromText("Wg")));
        workbook.Execute(new SetCellStyleCommand("Sheet1", CellRef.Parse("A1"), style));

        var controller = new SpreadsheetController(workbook, workbook.Sheets[0]);
        controller.Resize(Width, Height);

        var bitmap = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var measurer = new SkiaTextMeasurer(registry);
        using var painter = new SpreadsheetPainter(measurer);

        painter.Paint(canvas, new SpreadsheetPaintRequest
        {
            Workbook = workbook,
            Sheet = controller.Sheet,
            Viewport = controller.Viewport,
            Selection = controller.Selection,
            Theme = SpreadsheetTheme.Light
        });

        return bitmap;
    }

    static bool Differ(SKBitmap a, SKBitmap b)
    {
        for (var y = 0; y < a.Height; y++)
        {
            for (var x = 0; x < a.Width; x++)
            {
                if (a.GetPixel(x, y) != b.GetPixel(x, y))
                    return true;
            }
        }

        return false;
    }

    [Fact]
    public void ThePainterResolvesFontsThroughTheRegistry()
    {
        // The defect: the painter called SKTypeface.FromFamilyName itself. That never returns null -
        // it returns the platform default - so a registered face was ignored with nothing to say so,
        // and on WebAssembly, where there is no font manager at all, every cell got one fallback face.
        var registry = new OfficeFontRegistry();
        var family = registry.Register(Font("Carlito-Regular.ttf"));
        family.ShouldNotBeNull();

        using var withRegistry = Render(family, bold: false, registry);
        using var without = Render(family, bold: false, new OfficeFontRegistry());

        Differ(withRegistry, without).ShouldBeTrue(
            "a registered face must reach the grid; identical output means the registry was skipped");
    }

    [Fact]
    public void BoldReachesTheTypefaceRatherThanBeingDropped()
    {
        // What the toolbar's Bold button looked like when it was broken: the command ran, the style
        // was written, the cell repainted, and the glyphs came back the same weight.
        var registry = new OfficeFontRegistry();
        var family = registry.Register(Font("Carlito-Regular.ttf"))!;
        registry.Register(Font("Carlito-Bold.ttf")).ShouldBe(family);

        using var regular = Render(family, bold: false, registry);
        using var bold = Render(family, bold: true, registry);

        Differ(regular, bold).ShouldBeTrue("a bold cell must not paint identically to a regular one");
    }

    [Fact]
    public void APainterGivenAMeasurer_DoesNotDisposeIt()
    {
        // The measurer is usually shared with whatever else is drawing; disposing the caller's would
        // take the fonts out from under it, and the failure would surface somewhere else entirely.
        var registry = new OfficeFontRegistry();
        var family = registry.Register(Font("Carlito-Regular.ttf"))!;

        using var measurer = new SkiaTextMeasurer(registry);
        using (var painter = new SpreadsheetPainter(measurer))
        {
            painter.Fonts.ShouldBeSameAs(registry);
        }

        // Still usable: a disposed measurer's cached SKFonts would throw or hand back a dead handle.
        measurer.GetFont(Text.TextStyle.Default with { FontFamily = family }).Size.ShouldBeGreaterThan(0);
    }
}
