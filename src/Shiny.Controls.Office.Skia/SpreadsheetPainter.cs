using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Spreadsheet.View;
using Shiny.Controls.Office.Text;
using SkiaSharp;

namespace Shiny.Controls.Office.Skia;

/// <summary>
/// Everything the painter needs for one frame.
/// </summary>
public sealed record SpreadsheetPaintRequest
{
    public required Workbook Workbook { get; init; }
    public required Worksheet Sheet { get; init; }
    public required GridViewport Viewport { get; init; }
    public required SpreadsheetSelection Selection { get; init; }
    public SpreadsheetTheme Theme { get; init; } = SpreadsheetTheme.Light;

    /// <summary>Device pixels per logical pixel. The canvas is scaled by this before anything is drawn.</summary>
    public float Scale { get; init; } = 1f;

    /// <summary>Hides the active cell's content while an editor is overlaid on it.</summary>
    public CellRef? EditingCell { get; init; }
}

/// <summary>
/// Draws the spreadsheet grid onto an <see cref="SKCanvas"/>.
/// </summary>
/// <remarks>
/// This is the single paint routine both hosts use: MAUI hands it a Skia-backed drawable and Blazor
/// hands it an <c>SKCanvasView</c> surface. Keeping it here rather than in either host package is what
/// makes the two genuinely the same renderer instead of two implementations kept in step by hand.
/// </remarks>
public sealed class SpreadsheetPainter : IDisposable
{
    readonly SkiaTextMeasurer measurer;
    readonly bool ownsMeasurer;
    readonly SKPaint fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    readonly SKPaint stroke = new() { IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

    public SpreadsheetPainter()
        : this(null)
    {
    }

    /// <summary>
    /// Shares a measurer with the rest of the app, rather than resolving fonts on its own.
    /// </summary>
    /// <param name="measurer">
    /// The measurer to take fonts from, or null to own one over
    /// <see cref="OfficeFontRegistry.Default"/>.
    /// </param>
    public SpreadsheetPainter(SkiaTextMeasurer? measurer)
    {
        this.ownsMeasurer = measurer is null;
        this.measurer = measurer ?? new SkiaTextMeasurer();
    }

    /// <summary>The application-supplied faces this painter resolves against.</summary>
    public OfficeFontRegistry Fonts => this.measurer.Registry;

    public void Paint(SKCanvas canvas, SpreadsheetPaintRequest request)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(request);

        canvas.Save();
        canvas.Scale(request.Scale);

        var viewport = request.Viewport;
        var theme = request.Theme;

        canvas.Clear(ToSk(theme.Background));

        var (firstColumn, lastColumn) = viewport.VisibleColumns();
        var (firstRow, lastRow) = viewport.VisibleRows();
        var frozen = viewport.Metrics.FrozenPane;

        // Four panes, each clipped to its own band so a scrolled cell cannot paint over a pinned one.
        this.PaintPane(canvas, request, frozen.Column, lastColumn, frozen.Row, lastRow, firstColumn, firstRow, PaneKind.Scrollable);

        if (frozen.Column > 0)
            this.PaintPane(canvas, request, 0, frozen.Column - 1, frozen.Row, lastRow, 0, firstRow, PaneKind.FrozenColumns);

        if (frozen.Row > 0)
            this.PaintPane(canvas, request, frozen.Column, lastColumn, 0, frozen.Row - 1, firstColumn, 0, PaneKind.FrozenRows);

        if (frozen.Column > 0 && frozen.Row > 0)
            this.PaintPane(canvas, request, 0, frozen.Column - 1, 0, frozen.Row - 1, 0, 0, PaneKind.Corner);

        this.PaintHeaders(canvas, request, firstColumn, lastColumn, firstRow, lastRow);
        this.PaintFrozenDividers(canvas, request);

        canvas.Restore();
    }

