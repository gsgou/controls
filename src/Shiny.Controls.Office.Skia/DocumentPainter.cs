using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;
using SkiaSharp;

namespace Shiny.Controls.Office.Skia;

public sealed record DocumentTheme
{
    public static readonly DocumentTheme Light = new();

    public static readonly DocumentTheme Dark = new()
    {
        PageBackground = new ArgbColor(255, 0x25, 0x25, 0x25),
        SurroundBackground = new ArgbColor(255, 0x18, 0x18, 0x18),
        Text = new ArgbColor(255, 0xE4, 0xE4, 0xE4),
        Rule = new ArgbColor(255, 0x55, 0x55, 0x55),
        TableBorder = new ArgbColor(255, 0x55, 0x55, 0x55),
        Link = new ArgbColor(255, 0x6C, 0xB6, 0xFF),
        SelectionFill = new ArgbColor(80, 0x4C, 0x9A, 0xFF),
        Caret = new ArgbColor(255, 0xEE, 0xEE, 0xEE)
    };

    public ArgbColor PageBackground { get; init; } = new(255, 255, 255, 255);
    public ArgbColor SurroundBackground { get; init; } = new(255, 0xF0, 0xF0, 0xF2);
    public ArgbColor Text { get; init; } = new(255, 0x1A, 0x1A, 0x1A);
    public ArgbColor Rule { get; init; } = new(255, 0xC8, 0xC8, 0xC8);
    public ArgbColor TableBorder { get; init; } = new(255, 0xBB, 0xBB, 0xBB);
    public ArgbColor Link { get; init; } = new(255, 0x06, 0x5F, 0xD8);

    /// <summary>Selection wash. Alpha matters: the text has to stay readable underneath it.</summary>
    public ArgbColor SelectionFill { get; init; } = new(70, 0x21, 0x7A, 0xD8);

    public ArgbColor Caret { get; init; } = new(255, 0x11, 0x11, 0x11);

    /// <summary>The spelling squiggle. Red by convention, and the one place a document may not override.</summary>
    public ArgbColor SpellingUnderline { get; init; } = new(255, 0xD1, 0x34, 0x38);

    /// <summary>
    /// When true the document's own colours are discarded in favour of <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    /// The blunt instrument. It guarantees legibility but flattens every authored colour to one, so a
    /// document's blue headings and red warnings all come out the same grey. Prefer
    /// <see cref="AdaptDocumentColors"/>.
    /// </remarks>
    public bool OverrideDocumentColors { get; init; }

    /// <summary>
    /// Lightens or darkens authored colours that do not contrast with the page, keeping their hue.
    /// </summary>
    /// <remarks>
    /// A document authored for white paper is black text. Shown on a dark page it is invisible, and
    /// the alternative — replacing every colour with the theme's — throws away the distinction between
    /// a heading, a hyperlink and a red warning. Adapting the lightness while preserving hue keeps both
    /// the legibility and the meaning.
    /// </remarks>
    public bool AdaptDocumentColors { get; init; } = true;
}

public sealed record DocumentPaintRequest
{
    public required IReadOnlyList<LaidOutBlock> Blocks { get; init; }
    public required DocumentViewport Viewport { get; init; }
    public DocumentTheme Theme { get; init; } = DocumentTheme.Light;
    public float Scale { get; init; } = 1f;

    /// <summary>Horizontal offset of the page within the viewport, for centring a fixed-width page.</summary>
    public double PageX { get; init; }

    public double PageWidth { get; init; }

    /// <summary>Selection rectangles in document coordinates, painted under the text.</summary>
    public IReadOnlyList<GridRectLike> Selection { get; init; } = [];

    /// <summary>The caret, or null when the view is not focused or the caret is blinking off.</summary>
    public GridRectLike? Caret { get; init; }

    /// <summary>Spans to underline as misspelled, in document coordinates.</summary>
    public IReadOnlyList<GridRectLike> Spelling { get; init; } = [];
}

/// <summary>
/// Paints a laid-out Word document.
/// </summary>
public sealed class DocumentPainter(SkiaTextMeasurer measurer) : IDisposable
{
    readonly SKPaint fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    readonly SKPaint stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
    readonly Dictionary<int, SKImage> images = new();

    public void Paint(SKCanvas canvas, DocumentPaintRequest request)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(request);

        var theme = request.Theme;
        canvas.Save();
        canvas.Scale(request.Scale);
        canvas.Clear(ToSk(theme.SurroundBackground));

        // The page itself is a lighter panel inside the surround, so a narrow measure still reads as
        // a document rather than as text floating on a background.
        this.fill.Color = ToSk(theme.PageBackground);
        canvas.DrawRect(
            new SKRect(
                (float)request.PageX,
                0,
                (float)(request.PageX + request.PageWidth),
                (float)request.Viewport.Height),
            this.fill);

        canvas.Translate((float)request.PageX, (float)-request.Viewport.ScrollY);

