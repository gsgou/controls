using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Document;

/// <summary>Anything that can appear at block level in a document body or table cell.</summary>
public abstract record DocumentBlock;

/// <summary>Paragraph-level formatting, flattened from the style chain and direct formatting.</summary>
public sealed record ParagraphFormat
{
    public static readonly ParagraphFormat Default = new();

    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    /// <summary>Left indent in pixels.</summary>
    public double IndentLeft { get; init; }

    public double IndentRight { get; init; }

    /// <summary>Extra indent applied to the first line only; negative for a hanging indent.</summary>
    public double IndentFirstLine { get; init; }

    public double SpaceBefore { get; init; }
    public double SpaceAfter { get; init; }

    /// <summary>Line height multiplier. 1 is single spacing.</summary>
    public double LineSpacing { get; init; } = 1.0;

    public ArgbColor? Shading { get; init; }

    /// <summary>Heading level, 1-9, or 0 when this is body text. Drives outline and navigation.</summary>
    public int OutlineLevel { get; init; }
}

/// <summary>A list label already resolved to the text that should be drawn.</summary>
public sealed record ListLabel(string Text, TextStyle Style, double Indent, double HangingIndent);

public sealed record DocumentParagraph(IReadOnlyList<StyledRun> Runs, ParagraphFormat Format) : DocumentBlock
{
    /// <summary>
    /// The OOXML element this paragraph was read from.
    /// </summary>
    /// <remarks>
    /// The anchor for surgical editing: commands mutate this element and re-project the paragraph from
    /// it, so parts of the document the editor does not model are never rebuilt and never lost.
    /// Null for a paragraph the editor synthesised but has not yet attached.
    /// </remarks>
    internal DocumentFormat.OpenXml.Wordprocessing.Paragraph? Element { get; init; }

    public ListLabel? List { get; init; }

    /// <summary>The named style this paragraph came from, useful for outline extraction.</summary>
    public string? StyleName { get; init; }

    public string PlainText => string.Concat(this.Runs.Where(x => !x.IsBreak).Select(x => x.Text));
}

public sealed record DocumentTableCell(IReadOnlyList<DocumentBlock> Blocks)
{
    /// <summary>How many grid columns this cell spans.</summary>
    public int ColumnSpan { get; init; } = 1;

    /// <summary>
    /// True when this cell is continuing a vertical merge from above, in which case it renders as part
    /// of the cell that started the merge rather than as a cell of its own.
    /// </summary>
    public bool IsVerticalContinuation { get; init; }

    public ArgbColor? Shading { get; init; }

    /// <summary>Explicit width in pixels, or null to share the table's width evenly.</summary>
    public double? Width { get; init; }
}

public sealed record DocumentTableRow(IReadOnlyList<DocumentTableCell> Cells)
{
    /// <summary>True for a row marked to repeat as a header when the table breaks across pages.</summary>
    public bool IsHeader { get; init; }
}

public sealed record DocumentTable(IReadOnlyList<DocumentTableRow> Rows) : DocumentBlock
{
    /// <summary>Column widths in pixels, from the table grid. Empty when the grid is absent.</summary>
    public IReadOnlyList<double> ColumnWidths { get; init; } = [];

    public bool HasBorders { get; init; } = true;
}

/// <summary>A horizontal rule.</summary>
public sealed record DocumentRule : DocumentBlock;

/// <summary>Page geometry, used to give the reflowed view a sensible measure.</summary>
public sealed record PageSetup
{
    public static readonly PageSetup Letter = new();

    /// <summary>Page width in pixels.</summary>
    public double Width { get; init; } = 816;   // 8.5in at 96dpi

    public double Height { get; init; } = 1056; // 11in at 96dpi
    public double MarginLeft { get; init; } = 96;
    public double MarginRight { get; init; } = 96;
    public double MarginTop { get; init; } = 96;
    public double MarginBottom { get; init; } = 96;

    public double ContentWidth => Math.Max(1, this.Width - this.MarginLeft - this.MarginRight);
}
