using DocumentFormat.OpenXml;
using Shiny.Controls.Office.Shapes;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;
using D = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Presentation;


/// <summary>A paragraph inside a shape's text body.</summary>
public sealed record ShapeParagraph(IReadOnlyList<StyledRun> Runs)
{
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;

    /// <summary>Nesting level, 0-8, which drives indent and the bullet glyph.</summary>
    public int Level { get; init; }

    public string? Bullet { get; init; }

    public double SpaceBefore { get; init; }
    public double SpaceAfter { get; init; }
    public double LineSpacing { get; init; } = 1.0;

    public string PlainText => string.Concat(this.Runs.Where(x => !x.IsBreak).Select(x => x.Text));

    /// <summary>
    /// The <c>a:p</c> this was read from, or null for a paragraph that came from a layout or master.
    /// </summary>
    /// <remarks>
    /// Edits go straight into this element rather than into a rebuilt paragraph, for the same reason
    /// the Word editor keeps its runs: a run carries language, spelling state and formatting the model
    /// does not represent, and rebuilding one to change a character throws all of that away.
    /// </remarks>
    internal D.Paragraph? Element { get; init; }
}

public enum TextAnchor
{
    Top,
    Middle,
    Bottom
}

public sealed record ShapeTextBody(IReadOnlyList<ShapeParagraph> Paragraphs)
{
    public TextAnchor Anchor { get; init; } = TextAnchor.Top;
    public double InsetLeft { get; init; } = 9.6;
    public double InsetRight { get; init; } = 9.6;
    public double InsetTop { get; init; } = 4.8;
    public double InsetBottom { get; init; } = 4.8;
    public bool WordWrap { get; init; } = true;

    /// <summary>
    /// Scale PowerPoint recorded for shrink-on-overflow autofit. Honoured rather than recomputed,
    /// because recomputing it needs the exact font metrics PowerPoint used.
    /// </summary>
    public double FontScale { get; init; } = 1.0;

    public double LineSpaceReduction { get; init; }

    public string PlainText => string.Join(Environment.NewLine, this.Paragraphs.Select(x => x.PlainText));

    /// <summary>
    /// The text body this was read from, when the shape lives on the slide itself.
    /// </summary>
    /// <remarks>
    /// Typed as the base rather than <c>D.TextBody</c> because a shape's is <c>p:txBody</c> and a
    /// table cell's is <c>a:txBody</c> — two unrelated classes with the same <c>a:p</c> children.
    /// </remarks>
    internal OpenXmlCompositeElement? Element { get; init; }
}

/// <summary>One shape on a slide, positioned in slide coordinates.</summary>
public sealed record SlideShape
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }

    public ShapeGeometry Geometry { get; init; } = ShapeGeometry.Rectangle;
    public ShapeFill Fill { get; init; } = ShapeFill.None;
    public ShapeOutline? Outline { get; init; }
    public ShapeTextBody? Text { get; init; }

    /// <summary>Rotation in degrees, clockwise.</summary>
    public double Rotation { get; init; }

    public bool FlipHorizontal { get; init; }
    public bool FlipVertical { get; init; }

    /// <summary>Image bytes when this shape is a picture.</summary>
    public byte[]? Image { get; init; }

    public string? Name { get; init; }

    /// <summary>Corner radius as a fraction of the smaller side, for rounded rectangles.</summary>
    public double CornerRadius { get; init; } = 0.16;

    /// <summary>A table's laid-out cells, when this shape is a graphic frame holding one.</summary>
    public SlideTable? Table { get; init; }

    /// <summary>
    /// False for shapes painted from the layout or master.
    /// </summary>
    /// <remarks>
    /// Those are template decoration shared by every slide using that layout, not this slide's
    /// content. Letting a click select one would let a user drag the company logo off every slide in
    /// the deck at once, which is never what they meant.
    /// </remarks>
    public bool IsEditable { get; init; }

    /// <summary>The element this was read from — <c>p:sp</c>, <c>p:pic</c> or <c>p:graphicFrame</c>.</summary>
    internal OpenXmlElement? Element { get; init; }
}

public sealed record SlideTableCell(ShapeTextBody? Text, ArgbColor? Fill, int ColumnSpan = 1, int RowSpan = 1, bool IsMerged = false);

public sealed record SlideTable(
    IReadOnlyList<double> ColumnWidths,
    IReadOnlyList<double> RowHeights,
    IReadOnlyList<IReadOnlyList<SlideTableCell>> Rows);

/// <summary>One slide, with its shapes already resolved through the layout and master.</summary>
public sealed record Slide
{
    public required int Number { get; init; }
    public required IReadOnlyList<SlideShape> Shapes { get; init; }

    public ShapeFill Background { get; init; } = ShapeFill.None;

    /// <summary>Speaker notes as plain text, or null when the slide has none.</summary>
    public string? Notes { get; init; }

    /// <summary>The slide's title, taken from its title placeholder.</summary>
    public string? Title { get; init; }
}
