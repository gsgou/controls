namespace Shiny.Maui.Controls.SignaturePad;

internal sealed class SignaturePadDrawable : IDrawable
{
    readonly List<List<PointF>> committedStrokes = new();
    List<PointF>? activeStroke;

    // The on-screen canvas size (captured on each live Draw) so the export can scale the strokes — which
    // are recorded in canvas coordinates — into the export bitmap instead of clipping them.
    float canvasWidth;
    float canvasHeight;

    public Color BackgroundColor { get; set; } = Colors.White;
    public Color StrokeColor { get; set; } = Colors.Black;
    public float StrokeWidth { get; set; } = 3f;
    public bool HasSignature => committedStrokes.Count > 0;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Remember the live canvas size so ExportToPng can map canvas coordinates → export bitmap.
        if (dirtyRect.Width > 0 && dirtyRect.Height > 0)
        {
            canvasWidth = dirtyRect.Width;
            canvasHeight = dirtyRect.Height;
        }

        canvas.FillColor = BackgroundColor;
        canvas.FillRectangle(dirtyRect);

        DrawStrokes(canvas, committedStrokes, 1f, 0f, 0f, StrokeWidth);

        if (activeStroke is { Count: >= 2 })
            DrawSingleStroke(canvas, activeStroke, 1f, 0f, 0f, StrokeWidth);
    }

    public void BeginStroke(PointF point)
    {
        activeStroke = new List<PointF> { point };
    }

    public void AddPoint(PointF point)
    {
        activeStroke?.Add(point);
    }

    public void EndStroke()
    {
        if (activeStroke is { Count: >= 2 })
            committedStrokes.Add(activeStroke);

        activeStroke = null;
    }

    public void Clear()
    {
        committedStrokes.Clear();
        activeStroke = null;
    }

    public Stream ExportToPng(int width, int height)
    {
#if IOS || MACCATALYST || ANDROID
        using var context = new Microsoft.Maui.Graphics.Platform.PlatformBitmapExportContext(width, height, 1f);
        var canvas = context.Canvas;

        canvas.FillColor = BackgroundColor;
        canvas.FillRectangle(0, 0, width, height);

        // Strokes are stored in the on-screen canvas coordinate space, which is usually larger (and a
        // different aspect ratio) than the export bitmap. Scale them uniformly to fit and center, so the
        // whole signature is captured without clipping the bottom and without distorting the aspect ratio.
        var srcWidth = canvasWidth > 0 ? canvasWidth : width;
        var srcHeight = canvasHeight > 0 ? canvasHeight : height;
        var scale = Math.Min(width / srcWidth, height / srcHeight);
        var offsetX = (width - srcWidth * scale) / 2f;
        var offsetY = (height - srcHeight * scale) / 2f;
        var strokeWidth = Math.Max(StrokeWidth * scale, 1f);

        DrawStrokes(canvas, committedStrokes, scale, offsetX, offsetY, strokeWidth);

        return context.Image.AsStream(Microsoft.Maui.Graphics.ImageFormat.Png);
#else
        return Stream.Null;
#endif
    }

    void DrawStrokes(ICanvas canvas, List<List<PointF>> strokes, float scale, float offsetX, float offsetY, float strokeWidth)
    {
        foreach (var stroke in strokes)
        {
            if (stroke.Count >= 2)
                DrawSingleStroke(canvas, stroke, scale, offsetX, offsetY, strokeWidth);
        }
    }

    void DrawSingleStroke(ICanvas canvas, IList<PointF> points, float scale, float offsetX, float offsetY, float strokeWidth)
    {
        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        var path = new PathF();
        path.MoveTo(points[0].X * scale + offsetX, points[0].Y * scale + offsetY);
        for (var i = 1; i < points.Count; i++)
            path.LineTo(points[i].X * scale + offsetX, points[i].Y * scale + offsetY);

        canvas.DrawPath(path);
    }
}
