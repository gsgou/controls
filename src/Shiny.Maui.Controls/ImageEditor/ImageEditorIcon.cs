namespace Shiny.Maui.Controls.ImageEditor;

/// <summary>Icons drawn by the default <see cref="ImageEditor"/> toolbar.</summary>
public enum ImageEditorIcon
{
    Move,
    Crop,
    Rotate,
    Draw,
    Line,
    Arrow,
    Text,
    Undo,
    Redo,
    Reset,
    ZoomIn,
    ZoomOut,
    ZoomFit,
    Check,
    Close
}

/// <summary>
/// Vector toolbar icons. The previous toolbar leaned on unicode glyphs, which render at wildly
/// different weights and sizes per platform (and fall back to tofu on some Android fonts) — these
/// are stroked paths on a 24x24 grid so every button matches on every platform.
/// </summary>
internal sealed class ImageEditorIconDrawable : IDrawable
{
    const float Grid = 24f;

    public ImageEditorIcon Icon { get; set; }
    public Color Color { get; set; } = Colors.White;
    public float StrokeWidth { get; set; } = 1.9f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        if (size <= 0)
            return;

        var scale = size / Grid;

        canvas.SaveState();
        canvas.Translate(
            dirtyRect.X + (dirtyRect.Width - size) / 2f,
            dirtyRect.Y + (dirtyRect.Height - size) / 2f);
        canvas.Scale(scale, scale);

        canvas.StrokeColor = Color;
        canvas.FillColor = Color;
        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        switch (Icon)
        {
            case ImageEditorIcon.Move: DrawMove(canvas); break;
            case ImageEditorIcon.Crop: DrawCrop(canvas); break;
            case ImageEditorIcon.Rotate: DrawRotate(canvas); break;
            case ImageEditorIcon.Draw: DrawPencil(canvas); break;
            case ImageEditorIcon.Line: canvas.DrawLine(5, 19, 19, 5); break;
            case ImageEditorIcon.Arrow: DrawArrow(canvas); break;
            case ImageEditorIcon.Text: DrawText(canvas); break;
            case ImageEditorIcon.Undo: DrawUndo(canvas, false); break;
            case ImageEditorIcon.Redo: DrawUndo(canvas, true); break;
            case ImageEditorIcon.Reset: DrawReset(canvas); break;
            case ImageEditorIcon.ZoomIn: DrawMagnifier(canvas, 1); break;
            case ImageEditorIcon.ZoomOut: DrawMagnifier(canvas, -1); break;
            case ImageEditorIcon.ZoomFit: DrawFit(canvas); break;
            case ImageEditorIcon.Check: DrawCheck(canvas); break;
            case ImageEditorIcon.Close: DrawClose(canvas); break;
        }

        canvas.RestoreState();
    }

    static void DrawMove(ICanvas canvas)
    {
        canvas.DrawLine(12, 3, 12, 21);
        canvas.DrawLine(3, 12, 21, 12);
        Chevron(canvas, 12, 3, 8.5f, 6.5f, 15.5f, 6.5f);
        Chevron(canvas, 12, 21, 8.5f, 17.5f, 15.5f, 17.5f);
        Chevron(canvas, 3, 12, 6.5f, 8.5f, 6.5f, 15.5f);
        Chevron(canvas, 21, 12, 17.5f, 8.5f, 17.5f, 15.5f);
    }

    static void Chevron(ICanvas canvas, float tipX, float tipY, float x1, float y1, float x2, float y2)
    {
        canvas.DrawLine(x1, y1, tipX, tipY);
        canvas.DrawLine(tipX, tipY, x2, y2);
    }

    static void DrawCrop(ICanvas canvas)
    {
        canvas.DrawLine(6, 2, 6, 16);
        canvas.DrawLine(6, 16, 20, 16);
        canvas.DrawLine(4, 6, 18, 6);
        canvas.DrawLine(18, 6, 18, 20);
    }

    static void DrawRotate(ICanvas canvas)
    {
        // Three-quarter arc with an arrow head at the open end
        canvas.DrawArc(5, 5, 14, 14, 110, -240, true, false);
        canvas.DrawLine(16.5f, 3.5f, 19.5f, 6.8f);
        canvas.DrawLine(19.5f, 6.8f, 15.8f, 9);
    }

    static void DrawPencil(ICanvas canvas)
    {
        var body = new PathF();
        body.MoveTo(4, 20);
        body.LineTo(4.8f, 16.2f);
        body.LineTo(15.6f, 5.4f);
        body.LineTo(18.6f, 8.4f);
        body.LineTo(7.8f, 19.2f);
        body.Close();
        canvas.DrawPath(body);

        // Nib
        canvas.DrawLine(13.4f, 7.6f, 16.4f, 10.6f);
    }

    static void DrawArrow(ICanvas canvas)
    {
        canvas.DrawLine(4, 20, 19, 5);
        canvas.DrawLine(11.5f, 5, 19, 5);
        canvas.DrawLine(19, 5, 19, 12.5f);
    }

    static void DrawText(ICanvas canvas)
    {
        canvas.DrawLine(5, 5.5f, 19, 5.5f);
        canvas.DrawLine(12, 5.5f, 12, 19);
        canvas.DrawLine(8.5f, 19, 15.5f, 19);
    }

    static void DrawUndo(ICanvas canvas, bool mirrored)
    {
        canvas.SaveState();
        if (mirrored)
        {
            canvas.Translate(24, 0);
            canvas.Scale(-1, 1);
        }

        // Arrow head pointing back into the arc
        canvas.DrawLine(4, 9.5f, 9.5f, 9.5f);
        canvas.DrawLine(4, 9.5f, 4, 4);
        canvas.DrawArc(4, 6, 16, 14, 175, -70, false, false);
        canvas.RestoreState();
    }

    static void DrawReset(ICanvas canvas)
    {
        // Mirrored (anti-clockwise) so it never reads as the rotate icon
        canvas.SaveState();
        canvas.Translate(24, 0);
        canvas.Scale(-1, 1);
        canvas.DrawArc(5, 5, 14, 14, 110, -260, true, false);
        canvas.DrawLine(16.5f, 3.2f, 19.8f, 6.6f);
        canvas.DrawLine(19.8f, 6.6f, 15.8f, 8.9f);
        canvas.RestoreState();
    }

    static void DrawMagnifier(ICanvas canvas, int sign)
    {
        canvas.DrawEllipse(4, 4, 13, 13);
        canvas.DrawLine(16.5f, 16.5f, 20.5f, 20.5f);
        canvas.DrawLine(7.4f, 10.5f, 13.6f, 10.5f);
        if (sign > 0)
            canvas.DrawLine(10.5f, 7.4f, 10.5f, 13.6f);
    }

    static void DrawFit(ICanvas canvas)
    {
        // Four corner brackets
        canvas.DrawLine(3, 8, 3, 3); canvas.DrawLine(3, 3, 8, 3);
        canvas.DrawLine(16, 3, 21, 3); canvas.DrawLine(21, 3, 21, 8);
        canvas.DrawLine(21, 16, 21, 21); canvas.DrawLine(21, 21, 16, 21);
        canvas.DrawLine(8, 21, 3, 21); canvas.DrawLine(3, 21, 3, 16);
    }

    static void DrawCheck(ICanvas canvas)
    {
        canvas.DrawLine(4.5f, 12.5f, 9.8f, 18);
        canvas.DrawLine(9.8f, 18, 19.5f, 6.5f);
    }

    static void DrawClose(ICanvas canvas)
    {
        canvas.DrawLine(6, 6, 18, 18);
        canvas.DrawLine(18, 6, 6, 18);
    }
}
