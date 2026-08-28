using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;
using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;
using D = DocumentFormat.OpenXml.Drawing;

namespace Shiny.Controls.Office.Presentation;

/// <summary>
/// A caret position inside a deck: which slide, which shape, which paragraph, and how far into it.
/// </summary>
/// <remarks>
/// Deeper than the Word editor's <c>(block, offset)</c> because a slide is not a flow — text lives
/// inside shapes that have no order relative to one another, so a position is meaningless without the
/// shape it belongs to.
/// </remarks>
public readonly record struct SlidePosition(int Slide, int Shape, int Paragraph, int Offset)
{
    public bool SameShape(SlidePosition other) => this.Slide == other.Slide && this.Shape == other.Shape;

    /// <summary>Ordering within one shape. Comparing across shapes is meaningless and returns 0.</summary>
    public int CompareWithin(SlidePosition other)
    {
        if (!this.SameShape(other))
            return 0;

        return this.Paragraph != other.Paragraph
            ? this.Paragraph.CompareTo(other.Paragraph)
            : this.Offset.CompareTo(other.Offset);
    }
}

/// <summary>A span of text inside one shape.</summary>
public readonly record struct SlideTextRange(SlidePosition Start, SlidePosition End)
{
    public bool IsEmpty => this.Start == this.End;

    public bool IsWithinOneParagraph => this.Start.Paragraph == this.End.Paragraph;

    /// <summary>The range with its ends in ascending order.</summary>
    public SlideTextRange Normalized()
        => this.Start.CompareWithin(this.End) <= 0 ? this : new SlideTextRange(this.End, this.Start);
}

/// <summary>
/// Base for slide edits.
/// </summary>
/// <remarks>
/// Every command captures what it needs to reverse itself while it runs, and reprojects the slide it
/// touched so the model and the XML never disagree.
/// </remarks>
public abstract record SlideCommand : IEditCommand<SlideDeck>
{
    public abstract string Name { get; }

    public abstract IEditCommand<SlideDeck> Apply(SlideDeck context);

    /// <summary>The <c>a:p</c> a position points at, or null when the position is stale.</summary>
    private protected static D.Paragraph? ParagraphAt(SlideDeck deck, SlidePosition at)
        => deck.Slides.ElementAtOrDefault(at.Slide)?
            .Shapes.ElementAtOrDefault(at.Shape)?
            .Text?.Paragraphs.ElementAtOrDefault(at.Paragraph)?
            .Element;

    private protected static SlideShape? ShapeAt(SlideDeck deck, int slide, int shape)
        => deck.Slides.ElementAtOrDefault(slide)?.Shapes.ElementAtOrDefault(shape);

    /// <summary>
    /// Clones a whole shape so an edit can be reversed by putting it back.
    /// </summary>
    /// <remarks>
    /// Text edits within a paragraph invert precisely, but anything that adds or removes paragraphs
    /// shifts every index after it — so those capture the shape wholesale rather than trying to
    /// describe an inverse that would go stale.
    /// </remarks>
    private protected static RestoreShapeCommand CaptureShape(SlideDeck deck, int slide, int shape)
    {
        var element = ShapeAt(deck, slide, shape)?.Element;
        return new RestoreShapeCommand(slide, shape, element?.CloneNode(true));
    }
}

/// <summary>Puts a previously captured shape back, replacing whatever is there now.</summary>
public sealed record RestoreShapeCommand(int Slide, int Shape, OpenXmlElement? Snapshot) : SlideCommand
{
    public override string Name => "Restore";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        if (this.Snapshot is null)
            return new NoOpSlideCommand();

        var inverse = CaptureShape(context, this.Slide, this.Shape);

        var current = ShapeAt(context, this.Slide, this.Shape)?.Element;
        if (current is null)
            return new NoOpSlideCommand();

        current.Parent?.ReplaceChild(this.Snapshot.CloneNode(true), current);
        context.Reproject(this.Slide);

        return inverse;
    }
}

public sealed record NoOpSlideCommand : SlideCommand
{
    public override string Name => "Nothing";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context) => this;
}

/// <summary>Inserts text at a position inside a shape.</summary>
public sealed record InsertSlideTextCommand(SlidePosition At, string Text) : SlideCommand, IMergeableCommand<SlideDeck>
{
    public override string Name => "Typing";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        if (ParagraphAt(context, this.At) is not { } paragraph)
            return new NoOpSlideCommand();

