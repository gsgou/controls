using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Shapes;
using Shiny.Controls.Office.Spreadsheet;
using A = DocumentFormat.OpenXml.Drawing;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Pic = DocumentFormat.OpenXml.Drawing.Pictures;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Builds the WordprocessingML the editor inserts.
/// </summary>
/// <remarks>
/// <para>
/// Written as raw OOXML rather than through the block model, for the same reason the slide side has a
/// shape factory: a <c>w:drawing</c> whose children are in the wrong order, or whose
/// <c>a:graphicData</c> carries the wrong URI, produces a file Word calls corrupt and offers to repair
/// rather than open. The projection is built by re-reading what is written here, never the other way
/// round.
/// </para>
/// <para>
/// Everything inserted is <b>inline</b> — a <c>wp:inline</c>, not a <c>wp:anchor</c>. The view is a
/// reflow engine with no fixed positions to anchor to, so a floating object could be written but never
/// drawn where it claimed to be.
/// </para>
/// </remarks>
static class WordContentFactory
{
    /// <summary>
    /// The <c>a:graphicData</c> URI that marks a drawing as holding a shape.
    /// </summary>
    /// <remarks>
    /// A 2010 Microsoft extension namespace rather than an ECMA one, because ECMA-376 never defined a
    /// way to put a DrawingML shape in a Word drawing — the original answer was VML. Every version of
    /// Word since 2010 writes and reads this, and older ones fall back to the VML in
    /// <c>mc:Fallback</c>, which is not written here: the shape is simply absent in Word 2007 rather
    /// than wrong.
    /// </remarks>
    const string ShapeUri = "http://schemas.microsoft.com/office/word/2010/wordprocessingShape";

    const string PictureUri = "http://schemas.openxmlformats.org/drawingml/2006/picture";

    /// <summary>A run holding an inline preset-geometry shape.</summary>
    public static Run Shape(
        uint id,
        ShapeGeometry geometry,
        double width,
        double height,
        ArgbColor? fill,
        ArgbColor? outline,
        string? text = null)
    {
        var properties = new Wps.ShapeProperties(
            new A.Transform2D(
                new A.Offset { X = 0L, Y = 0L },
                new A.Extents { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) }),
            new A.PresetGeometry(new A.AdjustValueList()) { Preset = PresetOf(geometry) });

        properties.AppendChild<OpenXmlElement>(fill is { } fillColor
            ? new A.SolidFill(new A.RgbColorModelHex { Val = Hex(fillColor) })
            : new A.NoFill());

        if (outline is { } outlineColor)
        {
            properties.AppendChild(new A.Outline(
                new A.SolidFill(new A.RgbColorModelHex { Val = Hex(outlineColor) }))
            {
                Width = (int)OoxmlUnits.PixelsToEmu(1)
            });
        }

        var shape = new Wps.WordprocessingShape(
            new Wps.NonVisualDrawingProperties { Id = id, Name = $"Shape {id}" },
            new Wps.NonVisualDrawingShapeProperties(),
            properties);

        if (!string.IsNullOrEmpty(text))
        {
            shape.AppendChild(new Wps.TextBoxInfo2(
                new TextBoxContent(
                    new Paragraph(
                        new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                        new Run(new W.Text(text) { Space = SpaceProcessingModeValues.Preserve })))));
        }

        // Required, and required last: a wsp without a bodyPr is schema-invalid even when the shape
        // holds no text at all.
        shape.AppendChild(new Wps.TextBodyProperties
        {
            Rotation = 0,
            UseParagraphSpacing = false,
            Anchor = A.TextAnchoringTypeValues.Center,
            AnchorCenter = false
        });

