using Shiny.Controls.Office.Spelling;
using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Text;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Controls.Office.Document;

/// <summary>Formatting of the text at the caret, for a toolbar to reflect.</summary>
public sealed record CaretFormat
{
    public static readonly CaretFormat Default = new();

    public string FontFamily { get; init; } = "Calibri";

    /// <summary>Font size in points, which is what a size picker shows.</summary>
    public double FontSize { get; init; } = 11;

    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool Strike { get; init; }
    public ArgbColor Color { get; init; } = new(255, 0, 0, 0);
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;
    public string? StyleName { get; init; }
}

public enum CaretMove
{
    Left,
    Right,
    Up,
    Down,
    LineStart,
    LineEnd,
    DocumentStart,
    DocumentEnd,
    WordLeft,
    WordRight
}

/// <summary>
/// Host-independent editing behaviour for a document: caret, selection, typing and formatting.
/// </summary>
/// <remarks>
/// Both hosts forward raw input here rather than implementing behaviour themselves. A host owns only
/// two things: painting, and giving the platform somewhere to send text — a hidden contenteditable on
/// the web, a hidden entry on MAUI.
/// </remarks>
public sealed class DocumentEditorController : DocumentController
{
    readonly WordDocument document;

    /// <summary>Remembered x for vertical movement, so up/down through short lines does not drift left.</summary>
    double? desiredX;

    /// <summary>
    /// The context the controller was created on - the UI thread on both hosts.
    /// </summary>
    /// <remarks>
    /// Captured rather than injected because the one thing every host has in common is that the
    /// control is constructed on the thread that is allowed to repaint it.
    /// </remarks>
    readonly SynchronizationContext? sync = SynchronizationContext.Current;

    CancellationTokenSource? spellCheckDebounce;

    public DocumentEditorController(WordDocument document, ITextMeasurer measurer, ISpellChecker? spellChecker = null)
        : base(document, measurer)
    {
        this.document = document;
        this.Spelling = new DocumentSpellCheck(spellChecker ?? SpellCheckers.Default);

        // A check finishes on whatever thread the platform answered on - a binder thread on Android -
        // and repainting is the host's reaction to Changed, so the hop back has to happen here rather
        // than in each host.
        this.Spelling.Updated += (_, _) => this.Post(this.RaiseChanged);
        this.Selection.Changed += (_, _) =>
        {
            this.RefreshCaretFormat();
            this.RaiseChanged();
        };

        document.ContentChanged += (_, _) =>
        {
            this.InvalidateLayout();
            this.RaiseChanged();
        };

        this.RefreshCaretFormat();
    }

    public DocumentSelection Selection { get; } = new();

    /// <summary>Spell checking over the document. Replace <c>Spelling.Checker</c> to override it.</summary>
    public DocumentSpellCheck Spelling { get; }

    /// <summary>
    /// The checker in use. Assigning one discards what the previous checker found and re-checks.
    /// </summary>
    /// <remarks>
    /// Cached results belong to the checker that produced them - a different dictionary, or a
    /// different language, disagrees about which words are wrong - so swapping one in has to clear
    /// the cache rather than leave the old squiggles on screen.
    /// </remarks>
    public ISpellChecker SpellChecker
    {
        get => this.Spelling.Checker;
        set
        {
            if (ReferenceEquals(this.Spelling.Checker, value))
                return;

            this.Spelling.Checker = value ?? NullSpellChecker.Instance;
            this.Spelling.Invalidate();
            this.ScheduleSpellCheck(0);
        }
    }

    /// <summary>Whether misspellings are checked and underlined at all.</summary>
    public bool IsSpellCheckEnabled
    {
        get => this.Spelling.IsEnabled;
        set
        {
            if (this.Spelling.IsEnabled == value)
                return;

            this.Spelling.IsEnabled = value;
            this.Spelling.Invalidate();

            if (value)
                this.ScheduleSpellCheck(0);
        }
    }

