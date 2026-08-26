namespace Shiny.Maui.Controls.ImageEditor.EditActions;

public sealed class ShapeAction : IEditAction
{
    public string Description => Shape.ToString();

    public required ImageEditorShape Shape { get; init; }

    /// <summary>Bounds normalised against the on-screen image rect, the way strokes and text are.</summary>
    public required RectF Bounds { get; init; }

    /// <summary>Interior colour. Null leaves the shape unfilled — an outline over the photo.</summary>
    public Color? FillColor { get; init; }

    /// <summary>Border colour. Null, or a zero <see cref="StrokeWidth"/>, leaves the shape unstroked.</summary>
    public Color? StrokeColor { get; init; }

    public float StrokeWidth { get; init; }

    /// <inheritdoc cref="DrawStrokeAction.ReferenceWidth"/>
    public float ReferenceWidth { get; init; }
}