        return new Run(new Drawing(
            Inline(id, width, height, $"Shape {id}", new A.GraphicData(shape) { Uri = ShapeUri })));
    }

    /// <summary>
    /// A run holding an inline picture, referencing an image part already added to the document.
    /// </summary>
    /// <param name="relationshipId">The id of the <c>ImagePart</c> relationship on the main part.</param>
    public static Run Picture(uint id, string relationshipId, double width, double height, string name)
    {
        var picture = new Pic.Picture(
            new Pic.NonVisualPictureProperties(
                new Pic.NonVisualDrawingProperties { Id = 0U, Name = name },
                new Pic.NonVisualPictureDrawingProperties()),
            new Pic.BlipFill(
                new A.Blip { Embed = relationshipId },
                new A.Stretch(new A.FillRectangle())),
            new Pic.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) }),

                // A picture is always a plain rectangle here. Cropping to a shape is a a:prstGeom of
                // its own plus a srcRect, which is a feature rather than a default.
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));

        return new Run(new Drawing(
            Inline(id, width, height, name, new A.GraphicData(picture) { Uri = PictureUri })));
    }

    /// <summary>
    /// The <c>wp:inline</c> wrapper both of the above sit in.
    /// </summary>
    /// <remarks>
    /// The four <c>dist*</c> attributes are the margins around the object and are required, not
    /// optional — Word rejects an inline without them. Zero is what it writes for an object with no
    /// wrapping to keep clear of, which is every inline object.
    /// </remarks>
    static Wp.Inline Inline(uint id, double width, double height, string name, A.GraphicData data)
        => new(
            new Wp.Extent { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) },
            new Wp.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
            new Wp.DocProperties { Id = id, Name = name },
            new Wp.NonVisualGraphicFrameDrawingProperties(),
            new A.Graphic(data))
        {
            DistanceFromTop = 0U,
            DistanceFromBottom = 0U,
            DistanceFromLeft = 0U,
            DistanceFromRight = 0U
        };

    /// <summary>
    /// An empty table with a fixed grid and single-line borders.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sized in fiftieths of a percent (<c>pct</c>) rather than twips, so the table fills the measure
    /// it is dropped into and keeps doing so when the page margins change. Word's own Insert Table
    /// does the same.
    /// </para>
    /// <para>
    /// Every cell gets an empty paragraph. That is not padding: a <c>w:tc</c> whose content is empty
    /// is invalid, and Word repairs the file by discarding the table.
    /// </para>
    /// </remarks>
    public static Table Table(int rows, int columns, ArgbColor? borderColor = null)
    {
        rows = Math.Max(1, rows);
        columns = Math.Max(1, columns);

        var hex = borderColor is { } color ? Hex(color) : "auto";

        static TableBorders Borders(string hex)
        {
            BorderType Edge<T>() where T : BorderType, new()
                => new T { Val = BorderValues.Single, Size = 4U, Space = 0U, Color = hex };

            return new TableBorders(
                Edge<TopBorder>(),
                Edge<LeftBorder>(),
                Edge<BottomBorder>(),
                Edge<RightBorder>(),
                Edge<InsideHorizontalBorder>(),
                Edge<InsideVerticalBorder>());
        }

        var table = new Table(
            new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                Borders(hex)),
            new TableGrid(Enumerable.Range(0, columns).Select(_ => new GridColumn())));

        // Each column takes an equal share; 5000 pct-fiftieths is the full measure.
        var share = (5000 / columns).ToString();

        for (var r = 0; r < rows; r++)
        {
            var row = new TableRow();

            for (var c = 0; c < columns; c++)
            {
                row.AppendChild(new TableCell(
                    new TableCellProperties(new TableCellWidth { Width = share, Type = TableWidthUnitValues.Pct }),
                    new Paragraph()));
            }

            table.AppendChild(row);
        }

        return table;
    }

    /// <summary>An empty paragraph, for the block that has to follow a table at the end of a body.</summary>
    /// <remarks>
    /// A body may not end with a table: Word requires a trailing paragraph, and a document whose last
    /// element is a <c>w:tbl</c> is one it offers to repair.
    /// </remarks>
    public static Paragraph EmptyParagraph() => new();

    static string Hex(ArgbColor color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>The DrawingML preset name for a geometry the editor can insert.</summary>
    /// <remarks>
    /// The inverse of the reader's mapping, which is many-to-one — several presets read back as the
    /// same geometry — so this picks the canonical one for each. A round trip through the two is
    /// therefore stable even though it is not always identity.
    /// </remarks>
    static A.ShapeTypeValues PresetOf(ShapeGeometry geometry) => geometry switch
    {
        ShapeGeometry.RoundedRectangle => A.ShapeTypeValues.RoundRectangle,
        ShapeGeometry.Ellipse => A.ShapeTypeValues.Ellipse,
        ShapeGeometry.Triangle => A.ShapeTypeValues.Triangle,
        ShapeGeometry.RightTriangle => A.ShapeTypeValues.RightTriangle,
        ShapeGeometry.Diamond => A.ShapeTypeValues.Diamond,
        ShapeGeometry.Line => A.ShapeTypeValues.Line,
        ShapeGeometry.RightArrow => A.ShapeTypeValues.RightArrow,
        ShapeGeometry.LeftArrow => A.ShapeTypeValues.LeftArrow,
        ShapeGeometry.UpArrow => A.ShapeTypeValues.UpArrow,
        ShapeGeometry.DownArrow => A.ShapeTypeValues.DownArrow,
        ShapeGeometry.Pentagon => A.ShapeTypeValues.Pentagon,
        ShapeGeometry.Hexagon => A.ShapeTypeValues.Hexagon,
        ShapeGeometry.Star5 => A.ShapeTypeValues.Star5,
        ShapeGeometry.Chevron => A.ShapeTypeValues.Chevron,
        ShapeGeometry.Parallelogram => A.ShapeTypeValues.Parallelogram,
        ShapeGeometry.Trapezoid => A.ShapeTypeValues.Trapezoid,
        ShapeGeometry.Plus => A.ShapeTypeValues.Plus,
        ShapeGeometry.Can => A.ShapeTypeValues.Can,
        ShapeGeometry.Cloud => A.ShapeTypeValues.Cloud,
        _ => A.ShapeTypeValues.Rectangle
    };
}