    void PaintPane(
        SKCanvas canvas,
        SpreadsheetPaintRequest request,
        int columnStart,
        int columnEnd,
        int rowStart,
        int rowEnd,
        int firstColumn,
        int firstRow,
        PaneKind pane)
    {
        var viewport = request.Viewport;
        var metrics = viewport.Metrics;

        // A frozen band occupies a fixed strip; the scrollable pane gets whatever is left.
        var clipLeft = pane.HasFlag(PaneKind.FrozenColumns) ? metrics.RowHeaderWidth : viewport.ContentOriginX;
        var clipRight = pane.HasFlag(PaneKind.FrozenColumns) ? viewport.ContentOriginX : viewport.Width;
        var clipTop = pane.HasFlag(PaneKind.FrozenRows) ? metrics.ColumnHeaderHeight : viewport.ContentOriginY;
        var clipBottom = pane.HasFlag(PaneKind.FrozenRows) ? viewport.ContentOriginY : viewport.Height;

        if (clipRight <= clipLeft || clipBottom <= clipTop)
            return;

        var actualColumnStart = pane.HasFlag(PaneKind.FrozenColumns) ? columnStart : firstColumn;
        var actualRowStart = pane.HasFlag(PaneKind.FrozenRows) ? rowStart : firstRow;

        canvas.Save();
        canvas.ClipRect(new SKRect((float)clipLeft, (float)clipTop, (float)clipRight, (float)clipBottom));

        this.PaintCells(canvas, request, actualColumnStart, columnEnd, actualRowStart, rowEnd);
        this.PaintGridLines(canvas, request, actualColumnStart, columnEnd, actualRowStart, rowEnd);
        this.PaintSelection(canvas, request, actualColumnStart, columnEnd, actualRowStart, rowEnd);

        canvas.Restore();
    }

    void PaintCells(SKCanvas canvas, SpreadsheetPaintRequest request, int columnStart, int columnEnd, int rowStart, int rowEnd)
    {
        var viewport = request.Viewport;
        var sheet = request.Sheet;
        var styles = request.Workbook.Styles;
        var merges = sheet.MergedRanges;

        for (var row = rowStart; row <= rowEnd; row++)
        {
            if (viewport.Metrics.Rows.IsHidden(row))
                continue;

            for (var column = columnStart; column <= columnEnd; column++)
            {
                if (viewport.Metrics.Columns.IsHidden(column))
                    continue;

                var cell = new CellRef(column, row);

                // A merged region paints once, from its anchor, across the whole span.
                var merge = merges.FirstOrDefault(x => x.Contains(cell));
                if (merge != default && merge.TopLeft != cell)
                    continue;

                var bounds = merge != default ? viewport.RangeRect(merge) : viewport.CellRect(cell);
                this.PaintCell(canvas, request, cell, bounds, styles);
            }
        }
    }

