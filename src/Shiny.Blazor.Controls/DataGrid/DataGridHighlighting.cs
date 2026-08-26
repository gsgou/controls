using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

public partial class DataGrid<TItem>
{
    /// <summary>
    /// Highlights whole rows - a fill, a stroke, or both, applied to every cell of the rows the
    /// delegate returns a style for. Return <c>null</c> for a row to leave it alone.
    /// </summary>
    /// <remarks>
    /// Evaluated when a row renders, not when a property on the item changes.
    /// </remarks>
    [Parameter] public Func<TItem, DataGridCellStyle?>? RowHighlight { get; set; }

    /// <summary>
    /// Declarative highlighting rules, each covering a row, a column, one cell, or the whole grid
    /// depending on which of its targeting members are set. Later rules win over earlier ones at the
    /// same scope; a narrower scope always wins over a wider one.
    /// </summary>
    [Parameter] public IEnumerable<DataGridHighlight<TItem>>? Highlights { get; set; }

    /// <summary>
    /// The final style for one cell: every rule that covers it, laid over each other from the widest
    /// scope to the narrowest. The position flags are what let a stroke trace the perimeter of the
    /// region rather than boxing each cell in it.
    /// </summary>
    internal DataGridCellStyle? ResolveCellStyle(
        ColumnBase<TItem> col,
        TItem item,
        bool firstColumn,
        bool lastColumn,
        bool firstRow,
        bool lastRow
    )
    {
        // Every cell of every row asks, so the whole resolution path is skipped for the cells - most of
        // them - that nothing could possibly paint.
        if (this.RowHighlight is null
            && this.Highlights is null
            && col.Highlight is null
            && col.CellStyle is null)
            return null;

        DataGridCellStyle? result = null;

        void Apply(DataGridCellStyle? style, DataGridHighlightScope scope)
        {
            if (style is null)
                return;

            if (style.HasBorder && style.BorderEdges is null)
                style = style.WithEdges(EdgesFor(scope, firstColumn, lastColumn, firstRow, lastRow));

            result = DataGridCellStyle.Merge(result, style);
        }

        // Widest first. Within one scope the declared column parameter goes under the collection, so a
        // rule handed in at runtime (a user clicking "highlight this") can override the markup.
        this.ApplyRules(col, item, DataGridHighlightScope.Grid, Apply);
        Apply(col.Highlight, DataGridHighlightScope.Column);
        this.ApplyRules(col, item, DataGridHighlightScope.Column, Apply);
        Apply(this.RowHighlight?.Invoke(item), DataGridHighlightScope.Row);
        this.ApplyRules(col, item, DataGridHighlightScope.Row, Apply);
        this.ApplyRules(col, item, DataGridHighlightScope.Cell, Apply);
        Apply(col.CellStyle?.Invoke(item), DataGridHighlightScope.Cell);

        return result;
    }

    void ApplyRules(
        ColumnBase<TItem> col,
        TItem item,
        DataGridHighlightScope scope,
        Action<DataGridCellStyle?, DataGridHighlightScope> apply
    )
    {
        if (this.Highlights is null)
            return;

        foreach (var rule in this.Highlights)
        {
            if (rule is null || !rule.IsEnabled || rule.Scope != scope)
                continue;

            if (rule.MatchesRow(item) && rule.MatchesColumn(col))
                apply(rule, scope);
        }
    }

    /// <summary>
    /// The sides of one cell that fall on the outside of its highlight's region. A region only one
    /// column wide is stroked on both vertical edges of every cell in it; one that spans the columns
    /// is stroked only where it actually ends. Same, transposed, for rows.
    /// </summary>
    internal static DataGridBorderEdges EdgesFor(
        DataGridHighlightScope scope,
        bool firstColumn,
        bool lastColumn,
        bool firstRow,
        bool lastRow
    )
    {
        var spansColumns = scope is DataGridHighlightScope.Row or DataGridHighlightScope.Grid;
        var spansRows = scope is DataGridHighlightScope.Column or DataGridHighlightScope.Grid;

        var edges = DataGridBorderEdges.None;
        if (!spansColumns || firstColumn) edges |= DataGridBorderEdges.Left;
        if (!spansColumns || lastColumn) edges |= DataGridBorderEdges.Right;
        if (!spansRows || firstRow) edges |= DataGridBorderEdges.Top;
        if (!spansRows || lastRow) edges |= DataGridBorderEdges.Bottom;
        return edges;
    }

    /// <summary>
    /// The fill is emitted as a <c>background-image</c> gradient rather than a <c>background-color</c>
    /// on purpose: it then layers <b>over</b> the row's stripe/selection tint and, on a frozen cell,
    /// over the opaque pane background that cell has to keep - and it is still painted behind the
    /// text, so the content of the cell is never obscured by its own highlight.
    /// </summary>
    internal static string? FillCss(DataGridCellStyle style)
    {
        if (!style.HasFill)
            return null;

        var opacity = Math.Clamp(style.FillOpacity ?? DataGridCellStyle.DefaultFillOpacity, 0d, 1d);
        if (opacity <= 0)
            return null;

        var colour = opacity >= 1
            ? style.Fill!.Trim()
            : string.Create(
                CultureInfo.InvariantCulture,
                $"color-mix(in srgb, {style.Fill!.Trim()} {opacity * 100:0.##}%, transparent)"
            );

        return $"background-image:linear-gradient({colour},{colour});";
    }

    /// <summary>The per-edge <c>border-*</c> declarations for a highlight's stroke.</summary>
    internal static string? BorderCss(DataGridCellStyle style)
    {
        if (!style.HasBorder)
            return null;

        var edges = style.BorderEdges ?? DataGridBorderEdges.All;
        if (edges == DataGridBorderEdges.None)
            return null;

        var line = (string.IsNullOrWhiteSpace(style.BorderWidth)
                ? DataGridCellStyle.DefaultBorderWidth
                : style.BorderWidth!.Trim())
            + " " + BorderKeyword(style.BorderStyle)
            + " " + style.BorderColor!.Trim();

        var sb = new System.Text.StringBuilder();
        if (edges.HasFlag(DataGridBorderEdges.Top)) sb.Append("border-top:").Append(line).Append(';');
        if (edges.HasFlag(DataGridBorderEdges.Right)) sb.Append("border-right:").Append(line).Append(';');
        if (edges.HasFlag(DataGridBorderEdges.Bottom)) sb.Append("border-bottom:").Append(line).Append(';');
        if (edges.HasFlag(DataGridBorderEdges.Left)) sb.Append("border-left:").Append(line).Append(';');
        return sb.ToString();
    }

    static string BorderKeyword(DataGridBorderStyle style)
        => style switch
        {
            DataGridBorderStyle.Dashed => "dashed",
            DataGridBorderStyle.Dotted => "dotted",
            DataGridBorderStyle.Double => "double",
            _ => "solid"
        };
}
