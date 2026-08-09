namespace Shiny.Maui.Controls.ImageEditor.EditActions;

public sealed class DrawStrokeAction : IEditAction
{
    public string Description => "Draw";
    public required PointF[] Points { get; init; }
    public required Color StrokeColor { get; init; }
    public required float StrokeWidth { get; init; }

    /// <summary>
    /// Width of the on-screen image rect when the stroke was captured. Rendering scales
    /// <see cref="StrokeWidth"/> by <c>imageRect.Width / ReferenceWidth</c> so a stroke drawn on a
    /// small preview (or while zoomed in) keeps its proportions when exported at full resolution.
    /// Zero means "no scaling" — the stroke width is used verbatim.
    /// </summary>
    public float ReferenceWidth { get; init; }
}
