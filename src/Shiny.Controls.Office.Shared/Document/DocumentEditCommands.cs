using DocumentFormat.OpenXml.Wordprocessing;
using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Base for document edits.
/// </summary>
/// <remarks>
/// Every command captures whatever it needs to reverse itself while it runs, exactly as the
/// spreadsheet's do — most edits cannot describe their inverse until they see what was there.
/// </remarks>
public abstract record DocumentCommand : IEditCommand<WordDocument>
{
    public abstract string Name { get; }

    public abstract IEditCommand<WordDocument> Apply(WordDocument context);
}

/// <summary>Inserts text at a position.</summary>
public sealed record InsertTextCommand(DocumentPosition At, string Text) : DocumentCommand
{
    public override string Name => "Typing";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var paragraph = context.ParagraphElementAt(this.At.Block);
        if (paragraph is null)
            return new NoOpCommand();

        WordParagraphEditor.Insert(paragraph, this.At.Offset, this.Text);
        context.Reproject(this.At.Block);

        return new DeleteRangeCommand(new DocumentRange(
            this.At,
            this.At with { Offset = this.At.Offset + this.Text.Length }));
    }
}

/// <summary>Deletes a range, which may span paragraphs.</summary>
public sealed record DeleteRangeCommand(DocumentRange Range) : DocumentCommand
{
    public override string Name => "Delete";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var range = this.Range;
        if (range.IsEmpty)
            return new NoOpCommand();

        // The whole affected span is captured before anything moves, because after the edit the
        // paragraphs it came from may no longer exist.
        var restore = context.CaptureRange(range);

        if (range.IsWithinOneBlock)
        {
            var paragraph = context.ParagraphElementAt(range.Start.Block);
            if (paragraph is null)
                return new NoOpCommand();

            WordParagraphEditor.Delete(paragraph, range.Start.Offset, range.End.Offset);
            context.Reproject(range.Start.Block);
            return restore;
        }

        var first = context.ParagraphElementAt(range.Start.Block);
        var last = context.ParagraphElementAt(range.End.Block);
        if (first is null || last is null)
            return new NoOpCommand();

        // Trim both ends, drop everything between, then join the survivors.
        WordParagraphEditor.Delete(first, range.Start.Offset, WordParagraphEditor.LengthOf(first));
        WordParagraphEditor.Delete(last, 0, range.End.Offset);

        for (var block = range.End.Block - 1; block > range.Start.Block; block--)
            context.RemoveBlock(block);

        WordParagraphEditor.Merge(first, last);
        context.RemoveBlockAfter(range.Start.Block);
        context.Reproject(range.Start.Block);

        // The span the snapshot came from was several paragraphs; the edit collapsed it to one. Undo
        // has to replace what is there *now*, not what was captured, or it removes a paragraph that
        // was never part of the edit.
        return restore with { RemovedCount = 1 };
    }
}

/// <summary>Splits a paragraph in two — what Enter does.</summary>
public sealed record SplitParagraphCommand(DocumentPosition At) : DocumentCommand
{
    public override string Name => "New Paragraph";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var paragraph = context.ParagraphElementAt(this.At.Block);
        if (paragraph is null)
            return new NoOpCommand();

        var tail = WordParagraphEditor.Split(paragraph, this.At.Offset);
        context.InsertBlockAfter(this.At.Block, tail);
        context.Reproject(this.At.Block);

        return new MergeParagraphCommand(this.At.Block);
    }
}

/// <summary>Joins a paragraph with the one after it — what Backspace at offset zero does.</summary>
public sealed record MergeParagraphCommand(int Block) : DocumentCommand
{
    public override string Name => "Merge Paragraphs";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var first = context.ParagraphElementAt(this.Block);
        var second = context.ParagraphElementAt(this.Block + 1);
        if (first is null || second is null)
            return new NoOpCommand();

        var joinAt = new DocumentPosition(this.Block, WordParagraphEditor.LengthOf(first));

        WordParagraphEditor.Merge(first, second);
        context.RemoveBlockAfter(this.Block);
        context.Reproject(this.Block);

        return new SplitParagraphCommand(joinAt);
    }
}

/// <summary>Applies run-level formatting to a range.</summary>
public sealed record FormatRunsCommand(DocumentRange Range, RunFormatChange Change) : DocumentCommand
{
    public override string Name => this.Change.Name;

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        if (this.Range.IsEmpty)
            return new NoOpCommand();