        ShapeTextEditor.Insert(paragraph, this.At.Offset, this.Text);
        context.Reproject(this.At.Slide);

        return new DeleteSlideRangeCommand(new SlideTextRange(
            this.At,
            this.At with { Offset = this.At.Offset + this.Text.Length }));
    }

    /// <summary>
    /// Absorbs the next typed character, so a typed word undoes in one step.
    /// </summary>
    /// <remarks>
    /// Only when it continues immediately where this one ended — moving the caret, or typing in a
    /// different shape, ends the run.
    /// </remarks>
    public bool TryMerge(IEditCommand<SlideDeck> next, out IEditCommand<SlideDeck> merged)
    {
        merged = this;

        if (next is not InsertSlideTextCommand following)
            return false;

        if (!following.At.SameShape(this.At) ||
            following.At.Paragraph != this.At.Paragraph ||
            following.At.Offset != this.At.Offset + this.Text.Length)
            return false;

        if (this.Text.Length + following.Text.Length > 64)
            return false;

        merged = this with { Text = this.Text + following.Text };
        return true;
    }
}

/// <summary>Deletes a range, which may span paragraphs within one shape.</summary>
public sealed record DeleteSlideRangeCommand(SlideTextRange Range) : SlideCommand
{
    public override string Name => "Delete";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        var range = this.Range.Normalized();
        if (range.IsEmpty || !range.Start.SameShape(range.End))
            return new NoOpSlideCommand();

        // Captured before anything moves: after the edit the paragraphs it came from may be gone.
        var restore = CaptureShape(context, range.Start.Slide, range.Start.Shape);

        if (range.IsWithinOneParagraph)
        {
            if (ParagraphAt(context, range.Start) is not { } paragraph)
                return new NoOpSlideCommand();

            ShapeTextEditor.Delete(paragraph, range.Start.Offset, range.End.Offset);
            context.Reproject(range.Start.Slide);
            return restore;
        }

        if (ParagraphAt(context, range.Start) is not { } first ||
            ParagraphAt(context, range.End) is not { } last)
            return new NoOpSlideCommand();

        // Trim both ends, drop everything between, then join the survivors — the same shape as the
        // Word editor's multi-paragraph delete.
        var between = new List<D.Paragraph>();
        for (var i = range.Start.Paragraph + 1; i < range.End.Paragraph; i++)
        {
            if (ParagraphAt(context, range.Start with { Paragraph = i }) is { } middle)
                between.Add(middle);
        }

        ShapeTextEditor.Delete(first, range.Start.Offset, ShapeTextEditor.LengthOf(first));
        ShapeTextEditor.Delete(last, 0, range.End.Offset);

        foreach (var middle in between)
            middle.Remove();

        ShapeTextEditor.Merge(first, last);
        context.Reproject(range.Start.Slide);
        return restore;
    }
}

/// <summary>Splits a paragraph at a position — what Enter does.</summary>
public sealed record SplitSlideParagraphCommand(SlidePosition At) : SlideCommand
{
    public override string Name => "New paragraph";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        var restore = CaptureShape(context, this.At.Slide, this.At.Shape);

        if (ParagraphAt(context, this.At) is not { } paragraph)
            return new NoOpSlideCommand();

        var tail = ShapeTextEditor.Split(paragraph, this.At.Offset);
        paragraph.Parent?.InsertAfter(tail, paragraph);

        context.Reproject(this.At.Slide);
        return restore;
    }
}

/// <summary>Joins a paragraph onto the one before it — what Backspace at offset 0 does.</summary>
public sealed record MergeSlideParagraphCommand(SlidePosition At) : SlideCommand
{
    public override string Name => "Join paragraphs";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        if (this.At.Paragraph <= 0)
            return new NoOpSlideCommand();

        var restore = CaptureShape(context, this.At.Slide, this.At.Shape);

        if (ParagraphAt(context, this.At) is not { } paragraph ||
            ParagraphAt(context, this.At with { Paragraph = this.At.Paragraph - 1 }) is not { } previous)
            return new NoOpSlideCommand();

        ShapeTextEditor.Merge(previous, paragraph);
        context.Reproject(this.At.Slide);
        return restore;
    }
}

