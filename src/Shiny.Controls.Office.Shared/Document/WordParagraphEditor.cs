using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;
using W = DocumentFormat.OpenXml.Wordprocessing;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Character-level surgery on a paragraph's OOXML runs.
/// </summary>
/// <remarks>
/// <para>
/// Everything here works in offsets into the paragraph's concatenated text and translates that into
/// run-level edits. Runs are split only where an edit actually needs a boundary, and never rebuilt
/// wholesale — a run carries formatting, language, proofing state and revision marks the editor does
/// not model, and re-creating it to change one character throws all of that away.
/// </para>
/// <para>
/// Only <see cref="W.Text"/>, <see cref="TabChar"/> and <see cref="Break"/> contribute to the offset
/// space, matching what the reader projects. Anything else in a run is left strictly alone.
/// </para>
/// </remarks>
static class WordParagraphEditor
{
    /// <summary>The paragraph's text as the reader projects it, which is the offset space edits use.</summary>
    public static string TextOf(Paragraph paragraph)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var (_, text) in Segments(paragraph))
            builder.Append(text);

        return builder.ToString();
    }

    public static int LengthOf(Paragraph paragraph)
    {
        var length = 0;
        foreach (var (_, text) in Segments(paragraph))
            length += text.Length;

        return length;
    }

    /// <summary>Text-bearing leaves in document order, paired with the text they contribute.</summary>
    static IEnumerable<(OpenXmlElement Element, string Text)> Segments(Paragraph paragraph)
    {
        foreach (var run in paragraph.Descendants<Run>())
        {
            foreach (var child in run.ChildElements)
            {
                switch (child)
                {
                    case W.Text text:
                        yield return (text, text.Text ?? string.Empty);
                        break;

                    case TabChar:
                        // The reader projects a tab as four spaces; the offset space has to agree or
                        // every caret position after a tab is wrong by three.
                        yield return (child, "    ");
                        break;
                }
            }
        }
    }

    /// <summary>Inserts text at an offset, adopting the formatting of the run it lands in.</summary>
    public static void Insert(Paragraph paragraph, int offset, string text)
    {
        if (text.Length == 0)
            return;

        var cursor = 0;
        foreach (var (element, segment) in Segments(paragraph).ToList())
        {
            var end = cursor + segment.Length;

            if (offset <= end && element is W.Text target)
            {
                var local = Math.Clamp(offset - cursor, 0, segment.Length);
                var value = target.Text ?? string.Empty;
                target.Text = value[..local] + text + value[local..];
                Preserve(target);
                return;
            }

            cursor = end;
        }

        // An empty paragraph, or one whose only content is not text: start a run for the text to live in.
        var run = paragraph.Descendants<Run>().LastOrDefault();
        if (run is null)
        {
            run = new Run();
            paragraph.AppendChild(run);
        }

        var created = new W.Text(text);
        Preserve(created);
        run.AppendChild(created);
    }

    /// <summary>Deletes a half-open offset range, dropping runs that end up with no content.</summary>
    public static void Delete(Paragraph paragraph, int start, int end)
    {
        if (end <= start)
            return;

        var cursor = 0;
        foreach (var (element, segment) in Segments(paragraph).ToList())
        {
            var segmentStart = cursor;
            var segmentEnd = cursor + segment.Length;
            cursor = segmentEnd;

            if (segmentEnd <= start || segmentStart >= end)
                continue;

            var from = Math.Max(0, start - segmentStart);
            var to = Math.Min(segment.Length, end - segmentStart);

            if (element is W.Text text)
            {
                var value = text.Text ?? string.Empty;
                text.Text = value[..from] + value[Math.Min(to, value.Length)..];
                Preserve(text);
            }
            else if (from == 0 && to == segment.Length)
            {
                // A tab is atomic: it goes entirely or not at all.
                element.Remove();
            }
        }

        RemoveEmptyRuns(paragraph);
    }

    /// <summary>
    /// Applies a formatting change to an offset range, splitting runs at the boundaries.
    /// </summary>
    /// <remarks>
    /// The mutation is expressed as an action on the run's <see cref="RunProperties"/> rather than as a
    /// finished style, so a run keeps every property the change does not touch. Handing over a whole
    /// style would flatten italics and colours the user never asked to change.
    /// </remarks>
    public static void Format(Paragraph paragraph, int start, int end, Action<RunProperties> apply)
    {
        if (end <= start)
            return;

        foreach (var original in paragraph.Descendants<Run>().ToList())
        {
            var text = original.GetFirstChild<W.Text>();
            if (text is null)
                continue;

            var runStart = OffsetOf(paragraph, text);
            var value = text.Text ?? string.Empty;
            var runEnd = runStart + value.Length;

            if (runEnd <= start || runStart >= end)
                continue;

            var from = Math.Max(0, start - runStart);
            var to = Math.Min(value.Length, end - runStart);

            // Split off the tail first: splitting the head would shift the offsets the tail split
            // depends on, and the second split would land in the wrong place.
            if (to < value.Length)
                SplitAt(original, to);

            var target = from > 0 ? SplitAt(original, from) ?? original : original;
            apply(target.RunProperties ??= new RunProperties());
        }

        RemoveEmptyRuns(paragraph);
    }

    /// <summary>
    /// Splits a run at a local offset and returns the new trailing run, which carries a clone of the
    /// original's properties so nothing is lost across the boundary.
    /// </summary>
    static Run? SplitAt(Run run, int localOffset)
    {
        var text = run.GetFirstChild<W.Text>();
        if (text is null)
            return null;

        var value = text.Text ?? string.Empty;
        if (localOffset <= 0 || localOffset >= value.Length)
            return null;

        var tail = new Run();
        if (run.RunProperties is { } properties)
            tail.RunProperties = (RunProperties)properties.CloneNode(true);

        var tailText = new W.Text(value[localOffset..]);
        Preserve(tailText);
        tail.AppendChild(tailText);

        text.Text = value[..localOffset];
        Preserve(text);

        run.InsertAfterSelf(tail);
        return tail;
    }

    static int OffsetOf(Paragraph paragraph, OpenXmlElement target)
    {
        var cursor = 0;
        foreach (var (element, segment) in Segments(paragraph))
        {
            if (ReferenceEquals(element, target))
                return cursor;

            cursor += segment.Length;
        }

        return cursor;
    }

    /// <summary>
    /// Splits a paragraph at an offset, returning the new paragraph that follows it.
    /// </summary>
    /// <remarks>
    /// The tail inherits a clone of the original's properties, so pressing Enter mid-paragraph keeps
    /// the style, indent and numbering on both halves rather than dropping the second into Normal.
    /// </remarks>
    public static Paragraph Split(Paragraph paragraph, int offset)
    {
        var tail = new Paragraph();
        if (paragraph.ParagraphProperties is { } properties)
            tail.ParagraphProperties = (ParagraphProperties)properties.CloneNode(true);

        var text = TextOf(paragraph);
        var trailing = offset >= text.Length ? string.Empty : text[offset..];

        // Move the trailing runs across, splitting the one the caret sits inside.
        var cursor = 0;
        foreach (var run in paragraph.Descendants<Run>().ToList())
        {
            var runText = run.GetFirstChild<W.Text>();
            var value = runText?.Text ?? string.Empty;
            var runStart = cursor;
            cursor += value.Length;

            if (runStart >= offset)
            {
                run.Remove();
                tail.AppendChild(run);
                continue;
            }

            if (runStart + value.Length > offset && runText is not null)
            {
                var local = offset - runStart;
                var moved = SplitAt(run, local);
                if (moved is not null)
                {
                    moved.Remove();
                    tail.AppendChild(moved);
                }
            }
        }

        // A tail with no runs still needs one carrying the caret's formatting, or the new paragraph
        // renders with document defaults and typing into it changes font unexpectedly.
        if (!tail.Descendants<Run>().Any())
        {
            var seed = new Run();
            if (paragraph.Descendants<Run>().LastOrDefault()?.RunProperties is { } runProperties)
                seed.RunProperties = (RunProperties)runProperties.CloneNode(true);

            tail.AppendChild(seed);
        }

        _ = trailing;
        paragraph.InsertAfterSelf(tail);
        return tail;
    }

    /// <summary>Appends one paragraph's runs onto another and removes the source.</summary>
    public static void Merge(Paragraph target, Paragraph source)
    {
        foreach (var run in source.Descendants<Run>().ToList())
        {
            run.Remove();
            target.AppendChild(run);
        }

        source.Remove();
        RemoveEmptyRuns(target);
    }

    /// <summary>Mutates a paragraph's properties, creating the element when it does not exist yet.</summary>
    public static void FormatParagraph(Paragraph paragraph, Action<ParagraphProperties> apply)
    {
        var properties = paragraph.ParagraphProperties;
        if (properties is null)
        {
            properties = new ParagraphProperties();

            // pPr must be the first child of w:p; appending it puts the document out of schema order
            // and Word reports the file as corrupt.
            paragraph.InsertAt(properties, 0);
        }

        apply(properties);
    }

    static void RemoveEmptyRuns(Paragraph paragraph)
    {
        foreach (var run in paragraph.Descendants<Run>().ToList())
        {
            // A run with no children at all is debris. One holding an empty w:t is kept only when it is
            // the paragraph's last, so an emptied paragraph still has somewhere to carry formatting.
            if (run.ChildElements.Count == 0 || run.ChildElements.All(x => x is RunProperties))
            {
                if (paragraph.Descendants<Run>().Count() > 1)
                    run.Remove();

                continue;
            }

            foreach (var text in run.Elements<W.Text>().ToList())
            {
                if (text.Text?.Length == 0 && run.Elements<W.Text>().Count() > 1)
                    text.Remove();
            }
        }
    }

    /// <summary>
    /// Marks a text element to keep its whitespace.
    /// </summary>
    /// <remarks>
    /// Without <c>xml:space="preserve"</c> Word discards leading and trailing spaces on load, so text
    /// typed with a trailing space loses it the next time the file is opened.
    /// </remarks>
    static void Preserve(W.Text text)
    {
        var value = text.Text;
        if (!string.IsNullOrEmpty(value) && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])))
            text.Space = SpaceProcessingModeValues.Preserve;
    }

    /// <summary>Builds the run-property mutation for a formatting toggle.</summary>
    public static Action<RunProperties> ToggleBold(bool on) => properties =>
    {
        properties.RemoveAllChildren<Bold>();
        if (on)
            properties.InsertAt(new Bold(), 0);
    };

    public static Action<RunProperties> ToggleItalic(bool on) => properties =>
    {
        properties.RemoveAllChildren<Italic>();
        if (on)
            properties.InsertAt(new Italic(), 0);
    };

    public static Action<RunProperties> ToggleUnderline(bool on) => properties =>
    {
        properties.RemoveAllChildren<W.Underline>();
        if (on)
            properties.AppendChild(new W.Underline { Val = UnderlineValues.Single });
    };

    public static Action<RunProperties> ToggleStrike(bool on) => properties =>
    {
        properties.RemoveAllChildren<Strike>();
        if (on)
            properties.AppendChild(new Strike());
    };

    public static Action<RunProperties> SetFontFamily(string family) => properties =>
    {
        properties.RemoveAllChildren<RunFonts>();
        properties.InsertAt(new RunFonts { Ascii = family, HighAnsi = family, ComplexScript = family }, 0);
    };

    public static Action<RunProperties> SetFontSize(double points) => properties =>
    {
        properties.RemoveAllChildren<FontSize>();
        properties.RemoveAllChildren<FontSizeComplexScript>();

        // Word stores run size in half-points.
        var halfPoints = Math.Max(1, (int)Math.Round(points * 2)).ToString();
        properties.AppendChild(new FontSize { Val = halfPoints });
        properties.AppendChild(new FontSizeComplexScript { Val = halfPoints });
    };

    public static Action<RunProperties> SetColor(ArgbColor color) => properties =>
    {
        properties.RemoveAllChildren<W.Color>();
        properties.AppendChild(new W.Color { Val = $"{color.R:X2}{color.G:X2}{color.B:X2}" });
    };

    public static Action<ParagraphProperties> SetAlignment(TextAlignment alignment) => properties =>
    {
        properties.RemoveAllChildren<Justification>();
        properties.AppendChild(new Justification
        {
            Val = alignment switch
            {
                TextAlignment.Center => JustificationValues.Center,
                TextAlignment.Right => JustificationValues.Right,
                TextAlignment.Justify => JustificationValues.Both,
                _ => JustificationValues.Left
            }
        });
    };

    public static Action<ParagraphProperties> SetStyle(string? styleId) => properties =>
    {
        properties.RemoveAllChildren<ParagraphStyleId>();
        if (styleId is not null)
            properties.InsertAt(new ParagraphStyleId { Val = styleId }, 0);
    };
}
