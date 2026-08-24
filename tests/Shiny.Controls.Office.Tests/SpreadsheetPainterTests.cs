using Shiny.Controls.Office.Skia;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.View;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// Rasterises the grid headlessly and inspects the pixels.
/// </summary>
/// <remarks>
/// Not a pixel-perfect snapshot suite — those are brittle across font versions. These assert the things
/// that are actually load-bearing: that painting happens at all, that the panes land where the layout
/// says they do, and that a frozen band does not scroll away.
/// </remarks>
public class SpreadsheetPainterTests
{
    const int Width = 400;
    const int Height = 260;

    static async Task<(Workbook Workbook, SpreadsheetController Controller)> SetupAsync(int frozenColumns = 0, int frozenRows = 0)
    {
        var workbook = await Workbook.OpenAsync(new MemoryStream(WorkbookFixture.Build()));
        var controller = new SpreadsheetController(workbook, workbook["Data"]);
        controller.Resize(Width, Height);
        controller.Metrics.FrozenPane = new CellRef(frozenColumns, frozenRows);
        return (workbook, controller);
    }

    static SKBitmap Render(SpreadsheetController controller, SpreadsheetTheme? theme = null)
    {
        var bitmap = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var painter = new SpreadsheetPainter();

        painter.Paint(canvas, new SpreadsheetPaintRequest
        {
            Workbook = controller.Workbook,
            Sheet = controller.Sheet,
            Viewport = controller.Viewport,
            Selection = controller.Selection,
            Theme = theme ?? SpreadsheetTheme.Light,
            EditingCell = controller.EditingCell
        });

        return bitmap;
    }

    static int DistinctColours(SKBitmap bitmap)
    {
        var seen = new HashSet<uint>();
        for (var y = 0; y < bitmap.Height; y += 2)
            for (var x = 0; x < bitmap.Width; x += 2)
                seen.Add((uint)bitmap.GetPixel(x, y));

        return seen.Count;
    }

    [Fact]
    public async Task PaintsSomethingRatherThanABlankSurface()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;
        using var bitmap = Render(controller);

        // Background, gridlines, header fill, header text, cell text, selection border.
        DistinctColours(bitmap).ShouldBeGreaterThan(4);
    }

    [Fact]
    public async Task HeadersArePaintedInTheHeaderBands()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;
        using var bitmap = Render(controller);

        var theme = SpreadsheetTheme.Light;
        var headerPixel = bitmap.GetPixel(Width - 5, 5);
        var expected = theme.HeaderBackground;

        // The far right of the top strip is header background with no text over it.
        headerPixel.Red.ShouldBe(expected.R);
        headerPixel.Green.ShouldBe(expected.G);
        headerPixel.Blue.ShouldBe(expected.B);
    }

    [Fact]
    public async Task TheStyledCellsFillIsPainted()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;
        using var bitmap = Render(controller);

        // A2 in the fixture carries a solid FFF2CC fill.
        var rect = controller.Viewport.CellRect(CellRef.Parse("A2"));
        var pixel = bitmap.GetPixel((int)(rect.X + rect.Width - 3), (int)(rect.Y + rect.Height / 2));

        pixel.Red.ShouldBe((byte)0xFF);
        pixel.Green.ShouldBe((byte)0xF2);
        pixel.Blue.ShouldBe((byte)0xCC);
    }

    [Fact]
    public async Task TheSelectionBorderFollowsTheActiveCell()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("C3"));
        using var bitmap = Render(controller);

        var rect = controller.Viewport.CellRect(CellRef.Parse("C3"));
        var border = SpreadsheetTheme.Light.SelectionBorder;

        // Sample just inside the top edge, where the 2px border sits.
        var pixel = bitmap.GetPixel((int)(rect.X + rect.Width / 2), (int)rect.Y);
        pixel.Red.ShouldBe(border.R);
        pixel.Green.ShouldBe(border.G);
        pixel.Blue.ShouldBe(border.B);
    }

    [Fact]
    public async Task DarkThemeChangesTheBackground()
    {
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        using var light = Render(controller, SpreadsheetTheme.Light);
        using var dark = Render(controller, SpreadsheetTheme.Dark);

        light.GetPixel(Width - 5, Height - 5).ShouldNotBe(dark.GetPixel(Width - 5, Height - 5));
    }

    [Fact]
    public async Task ScrollingMovesContentButNotTheFrozenBand()
    {
        var (workbook, controller) = await SetupAsync(frozenColumns: 1, frozenRows: 1);
        using var _ = workbook;

        using var before = Render(controller);
        controller.Scroll(200, 100);
        using var after = Render(controller);

        // The pinned corner must be untouched by scrolling.
        var corner = controller.Viewport.CellRect(CellRef.Parse("A1"));
        var x = (int)(corner.X + corner.Width / 2);
        var y = (int)(corner.Y + corner.Height / 2);
        before.GetPixel(x, y).ShouldBe(after.GetPixel(x, y));
    }

    [Fact]
    public async Task TheCellBeingEditedIsNotPainted()
    {
        // The editor is a real text input overlaid on the cell; painting the value underneath it would
        // show through as a double image.
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Selection.MoveTo(CellRef.Parse("A1"));
        using var normal = Render(controller);

        controller.BeginEdit();
        using var editing = Render(controller);

        var rect = controller.Viewport.CellRect(CellRef.Parse("A1"));
        var differing = 0;
        for (var x = (int)rect.X + 2; x < (int)rect.Right - 2; x++)
            for (var y = (int)rect.Y + 3; y < (int)rect.Bottom - 3; y++)
            {
                if (normal.GetPixel(x, y) != editing.GetPixel(x, y))
                    differing++;
            }

        differing.ShouldBeGreaterThan(0, "the cell's text should disappear while it is being edited");
    }

    [Fact]
    public async Task PaintingALargeSheetStaysBoundedByTheViewport()
    {
        // Virtualisation check: painting must cost the visible cells, not the used range.
        var (workbook, controller) = await SetupAsync();
        using var _ = workbook;

        controller.Viewport.ScrollTo(0, 0);
        var (firstRow, lastRow) = controller.Viewport.VisibleRows();
        var (firstColumn, lastColumn) = controller.Viewport.VisibleColumns();

        ((lastRow - firstRow + 1) * (lastColumn - firstColumn + 1)).ShouldBeLessThan(200);

        controller.Viewport.ScrollTo(0, 500_000 * 20);
        var (farFirst, farLast) = controller.Viewport.VisibleRows();
        (farLast - farFirst).ShouldBeLessThan(20, "scrolling deep into the sheet must not widen the painted range");
    }
}