        // Formatting is reversed by restoring the affected paragraphs wholesale. Inverting a property
        // change per run would need the prior value of every run the range touches, and runs get split
        // and merged by the change itself.
        var restore = context.CaptureRange(this.Range);

        for (var block = this.Range.Start.Block; block <= this.Range.End.Block; block++)
        {
            var paragraph = context.ParagraphElementAt(block);
            if (paragraph is null)
                continue;

            var length = WordParagraphEditor.LengthOf(paragraph);
            var from = block == this.Range.Start.Block ? this.Range.Start.Offset : 0;
            var to = block == this.Range.End.Block ? this.Range.End.Offset : length;

            WordParagraphEditor.Format(paragraph, from, Math.Min(to, length), this.Change.Apply);
            context.Reproject(block);
        }

        return restore;
    }
}

/// <summary>A named run-property mutation, so a command can describe itself in the undo menu.</summary>
public sealed record RunFormatChange(string Name, Action<RunProperties> Apply)
{
    public static RunFormatChange Bold(bool on) => new(on ? "Bold" : "Remove Bold", WordParagraphEditor.ToggleBold(on));
    public static RunFormatChange Italic(bool on) => new(on ? "Italic" : "Remove Italic", WordParagraphEditor.ToggleItalic(on));
    public static RunFormatChange Underline(bool on) => new(on ? "Underline" : "Remove Underline", WordParagraphEditor.ToggleUnderline(on));
    public static RunFormatChange Strike(bool on) => new(on ? "Strikethrough" : "Remove Strikethrough", WordParagraphEditor.ToggleStrike(on));
    public static RunFormatChange FontFamily(string family) => new("Font", WordParagraphEditor.SetFontFamily(family));
    public static RunFormatChange FontSize(double points) => new("Font Size", WordParagraphEditor.SetFontSize(points));
    public static RunFormatChange Color(ArgbColor color) => new("Text Colour", WordParagraphEditor.SetColor(color));
}

/// <summary>Applies paragraph-level formatting to every paragraph a range touches.</summary>
public sealed record FormatParagraphsCommand(DocumentRange Range, ParagraphFormatChange Change) : DocumentCommand
{
    public override string Name => this.Change.Name;

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var restore = context.CaptureRange(this.Range with { End = this.Range.End });

        for (var block = this.Range.Start.Block; block <= this.Range.End.Block; block++)
        {
            var paragraph = context.ParagraphElementAt(block);
            if (paragraph is null)
                continue;

            WordParagraphEditor.FormatParagraph(paragraph, this.Change.Apply);
            context.Reproject(block);
        }

        return restore;
    }
}

public sealed record ParagraphFormatChange(string Name, Action<ParagraphProperties> Apply)
{
    public static ParagraphFormatChange Alignment(TextAlignment alignment)
        => new("Alignment", WordParagraphEditor.SetAlignment(alignment));

    public static ParagraphFormatChange Style(string? styleId)
        => new("Paragraph Style", WordParagraphEditor.SetStyle(styleId));
}

/// <summary>
/// Restores a span of paragraphs to a previously captured state.
/// </summary>
/// <remarks>
/// The general-purpose inverse. Cloned XML is kept rather than a description of the change, because
/// most edits split, merge or delete runs and there is no property-level undo that survives that.
/// </remarks>
public sealed record RestoreBlocksCommand(int Start, int RemovedCount, IReadOnlyList<Paragraph> Snapshot) : DocumentCommand
{
    public override string Name => "Undo";

    public override IEditCommand<WordDocument> Apply(WordDocument context)
    {
        var inverse = context.CaptureBlocks(this.Start, this.RemovedCount);
        context.ReplaceBlocks(this.Start, this.RemovedCount, this.Snapshot);

        // Redo must remove however many paragraphs this restore just put in.
        return inverse with { RemovedCount = this.Snapshot.Count };
    }
}

/// <summary>Does nothing, and undoes to nothing. Returned when a command finds no work to do.</summary>
public sealed record NoOpCommand : DocumentCommand
{
    public override string Name => "No Change";

    public override IEditCommand<WordDocument> Apply(WordDocument context) => this;
}
