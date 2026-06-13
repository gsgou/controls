namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// An <see cref="IDrawable"/> that paints analyzer-supplied <see cref="OverlayBox"/>es (and their captions)
/// over the preview. Each box may carry its own stroke/text/fill color and stroke width; whatever it leaves
/// unset falls back to this drawable's defaults. Usually you don't use this directly — drop a
/// <see cref="CameraOverlayView"/> over a <see cref="CameraView"/>, which owns one of these and feeds it.
/// </summary>
public class CameraOverlayDrawable : IDrawable
{
    /// <summary>Boxes to draw (normalized, upright image space).</summary>
    public IReadOnlyList<OverlayBox> Boxes { get; set; } = [];

    /// <summary>Upright image aspect ratio (width / height) of the analyzed frame.</summary>
    public float ImageAspect { get; set; } = 1f;

    /// <summary>How the preview fills its view (must match <see cref="CameraView.ScaleMode"/>).</summary>
    public PreviewScaleMode ScaleMode { get; set; } = PreviewScaleMode.AspectFill;

    /// <summary>Default box stroke color used when an <see cref="OverlayBox.StrokeColor"/> is null.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#22D3EE");

    /// <summary>Default caption color used when an <see cref="OverlayBox.TextColor"/> is null.</summary>
    public Color LabelColor { get; set; } = Colors.White;

    /// <summary>Default box stroke width used when an <see cref="OverlayBox.StrokeWidth"/> is 0.</summary>
    public float StrokeWidth { get; set; } = 3f;

    /// <summary>Default fill used when an <see cref="OverlayBox.FillColor"/> is null (null = no fill).</summary>
    public Color? FillColor { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var boxes = this.Boxes;
        if (boxes.Count == 0)
            return;

        foreach (var b in boxes)
        {
            var r = CoordinateTransform.MapToView(b.Rect, dirtyRect.Width, dirtyRect.Height, this.ImageAspect, this.ScaleMode);

            if ((b.FillColor ?? this.FillColor) is { } fill)
            {
                canvas.FillColor = fill;
                canvas.FillRectangle(r);
            }

            canvas.StrokeColor = b.StrokeColor ?? this.BoxColor;
            canvas.StrokeSize = b.StrokeWidth > 0 ? b.StrokeWidth : this.StrokeWidth;
            canvas.DrawRectangle(r);

            if (!string.IsNullOrEmpty(b.Text))
            {
                canvas.FontColor = b.TextColor ?? this.LabelColor;
                canvas.FontSize = 14;
                canvas.DrawString(b.Text, r.X, Math.Max(0, r.Y - 18), r.Width <= 0 ? 200 : r.Width + 80, 18,
                    HorizontalAlignment.Left, VerticalAlignment.Top);
            }
        }
    }
}