        // Selection goes under the text: painting it over would wash out the glyphs it is meant to
        // highlight.
        if (request.Selection.Count > 0)
        {
            this.fill.Color = ToSk(theme.SelectionFill);
            foreach (var rect in request.Selection)
                canvas.DrawRect(new SKRect((float)rect.X, (float)rect.Y, (float)rect.Right, (float)rect.Bottom), this.fill);
        }

        foreach (var block in request.Viewport.Visible(request.Blocks))
            this.PaintBlock(canvas, block, theme);

        // Squiggles go over the text: they mark it rather than sit behind it, and a wash underneath
        // would be invisible against a descender.
        foreach (var rect in request.Spelling)
            this.PaintSquiggle(canvas, rect, theme);

        if (request.Caret is { } caret)
        {
            this.fill.Color = ToSk(theme.Caret);
            canvas.DrawRect(
                new SKRect((float)caret.X, (float)caret.Y, (float)(caret.X + Math.Max(1, caret.Width)), (float)caret.Bottom),
                this.fill);
        }

        canvas.Restore();
    }

    /// <summary>
    /// Draws the wavy underline used for a misspelling.
    /// </summary>
    /// <remarks>
    /// Drawn as an actual wave rather than a straight red line, because a straight one is easily
    /// mistaken for the document's own underline formatting — which the editor also draws.
    /// </remarks>
    void PaintSquiggle(SKCanvas canvas, GridRectLike rect, DocumentTheme theme)
    {
        const float Period = 4f;
        const float Amplitude = 1.6f;

        using var path = new SKPath();
        var y = (float)rect.Y;
        var x = (float)rect.X;
        var right = (float)rect.Right;

        path.MoveTo(x, y);
        var up = true;
        while (x < right)
        {
            var next = Math.Min(x + Period, right);
            path.QuadTo((x + next) / 2, y + (up ? -Amplitude : Amplitude), next, y);
            x = next;
            up = !up;
        }

        this.stroke.Color = ToSk(theme.SpellingUnderline);
        this.stroke.StrokeWidth = 1.1f;
        canvas.DrawPath(path, this.stroke);
        this.stroke.StrokeWidth = 1;
    }

    void PaintBlock(SKCanvas canvas, LaidOutBlock block, DocumentTheme theme)
    {
        switch (block)
        {
            case LaidOutParagraph paragraph:
                this.PaintParagraph(canvas, paragraph, theme);
                break;

            case LaidOutTable table:
                this.PaintTable(canvas, table, theme);
                break;

            case LaidOutRule rule:
                this.stroke.Color = ToSk(theme.Rule);
                this.stroke.StrokeWidth = 1;
                canvas.DrawLine((float)rule.X, (float)rule.Y, (float)(rule.X + rule.Width), (float)rule.Y, this.stroke);
                break;
        }
    }

    void PaintParagraph(SKCanvas canvas, LaidOutParagraph paragraph, DocumentTheme theme)
    {
        if (paragraph.Format.Shading is { } shading)
        {
            this.fill.Color = ToSk(shading);
            canvas.DrawRect(
                new SKRect((float)paragraph.X, (float)paragraph.Y, (float)(paragraph.X + paragraph.Width), (float)(paragraph.Y + paragraph.Height)),
                this.fill);
        }

        foreach (var line in paragraph.Lines)
        {
            var baseline = paragraph.Y + line.Y + line.Ascent;

            foreach (var run in line.Runs)
                this.PaintRun(canvas, run, paragraph.X, baseline, theme);
        }

        // The list label sits on the first line's baseline, outside the text body.
        if (paragraph.LabelText is { } label && paragraph.Lines.Count > 0)
        {
            var first = paragraph.Lines[0];
            var baseline = paragraph.Y + first.Y + first.Ascent;
            var style = paragraph.LabelStyle;

            this.fill.Color = ToSk(theme.OverrideDocumentColors ? theme.Text : Legible(style.Color, theme));
            canvas.DrawText(label, (float)paragraph.LabelX, (float)baseline, SKTextAlign.Left, measurer.GetFont(style), this.fill);
        }
    }

    void PaintRun(SKCanvas canvas, LaidOutRun run, double originX, double baseline, DocumentTheme theme)
    {
        var x = originX + run.X;

        if (run.Image is { } image)
        {
            this.DrawImage(canvas, image.Data, new SKRect(
                (float)x,
                (float)(baseline - image.Height),
                (float)(x + image.Width),
                (float)baseline));

            return;
        }

        if (run.Text.Length == 0)
            return;

        var style = run.Style;
        var font = measurer.GetFont(style);

        // Superscript and subscript raise or lower the baseline by a fraction of the font size.
        var y = baseline - style.BaselineShift * style.FontSize;

        if (style.Highlight is { } highlight)
        {
            this.fill.Color = ToSk(highlight);
            var metrics = font.Metrics;
            canvas.DrawRect(
                new SKRect((float)x, (float)(y + metrics.Ascent), (float)(x + run.Width), (float)(y + metrics.Descent)),
                this.fill);
        }

        var color = style.Link is not null
            ? theme.Link
            : theme.OverrideDocumentColors ? theme.Text : Legible(style.Color, theme);

        this.fill.Color = ToSk(color);
        canvas.DrawText(run.Text, (float)x, (float)y, SKTextAlign.Left, font, this.fill);

        var underline = style.Underline != UnderlineStyle.None || style.Link is not null;
        if (underline || style.Strike)
        {
            this.stroke.Color = ToSk(color);
            this.stroke.StrokeWidth = Math.Max(1, (float)(style.FontSize / 14));

            if (underline)
            {
                var offset = (float)(y + style.FontSize * 0.12);
                canvas.DrawLine((float)x, offset, (float)(x + run.Width), offset, this.stroke);

                if (style.Underline == UnderlineStyle.Double)
                {
                    var second = offset + this.stroke.StrokeWidth * 2;
                    canvas.DrawLine((float)x, second, (float)(x + run.Width), second, this.stroke);
                }
            }

            if (style.Strike)
            {
                var middle = (float)(y - style.FontSize * 0.28);
                canvas.DrawLine((float)x, middle, (float)(x + run.Width), middle, this.stroke);
            }
        }
    }

    void PaintTable(SKCanvas canvas, LaidOutTable table, DocumentTheme theme)
    {
        foreach (var cell in table.Cells)
        {
            var rect = new SKRect((float)cell.X, (float)cell.Y, (float)(cell.X + cell.Width), (float)(cell.Y + cell.Height));

            if (cell.Shading is { } shading)
            {
                this.fill.Color = ToSk(shading);
                canvas.DrawRect(rect, this.fill);
            }

            foreach (var block in cell.Blocks)
                this.PaintBlock(canvas, block, theme);

            if (!table.HasBorders)
                continue;

            this.stroke.Color = ToSk(theme.TableBorder);
            this.stroke.StrokeWidth = 1;

            // Half-pixel offset keeps a hairline on one device pixel rather than blurring across two.
            canvas.DrawRect(
                new SKRect(rect.Left + 0.5f, rect.Top + 0.5f, rect.Right - 0.5f, rect.Bottom - 0.5f),
                this.stroke);
        }
    }

    void DrawImage(SKCanvas canvas, byte[] data, SKRect destination)
    {
        // Decoded images are cached by content hash: the same logo repeated on every page would
        // otherwise be decoded once per paint.
        var key = System.HashCode.Combine(data.Length, data.Length > 0 ? data[0] : 0, data.Length > 64 ? data[64] : 0);

        if (!this.images.TryGetValue(key, out var image))
        {
            try
            {
                image = SKImage.FromEncodedData(data);
            }
            catch (Exception)
            {
                image = null;
            }

            this.images[key] = image!;
        }

        if (image is null)
        {
            // An undecodable image still occupies its space; an outline says something belongs here.
            this.stroke.Color = new SKColor(0x99, 0x99, 0x99);
            this.stroke.StrokeWidth = 1;
            canvas.DrawRect(destination, this.stroke);
            return;
        }

        canvas.DrawImage(image, destination, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
    }

    public void Dispose()
    {
        foreach (var image in this.images.Values)
            image?.Dispose();

        this.images.Clear();
        this.fill.Dispose();
        this.stroke.Dispose();
    }

    /// <summary>
    /// Adjusts an authored colour so it reads against the page, keeping its hue and saturation.
    /// </summary>
    /// <remarks>
    /// Only moves a colour that is genuinely on the wrong side of the page's lightness — a red that
    /// already contrasts is left exactly as authored, so the adaptation is invisible on a light theme
    /// and only does work where it is needed.
    /// </remarks>
    static ArgbColor Legible(ArgbColor color, DocumentTheme theme)
    {
        if (!theme.AdaptDocumentColors)
            return color;

        var pageLight = Luminance(theme.PageBackground);
        var textLight = Luminance(color);

        // Comfortably clear of the page: nothing to do.
        if (Math.Abs(pageLight - textLight) >= 0.34)
            return color;

        var target = pageLight < 0.5
            ? Math.Max(textLight, 0.72)   // dark page: lift the text
            : Math.Min(textLight, 0.34);  // light page: push it down

        return WithLightness(color, target);
    }

    /// <summary>Perceived brightness, weighted the way the eye responds rather than by raw average.</summary>
    static double Luminance(ArgbColor color)
        => (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255d;

    static ArgbColor WithLightness(ArgbColor color, double target)
    {
        var current = Luminance(color);

        if (current <= 0.001)
        {
            // Pure black carries no hue to preserve, so it simply becomes the corresponding grey.
            var grey = (byte)Math.Clamp(Math.Round(target * 255), 0, 255);
            return color with { R = grey, G = grey, B = grey };
        }

        // Scaling all three channels keeps the ratios between them, and therefore the hue.
        var factor = target / current;
        return color with
        {
            R = (byte)Math.Clamp(Math.Round(color.R * factor), 0, 255),
            G = (byte)Math.Clamp(Math.Round(color.G * factor), 0, 255),
            B = (byte)Math.Clamp(Math.Round(color.B * factor), 0, 255)
        };
    }

    static SKColor ToSk(ArgbColor color) => new(color.R, color.G, color.B, color.A);
}