/// <summary>Applies a run-property change over a range, splitting runs at the boundaries.</summary>
public sealed record FormatSlideRunsCommand(SlideTextRange Range, Action<D.RunProperties> Apply_, string Label)
    : SlideCommand
{
    public override string Name => this.Label;

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        var range = this.Range.Normalized();
        if (!range.Start.SameShape(range.End))
            return new NoOpSlideCommand();

        var restore = CaptureShape(context, range.Start.Slide, range.Start.Shape);

        if (range.IsEmpty)
        {
            // Nothing selected: the choice goes on the paragraph's end mark, which is where
            // PowerPoint keeps formatting for text that has not been typed yet.
            if (ParagraphAt(context, range.Start) is not { } only)
                return new NoOpSlideCommand();

            ShapeTextEditor.FormatEndMark(only, this.Apply_);
            context.Reproject(range.Start.Slide);
            return restore;
        }

        for (var i = range.Start.Paragraph; i <= range.End.Paragraph; i++)
        {
            if (ParagraphAt(context, range.Start with { Paragraph = i }) is not { } paragraph)
                continue;

            var from = i == range.Start.Paragraph ? range.Start.Offset : 0;
            var to = i == range.End.Paragraph ? range.End.Offset : ShapeTextEditor.LengthOf(paragraph);

            ShapeTextEditor.Format(paragraph, from, to, this.Apply_);
        }

        context.Reproject(range.Start.Slide);
        return restore;
    }
}

/// <summary>Applies a paragraph-property change across every paragraph a range touches.</summary>
public sealed record FormatSlideParagraphsCommand(SlideTextRange Range, Action<D.ParagraphProperties> Apply_, string Label)
    : SlideCommand
{
    public override string Name => this.Label;

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        var range = this.Range.Normalized();
        if (!range.Start.SameShape(range.End))
            return new NoOpSlideCommand();

        var restore = CaptureShape(context, range.Start.Slide, range.Start.Shape);

        for (var i = range.Start.Paragraph; i <= range.End.Paragraph; i++)
        {
            if (ParagraphAt(context, range.Start with { Paragraph = i }) is { } paragraph)
                ShapeTextEditor.FormatParagraph(paragraph, this.Apply_);
        }

        context.Reproject(range.Start.Slide);
        return restore;
    }
}

/// <summary>
/// Moves and resizes a shape, in slide coordinates.
/// </summary>
/// <remarks>
/// <para>
/// A placeholder frequently has no <c>a:xfrm</c> of its own — its position comes from the layout —
/// so dragging one has to write a transform that was never there. That is correct and is what
/// PowerPoint itself does; from then on the shape no longer follows its layout's position.
/// </para>
/// <para>
/// This inverts exactly rather than through a snapshot, because the previous rectangle is all it
/// takes to undo and a drag produces a great many of these.
/// </para>
/// </remarks>
public sealed record SetShapeBoundsCommand(int Slide, int Shape, double X, double Y, double Width, double Height)
    : SlideCommand, IMergeableCommand<SlideDeck>
{
    public override string Name => "Move shape";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        if (ShapeAt(context, this.Slide, this.Shape) is not { } shape || shape.Element is null)
            return new NoOpSlideCommand();

        var inverse = new SetShapeBoundsCommand(this.Slide, this.Shape, shape.X, shape.Y, shape.Width, shape.Height);

        var transform = EnsureTransform(shape.Element);
        if (transform is null)
            return new NoOpSlideCommand();

        transform.Offset ??= new D.Offset();
        transform.Extents ??= new D.Extents();

        transform.Offset.X = OoxmlUnits.PixelsToEmu(this.X);
        transform.Offset.Y = OoxmlUnits.PixelsToEmu(this.Y);

        // A zero-sized shape cannot be grabbed again, so a resize can never take a shape below a
        // size that still has a handle on it.
        transform.Extents.Cx = OoxmlUnits.PixelsToEmu(Math.Max(4, this.Width));
        transform.Extents.Cy = OoxmlUnits.PixelsToEmu(Math.Max(4, this.Height));

        context.Reproject(this.Slide);
        return inverse;
    }

    /// <summary>A drag is one undo step, not one per pointer sample.</summary>
    public bool TryMerge(IEditCommand<SlideDeck> next, out IEditCommand<SlideDeck> merged)
    {
        merged = this;

        if (next is not SetShapeBoundsCommand following ||
            following.Slide != this.Slide ||
            following.Shape != this.Shape)
            return false;

        // Keep this command's identity but take the newer rectangle: the inverse the stack already
        // holds points at where the shape started, which is where an undo has to put it back.
        merged = following;
        return true;
    }

    /// <summary>
    /// The shape's transform, created if the shape inherited its position from a layout.
    /// </summary>
    /// <remarks>
    /// Every shape kind keeps its transform somewhere different — <c>p:spPr</c>, <c>p:grpSpPr</c>,
    /// <c>p:xfrm</c> on a graphic frame — so this reaches for the properties element by type rather
    /// than assuming the shape is a <c>p:sp</c>.
    /// </remarks>
    static D.Transform2D? EnsureTransform(OpenXmlElement element)
    {
        switch (element)
        {
            case Shape shape:
                var shapeProperties = shape.ShapeProperties ??= new ShapeProperties();
                return shapeProperties.Transform2D ??= NewTransform(shapeProperties);

            case Picture picture:
                var pictureProperties = picture.ShapeProperties ??= new ShapeProperties();
                return pictureProperties.Transform2D ??= NewTransform(pictureProperties);

            case ConnectionShape connection:
                var connectionProperties = connection.ShapeProperties ??= new ShapeProperties();
                return connectionProperties.Transform2D ??= NewTransform(connectionProperties);

            case GraphicFrame frame:
                // A graphic frame's transform is p:xfrm, a direct child, and is a different element
                // from the a:xfrm every other shape uses.
                frame.Transform ??= new Transform();
                return null;

            default:
                return null;
        }
    }

    /// <summary>a:xfrm is the first child of the properties element; appending it is invalid.</summary>
    static D.Transform2D NewTransform(ShapeProperties properties)
    {
        var transform = new D.Transform2D();
        properties.InsertAt(transform, 0);
        return transform;
    }
}

