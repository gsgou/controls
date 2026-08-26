using Shiny.Controls.Office.Shapes;
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

    /// <summary>The highlight behind the caret, or null when the text is not highlighted.</summary>
    public ArgbColor? Highlight { get; init; }

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

    /// <summary>
    /// Formatting chosen with nothing selected, waiting for text to apply itself to.
    /// </summary>
    /// <remarks>
    /// This is what Word does and what every editor is expected to do: put the caret somewhere, pick
    /// 24pt, type, and what you type is 24pt. Without it a format chosen at a bare caret has no range
    /// to act on, so the click does nothing at all — which reads as a broken toolbar rather than as a
    /// missing feature.
    /// </remarks>
    readonly List<RunFormatChange> pending = new();

    /// <summary>Where <see cref="pending"/> was chosen. Moving the caret off it abandons the choice.</summary>
    DocumentPosition? pendingAt;

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
            this.DropPendingIfCaretMoved();
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
        var end = at with { Offset = at.Offset + text.Length };

        if (this.pending.Count == 0)
        {
            // The common path stays a single command. A transaction closes as a composite, which ends
            // the coalescing run - so wrapping every keystroke in one would make each character its
            // own undo step.
            this.document.Execute(new InsertTextCommand(at, text));
        }
        else
        {
            // Taken and cleared before anything runs: applying the format moves through the document,
            // and a half-applied queue left in place would be applied again by the next keystroke.
            var formats = this.pending.ToList();
            this.ClearPending();

            // One undo step. A Ctrl+Z that took the characters away but left the formatting behind
            // would leave the caret carrying a format nobody chose.
            using (this.document.Undo.BeginTransaction("Typing"))
            {
                this.document.Execute(new InsertTextCommand(at, text));

                foreach (var change in formats)
                    this.document.Execute(new FormatRunsCommand(new DocumentRange(at, end), change));
            }
        }

        this.Selection.MoveTo(end);
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

    /// <summary>Highlights the selection, or clears it when passed null.</summary>
    public void SetHighlight(ArgbColor? color) => this.ApplyRunFormat(RunFormatChange.Highlight(color));

    /// <summary>Highlights with <paramref name="color"/>, or clears when that colour is already on.</summary>
    /// <remarks>
    /// What the toolbar button does, as opposed to what the swatch gallery does: pressing the
    /// highlight button on already-yellow text is a request to remove it, not to re-apply it.
    /// </remarks>
    public void ToggleHighlight(ArgbColor color)
        => this.SetHighlight(this.CaretFormat.Highlight == color ? null : color);

    public void SetAlignment(TextAlignment alignment)
        => this.ApplyParagraphFormat(ParagraphFormatChange.Alignment(alignment));

    public void SetParagraphStyle(string? styleId)
        => this.ApplyParagraphFormat(ParagraphFormatChange.Style(styleId));

    /// <summary>
    /// Applies run formatting to the selection, or holds it for the next thing typed.
    /// </summary>
    /// <remarks>
    /// With something selected this is immediate. With a bare caret there is no range to format, so the
    /// change is held in <see cref="pending"/> and applied by the next <see cref="InsertText"/> — which
    /// is what Word does. The toolbar shows it in the meantime, so the choice is visible before there
    /// is any text carrying it.
    /// </remarks>
    void ApplyRunFormat(RunFormatChange change)
    {
        if (this.Selection.IsEmpty)
        {
            this.RememberPending(change);
            this.RefreshCaretFormat();
            this.RaiseChanged();
            return;
        }

        this.ClearPending();
        this.document.Execute(new FormatRunsCommand(this.Selection.Range, change));
        this.AfterEdit();
    }

    /// <summary>Queues a change for the next insertion, replacing any earlier one of the same kind.</summary>
    void RememberPending(RunFormatChange change)
    {
        this.pendingAt = this.Selection.Focus;

        // Kind rather than Name: "Bold" and "Remove Bold" are the same attribute, and the second has to
        // replace the first rather than being applied after it.
        if (change.Kind != RunFormatKind.Other)
            this.pending.RemoveAll(x => x.Kind == change.Kind);

        this.pending.Add(change);
    }

    void ClearPending()
    {
        this.pending.Clear();
        this.pendingAt = null;
    }

    /// <summary>
    /// Abandons a pending format when the caret is no longer where it was chosen.
    /// </summary>
    /// <remarks>
    /// Anything else would be a trap: pick a colour, change your mind and click elsewhere, and the next
    /// thing typed - somewhere unrelated, possibly much later - would come out in that colour.
    /// </remarks>
    void DropPendingIfCaretMoved()
    {
        if (this.pending.Count == 0)
            return;

        if (this.Selection.IsEmpty && this.pendingAt == this.Selection.Focus)
            return;

        this.ClearPending();
    }

    void ApplyParagraphFormat(ParagraphFormatChange change)
    {
        this.document.Execute(new FormatParagraphsCommand(this.Selection.Range, change));
        this.AfterEdit();
    }

    // ---- inserting objects ----

    /// <summary>
    /// Inserts a preset-geometry shape at the caret.
    /// </summary>
    /// <remarks>
    /// Inline, in the text flow. The document view is a reflow engine with no float layer, so a shape
    /// behaves like a very large character: it wraps with the line it is on and moves as text is typed
    /// before it. That is a real limitation next to Word's anchored shapes, and it is also the only
    /// honest thing to draw in a view that has no fixed page positions to anchor against.
    /// </remarks>
    public void InsertShape(
        ShapeGeometry geometry,
        double width = 160,
        double height = 120,
        ArgbColor? fill = null,
        ArgbColor? outline = null,
        string? text = null)
    {
        if (this.IsReadOnlyDocument)
            return;

        var run = WordContentFactory.Shape(
            this.document.NextDrawingId(),
            geometry,
            width,
            height,
            fill ?? new ArgbColor(255, 0x44, 0x72, 0xC4),
            outline,
            text);

        this.InsertObjectRun(run);
    }

    /// <summary>
    /// Inserts a picture at the caret, adding its bytes to the package.
    /// </summary>
    /// <param name="data">The encoded image, in whatever format <paramref name="contentType"/> names.</param>
    /// <param name="contentType">The MIME type, e.g. <c>image/png</c>.</param>
    /// <param name="width">Display width in pixels. Null keeps the aspect ratio against <paramref name="height"/>.</param>
    /// <param name="height">Display height in pixels. Null keeps the aspect ratio against <paramref name="width"/>.</param>
    /// <remarks>
    /// The bytes go in as they arrived — never re-encoded. A drop of a 12-megapixel JPEG produces a
    /// large document, and the alternative is deciding on the user's behalf what quality their picture
    /// should be, which is not a renderer's call to make.
    /// </remarks>
    public void InsertImage(
        byte[] data,
        string contentType,
        double? width = null,
        double? height = null,
        string name = "Picture")
    {
        ArgumentNullException.ThrowIfNull(data);

        if (this.IsReadOnlyDocument || data.Length == 0)
            return;

        if (this.document.AddImagePart(data, contentType) is not { } relationshipId)
            return;

        var (w, h) = ResolveSize(width, height);

        this.InsertObjectRun(WordContentFactory.Picture(
            this.document.NextDrawingId(), relationshipId, w, h, name));
    }

    /// <summary>
    /// Inserts an empty table after the block the caret is in.
    /// </summary>
    /// <remarks>
    /// After the paragraph rather than inside it: a table is a block, and OOXML has no way to put one
    /// in the middle of a paragraph. Word splits the paragraph in that case; this places the table on
    /// the boundary below, which loses nothing and never divides a sentence the user was in the middle
    /// of writing.
    /// </remarks>
    public void InsertTable(int rows = 3, int columns = 3)
    {
        if (this.IsReadOnlyDocument)
            return;

        var block = Math.Clamp(this.Selection.Focus.Block, 0, Math.Max(0, this.Document.Blocks.Count - 1));

        this.document.Execute(new InsertTableCommand(block, rows, columns));

        // Into the first cell, which is where someone who just made a table wants to be typing.
        this.Selection.MoveTo(new DocumentPosition(block + 1, 0));
        this.AfterEdit();
    }

    void InsertObjectRun(DocumentFormat.OpenXml.Wordprocessing.Run run)
    {
        // An insertion replaces the selection, exactly as typing a character does.
        if (!this.Selection.Range.IsEmpty)
            this.document.Execute(new DeleteRangeCommand(this.Selection.Range));

        var at = this.Selection.Range.Start;

        this.document.Execute(new InsertInlineObjectCommand(at, run));
        this.Selection.MoveTo(at with { Offset = at.Offset + 1 });
        this.AfterEdit();
    }

    /// <summary>Fills in whichever of width and height was not given, keeping a 4:3 default.</summary>
    static (double Width, double Height) ResolveSize(double? width, double? height) => (width, height) switch
    {
        ({ } w, { } h) => (Math.Max(1, w), Math.Max(1, h)),
        ({ } w, null) => (Math.Max(1, w), Math.Max(1, w * 0.75)),
        (null, { } h) => (Math.Max(1, h * 4 / 3), Math.Max(1, h)),
        _ => (240d, 180d)
    };

    bool IsReadOnlyDocument => !this.document.IsEditable;

    // ---- inline object selection, drag and resize ----

    DocumentPosition? selectedObject;
    ShapeHandle dragging = ShapeHandle.None;
    double dragOriginX;
    double dragOriginY;
    double dragStartWidth;
    double dragStartHeight;

    /// <summary>Size of an object's resize handle, in document pixels.</summary>
    public double HandleSize { get; set; } = 8;

    /// <summary>The position of the selected inline object, or null when none is selected.</summary>
    public DocumentPosition? SelectedObject => this.selectedObject;

    /// <summary>The selected inline object itself, or null.</summary>
    public InlineObject? SelectedInline
        => this.selectedObject is { } at && this.Document.Blocks.ElementAtOrDefault(at.Block) is DocumentParagraph paragraph
            ? InlineAt(paragraph, at.Offset)
            : null;

    /// <summary>The inline object at an offset within a projected paragraph, or null.</summary>
    static InlineObject? InlineAt(DocumentParagraph paragraph, int offset)
    {
        var cursor = 0;

        foreach (var run in paragraph.Runs)
        {
            if (run.IsBreak)
                continue;

            var length = run.Inline is null ? run.Text.Length : 1;

            if (run.Inline is { } inline && offset >= cursor && offset < cursor + length)
                return inline;

            cursor += length;
        }

        return null;
    }

    public bool IsDraggingObject => this.dragging != ShapeHandle.None;

    /// <summary>Selects an inline object, or clears the selection when passed null.</summary>
    public void SelectObject(DocumentPosition? at)
    {
        if (this.selectedObject == at)
            return;

        this.selectedObject = at;

        // A selected object and a text caret are alternatives, not companions: leaving the caret
        // blinking somewhere else while an image is framed reads as two selections at once.
        if (at is { } position)
            this.Selection.MoveTo(position);

        this.CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    public void ClearObjectSelection() => this.SelectObject(null);

    /// <summary>The inline object under a point in viewport coordinates, or null.</summary>
    public DocumentPosition? ObjectAt(double x, double y)
    {
        var documentX = x - this.PageX - this.PagePadding;
        var documentY = y + this.Viewport.ScrollY;
        var blocks = this.Blocks;

        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is not LaidOutParagraph paragraph)
                continue;

            if (documentY < paragraph.Y || documentY > paragraph.Y + paragraph.Height)
                continue;

            foreach (var line in paragraph.Lines)
            {
                foreach (var run in line.Runs)
                {
                    if (run.Inline is null)
                        continue;

                    var rect = RunBounds(paragraph, line, run);
                    if (rect.Contains(documentX, documentY))
                        return new DocumentPosition(i, run.SourceOffset);
                }
            }
        }

        return null;
    }

    /// <summary>The selected object's rectangle in document coordinates, or null.</summary>
    public GridRectLike? SelectedObjectBounds()
    {
        if (this.selectedObject is not { } at)
            return null;

        if (this.Blocks.ElementAtOrDefault(at.Block) is not LaidOutParagraph paragraph)
            return null;

        foreach (var line in paragraph.Lines)
        {
            foreach (var run in line.Runs)
            {
                if (run.Inline is not null && run.SourceOffset == at.Offset)
                    return RunBounds(paragraph, line, run);
            }
        }

        return null;
    }

    /// <summary>
    /// The eight resize handles around the selected object, in document coordinates.
    /// </summary>
    /// <remarks>
    /// Emitted even for an object too small to hold them, for the same reason the slide editor does:
    /// a handle a user cannot grab is an object they cannot resize back.
    /// </remarks>
    public IEnumerable<(ShapeHandle Handle, GridRectLike Rect)> SelectedObjectHandles()
    {
        if (this.SelectedObjectBounds() is not { } bounds)
            yield break;

        var half = this.HandleSize / 2;

        GridRectLike At(double x, double y)
            => new(x - half, y - half, this.HandleSize, this.HandleSize);

        yield return (ShapeHandle.TopLeft, At(bounds.X, bounds.Y));
        yield return (ShapeHandle.Top, At(bounds.X + (bounds.Width / 2), bounds.Y));
        yield return (ShapeHandle.TopRight, At(bounds.Right, bounds.Y));
        yield return (ShapeHandle.Right, At(bounds.Right, bounds.Y + (bounds.Height / 2)));
        yield return (ShapeHandle.BottomRight, At(bounds.Right, bounds.Bottom));
        yield return (ShapeHandle.Bottom, At(bounds.X + (bounds.Width / 2), bounds.Bottom));
        yield return (ShapeHandle.BottomLeft, At(bounds.X, bounds.Bottom));
        yield return (ShapeHandle.Left, At(bounds.X, bounds.Y + (bounds.Height / 2)));
    }

    /// <summary>The handle under a point in viewport coordinates.</summary>
    public ShapeHandle HandleAt(double x, double y)
    {
        var documentX = x - this.PageX - this.PagePadding;
        var documentY = y + this.Viewport.ScrollY;

        foreach (var (handle, rect) in this.SelectedObjectHandles())
        {
            if (rect.Contains(documentX, documentY))
                return handle;
        }

        return ShapeHandle.None;
    }

    /// <summary>
    /// Starts a pointer interaction on an inline object, returning true when one was taken.
    /// </summary>
    /// <remarks>
    /// Returning false is the signal that the gesture belongs to the text caret instead, which is what
    /// lets a host call this first and fall through to ordinary click-and-drag selection without
    /// having to know anything about objects.
    /// </remarks>
    public bool BeginObjectDrag(double x, double y)
    {
        if (this.IsReadOnlyDocument)
            return false;

        // A handle on the current selection takes priority over whatever is underneath it: the
        // handles sit on the object's own edge, so the two always overlap.
        var handle = this.HandleAt(x, y);

        if (handle == ShapeHandle.None)
        {
            if (this.ObjectAt(x, y) is not { } hit)
            {
                this.ClearObjectSelection();
                return false;
            }

            this.SelectObject(hit);

            // Selecting is the whole gesture; a drag only starts from a handle, because an inline
            // object has no free position to be dragged to.
            return true;
        }

        if (this.SelectedInline is not { } inline)
            return false;

        this.dragging = handle;
        this.dragOriginX = x;
        this.dragOriginY = y;
        this.dragStartWidth = inline.Width;
        this.dragStartHeight = inline.Height;

        return true;
    }

    /// <summary>Extends a resize drag.</summary>
    /// <remarks>
    /// The size is always computed from where the drag <em>started</em> rather than from the previous
    /// move, so rounding cannot accumulate across a long drag and the object cannot creep.
    /// </remarks>
    public void DragObject(double x, double y)
    {
        if (this.dragging == ShapeHandle.None || this.selectedObject is not { } at)
            return;

        var dx = x - this.dragOriginX;
        var dy = y - this.dragOriginY;

        var width = this.dragStartWidth;
        var height = this.dragStartHeight;

        switch (this.dragging)
        {
            case ShapeHandle.Right or ShapeHandle.TopRight or ShapeHandle.BottomRight:
                width += dx;
                break;

            case ShapeHandle.Left or ShapeHandle.TopLeft or ShapeHandle.BottomLeft:
                width -= dx;
                break;
        }

        switch (this.dragging)
        {
            case ShapeHandle.Bottom or ShapeHandle.BottomLeft or ShapeHandle.BottomRight:
                height += dy;
                break;

            case ShapeHandle.Top or ShapeHandle.TopLeft or ShapeHandle.TopRight:
                height -= dy;
                break;
        }

        // A corner keeps the aspect ratio, which is what stops a dragged picture from being squashed.
        // An edge handle is the deliberate way to change one dimension alone.
        if (this.dragging is ShapeHandle.TopLeft or ShapeHandle.TopRight or ShapeHandle.BottomLeft or ShapeHandle.BottomRight
            && this.dragStartHeight > 0)
        {
            height = width * (this.dragStartHeight / this.dragStartWidth);
        }

        this.document.Execute(new ResizeInlineObjectCommand(
            at,
            Math.Max(MinimumObjectSize, width),
            Math.Max(MinimumObjectSize, height)));

        this.AfterEdit();
    }

    public void EndObjectDrag() => this.dragging = ShapeHandle.None;

    /// <summary>Removes the selected inline object.</summary>
    public void DeleteSelectedObject()
    {
        if (this.IsReadOnlyDocument || this.selectedObject is not { } at)
            return;

        this.document.Execute(new DeleteRangeCommand(new DocumentRange(at, at with { Offset = at.Offset + 1 })));
        this.ClearObjectSelection();
        this.Selection.MoveTo(at);
        this.AfterEdit();
    }

    /// <summary>How small a drag is allowed to make an object before it stops shrinking.</summary>
    /// <remarks>
    /// Not zero, and not one: an object dragged to nothing has no handles left to drag it back out by,
    /// so the floor is the size of a handle.
    /// </remarks>
    const double MinimumObjectSize = 12;

    /// <summary>A laid-out run's box in document coordinates.</summary>
    /// <remarks>
    /// An inline object sits on the baseline and runs upward from it, which is where the layout engine
    /// reserved its space and where the painter draws it.
    /// </remarks>
    static GridRectLike RunBounds(LaidOutParagraph paragraph, LaidOutLine line, LaidOutRun run)
    {
        var baseline = paragraph.Y + line.Y + line.Ascent;
        var height = run.Inline?.Height ?? run.Height;

        return new GridRectLike(paragraph.X + run.X, baseline - height, run.Width, height);
    }

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

    /// <summary>
    /// Selects the word under <paramref name="position"/> — what a double-click does.
    /// </summary>
    /// <remarks>
    /// The gesture matters more than it looks. Every formatting command needs a range to act on, so
    /// without a way to select a word by pointing at it the only route to bolding one is a careful
    /// drag from one edge of it to the other. Selecting a word is the ordinary way to say which word.
    /// </remarks>
    public void SelectWordAt(DocumentPosition position)
    {
        var range = this.WordRangeAt(position);
        this.Selection.Select(range.Start, range.End);
    }

    /// <summary>Selects the whole paragraph — what a triple-click does.</summary>
    public void SelectParagraphAt(DocumentPosition position)
        => this.Selection.Select(
            position with { Offset = 0 },
            position with { Offset = this.LengthOf(position.Block) });

    /// <summary>The span of the word at <paramref name="position"/>, without changing the selection.</summary>
    public DocumentRange WordRangeAt(DocumentPosition position)
    {
        var text = this.TextOf(position.Block);
        if (text.Length == 0)
            return new DocumentRange(position, position);

        var (start, end) = WordBoundaries.RangeAt(text, position.Offset);
        return new DocumentRange(position with { Offset = start }, position with { Offset = end });
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

            // An object is one character and cannot be clicked into, only on one side or the other.
            if (run.Inline is not null)
                return run.SourceOffset + (x > run.X + (run.Width / 2) ? 1 : 0);

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

            // An inline object has no text to measure through: the caret is either at its left edge
            // or, once the offset has passed it, at its right.
            if (run.Inline is not null)
            {
                x = paragraph.X + run.X + (position.Offset > run.SourceOffset ? run.Width : 0);
                continue;
            }

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

        var format = new CaretFormat
        {
            FontFamily = style.FontFamily,
            FontSize = OoxmlUnits.PixelsToPointsApprox(style.FontSize),
            Bold = style.Bold,
            Italic = style.Italic,
            Underline = style.Underline != UnderlineStyle.None,
            Strike = style.Strike,
            Color = style.Color,
            Highlight = style.Highlight,
            Alignment = paragraph.Format.Alignment,
            StyleName = paragraph.StyleName
        };

        // Layered over what the document says, in the order they were chosen. Without this the toolbar
        // would snap back to the run under the caret the moment anything raised Changed - which the
        // pending change itself does - and the choice would look like it had been ignored.
        foreach (var change in this.pending)
            format = change.PreviewCaret?.Invoke(format) ?? format;

        this.CaretFormat = format;
    }
}

/// <summary>A rectangle in document coordinates. Named to avoid clashing with the spreadsheet's.</summary>
public readonly record struct GridRectLike(double X, double Y, double Width, double Height)
{
    public double Right => this.X + this.Width;
    public double Bottom => this.Y + this.Height;

    public bool Contains(double x, double y)
        => x >= this.X && x <= this.Right && y >= this.Y && y <= this.Bottom;
}
