using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Presentation;

/// <summary>One hit in a deck: the slide, the shape on it, the paragraph, and where in that paragraph.</summary>
public readonly record struct SlideFindMatch(int Slide, int Shape, int Paragraph, int Start, int Length)
{
    public int End => this.Start + this.Length;

    /// <summary>Where the match begins, as a caret position.</summary>
    public SlidePosition Position => new(this.Slide, this.Shape, this.Paragraph, this.Start);

    /// <summary>The match as a selectable range.</summary>
    public SlideTextRange Range => new(this.Position, this.Position with { Offset = this.End });
}


/// <summary>
/// Finds text across a deck and drives the editor onto each hit — the slide, then the shape, then the
/// word inside it.
/// </summary>
/// <remarks>
/// <para>
/// Only shapes a person could edit are searched. The rest come from the slide's layout and master and
/// are template decoration shared by every slide using them, so a hit inside one would count the
/// company name once per slide and step the user into something they cannot select.
/// </para>
/// <para>
/// Tables and speaker notes are out for the same reason the document finder skips table cells: a
/// caret position on a slide is a shape, a paragraph and an offset, and neither of those has one.
/// </para>
/// </remarks>
public sealed class SlideFinder(SlideEditorController controller) : FindController<SlideFindMatch>
{
    /// <inheritdoc/>
    protected override IReadOnlyList<SlideFindMatch> Collect(string query, FindOptions options)
    {
        var slides = controller.Deck.Slides;
        var results = new List<SlideFindMatch>();

        for (var s = 0; s < slides.Count; s++)
        {
            var shapes = slides[s].Shapes;

            for (var h = 0; h < shapes.Count; h++)
            {
                if (!shapes[h].IsEditable || shapes[h].Text is not { } body)
                    continue;

                for (var p = 0; p < body.Paragraphs.Count; p++)
                {
                    foreach (var match in TextSearch.Matches(body.Paragraphs[p].PlainText, query, options))
                        results.Add(new SlideFindMatch(s, h, p, match.Start, match.Length));
                }
            }
        }

        return results;
    }

    /// <inheritdoc/>
    protected override void MoveTo(SlideFindMatch match) => controller.SelectFindMatch(match);

    /// <inheritdoc/>
    protected override int IndexAtOrAfterCaret(IReadOnlyList<SlideFindMatch> matches)
    {
        var caret = controller.TextSelection.Normalized().Start;
        var slide = controller.Index;

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];

            if (match.Slide != slide)
            {
                // Everything on a later slide is ahead of the caret whatever its offsets say; anything
                // on an earlier one is behind it.
                if (match.Slide > slide)
                    return i;

                continue;
            }

            // On the caret's own slide the shape order is the reading order, and within a shape the
            // paragraph and offset decide it.
            if (match.Shape > caret.Shape)
                return i;

            if (match.Shape < caret.Shape)
                continue;

            if (match.Paragraph > caret.Paragraph || (match.Paragraph == caret.Paragraph && match.Start >= caret.Offset))
                return i;
        }

        return matches.Count;
    }
}
