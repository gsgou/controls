using Shiny.Maui.Controls.ImageEditor.EditActions;

namespace Shiny.Maui.Controls.ImageEditor;

internal sealed class ImageEditorDrawable : IDrawable
{
    public Microsoft.Maui.Graphics.IImage? Image { get; set; }
    public ImageEditorState State { get; set; } = new();

    // Current tool mode
    public ImageEditorToolMode ToolMode { get; set; }

    // In-progress crop rect (normalized 0-1, relative to current image bounds)
    public RectF? ActiveCropRect { get; set; }

    // In-progress draw stroke
    public List<PointF>? ActiveStrokePoints { get; set; }
    public Color ActiveStrokeColor { get; set; } = Colors.White;
    public float ActiveStrokeWidth { get; set; } = 3f;

    // In-progress line / arrow (world coordinates — see the view transform below)
    public PointF? ActiveLineStart { get; set; }
    public PointF? ActiveLineEnd { get; set; }
    public bool ActiveLineIsArrow { get; set; }

    // In-progress shape, dragged corner to corner (world coordinates)
    public PointF? ActiveShapeStart { get; set; }
    public PointF? ActiveShapeEnd { get; set; }
    public ImageEditorShape ActiveShapeKind { get; set; }

    /// <summary>Interior colour for the shape tools. Null draws the outline only.</summary>
    public Color? ActiveFillColor { get; set; }

    // Zoom/pan view transform. This is applied to the canvas rather than to the native
    // GraphicsView so that every tool keeps working while zoomed: the drawable re-renders
    // at the zoomed scale (staying crisp) and the editor converts touch points back into
    // "world" space, which is the un-zoomed coordinate system imageRect lives in.
    public float ViewScale { get; set; } = 1f;
    public float ViewOffsetX { get; set; }
    public float ViewOffsetY { get; set; }

    // Cached effective image bounds after applying all crop/rotate actions (world space)
    RectF imageRect;
    RectF viewport;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(dirtyRect);

        if (Image == null)
            return;

        viewport = dirtyRect;

        canvas.SaveState();
        canvas.ClipRectangle(dirtyRect);

        if (Math.Abs(ViewScale - 1f) > 0.001f || ViewOffsetX != 0 || ViewOffsetY != 0)
        {
            var cx = dirtyRect.Center.X;
            var cy = dirtyRect.Center.Y;
            canvas.Translate(cx + ViewOffsetX, cy + ViewOffsetY);
            canvas.Scale(ViewScale, ViewScale);
            canvas.Translate(-cx, -cy);
        }

        DrawImageWithActions(canvas, dirtyRect);

        // Draw border around the image surface area
        if (imageRect is { Width: > 0, Height: > 0 })
        {
            canvas.StrokeColor = Color.FromRgba(255, 255, 255, 0.3f);
            canvas.StrokeSize = 1f / ViewScale;
            canvas.DrawRectangle(imageRect);
        }

        // Draw in-progress tool overlays
        if (ToolMode == ImageEditorToolMode.Crop && ActiveCropRect.HasValue)
            DrawCropOverlay(canvas, ActiveCropRect.Value);

        if (ToolMode == ImageEditorToolMode.Draw && ActiveStrokePoints is { Count: >= 2 })
            DrawStroke(canvas, ActiveStrokePoints, ActiveStrokeColor, ActiveStrokeWidth);

        if ((ToolMode == ImageEditorToolMode.Line || ToolMode == ImageEditorToolMode.Arrow)
            && ActiveLineStart.HasValue && ActiveLineEnd.HasValue)
        {
            DrawLine(canvas, ActiveLineStart.Value, ActiveLineEnd.Value, ActiveStrokeColor, ActiveStrokeWidth, ActiveLineIsArrow);
        }

        if (ActiveShapeStart.HasValue && ActiveShapeEnd.HasValue && IsShapeMode(ToolMode))
        {
            DrawShape(
                canvas,
                ActiveShapeKind,
                BuildShapeRect(ActiveShapeStart.Value, ActiveShapeEnd.Value, ActiveShapeKind),
                ActiveFillColor,
                ActiveStrokeColor,
                ActiveStrokeWidth);
        }

