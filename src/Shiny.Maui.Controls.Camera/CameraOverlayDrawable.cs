namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// An <see cref="IDrawable"/> that paints <see cref="Detection"/> bounding boxes (and labels) over the
/// preview. Place a <see cref="GraphicsView"/> on top of a <see cref="CameraView"/> in the same layout
/// cell, point it at one of these, and on <see cref="CameraView.DetectionsChanged"/> update
/// <see cref="Detections"/> / <see cref="ImageAspect"/> and call <c>Invalidate()</c>.
/// </summary>
public class CameraOverlayDrawable : IDrawable
{
    /// <summary>Detections to draw (normalized, upright image space).</summary>
    public IReadOnlyList<Detection> Detections { get; set; } = [];

    /// <summary>Upright image aspect ratio (width / height) of the analyzed frame.</summary>
    public float ImageAspect { get; set; } = 1f;

    /// <summary>How the preview fills its view (must match <see cref="CameraView.ScaleMode"/>).</summary>
    public PreviewScaleMode ScaleMode { get; set; } = PreviewScaleMode.AspectFill;

    /// <summary>Bounding-box stroke color.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#22D3EE");

    /// <summary>Label text color.</summary>
    public Color LabelColor { get; set; } = Colors.White;

    /// <summary>Bounding-box stroke width.</summary>
    public float StrokeWidth { get; set; } = 3f;

    /// <summary>Optional fill drawn inside each box (e.g. a translucent highlight).</summary>
    public Color? FillColor { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var detections = this.Detections;
        if (detections.Count == 0)
            return;

        foreach (var d in detections)
        {
            var r = CoordinateTransform.MapToView(d.BoundingBox, dirtyRect.Width, dirtyRect.Height, this.ImageAspect, this.ScaleMode);

            if (this.FillColor is { } fill)
            {
                canvas.FillColor = fill;
                canvas.FillRectangle(r);
            }

            canvas.StrokeColor = this.BoxColor;
            canvas.StrokeSize = this.StrokeWidth;
            canvas.DrawRectangle(r);

            var label = d.Value ?? d.Label;
            if (!string.IsNullOrEmpty(label))
            {
                canvas.FontColor = this.LabelColor;
                canvas.FontSize = 14;
                canvas.DrawString(label, r.X, Math.Max(0, r.Y - 18), r.Width <= 0 ? 200 : r.Width + 80, 18,
                    HorizontalAlignment.Left, VerticalAlignment.Top);
            }
        }
    }
}