    protected override void OnViewportChanged() => this.ScheduleSpellCheck();

    /// <summary>Formatting at the caret. A toolbar binds to this to show what is active.</summary>
    public CaretFormat CaretFormat { get; private set; } = CaretFormat.Default;

    public bool CanUndo => this.document.Undo.CanUndo;
    public bool CanRedo => this.document.Undo.CanRedo;

    /// <summary>Raised when the caret moves, so a host can scroll it into view.</summary>
    public event EventHandler? CaretMoved;

    // ---- spelling ----

    /// <summary>
    /// Re-checks the paragraphs currently on screen.
    /// </summary>
    /// <remarks>
    /// Called by the host when the view settles rather than on every keystroke: a platform checker is
    /// an interop call (a service round trip on Android), and running one per character typed would
    /// both stutter and flag every half-finished word as the user types it.
    /// </remarks>
    public Task RefreshSpellingAsync(CancellationToken cancellationToken = default)
    {
        var (first, last) = this.VisibleBlockRange();
        return this.Spelling.RefreshAsync(this.Document.Blocks, first, last, cancellationToken);
    }

    /// <summary>
    /// Queues a re-check of the visible paragraphs, coalescing bursts of edits and scrolls.
    /// </summary>
    /// <remarks>
    /// Hosts call this freely - on every keystroke, every scroll - and the debounce is what makes that
    /// safe. The delay also serves the user: a word is only half-typed while they are typing it, and
    /// flagging it mid-word is noise.
    /// </remarks>
    public void ScheduleSpellCheck(int delayMilliseconds = 500)
    {
        if (!this.Spelling.IsEnabled || !this.Spelling.Checker.IsAvailable)
            return;

        var previous = this.spellCheckDebounce;
        var cts = new CancellationTokenSource();
        this.spellCheckDebounce = cts;

        previous?.Cancel();
        previous?.Dispose();

        _ = RunAsync(cts.Token);

        async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                await this.RefreshSpellingAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer request, which is the normal case while typing.
            }
        }
    }

    /// <summary>Runs an action on the thread the controller was created on.</summary>
    void Post(Action action)
    {
        if (this.sync is null || this.sync == SynchronizationContext.Current)
            action();
        else
            this.sync.Post(_ => action(), null);
    }

    (int First, int Last) VisibleBlockRange()
    {
        var blocks = this.Blocks;
        var top = this.Viewport.ScrollY;
        var bottom = top + this.Viewport.Height;

        var first = -1;
        var last = -1;

        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].Y + blocks[i].Height < top)
                continue;

            if (blocks[i].Y > bottom)
                break;

            if (first < 0)
                first = i;

            last = i;
        }

        return first < 0 ? (0, -1) : (first, last);
    }

    /// <summary>Underline rectangles for misspelled words, in document coordinates.</summary>
    public IEnumerable<GridRectLike> SpellingRects()
    {
        var blocks = this.Blocks;
        var (first, last) = this.VisibleBlockRange();

        for (var i = first; i <= last && i < blocks.Count; i++)
        {
            if (blocks[i] is not LaidOutParagraph paragraph)
                continue;

            if (this.Document.Blocks.ElementAtOrDefault(i) is not DocumentParagraph source)
                continue;

            foreach (var error in this.Spelling.ErrorsFor(i, source.PlainText))
            {
                // A misspelled word can wrap, so the underline is emitted per line rather than as one
                // rectangle spanning from start to end - which would otherwise stretch across the page.
                foreach (var line in paragraph.Lines)
                {
                    var from = Math.Max(error.Start, line.SourceOffset);
                    var to = Math.Min(error.End, line.SourceEnd);
                    if (to <= from)
                        continue;

                    var start = this.CaretRect(new DocumentPosition(i, from));
                    var end = this.CaretRect(new DocumentPosition(i, to));

                    yield return new GridRectLike(
                        start.X,
                        paragraph.Y + line.Y + line.Ascent + 2,
                        Math.Max(2, end.X - start.X),
                        3);
                }
            }
        }
    }

    /// <summary>The misspelling under a position, or null.</summary>
    public SpellingError? SpellingErrorAt(DocumentPosition position)
    {
        if (this.Document.Blocks.ElementAtOrDefault(position.Block) is not DocumentParagraph paragraph)
            return null;

        foreach (var error in this.Spelling.ErrorsFor(position.Block, paragraph.PlainText))
        {
            if (error.Contains(position.Offset) || error.End == position.Offset)
                return error;
        }

        return null;
    }

    /// <summary>Replacement candidates for the misspelling under a position.</summary>
    public async Task<IReadOnlyList<string>> SuggestAtAsync(DocumentPosition position, CancellationToken cancellationToken = default)
    {
        if (this.SpellingErrorAt(position) is not { } error)
            return [];

        return await this.Spelling.Checker.SuggestAsync(error.Word, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Replaces the misspelling under a position with a suggestion, as one undo step.</summary>
    public bool ApplySuggestion(DocumentPosition position, string replacement)
    {
        if (this.SpellingErrorAt(position) is not { } error || string.IsNullOrEmpty(replacement))
            return false;

        var start = new DocumentPosition(position.Block, error.Start);
        var end = new DocumentPosition(position.Block, error.End);

        using (this.document.Undo.BeginTransaction("Correct Spelling"))
        {
            this.document.Execute(new DeleteRangeCommand(new DocumentRange(start, end)));
            this.document.Execute(new InsertTextCommand(start, replacement));
        }

        this.Selection.MoveTo(start with { Offset = error.Start + replacement.Length });
        this.Spelling.InvalidateFrom(position.Block);
        this.AfterEdit();
        return true;
    }

    /// <summary>Stops reporting a word for the rest of the session.</summary>
    public void IgnoreSpelling(string word)
    {
        this.Spelling.Checker.Ignore(word);
        this.Spelling.Invalidate();
    }

    /// <summary>Adds a word to the user's dictionary, where the platform supports it.</summary>
    public void LearnSpelling(string word)
    {
        this.Spelling.Checker.Learn(word);
        this.Spelling.Invalidate();
    }

    // ---- text input ----

    /// <summary>Inserts text at the caret, replacing the selection first.</summary>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var at = this.DeleteSelectionIfAny();
        this.document.Execute(new InsertTextCommand(at, text));
        this.Selection.MoveTo(at with { Offset = at.Offset + text.Length });
        this.AfterEdit();
    }

    /// <summary>Enter: splits the paragraph at the caret.</summary>
    public void InsertParagraph()
    {
        var at = this.DeleteSelectionIfAny();
        this.document.Execute(new SplitParagraphCommand(at));
        this.Selection.MoveTo(new DocumentPosition(at.Block + 1, 0));
        this.AfterEdit();
    }

    /// <summary>Backspace.</summary>
    public void DeleteBackward()
    {
        if (!this.Selection.IsEmpty)
        {
            this.DeleteSelectionIfAny();
            this.AfterEdit();
            return;
        }

        var caret = this.Selection.Focus;

        if (caret.Offset > 0)
        {
            var from = caret with { Offset = caret.Offset - 1 };
            this.document.Execute(new DeleteRangeCommand(new DocumentRange(from, caret)));
            this.Selection.MoveTo(from);
            this.AfterEdit();
            return;
        }

        // At the very start of a paragraph, backspace joins it to the one above.
        if (caret.Block == 0)
            return;

        var previousLength = this.LengthOf(caret.Block - 1);
        this.document.Execute(new MergeParagraphCommand(caret.Block - 1));
        this.Selection.MoveTo(new DocumentPosition(caret.Block - 1, previousLength));
        this.AfterEdit();
    }

    /// <summary>Delete.</summary>
    public void DeleteForward()
    {
        if (!this.Selection.IsEmpty)
        {
            this.DeleteSelectionIfAny();
            this.AfterEdit();
            return;
        }

        var caret = this.Selection.Focus;
        var length = this.LengthOf(caret.Block);

        if (caret.Offset < length)
        {
            this.document.Execute(new DeleteRangeCommand(new DocumentRange(caret, caret with { Offset = caret.Offset + 1 })));
            this.AfterEdit();
            return;
        }

        if (caret.Block + 1 >= this.Document.Blocks.Count)
            return;

        this.document.Execute(new MergeParagraphCommand(caret.Block));
        this.AfterEdit();
    }

    DocumentPosition DeleteSelectionIfAny()
    {
        if (this.Selection.IsEmpty)
            return this.Selection.Focus;

        var range = this.Selection.Range;
        this.document.Execute(new DeleteRangeCommand(range));
        this.Selection.MoveTo(range.Start);
        return range.Start;
    }

    // ---- formatting ----

    public void ToggleBold() => this.ApplyRunFormat(RunFormatChange.Bold(!this.CaretFormat.Bold));

    public void ToggleItalic() => this.ApplyRunFormat(RunFormatChange.Italic(!this.CaretFormat.Italic));

    public void ToggleUnderline() => this.ApplyRunFormat(RunFormatChange.Underline(!this.CaretFormat.Underline));

    public void ToggleStrikethrough() => this.ApplyRunFormat(RunFormatChange.Strike(!this.CaretFormat.Strike));

    public void SetFontFamily(string family) => this.ApplyRunFormat(RunFormatChange.FontFamily(family));

    public void SetFontSize(double points) => this.ApplyRunFormat(RunFormatChange.FontSize(points));

    public void SetTextColor(ArgbColor color) => this.ApplyRunFormat(RunFormatChange.Color(color));

    public void SetAlignment(TextAlignment alignment)
        => this.ApplyParagraphFormat(ParagraphFormatChange.Alignment(alignment));

    public void SetParagraphStyle(string? styleId)
        => this.ApplyParagraphFormat(ParagraphFormatChange.Style(styleId));

    /// <summary>
    /// Applies run formatting to the selection.
    /// </summary>
    /// <remarks>
    /// With an empty selection the change is only reflected in <see cref="CaretFormat"/>. Word applies
    /// it to whatever is typed next; carrying that through the OOXML needs a pending-format concept the
    /// editor does not have yet, so the toolbar updates and the document does not.
    /// </remarks>
    void ApplyRunFormat(RunFormatChange change)
    {
        if (this.Selection.IsEmpty)
        {
            this.CaretFormat = Preview(this.CaretFormat, change);
            this.RaiseChanged();
            return;
        }

        this.document.Execute(new FormatRunsCommand(this.Selection.Range, change));
        this.AfterEdit();
    }

    void ApplyParagraphFormat(ParagraphFormatChange change)
    {
        this.document.Execute(new FormatParagraphsCommand(this.Selection.Range, change));
        this.AfterEdit();
    }

    /// <summary>Reflects a change in the caret format without touching the document.</summary>
    static CaretFormat Preview(CaretFormat format, RunFormatChange change) => change.Name switch
    {
        "Bold" => format with { Bold = true },
        "Remove Bold" => format with { Bold = false },
        "Italic" => format with { Italic = true },
        "Remove Italic" => format with { Italic = false },
        "Underline" => format with { Underline = true },
        "Remove Underline" => format with { Underline = false },
        "Strikethrough" => format with { Strike = true },
        "Remove Strikethrough" => format with { Strike = false },
        _ => format
    };

    public void Undo()
    {
        this.document.Undo.Undo();
        this.ClampSelection();
        this.AfterEdit();
    }

    public void Redo()
    {
        this.document.Undo.Redo();
        this.ClampSelection();
        this.AfterEdit();
    }

    // ---- caret movement ----

    public void Move(CaretMove move, bool extend = false)
    {
        var target = this.Resolve(move);

        if (extend)
            this.Selection.ExtendTo(target);
        else
            this.Selection.MoveTo(target);

        // Vertical movement keeps its column; anything else resets it.
        if (move is not (CaretMove.Up or CaretMove.Down))
            this.desiredX = null;

        this.ScrollCaretIntoView();
        this.CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    public void SelectAll()
    {
        var last = this.Document.Blocks.Count - 1;
        this.Selection.Select(DocumentPosition.Start, new DocumentPosition(last, this.LengthOf(last)));
    }

    DocumentPosition Resolve(CaretMove move)
    {
        var caret = this.Selection.Focus;
        var length = this.LengthOf(caret.Block);

        switch (move)
        {
            case CaretMove.Left:
                if (caret.Offset > 0)
                    return caret with { Offset = caret.Offset - 1 };

                return caret.Block > 0
                    ? new DocumentPosition(caret.Block - 1, this.LengthOf(caret.Block - 1))
                    : caret;

            case CaretMove.Right:
                if (caret.Offset < length)
                    return caret with { Offset = caret.Offset + 1 };

                return caret.Block + 1 < this.Document.Blocks.Count
                    ? new DocumentPosition(caret.Block + 1, 0)
                    : caret;

            case CaretMove.LineStart:
                return caret with { Offset = this.LineBoundsAt(caret).Start };

            case CaretMove.LineEnd:
                return caret with { Offset = this.LineBoundsAt(caret).End };

            case CaretMove.DocumentStart:
                return DocumentPosition.Start;

            case CaretMove.DocumentEnd:
                var last = this.Document.Blocks.Count - 1;
                return new DocumentPosition(last, this.LengthOf(last));

            case CaretMove.WordLeft:
                return this.WordBoundary(caret, forward: false);

            case CaretMove.WordRight:
                return this.WordBoundary(caret, forward: true);

            case CaretMove.Up:
            case CaretMove.Down:
                return this.VerticalMove(caret, move == CaretMove.Down);

            default:
                return caret;
        }
    }

    /// <summary>
    /// Moves the caret one visual line, not one paragraph.
    /// </summary>
    /// <remarks>
    /// A wrapped paragraph is many lines, so moving by block would jump past all of them. The column
    /// is remembered across consecutive vertical moves, which is what stops the caret drifting left
    /// when it passes through a short line.
    /// </remarks>
    DocumentPosition VerticalMove(DocumentPosition caret, bool down)
    {
        var caretRect = this.CaretRect(caret);
        this.desiredX ??= caretRect.X;

        var probeY = down
            ? caretRect.Y + caretRect.Height + 1
            : caretRect.Y - 1;

        var target = this.PositionAt(this.desiredX.Value, probeY);
        return target ?? caret;
    }

    DocumentPosition WordBoundary(DocumentPosition caret, bool forward)
    {
        var text = this.TextOf(caret.Block);
        var offset = caret.Offset;

        if (forward)
        {
            while (offset < text.Length && char.IsWhiteSpace(text[offset]))
                offset++;

            while (offset < text.Length && !char.IsWhiteSpace(text[offset]))
                offset++;

            return offset == caret.Offset && caret.Block + 1 < this.Document.Blocks.Count
                ? new DocumentPosition(caret.Block + 1, 0)
                : caret with { Offset = offset };
        }

        while (offset > 0 && char.IsWhiteSpace(text[offset - 1]))
            offset--;

        while (offset > 0 && !char.IsWhiteSpace(text[offset - 1]))
            offset--;

        return offset == caret.Offset && caret.Block > 0
            ? new DocumentPosition(caret.Block - 1, this.LengthOf(caret.Block - 1))
            : caret with { Offset = offset };
    }

    // ---- hit testing and geometry ----

    /// <summary>The document position under a point in viewport coordinates, or null when there is none.</summary>
    public DocumentPosition? PositionAt(double x, double y)
    {
        var documentX = x - this.PageX - this.PagePadding;
        var documentY = y + this.Viewport.ScrollY;

        LaidOutParagraph? best = null;
        var index = -1;
        var blocks = this.Blocks;

        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is not LaidOutParagraph block)
                continue;

            if (documentY >= block.Y && documentY <= block.Y + block.Height)
            {
                best = block;
                index = i;
                break;
            }

            // Remember the last block above the point, so clicking in the gap below the document
            // still lands somewhere sensible rather than nowhere.
            if (block.Y <= documentY)
            {
                best = block;
                index = i;
            }
        }

        if (best is null || index < 0)
            return null;

        var localY = documentY - best.Y;
        var line = best.Lines.FirstOrDefault(l => localY >= l.Y && localY <= l.Y + l.Height)
            ?? (localY < 0 ? best.Lines[0] : best.Lines[^1]);

        return new DocumentPosition(index, this.OffsetInLine(line, documentX - best.X));
    }

    /// <summary>Finds the character boundary nearest an x within a line.</summary>
    int OffsetInLine(LaidOutLine line, double x)
    {
        if (line.Runs.Count == 0)
            return line.SourceOffset;

        foreach (var run in line.Runs)
        {
            if (x > run.X + run.Width)
                continue;

            if (x < run.X)
                return run.SourceOffset;

            // Walk the run's characters and take the boundary the click is closest to, so clicking the
            // right half of a glyph puts the caret after it rather than before.
            var local = 0;
            var previous = 0d;
            for (var i = 1; i <= run.Text.Length; i++)
            {
                var width = this.Measurer.Measure(run.Text.AsSpan(0, i), run.Style).Width;
                if (run.X + width >= x)
                {
                    local = x - (run.X + previous) < (run.X + width) - x ? i - 1 : i;
                    break;
                }

                previous = width;
                local = i;
            }

            return run.SourceOffset + local;
        }

        return line.SourceEnd;
    }

    /// <summary>The caret's rectangle in document coordinates.</summary>
    public GridRectLike CaretRect(DocumentPosition position)
    {
        var blocks = this.Blocks;
        if (position.Block < 0 || position.Block >= blocks.Count || blocks[position.Block] is not LaidOutParagraph paragraph)
            return new GridRectLike(0, 0, 1, 16);

        var line = paragraph.Lines.LastOrDefault(l => position.Offset >= l.SourceOffset) ?? paragraph.Lines[0];
        var x = paragraph.X;

        foreach (var run in line.Runs)
        {
            if (position.Offset < run.SourceOffset)
                break;

            var local = Math.Min(position.Offset - run.SourceOffset, run.Text.Length);
            x = paragraph.X + run.X + this.Measurer.Measure(run.Text.AsSpan(0, local), run.Style).Width;
        }

        if (line.Runs.Count == 0)
            x = paragraph.X;

        return new GridRectLike(x, paragraph.Y + line.Y, 1.5, line.Height);
    }

    (int Start, int End) LineBoundsAt(DocumentPosition position)
    {
        if (this.Blocks.ElementAtOrDefault(position.Block) is not LaidOutParagraph paragraph)
            return (0, 0);

        var line = paragraph.Lines.LastOrDefault(l => position.Offset >= l.SourceOffset) ?? paragraph.Lines[0];
        return (line.SourceOffset, line.SourceEnd);
    }

    /// <summary>Rectangles covering the selection, for the painter to fill.</summary>
    public IEnumerable<GridRectLike> SelectionRects()
    {
        var range = this.Selection.Range;
        if (range.IsEmpty)
            yield break;

        for (var block = range.Start.Block; block <= range.End.Block; block++)
        {
            if (this.Blocks.ElementAtOrDefault(block) is not LaidOutParagraph paragraph)
                continue;

            var from = block == range.Start.Block ? range.Start.Offset : 0;
            var to = block == range.End.Block ? range.End.Offset : int.MaxValue;

            foreach (var line in paragraph.Lines)
            {
                var lineFrom = Math.Max(from, line.SourceOffset);
                var lineTo = Math.Min(to, line.SourceEnd);
                if (lineTo <= lineFrom)
                    continue;

                var start = this.CaretRect(new DocumentPosition(block, lineFrom));
                var end = this.CaretRect(new DocumentPosition(block, lineTo));

                yield return new GridRectLike(start.X, paragraph.Y + line.Y, Math.Max(2, end.X - start.X), line.Height);
            }
        }
    }

    void ScrollCaretIntoView()
    {
        var caret = this.CaretRect(this.Selection.Focus);
        var top = this.Viewport.ScrollY;
        var bottom = top + this.Viewport.Height;

        if (caret.Y < top)
            this.ScrollTo(caret.Y - 8);
        else if (caret.Y + caret.Height > bottom)
            this.ScrollTo(caret.Y + caret.Height - this.Viewport.Height + 8);
    }

    // ---- state ----

    string TextOf(int block)
        => this.Document.Blocks.ElementAtOrDefault(block) is DocumentParagraph paragraph ? paragraph.PlainText : string.Empty;

    int LengthOf(int block) => this.TextOf(block).Length;

    void ClampSelection()
    {
        var blocks = this.Document.Blocks.Count;
        if (blocks == 0)
            return;

        var block = Math.Clamp(this.Selection.Focus.Block, 0, blocks - 1);
        var offset = Math.Clamp(this.Selection.Focus.Offset, 0, this.LengthOf(block));
        this.Selection.MoveTo(new DocumentPosition(block, offset));
    }

    void AfterEdit()
    {
        // Paragraph indices below an edit can shift, so their cached spelling no longer belongs to the
        // text it was computed from.
        this.Spelling.InvalidateFrom(this.Selection.Focus.Block);
        this.ScheduleSpellCheck();

        this.InvalidateLayout();
        this.RefreshCaretFormat();
        this.ScrollCaretIntoView();
        this.RaiseChanged();
    }

    /// <summary>Reads the formatting under the caret so a toolbar can show what is active.</summary>
    void RefreshCaretFormat()
    {
        if (this.Document.Blocks.ElementAtOrDefault(this.Selection.Focus.Block) is not DocumentParagraph paragraph)
            return;

        // The character *before* the caret is what Word reports, so typing continues the run the caret
        // just left rather than the one it is about to enter.
        var offset = Math.Max(0, this.Selection.Focus.Offset - 1);
        var cursor = 0;
        var style = paragraph.Runs.Count > 0 ? paragraph.Runs[0].Style : TextStyle.Default;

        foreach (var run in paragraph.Runs)
        {
            if (run.IsBreak)
                continue;

            if (offset < cursor + run.Text.Length || run.Text.Length == 0)
            {
                style = run.Style;
                break;
            }

            cursor += run.Text.Length;
            style = run.Style;
        }

        this.CaretFormat = new CaretFormat
        {
            FontFamily = style.FontFamily,
            FontSize = OoxmlUnits.PixelsToPointsApprox(style.FontSize),
            Bold = style.Bold,
            Italic = style.Italic,
            Underline = style.Underline != UnderlineStyle.None,
            Strike = style.Strike,
            Color = style.Color,
            Alignment = paragraph.Format.Alignment,
            StyleName = paragraph.StyleName
        };
    }
}

/// <summary>A rectangle in document coordinates. Named to avoid clashing with the spreadsheet's.</summary>
public readonly record struct GridRectLike(double X, double Y, double Width, double Height)
{
    public double Right => this.X + this.Width;
    public double Bottom => this.Y + this.Height;
}