/// <summary>Removes a shape from its slide.</summary>
public sealed record DeleteShapeCommand(int Slide, int Shape) : SlideCommand
{
    public override string Name => "Delete shape";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        if (ShapeAt(context, this.Slide, this.Shape) is not { Element: { } element } shape || !shape.IsEditable)
            return new NoOpSlideCommand();

        // The index a shape sits at in the *slide's own* tree, which is what an undo has to put it
        // back at — the model's index also counts the layout and master shapes painted underneath.
        var tree = element.Parent;
        var position = tree is null ? -1 : tree.ChildElements.ToList().IndexOf(element);

        var snapshot = element.CloneNode(true);
        element.Remove();
        context.Reproject(this.Slide);

        return new InsertShapeCommand(this.Slide, position, snapshot);
    }
}

/// <summary>Puts a shape into a slide's tree at a known position.</summary>
public sealed record InsertShapeCommand(int Slide, int TreeIndex, OpenXmlElement Element) : SlideCommand
{
    public override string Name => "Add shape";

    public override IEditCommand<SlideDeck> Apply(SlideDeck context)
    {
        if (context.TreeAt(this.Slide) is not { } tree)
            return new NoOpSlideCommand();

        var clone = this.Element.CloneNode(true);

        if (this.TreeIndex >= 0 && this.TreeIndex < tree.ChildElements.Count)
            tree.InsertAt(clone, this.TreeIndex);
        else
            tree.AppendChild(clone);

        context.Reproject(this.Slide);

        // The model index of what was just added, so the inverse deletes the right shape.
        var added = context.Slides.ElementAtOrDefault(this.Slide)?
            .Shapes.ToList()
            .FindIndex(x => ReferenceEquals(x.Element, clone)) ?? -1;

        return added < 0
            ? new NoOpSlideCommand()
            : new DeleteShapeCommand(this.Slide, added);
    }
}

/// <summary>The formatting under the caret, so a toolbar can show what is active.</summary>
public readonly record struct SlideCaretFormat(
    bool Bold,
    bool Italic,
    bool Underline,
    bool Strike,
    double FontSize,
    string FontFamily,
    ArgbColor Color,
    TextAlignment Alignment,
    ArgbColor? Highlight = null)
{
    /// <summary>Which list the caret's paragraph is in, so a toolbar can light the right button.</summary>
    public ListStyle List { get; init; }

    /// <summary>The paragraph's outline level, 0-8. What Tab moves.</summary>
    public int Level { get; init; }

    public static SlideCaretFormat Default => new(
        false, false, false, false, 18, "Calibri", new ArgbColor(255, 0, 0, 0), TextAlignment.Left);
}
