namespace Shiny.Maui.Controls.ImageEditor;

/// <summary>The shapes the shape tools stamp onto the image.</summary>
public enum ImageEditorShape
{
    Rectangle,

    Ellipse,

    /// <summary>
    /// An ellipse whose drag is constrained to equal width and height. It is stored and rendered
    /// exactly like an <see cref="Ellipse"/> — the constraint applies while it is being drawn, so a
    /// later crop that changes the image's aspect stretches it the same way it stretches a stroke.
    /// </summary>
    Circle
}