        canvas.RestoreState();
    }

    void DrawImageWithActions(ICanvas canvas, RectF dirtyRect)
    {
        // Compute cumulative crop region in normalized image coordinates (0-1)
        // and cumulative rotation
        var cropX = 0f;
        var cropY = 0f;
        var cropW = 1f;
        var cropH = 1f;
        float cumulativeRotation = 0f;

        foreach (var action in State.Actions)
        {
            switch (action)
            {
                case RotateAction rotate:
                    cumulativeRotation += rotate.AngleDegrees;
                    break;

                case CropAction crop:
                    // Compose crops: new crop is relative to current visible region
                    cropX += crop.CropRect.X * cropW;
                    cropY += crop.CropRect.Y * cropH;
                    cropW *= crop.CropRect.Width;
                    cropH *= crop.CropRect.Height;
                    break;
            }
        }

        cumulativeRotation %= 360f;
        if (cumulativeRotation < 0) cumulativeRotation += 360f;

        // Determine the effective visible image dimensions (in source pixels)
        var srcW = Image!.Width * cropW;
        var srcH = Image.Height * cropH;

        // For 90/270 rotation, the visible dimensions are swapped
        var needsSwap = Math.Abs(cumulativeRotation % 180 - 90) < 0.1f;
        var displayW = needsSwap ? srcH : srcW;
        var displayH = needsSwap ? srcW : srcH;

        // Fit the effective visible portion into the available area
        imageRect = CalculateFitRect(displayW, displayH, dirtyRect);

        // Now we need to draw only the cropped portion of the image, filling imageRect.
        // Strategy: clip to imageRect, then position/scale the full image so the cropped
        // portion aligns with imageRect.
        canvas.SaveState();
        canvas.ClipRectangle(imageRect);

        // Calculate where the full image would go such that the crop region fills imageRect
        var fullDrawW = imageRect.Width / (needsSwap ? cropH : cropW);
        var fullDrawH = imageRect.Height / (needsSwap ? cropW : cropH);

        float fullDrawX, fullDrawY;

        if (Math.Abs(cumulativeRotation) > 0.1f)
        {
            // With rotation, translate to center of imageRect, rotate, then draw offset
            canvas.Translate(imageRect.Center.X, imageRect.Center.Y);
            canvas.Rotate(cumulativeRotation);
            canvas.Translate(-imageRect.Center.X, -imageRect.Center.Y);

            if (needsSwap)
            {
                // After rotation, source X maps to display Y and vice versa
                var unrotatedW = imageRect.Height / cropW;
                var unrotatedH = imageRect.Width / cropH;
                fullDrawX = imageRect.Center.X - unrotatedW / 2f - cropX / cropW * imageRect.Height;
                fullDrawY = imageRect.Center.Y - unrotatedH / 2f - cropY / cropH * imageRect.Width;
                canvas.DrawImage(Image, fullDrawX, fullDrawY, unrotatedW, unrotatedH);
            }
            else
            {
                fullDrawX = imageRect.X - cropX / cropW * imageRect.Width;
                fullDrawY = imageRect.Y - cropY / cropH * imageRect.Height;
                canvas.DrawImage(Image, fullDrawX, fullDrawY, fullDrawW, fullDrawH);
            }
        }
        else
        {
            // No rotation: position the full image so crop region aligns with imageRect
            fullDrawX = imageRect.X - cropX / cropW * imageRect.Width;
            fullDrawY = imageRect.Y - cropY / cropH * imageRect.Height;
            canvas.DrawImage(Image, fullDrawX, fullDrawY, fullDrawW, fullDrawH);
        }

        canvas.RestoreState();

        // Draw overlay actions (strokes, text) mapped to the visible imageRect
        foreach (var action in State.Actions)
        {
            switch (action)
            {
                case DrawStrokeAction stroke:
                    var scaledPoints = stroke.Points
                        .Select(p => new PointF(
                            imageRect.X + p.X * imageRect.Width,
                            imageRect.Y + p.Y * imageRect.Height))
                        .ToList();
                    DrawStroke(canvas, scaledPoints, stroke.StrokeColor, Rescale(stroke.StrokeWidth, stroke.ReferenceWidth));
                    break;

                case LineAction line:
                    var lineStart = new PointF(
                        imageRect.X + line.Start.X * imageRect.Width,
                        imageRect.Y + line.Start.Y * imageRect.Height);
                    var lineEnd = new PointF(
                        imageRect.X + line.End.X * imageRect.Width,
                        imageRect.Y + line.End.Y * imageRect.Height);
                    DrawLine(canvas, lineStart, lineEnd, line.StrokeColor, Rescale(line.StrokeWidth, line.ReferenceWidth), line.IsArrow);
                    break;

                case ShapeAction shape:
                    var bounds = new RectF(
                        imageRect.X + shape.Bounds.X * imageRect.Width,
                        imageRect.Y + shape.Bounds.Y * imageRect.Height,
                        shape.Bounds.Width * imageRect.Width,
                        shape.Bounds.Height * imageRect.Height);
                    DrawShape(
                        canvas,
                        shape.Shape,
                        bounds,
                        shape.FillColor,
                        shape.StrokeColor,
                        Rescale(shape.StrokeWidth, shape.ReferenceWidth));
                    break;

                case TextAnnotationAction text:
                    var textX = imageRect.X + text.Position.X * imageRect.Width;
                    var textY = imageRect.Y + text.Position.Y * imageRect.Height;
                    var fontSize = Rescale(text.FontSize, text.ReferenceWidth);
                    canvas.FontSize = fontSize;
                    canvas.FontColor = text.TextColor;
                    if (!string.IsNullOrEmpty(text.FontFamily))
                        canvas.Font = new Microsoft.Maui.Graphics.Font(text.FontFamily);
                    else
                        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                    canvas.DrawString(
                        text.Text,
                        textX, textY,
                        imageRect.Width - (textX - imageRect.X),
                        fontSize * 1.5f,
                        HorizontalAlignment.Left,
                        VerticalAlignment.Top);
                    break;
            }
        }
    }

    void DrawCropOverlay(ICanvas canvas, RectF normalizedCrop)
    {
        var cropRect = new RectF(
            imageRect.X + normalizedCrop.X * imageRect.Width,
            imageRect.Y + normalizedCrop.Y * imageRect.Height,
            normalizedCrop.Width * imageRect.Width,
            normalizedCrop.Height * imageRect.Height
        );

        // Dim overlay around crop area
        var dimColor = Color.FromRgba(0, 0, 0, 0.5f);
        canvas.FillColor = dimColor;

        canvas.FillRectangle(imageRect.X, imageRect.Y, imageRect.Width, cropRect.Y - imageRect.Y);
        var bottomY = cropRect.Bottom;
        canvas.FillRectangle(imageRect.X, bottomY, imageRect.Width, imageRect.Bottom - bottomY);
        canvas.FillRectangle(imageRect.X, cropRect.Y, cropRect.X - imageRect.X, cropRect.Height);
        var rightX = cropRect.Right;
        canvas.FillRectangle(rightX, cropRect.Y, imageRect.Right - rightX, cropRect.Height);

        // Crop border. The chrome divides by ViewScale so handles and hairlines keep a
        // constant on-screen size no matter how far the user has zoomed in.
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2f / ViewScale;
        canvas.DrawRectangle(cropRect);

        // Rule-of-thirds
        canvas.StrokeColor = Color.FromRgba(255, 255, 255, 0.3f);
        canvas.StrokeSize = 1f / ViewScale;
        var thirdW = cropRect.Width / 3f;
        var thirdH = cropRect.Height / 3f;
        canvas.DrawLine(cropRect.X + thirdW, cropRect.Y, cropRect.X + thirdW, cropRect.Bottom);
        canvas.DrawLine(cropRect.X + thirdW * 2, cropRect.Y, cropRect.X + thirdW * 2, cropRect.Bottom);
        canvas.DrawLine(cropRect.X, cropRect.Y + thirdH, cropRect.Right, cropRect.Y + thirdH);
        canvas.DrawLine(cropRect.X, cropRect.Y + thirdH * 2, cropRect.Right, cropRect.Y + thirdH * 2);

        // 8 drag handles
        canvas.FillColor = Colors.White;
        var halfHandle = 5f / ViewScale;

        DrawHandle(canvas, cropRect.X, cropRect.Y, halfHandle);
        DrawHandle(canvas, cropRect.Right, cropRect.Y, halfHandle);
        DrawHandle(canvas, cropRect.X, cropRect.Bottom, halfHandle);
        DrawHandle(canvas, cropRect.Right, cropRect.Bottom, halfHandle);
        DrawHandle(canvas, cropRect.Center.X, cropRect.Y, halfHandle);
        DrawHandle(canvas, cropRect.Center.X, cropRect.Bottom, halfHandle);
        DrawHandle(canvas, cropRect.X, cropRect.Center.Y, halfHandle);
        DrawHandle(canvas, cropRect.Right, cropRect.Center.Y, halfHandle);
    }

    static void DrawHandle(ICanvas canvas, float x, float y, float halfSize)
    {
        canvas.FillRoundedRectangle(x - halfSize, y - halfSize, halfSize * 2, halfSize * 2, halfSize * 0.4f);
    }

    static void DrawLine(ICanvas canvas, PointF start, PointF end, Color color, float width, bool arrow)
    {
        canvas.StrokeColor = color;
        canvas.StrokeSize = width;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawLine(start, end);

        if (!arrow)
            return;

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f)
            return;

        // Arrow head: lines back along the direction at +/-30°, length scaled with stroke width
        var headLen = MathF.Max(width * 4f, 12f);
        var ux = dx / len;
        var uy = dy / len;
        const float angle = 0.5236f; // ~30°
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);

        var leftX = end.X - headLen * (ux * cos + uy * sin);
        var leftY = end.Y - headLen * (uy * cos - ux * sin);
        var rightX = end.X - headLen * (ux * cos - uy * sin);
        var rightY = end.Y - headLen * (uy * cos + ux * sin);

        canvas.FillColor = color;
        var path = new PathF();
        path.MoveTo(end);
        path.LineTo(leftX, leftY);
        path.LineTo(rightX, rightY);
        path.Close();
        canvas.FillPath(path);
    }

    public static bool IsShapeMode(ImageEditorToolMode mode)
        => mode is ImageEditorToolMode.Rectangle or ImageEditorToolMode.Ellipse or ImageEditorToolMode.Circle;

    /// <summary>
    /// Turns the two corners of a shape drag into its bounds. A circle takes the smaller of the two
    /// extents rather than the larger, so it can never escape the bounds the drag was clamped to.
    /// </summary>
    public static RectF BuildShapeRect(PointF start, PointF end, ImageEditorShape shape)
    {
        var x = MathF.Min(start.X, end.X);
        var y = MathF.Min(start.Y, end.Y);
        var w = MathF.Abs(end.X - start.X);
        var h = MathF.Abs(end.Y - start.Y);

        if (shape == ImageEditorShape.Circle)
        {
            var side = MathF.Min(w, h);

            // Grow from whichever corner the drag started at, so the shape tracks the finger
            if (end.X < start.X) x = start.X - side;
            if (end.Y < start.Y) y = start.Y - side;
            w = h = side;
        }

        return new RectF(x, y, w, h);
    }

    static void DrawShape(ICanvas canvas, ImageEditorShape shape, RectF rect, Color? fill, Color? stroke, float strokeWidth)
    {
        if (rect is { Width: <= 0 } or { Height: <= 0 })
            return;

        if (fill != null)
        {
            canvas.FillColor = fill;
            if (shape == ImageEditorShape.Rectangle)
                canvas.FillRectangle(rect);
            else
                canvas.FillEllipse(rect);
        }

        if (stroke == null || strokeWidth <= 0)
            return;

        canvas.StrokeColor = stroke;
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineJoin = LineJoin.Round;

        if (shape == ImageEditorShape.Rectangle)
            canvas.DrawRectangle(rect);
        else
            canvas.DrawEllipse(rect);
    }

    static void DrawStroke(ICanvas canvas, IList<PointF> points, Color color, float width)
    {
        if (points.Count < 2)
            return;

        canvas.StrokeColor = color;
        canvas.StrokeSize = width;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        var path = new PathF();
        path.MoveTo(points[0]);
        for (var i = 1; i < points.Count; i++)
            path.LineTo(points[i]);

        canvas.DrawPath(path);
    }

    static RectF CalculateFitRect(float imageWidth, float imageHeight, RectF availableRect)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
            return RectF.Zero;

        var scaleX = availableRect.Width / imageWidth;
        var scaleY = availableRect.Height / imageHeight;
        var scale = Math.Min(scaleX, scaleY);

        var fitWidth = imageWidth * scale;
        var fitHeight = imageHeight * scale;

        return new RectF(
            availableRect.X + (availableRect.Width - fitWidth) / 2f,
            availableRect.Y + (availableRect.Height - fitHeight) / 2f,
            fitWidth,
            fitHeight
        );
    }

    public RectF GetImageRect() => imageRect;

    public RectF GetViewport() => viewport;

    /// <summary>
    /// Scales a stroke width / font size that was captured against <paramref name="referenceWidth"/>
    /// to the image rect currently being drawn. Zero reference means legacy/unscaled.
    /// </summary>
    float Rescale(float value, float referenceWidth)
        => referenceWidth > 0.01f ? value * (imageRect.Width / referenceWidth) : value;

    /// <summary>Converts a touch point on the view into the un-zoomed coordinate space of the image rect.</summary>
    public PointF ScreenToWorld(PointF screen)
    {
        if (Math.Abs(ViewScale - 1f) < 0.001f && ViewOffsetX == 0 && ViewOffsetY == 0)
            return screen;

        var cx = viewport.Center.X;
        var cy = viewport.Center.Y;
        return new PointF(
            (screen.X - cx - ViewOffsetX) / ViewScale + cx,
            (screen.Y - cy - ViewOffsetY) / ViewScale + cy);
    }

    /// <summary>Converts an un-zoomed point back into view coordinates (used to place the text entry).</summary>
    public PointF WorldToScreen(PointF world)
    {
        if (Math.Abs(ViewScale - 1f) < 0.001f && ViewOffsetX == 0 && ViewOffsetY == 0)
            return world;

        var cx = viewport.Center.X;
        var cy = viewport.Center.Y;
        return new PointF(
            (world.X - cx) * ViewScale + cx + ViewOffsetX,
            (world.Y - cy) * ViewScale + cy + ViewOffsetY);
    }
}
