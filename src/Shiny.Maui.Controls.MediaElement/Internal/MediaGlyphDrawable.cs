namespace Shiny.Maui.Controls.Media.Internal;

/// <summary>The icons the transport bar draws.</summary>
enum MediaGlyph
{
    Play,
    Pause,
    Stop,
    VolumeOn,
    VolumeOff,
    FullScreenEnter,
    FullScreenExit,
    PictureInPicture
}


/// <summary>
/// Draws the transport glyphs as vectors rather than text.
/// </summary>
/// <remarks>
/// Unicode media symbols (▶ ⏸ 🔊) were the obvious shortcut and the wrong one: several of them carry
/// emoji presentation, so Android and Windows render them full-colour at a different optical size than
/// iOS, and none of them take the control's tint. Paths give one crisp, tintable icon set everywhere and
/// cost no font asset.
/// </remarks>
class MediaGlyphDrawable : IDrawable
{
    public MediaGlyph Glyph { get; set; }
    public Color Color { get; set; } = Colors.White;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Everything below is authored in a 24×24 box, then scaled into whatever the view got.
        var scale = Math.Min(dirtyRect.Width, dirtyRect.Height) / 24f;
        if (scale <= 0)
            return;

        canvas.SaveState();
        canvas.Translate(
            dirtyRect.X + (dirtyRect.Width - 24f * scale) / 2f,
            dirtyRect.Y + (dirtyRect.Height - 24f * scale) / 2f);
        canvas.Scale(scale, scale);

        canvas.FillColor = this.Color;
        canvas.StrokeColor = this.Color;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        switch (this.Glyph)
        {
            case MediaGlyph.Play:
                DrawPlay(canvas);
                break;
            case MediaGlyph.Pause:
                DrawPause(canvas);
                break;
            case MediaGlyph.Stop:
                canvas.FillRoundedRectangle(5, 5, 14, 14, 1.5f);
                break;
            case MediaGlyph.VolumeOn:
                DrawSpeaker(canvas);
                DrawWaves(canvas);
                break;
            case MediaGlyph.VolumeOff:
                DrawSpeaker(canvas);
                DrawMuteCross(canvas);
                break;
            case MediaGlyph.FullScreenEnter:
                DrawCorners(canvas, expand: true);
                break;
            case MediaGlyph.FullScreenExit:
                DrawCorners(canvas, expand: false);
                break;
            case MediaGlyph.PictureInPicture:
                DrawPictureInPicture(canvas);
                break;
        }

        canvas.RestoreState();
    }

    static void DrawPlay(ICanvas canvas)
    {
        var path = new PathF();
        path.MoveTo(7, 4.5f);
        path.LineTo(19, 12);
        path.LineTo(7, 19.5f);
        path.Close();
        canvas.FillPath(path);
    }

    static void DrawPause(ICanvas canvas)
    {
        canvas.FillRoundedRectangle(6, 4.5f, 4, 15, 1.2f);
        canvas.FillRoundedRectangle(14, 4.5f, 4, 15, 1.2f);
    }

    static void DrawSpeaker(ICanvas canvas)
    {
        var path = new PathF();
        path.MoveTo(3, 9);
        path.LineTo(6.5f, 9);
        path.LineTo(11, 5);
        path.LineTo(11, 19);
        path.LineTo(6.5f, 15);
        path.LineTo(3, 15);
        path.Close();
        canvas.FillPath(path);
    }

    static void DrawWaves(ICanvas canvas)
    {
        canvas.StrokeSize = 1.6f;
        canvas.DrawArc(11.5f, 8f, 5f, 8f, -60, 60, false, false);
        canvas.DrawArc(12.5f, 5f, 8f, 14f, -55, 55, false, false);
    }

    static void DrawMuteCross(ICanvas canvas)
    {
        canvas.StrokeSize = 1.8f;
        canvas.DrawLine(14.5f, 9.5f, 20.5f, 15f);
        canvas.DrawLine(20.5f, 9.5f, 14.5f, 15f);
    }

    static void DrawCorners(ICanvas canvas, bool expand)
    {
        canvas.StrokeSize = 1.9f;

        if (expand)
        {
            // arrows pointing out of the corners
            canvas.DrawLine(4, 9, 4, 4);
            canvas.DrawLine(4, 4, 9, 4);
            canvas.DrawLine(15, 4, 20, 4);
            canvas.DrawLine(20, 4, 20, 9);
            canvas.DrawLine(20, 15, 20, 20);
            canvas.DrawLine(20, 20, 15, 20);
            canvas.DrawLine(9, 20, 4, 20);
            canvas.DrawLine(4, 20, 4, 15);
        }
        else
        {
            // arrows folding back in
            canvas.DrawLine(9, 4, 9, 9);
            canvas.DrawLine(9, 9, 4, 9);
            canvas.DrawLine(15, 9, 20, 9);
            canvas.DrawLine(15, 9, 15, 4);
            canvas.DrawLine(15, 20, 15, 15);
            canvas.DrawLine(15, 15, 20, 15);
            canvas.DrawLine(9, 15, 4, 15);
            canvas.DrawLine(9, 15, 9, 20);
        }
    }

    static void DrawPictureInPicture(ICanvas canvas)
    {
        canvas.StrokeSize = 1.8f;
        canvas.DrawRoundedRectangle(3, 5, 18, 14, 2f);
        canvas.FillRoundedRectangle(11.5f, 11, 8, 6.5f, 1.2f);
    }
}
