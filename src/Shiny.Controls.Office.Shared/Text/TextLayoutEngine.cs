namespace Shiny.Controls.Office.Text;

/// <summary>A run of text sharing one style, as it comes out of a document.</summary>
public sealed record StyledRun(string Text, TextStyle Style)
{
    /// <summary>A run that forces a line break rather than carrying text.</summary>
    public bool IsBreak { get; init; }

    /// <summary>Non-null when this run is an inline image rather than text.</summary>
    public InlineImage? Image { get; init; }
}

public sealed record InlineImage(byte[] Data, double Width, double Height, string? Description = null);

/// <summary>A run positioned on a line.</summary>
public sealed record LaidOutRun(string Text, TextStyle Style, double X, double Width, InlineImage? Image = null)
{
    public double Height { get; init; }

    /// <summary>
    /// Character index of this piece within the paragraph's concatenated text.
    /// </summary>
    /// <remarks>
    /// Wrapping splits a run into pieces and drops nothing, but the pieces no longer line up with the
    /// source. Carrying the offset is what lets a click be turned back into a caret position, and it
    /// has to be recorded during layout because afterwards the mapping is gone.
    /// </remarks>
    public int SourceOffset { get; init; }
}

/// <summary>One line of a laid-out paragraph.</summary>
public sealed record LaidOutLine(IReadOnlyList<LaidOutRun> Runs, double Y, double Width, double Ascent, double Descent)
{
    public double Height => this.Ascent + this.Descent;

    /// <summary>Character index of the line's first character within the paragraph.</summary>
    public int SourceOffset { get; init; }

    /// <summary>One past the line's last character. Trailing whitespace is included.</summary>
    public int SourceEnd { get; init; }
}

/// <summary>
/// Breaks styled runs into lines that fit a width.
/// </summary>
/// <remarks>
/// <para>
/// A greedy word-wrapper: it accumulates whole words and breaks at the last opportunity that still
/// fits. That is what Word and PowerPoint do too — neither uses Knuth-Plass — so matching them here is
/// both simpler and more faithful than being cleverer.
/// </para>
/// <para>
/// Break opportunities are whitespace, plus after a hyphen. A single word longer than the line is
/// broken mid-word rather than allowed to overflow, because a table cell one character wide would
/// otherwise paint across the whole page.
/// </para>
/// </remarks>
public sealed class TextLayoutEngine(ITextMeasurer measurer)
{
    public ITextMeasurer Measurer { get; } = measurer;

    /// <summary>
    /// Lays out a paragraph's runs into lines no wider than <paramref name="width"/>.
    /// </summary>
    /// <param name="lineSpacing">Multiplier applied to each line's natural height.</param>
    public IReadOnlyList<LaidOutLine> Layout(
        IReadOnlyList<StyledRun> runs,
        double width,
        TextAlignment alignment = TextAlignment.Left,
        double lineSpacing = 1.0,
        double firstLineIndent = 0)
    {
        ArgumentNullException.ThrowIfNull(runs);

        var lines = new List<LaidOutLine>();
        var current = new List<PendingPiece>();
        var y = 0d;
        var indent = firstLineIndent;

        // Running character index into the paragraph's concatenated text.
        var sourceOffset = 0;

        void Flush(bool lastLineOfParagraph)
        {
            if (current.Count == 0 && !lastLineOfParagraph)
                return;

            var line = Commit(current, y, width - indent, indent, alignment, lastLineOfParagraph, this.Measurer);
            lines.Add(line);
            y += line.Height * lineSpacing;
            current.Clear();
            indent = 0;
        }

        foreach (var run in runs)
        {
            if (run.IsBreak)
            {
                Flush(lastLineOfParagraph: true);
                continue;
            }

            if (run.Image is { } image)
            {
                var available = width - indent - current.Sum(p => p.Width);
                if (image.Width > available && current.Count > 0)
                    Flush(lastLineOfParagraph: false);

                current.Add(new PendingPiece(string.Empty, run.Style, image.Width, image.Height, 0, image, sourceOffset));
                sourceOffset++;
                continue;
            }

            foreach (var piece in Split(run.Text))
            {
                var pieceOffset = sourceOffset;
                sourceOffset += piece.Length;
                var metrics = this.Measurer.Measure(piece, run.Style);
                var used = current.Sum(p => p.Width);
                var available = width - indent;

                if (used + metrics.Width > available && current.Count > 0)
                {
                    // A trailing space is allowed to hang past the edge rather than forcing a break.
                    if (!IsWhitespace(piece))
                    {
                        Flush(lastLineOfParagraph: false);
                        used = 0;
                        available = width;
                    }
                }

                // A single piece wider than the whole line has to be broken mid-word.
                if (metrics.Width > available && current.Count == 0 && piece.Length > 1)
                {
                    var fragmentOffset = pieceOffset;
                    foreach (var fragment in this.BreakOversized(piece, run.Style, available))
                    {
                        var fragmentMetrics = this.Measurer.Measure(fragment, run.Style);
                        if (current.Sum(p => p.Width) + fragmentMetrics.Width > available && current.Count > 0)
                            Flush(lastLineOfParagraph: false);

                        current.Add(new PendingPiece(fragment, run.Style, fragmentMetrics.Width, fragmentMetrics.Ascent, fragmentMetrics.Descent, null, fragmentOffset));
                        fragmentOffset += fragment.Length;
                    }

                    continue;
                }

                current.Add(new PendingPiece(piece, run.Style, metrics.Width, metrics.Ascent, metrics.Descent, null, pieceOffset));
            }
        }

        Flush(lastLineOfParagraph: true);

        // A paragraph with no content at all still occupies one empty line.
        if (lines.Count == 0)
        {
            var style = runs.Count > 0 ? runs[0].Style : TextStyle.Default;
            var metrics = this.Measurer.LineMetrics(style);
            lines.Add(new LaidOutLine([], 0, 0, metrics.Ascent, metrics.Descent));
        }

        return lines;
    }