    void PaintCell(SKCanvas canvas, SpreadsheetPaintRequest request, CellRef cell, GridRect bounds, StyleResolver styles)
    {
        var format = styles.Resolve(request.Sheet.GetEffectiveStyleIndex(cell));
        var rect = ToSk(bounds);

        if (!format.Background.IsTransparent)
        {
            this.fill.Color = ToSk(format.Background);
            canvas.DrawRect(rect, this.fill);
        }

        if (request.EditingCell == cell)
            return;

        var value = request.Sheet.GetDisplayValue(cell);
        if (value.IsBlank)
            return;

        var text = styles.Format(value, format);
        if (text.Length == 0)
            return;

        var theme = request.Theme;
        var font = this.GetFont(format.FontName, (float)format.FontSize, format.Bold, format.Italic);

        this.fill.Color = ToSk(format.Foreground == ResolvedFormat.Default.Foreground ? theme.CellText : format.Foreground);

        var padding = (float)theme.CellPadding;
        var indent = (float)(format.Indent * theme.IndentWidth);
        var measured = font.MeasureText(text);

        var alignment = format.EffectiveAlignment(value.Kind);
        var x = alignment switch
        {
            CellHorizontalAlignment.Right => rect.Right - padding - measured,
            CellHorizontalAlignment.Center or CellHorizontalAlignment.CenterContinuous => rect.MidX - measured / 2,
            _ => rect.Left + padding + indent
        };

        // Baseline placement: metrics.Descent is positive downward, so this centres the glyph box.
        var metrics = font.Metrics;
        var y = format.VerticalAlignment switch
        {
            CellVerticalAlignment.Top => rect.Top + padding - metrics.Ascent,
            CellVerticalAlignment.Center => rect.MidY - (metrics.Ascent + metrics.Descent) / 2,
            _ => rect.Bottom - padding - metrics.Descent
        };

        // Text is clipped to its own cell. Excel spills unformatted text into empty neighbours; that
        // needs a scan of the cells to the side and is deliberately not done here yet.
        canvas.Save();
        canvas.ClipRect(rect);
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, this.fill);
        canvas.Restore();
    }

    void PaintGridLines(SKCanvas canvas, SpreadsheetPaintRequest request, int columnStart, int columnEnd, int rowStart, int rowEnd)
    {
        var viewport = request.Viewport;
        this.stroke.Color = ToSk(request.Theme.GridLine);
        this.stroke.StrokeWidth = 1;

        for (var column = columnStart; column <= columnEnd + 1; column++)
        {
            var bounds = viewport.CellRect(new CellRef(Math.Min(column, CellRef.MaxColumn), rowStart));

            // Half-pixel offset keeps a 1px line on a device pixel instead of blurring across two.
            var x = (float)Math.Floor(column > CellRef.MaxColumn ? bounds.Right : bounds.X) + 0.5f;
            canvas.DrawLine(x, (float)viewport.Metrics.ColumnHeaderHeight, x, (float)viewport.Height, this.stroke);
        }

        for (var row = rowStart; row <= rowEnd + 1; row++)
        {
            var bounds = viewport.CellRect(new CellRef(columnStart, Math.Min(row, CellRef.MaxRow)));
            var y = (float)Math.Floor(row > CellRef.MaxRow ? bounds.Bottom : bounds.Y) + 0.5f;
            canvas.DrawLine((float)viewport.Metrics.RowHeaderWidth, y, (float)viewport.Width, y, this.stroke);
        }
    }

    void PaintSelection(SKCanvas canvas, SpreadsheetPaintRequest request, int columnStart, int columnEnd, int rowStart, int rowEnd)
    {
        var range = request.Selection.Range;
        if (range.Left > columnEnd || range.Right < columnStart || range.Top > rowEnd || range.Bottom < rowStart)
            return;

        var viewport = request.Viewport;
        var theme = request.Theme;
        var rect = ToSk(viewport.RangeRect(range));

        if (!range.IsSingleCell)
        {
            this.fill.Color = ToSk(theme.SelectionFill);
            canvas.DrawRect(rect, this.fill);
        }

        this.stroke.Color = ToSk(theme.SelectionBorder);
        this.stroke.StrokeWidth = (float)theme.SelectionBorderWidth;
        canvas.DrawRect(rect, this.stroke);
        this.stroke.StrokeWidth = 1;

        // The fill handle sits on the outside corner, the way Excel draws it.
        var handle = (float)theme.FillHandleSize;
        this.fill.Color = ToSk(theme.SelectionBorder);
        canvas.DrawRect(new SKRect(rect.Right - handle / 2, rect.Bottom - handle / 2, rect.Right + handle / 2, rect.Bottom + handle / 2), this.fill);
    }

    void PaintHeaders(SKCanvas canvas, SpreadsheetPaintRequest request, int firstColumn, int lastColumn, int firstRow, int lastRow)
    {
        var viewport = request.Viewport;
        var metrics = viewport.Metrics;
        var theme = request.Theme;
        var selection = request.Selection.Range;
        var font = this.GetFont(theme.FontFamily, (float)theme.FontSize, bold: false, italic: false);
        var frozen = metrics.FrozenPane;

        // Column strip.
        canvas.Save();
        canvas.ClipRect(new SKRect((float)metrics.RowHeaderWidth, 0, (float)viewport.Width, (float)metrics.ColumnHeaderHeight));
        this.fill.Color = ToSk(theme.HeaderBackground);
        canvas.DrawRect(new SKRect((float)metrics.RowHeaderWidth, 0, (float)viewport.Width, (float)metrics.ColumnHeaderHeight), this.fill);

        foreach (var column in HeaderIndexes(frozen.Column, firstColumn, lastColumn))
        {
            if (metrics.Columns.IsHidden(column))
                continue;

            var bounds = viewport.CellRect(new CellRef(column, firstRow));
            var rect = new SKRect((float)bounds.X, 0, (float)bounds.Right, (float)metrics.ColumnHeaderHeight);

            if (column >= selection.Left && column <= selection.Right)
            {
                this.fill.Color = ToSk(theme.HeaderSelectedBackground);
                canvas.DrawRect(rect, this.fill);
            }

            this.fill.Color = ToSk(theme.HeaderText);
            DrawCentred(canvas, CellRef.ColumnName(column), rect, font, this.fill);
        }

        this.stroke.Color = ToSk(theme.HeaderBorder);
        canvas.DrawLine((float)metrics.RowHeaderWidth, (float)metrics.ColumnHeaderHeight - 0.5f, (float)viewport.Width, (float)metrics.ColumnHeaderHeight - 0.5f, this.stroke);
        canvas.Restore();

        // Row gutter.
        canvas.Save();
        canvas.ClipRect(new SKRect(0, (float)metrics.ColumnHeaderHeight, (float)metrics.RowHeaderWidth, (float)viewport.Height));
        this.fill.Color = ToSk(theme.HeaderBackground);
        canvas.DrawRect(new SKRect(0, (float)metrics.ColumnHeaderHeight, (float)metrics.RowHeaderWidth, (float)viewport.Height), this.fill);

        foreach (var row in HeaderIndexes(frozen.Row, firstRow, lastRow))
        {
            if (metrics.Rows.IsHidden(row))
                continue;

            var bounds = viewport.CellRect(new CellRef(firstColumn, row));
            var rect = new SKRect(0, (float)bounds.Y, (float)metrics.RowHeaderWidth, (float)bounds.Bottom);

            if (row >= selection.Top && row <= selection.Bottom)
            {
                this.fill.Color = ToSk(theme.HeaderSelectedBackground);
                canvas.DrawRect(rect, this.fill);
            }

            this.fill.Color = ToSk(theme.HeaderText);
            DrawCentred(canvas, (row + 1).ToString(), rect, font, this.fill);
        }

        this.stroke.Color = ToSk(theme.HeaderBorder);
        canvas.DrawLine((float)metrics.RowHeaderWidth - 0.5f, (float)metrics.ColumnHeaderHeight, (float)metrics.RowHeaderWidth - 0.5f, (float)viewport.Height, this.stroke);
        canvas.Restore();

        // Select-all corner.
        this.fill.Color = ToSk(theme.HeaderBackground);
        canvas.DrawRect(new SKRect(0, 0, (float)metrics.RowHeaderWidth, (float)metrics.ColumnHeaderHeight), this.fill);
    }

    /// <summary>Frozen indexes first, then the scrolled ones — the order the header strip needs.</summary>
    static IEnumerable<int> HeaderIndexes(int frozenCount, int first, int last)
    {
        for (var i = 0; i < frozenCount; i++)
            yield return i;

        for (var i = Math.Max(first, frozenCount); i <= last; i++)
            yield return i;
    }

    void PaintFrozenDividers(SKCanvas canvas, SpreadsheetPaintRequest request)
    {
        var viewport = request.Viewport;
        var metrics = viewport.Metrics;
        if (!metrics.HasFrozenColumns && !metrics.HasFrozenRows)
            return;

        this.stroke.Color = ToSk(request.Theme.FrozenDivider);
        this.stroke.StrokeWidth = 1;

        if (metrics.HasFrozenColumns)
        {
            var x = (float)viewport.ContentOriginX - 0.5f;
            canvas.DrawLine(x, 0, x, (float)viewport.Height, this.stroke);
        }

        if (metrics.HasFrozenRows)
        {
            var y = (float)viewport.ContentOriginY - 0.5f;
            canvas.DrawLine(0, y, (float)viewport.Width, y, this.stroke);
        }
    }

    static void DrawCentred(SKCanvas canvas, string text, SKRect rect, SKFont font, SKPaint paint)
    {
        var width = font.MeasureText(text);
        var metrics = font.Metrics;
        canvas.DrawText(text, rect.MidX - width / 2, rect.MidY - (metrics.Ascent + metrics.Descent) / 2, SKTextAlign.Left, font, paint);
    }

    /// <summary>
    /// The font for a cell, resolved and cached by the shared measurer.
    /// </summary>
    /// <remarks>
    /// Through the measurer rather than straight to <c>SKTypeface.FromFamilyName</c>, because that
    /// call returns the same embedded fallback for every family and every weight on WebAssembly,
    /// where there are no system fonts at all. A grid that asked the platform directly rendered a
    /// bold cell in regular and gave no sign of it — the request succeeded, it just meant nothing.
    /// The measurer consults the application's registered faces first, and carries the substitution
    /// table that turns a Calibri request into the bundled Carlito.
    /// </remarks>
    SKFont GetFont(string family, float size, bool bold, bool italic)
        => this.measurer.GetFont(TextStyle.Default with
        {
            FontFamily = family,
            FontSize = size,
            Bold = bold,
            Italic = italic
        });

    static SKColor ToSk(ArgbColor color) => new(color.R, color.G, color.B, color.A);

    static SKRect ToSk(GridRect rect) => new((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom);

    public void Dispose()
    {
        // Only the measurer this painter made. One passed in is the caller's, and disposing it would
        // take the fonts out from under whatever else is drawing with it.
        if (this.ownsMeasurer)
            this.measurer.Dispose();

        this.fill.Dispose();
        this.stroke.Dispose();
    }
}
