using DocumentFormat.OpenXml.Presentation;
using Shiny.Controls.Office.Document;
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
}