    /// <summary>Total height of a laid-out paragraph.</summary>
    public static double HeightOf(IReadOnlyList<LaidOutLine> lines, double lineSpacing = 1.0)
        => lines.Count == 0 ? 0 : lines[^1].Y + lines[^1].Height * lineSpacing;

    readonly record struct PendingPiece(string Text, TextStyle Style, double Width, double Ascent, double Descent, InlineImage? Image, int SourceOffset);

    static LaidOutLine Commit(
        List<PendingPiece> pieces,
        double y,
        double contentWidth,
        double indent,
        TextAlignment alignment,
        bool lastLine,
        ITextMeasurer measurer)
    {
        // Trailing whitespace never participates in width or alignment.
        var end = pieces.Count;
        while (end > 0 && IsWhitespace(pieces[end - 1].Text))
            end--;

        var used = 0d;
        for (var i = 0; i < end; i++)
            used += pieces[i].Width;

        var ascent = 0d;
        var descent = 0d;
        for (var i = 0; i < end; i++)
        {
            ascent = Math.Max(ascent, pieces[i].Image is { } image ? image.Height : pieces[i].Ascent);
            descent = Math.Max(descent, pieces[i].Image is null ? pieces[i].Descent : 0);
        }

        if (end == 0)
        {
            var fallback = measurer.LineMetrics(pieces.Count > 0 ? pieces[0].Style : TextStyle.Default);
            ascent = fallback.Ascent;
            descent = fallback.Descent;
        }

        var slack = Math.Max(0, contentWidth - used);
        var x = indent + alignment switch
        {
            TextAlignment.Center => slack / 2,
            TextAlignment.Right => slack,
            _ => 0
        };

        // Justification stretches the gaps, not the words - and never on the last line of a paragraph,
        // which is what stops a two-word closing line being spread across the full measure.
        var gapExtra = 0d;
        if (alignment == TextAlignment.Justify && !lastLine)
        {
            var gaps = 0;
            for (var i = 0; i < end; i++)
            {
                if (IsWhitespace(pieces[i].Text))
                    gaps++;
            }

            if (gaps > 0)
                gapExtra = slack / gaps;
        }

        var runs = new List<LaidOutRun>(end);
        for (var i = 0; i < end; i++)
        {
            var piece = pieces[i];
            runs.Add(new LaidOutRun(piece.Text, piece.Style, x, piece.Width, piece.Image)
            {
                Height = piece.Image?.Height ?? piece.Ascent + piece.Descent,
                SourceOffset = piece.SourceOffset
            });

            x += piece.Width;
            if (gapExtra > 0 && IsWhitespace(piece.Text))
                x += gapExtra;
        }

        // The line spans from its first piece to past its last, including the trailing whitespace that
        // was trimmed for width - a caret clicked past the end of a line belongs after that space.
        var lineStart = pieces.Count > 0 ? pieces[0].SourceOffset : 0;
        var lineEnd = pieces.Count > 0
            ? pieces[^1].SourceOffset + pieces[^1].Text.Length
            : lineStart;

        return new LaidOutLine(runs, y, used, ascent, descent)
        {
            SourceOffset = lineStart,
            SourceEnd = lineEnd
        };
    }

    /// <summary>
    /// Splits text into wrapping pieces: words, and the whitespace runs between them kept as their own
    /// pieces so they can hang past the right edge instead of forcing a break.
    /// </summary>
    static IEnumerable<string> Split(string text)
    {
        if (text.Length == 0)
            yield break;

        var start = 0;
        var inWhitespace = char.IsWhiteSpace(text[0]);

        for (var i = 1; i <= text.Length; i++)
        {
            var atEnd = i == text.Length;
            var isWhitespace = !atEnd && char.IsWhiteSpace(text[i]);

            // A hyphen is a break opportunity after it, not before.
            var afterHyphen = !atEnd && !isWhitespace && !inWhitespace && text[i - 1] == '-';

            if (atEnd || isWhitespace != inWhitespace || afterHyphen)
            {
                yield return text[start..i];
                if (atEnd)
                    yield break;

                start = i;
                inWhitespace = isWhitespace;
            }
        }
    }

    /// <summary>Chops a word too long for the line into as many fragments as it takes.</summary>
    IEnumerable<string> BreakOversized(string word, TextStyle style, double width)
    {
        var start = 0;
        while (start < word.Length)
        {
            var length = 1;
            while (start + length < word.Length &&
                   this.Measurer.Measure(word.AsSpan(start, length + 1), style).Width <= width)
                length++;

            yield return word.Substring(start, length);
            start += length;
        }
    }

    static bool IsWhitespace(string text) => text.Length > 0 && char.IsWhiteSpace(text[0]);
}
