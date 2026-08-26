using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;
using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Shapes;
using Shiny.Controls.Office.Spreadsheet;
using D = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Presentation;

/// <summary>
/// Builds the shapes the editor can add.
/// </summary>
/// <remarks>
/// Written as raw OOXML rather than through the model, because a shape that goes into a deck has to
/// satisfy the schema exactly — a <c>p:sp</c> without its non-visual properties, or with children in
/// the wrong order, produces a file PowerPoint calls corrupt and offers to repair rather than open.
/// </remarks>
static class SlideShapeFactory
{
    /// <summary>Name given to a text box the editor creates, so it can be found again right after.</summary>
    public const string TextBoxName = "Shiny TextBox";

    /// <summary>
    /// An empty text box.
    /// </summary>
    /// <remarks>
    /// <c>TextBox = true</c> on the non-visual properties is what makes PowerPoint treat this as a
    /// text box rather than a rectangle that happens to contain text — it is the difference between
    /// the shape having no fill by default and having the theme's.
    /// </remarks>
    public static Shape TextBox(double x, double y, double width, double height, double fontSize = 18)
    {
        var shape = new Shape();

        shape.NonVisualShapeProperties = new NonVisualShapeProperties(
            // Id 0 is reserved; PowerPoint renumbers on save, and any non-zero unique-enough value is
            // accepted on open.
            new NonVisualDrawingProperties { Id = 2U, Name = TextBoxName },
            new NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties());

        shape.ShapeProperties = new ShapeProperties(
            new D.Transform2D
            {
                Offset = new D.Offset { X = OoxmlUnits.PixelsToEmu(x), Y = OoxmlUnits.PixelsToEmu(y) },
                Extents = new D.Extents { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) }
            },
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle },
            new D.NoFill());

        shape.TextBody = new TextBody(
            new D.BodyProperties { Wrap = D.TextWrappingValues.Square },
            new D.ListStyle(),
            new D.Paragraph(
                // No runs: an empty paragraph whose end mark carries the size, which is exactly where
                // the text editor reads formatting from when the first character is typed.
                new D.EndParagraphRunProperties { Language = "en-US", FontSize = (int)Math.Round(fontSize * 100) }));

        // p:txBody's own BodyProperties is a required first child and a:lstStyle must follow it, both
        // before any a:p. Appending them in any other order is invalid.
        return shape;
    }

    /// <summary>
    /// A preset-geometry shape with a solid fill.
    /// </summary>
    /// <remarks>
    /// Deliberately not a text box: <c>TextBox</c> is left off the non-visual properties, which is what
    /// makes PowerPoint treat this as a drawn shape — one that takes the theme's fill by default, can
    /// be recoloured from the shape gallery, and reports itself as a rectangle rather than as text.
    /// It still gets an empty text body so that double-clicking it puts a caret inside, exactly as a
    /// shape drawn in PowerPoint does.
    /// </remarks>
    public static Shape Preset(
        ShapeGeometry geometry,
        double x,
        double y,
        double width,
        double height,
        ArgbColor? fill = null,
        ArgbColor? outline = null,
        double fontSize = 18)
    {
        var shape = new Shape
        {
            NonVisualShapeProperties = new NonVisualShapeProperties(
                new NonVisualDrawingProperties { Id = 2U, Name = $"{geometry} 2" },
                new NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties())
        };

        var properties = new ShapeProperties(
            new D.Transform2D
            {
                Offset = new D.Offset { X = OoxmlUnits.PixelsToEmu(x), Y = OoxmlUnits.PixelsToEmu(y) },
                Extents = new D.Extents { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) }
            },
            new D.PresetGeometry(new D.AdjustValueList()) { Preset = PresetOf(geometry) });

        properties.AppendChild<OpenXmlElement>(fill is { } fillColor
            ? new D.SolidFill(new D.RgbColorModelHex { Val = Hex(fillColor) })
            : new D.NoFill());

        if (outline is { } outlineColor)
        {
            properties.AppendChild(new D.Outline(
                new D.SolidFill(new D.RgbColorModelHex { Val = Hex(outlineColor) }))
            {
                Width = 12700
            });
        }

        shape.ShapeProperties = properties;

        shape.TextBody = new TextBody(
            new D.BodyProperties { Wrap = D.TextWrappingValues.Square, Anchor = D.TextAnchoringTypeValues.Center },
            new D.ListStyle(),
            new D.Paragraph(
                new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center },
                new D.EndParagraphRunProperties { Language = "en-US", FontSize = (int)Math.Round(fontSize * 100) }));

        return shape;
    }

    /// <summary>
    /// A picture referencing an image part already added to the slide.
    /// </summary>
    /// <param name="relationshipId">The id of the <c>ImagePart</c> relationship on the slide part.</param>
    public static Picture Image(string relationshipId, double x, double y, double width, double height, string name)
        => new(
            new NonVisualPictureProperties(
                new NonVisualDrawingProperties { Id = 2U, Name = name },
                new NonVisualPictureDrawingProperties(new D.PictureLocks { NoChangeAspect = true }),
                new ApplicationNonVisualDrawingProperties()),
            new BlipFill(
                new D.Blip { Embed = relationshipId },
                new D.Stretch(new D.FillRectangle())),
            new ShapeProperties(
                new D.Transform2D
                {
                    Offset = new D.Offset { X = OoxmlUnits.PixelsToEmu(x), Y = OoxmlUnits.PixelsToEmu(y) },
                    Extents = new D.Extents { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) }
                },
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }));

    /// <summary>
    /// An empty table, as the graphic frame PowerPoint stores one in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A table is not a shape: it is a <c>p:graphicFrame</c> whose <c>a:graphicData</c> holds an
    /// <c>a:tbl</c>, with its position on the frame and its size split between the frame's extent and
    /// the grid inside. Both have to agree — a frame sized differently from its grid renders at the
    /// frame's size with the columns still laid out for the grid's, so the table overhangs its own
    /// border.
    /// </para>
    /// <para>
    /// The <c>a:tableStyleId</c> is the built-in medium style every deck's table style list defines.
    /// Without one PowerPoint draws the table with no fill and no rules at all, which reads as a
    /// failed insert rather than as a plain table.
    /// </para>
    /// </remarks>
    public static GraphicFrame Table(int rows, int columns, double x, double y, double width, double height)
    {
        rows = Math.Max(1, rows);
        columns = Math.Max(1, columns);

        var table = new D.Table(
            new D.TableProperties(new D.TableStyleId { Text = MediumStyleId })
            {
                FirstRow = true,
                BandRow = true
            });

        var columnWidth = OoxmlUnits.PixelsToEmu(width / columns);
        var rowHeight = OoxmlUnits.PixelsToEmu(height / rows);

        var grid = new D.TableGrid();
        for (var c = 0; c < columns; c++)
            grid.AppendChild(new D.GridColumn { Width = columnWidth });

        table.AppendChild(grid);

        for (var r = 0; r < rows; r++)
        {
            var row = new D.TableRow { Height = rowHeight };

            for (var c = 0; c < columns; c++)
            {
                row.AppendChild(new D.TableCell(
                    new D.TextBody(
                        new D.BodyProperties(),
                        new D.ListStyle(),
                        new D.Paragraph(new D.EndParagraphRunProperties { Language = "en-US" })),
                    new D.TableCellProperties()));
            }

            table.AppendChild(row);
        }

        return new GraphicFrame(
            new NonVisualGraphicFrameProperties(
                new NonVisualDrawingProperties { Id = 2U, Name = "Table 2" },
                new NonVisualGraphicFrameDrawingProperties(new D.GraphicFrameLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties()),
            new Transform
            {
                Offset = new D.Offset { X = OoxmlUnits.PixelsToEmu(x), Y = OoxmlUnits.PixelsToEmu(y) },
                Extents = new D.Extents { Cx = OoxmlUnits.PixelsToEmu(width), Cy = OoxmlUnits.PixelsToEmu(height) }
            },
            new D.Graphic(new D.GraphicData(table) { Uri = TableUri }));
    }

    const string TableUri = "http://schemas.openxmlformats.org/drawingml/2006/table";

    /// <summary>PowerPoint's built-in "Medium Style 2 - Accent 1", which every deck's style list has.</summary>
    const string MediumStyleId = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}";

    static string Hex(ArgbColor color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>The DrawingML preset name for a geometry the editor can insert.</summary>
    static D.ShapeTypeValues PresetOf(ShapeGeometry geometry) => geometry switch
    {
        ShapeGeometry.RoundedRectangle => D.ShapeTypeValues.RoundRectangle,
        ShapeGeometry.Ellipse => D.ShapeTypeValues.Ellipse,
        ShapeGeometry.Triangle => D.ShapeTypeValues.Triangle,
        ShapeGeometry.RightTriangle => D.ShapeTypeValues.RightTriangle,
        ShapeGeometry.Diamond => D.ShapeTypeValues.Diamond,
        ShapeGeometry.Line => D.ShapeTypeValues.Line,
        ShapeGeometry.RightArrow => D.ShapeTypeValues.RightArrow,
        ShapeGeometry.LeftArrow => D.ShapeTypeValues.LeftArrow,
        ShapeGeometry.UpArrow => D.ShapeTypeValues.UpArrow,
        ShapeGeometry.DownArrow => D.ShapeTypeValues.DownArrow,
        ShapeGeometry.Pentagon => D.ShapeTypeValues.Pentagon,
        ShapeGeometry.Hexagon => D.ShapeTypeValues.Hexagon,
        ShapeGeometry.Star5 => D.ShapeTypeValues.Star5,
        ShapeGeometry.Chevron => D.ShapeTypeValues.Chevron,
        ShapeGeometry.Parallelogram => D.ShapeTypeValues.Parallelogram,
        ShapeGeometry.Trapezoid => D.ShapeTypeValues.Trapezoid,
        ShapeGeometry.Plus => D.ShapeTypeValues.Plus,
        ShapeGeometry.Can => D.ShapeTypeValues.Can,
        ShapeGeometry.Cloud => D.ShapeTypeValues.Cloud,
        _ => D.ShapeTypeValues.Rectangle
    };
}
