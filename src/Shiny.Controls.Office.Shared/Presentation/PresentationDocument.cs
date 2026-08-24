using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Shiny.Controls.Office.Editing;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Text;
using D = DocumentFormat.OpenXml.Drawing;
using Package = DocumentFormat.OpenXml.Packaging.PresentationDocument;

namespace Shiny.Controls.Office.Presentation;

/// <summary>
/// An open <c>.pptx</c>, read into a slide/shape model.
/// </summary>
/// <remarks>
/// Shapes come out already resolved through the layout and master, because a placeholder on a slide
/// routinely carries no position, no size and no text formatting of its own — all of that lives on the
/// layout it came from, and on the master behind that. Reading only the slide produces a deck of
/// correctly-worded shapes stacked in the top-left corner.
/// </remarks>
public sealed class SlideDeck : OfficeDocument
{
    readonly Package document;
    readonly List<Slide> slides = new();
    readonly List<SlidePart> parts = new();
    readonly IUnsupportedFeatureSink sink;
    bool contentChanged;

    SlideDeck(MemoryStream buffer, string? path, Package document, IUnsupportedFeatureSink unsupported)
        : base(buffer, path, unsupported)
    {
        this.document = document;
        this.sink = unsupported;

        var presentationPart = document.PresentationPart
            ?? throw new InvalidDataException("The package has no presentation part.");

        var size = presentationPart.Presentation?.SlideSize;
        this.SlideWidth = size?.Cx?.Value is { } cx ? OoxmlUnits.EmuToPixels(cx) : 960;
        this.SlideHeight = size?.Cy?.Value is { } cy ? OoxmlUnits.EmuToPixels(cy) : 540;

        var number = 1;
        foreach (var slidePart in EnumerateSlides(presentationPart))
        {
            var reader = new SlideReader(slidePart, unsupported);
            this.parts.Add(slidePart);
            this.slides.Add(reader.Read(number++));
        }

        this.Undo = new UndoStack<SlideDeck>(this);
    }

    public IReadOnlyList<Slide> Slides => this.slides;

    /// <summary>Undo history for edits. Empty for a deck opened read-only.</summary>
    public UndoStack<SlideDeck> Undo { get; }

    /// <summary>True when the deck was opened for editing.</summary>
    public bool IsEditable { get; private set; }

    /// <summary>Raised after any edit, so a view can repaint.</summary>
    public event EventHandler? ContentChanged;

    /// <summary>Applies an edit through the undo stack.</summary>
    public void Execute(IEditCommand<SlideDeck> command)
    {
        if (!this.IsEditable)
            throw new InvalidOperationException("This deck was opened read-only. Use OpenAsync(..., editable: true).");

        this.Undo.Execute(command);
    }

    /// <summary>Slide width in pixels at 96 dpi. 960x540 for the usual 16:9 deck.</summary>
    public double SlideWidth { get; }

    public double SlideHeight { get; }

    public double AspectRatio => this.SlideHeight <= 0 ? 16d / 9 : this.SlideWidth / this.SlideHeight;

    public static async Task<SlideDeck> OpenAsync(
        string path,
        IUnsupportedFeatureSink? unsupported = null,
        bool editable = false,
        CancellationToken cancellationToken = default)
    {
        var buffer = await ReadIntoBufferAsync(path, cancellationToken).ConfigureAwait(false);
        return Create(buffer, path, unsupported, editable);
    }

    public static async Task<SlideDeck> OpenAsync(
        Stream source,
        IUnsupportedFeatureSink? unsupported = null,
        bool editable = false,
        CancellationToken cancellationToken = default)
    {
        var buffer = await ReadIntoBufferAsync(source, cancellationToken).ConfigureAwait(false);
        return Create(buffer, null, unsupported, editable);
    }

    static SlideDeck Create(MemoryStream buffer, string? path, IUnsupportedFeatureSink? unsupported, bool editable)
    {
        var sink = unsupported ?? NullUnsupportedFeatureSink.Instance;
        Package document;
        try
        {
            // AutoSave off for the same reason as the workbook and the document: OpenXml otherwise
            // re-serialises every part it has materialised, so merely opening a deck rewrites it.
            document = Package.Open(buffer, isEditable: editable, new OpenSettings { AutoSave = false });
        }
        catch
        {
            buffer.Dispose();
            throw;
        }

        try
        {
            return new SlideDeck(buffer, path, document, sink) { IsEditable = editable };
        }
        catch
        {
            document.Dispose();
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Slides in presentation order.
    /// </summary>
    /// <remarks>
    /// <c>SlideParts</c> is in package order, which is not the order the deck plays in. The
    /// <c>sldIdLst</c> is the running order, and using the wrong one shuffles the deck.
    /// </remarks>
    static IEnumerable<SlidePart> EnumerateSlides(PresentationPart presentationPart)
    {
        var list = presentationPart.Presentation?.SlideIdList;
        if (list is null)
        {
            foreach (var part in presentationPart.SlideParts)
                yield return part;

            yield break;
        }

        foreach (var slideId in list.Elements<SlideId>())
        {
            if (slideId.RelationshipId?.Value is not { } id)
                continue;

            if (presentationPart.GetPartById(id) is SlidePart part)
                yield return part;
        }
    }

    /// <summary>
    /// Writes pending edits into the package.
    /// </summary>
    /// <remarks>
    /// An unedited deck is never re-serialised, so opening and saving without changing anything
    /// produces a byte-identical file — and a deck opened read-only can never reach the save branch
    /// at all.
    /// </remarks>
    protected override void FlushToPackage()
    {
        if (!this.contentChanged)
            return;

        foreach (var part in this.dirty)
            part.Slide?.Save();

        // document.Save() is the only public flush the SDK offers, and it re-serialises every part
        // whose DOM has been materialised - which, for a deck, is every slide, layout, master, theme
        // and notes part the reader had to walk. Those round-trip through the same object model, so
        // nothing is lost, but their bytes change. Byte-identity is therefore promised for an
        // *unedited* deck only, which is what the early return above guarantees.
        this.document.Save();
        this.dirty.Clear();
        this.contentChanged = false;
    }

    // ---- editing surface, driven by the commands ----

    readonly HashSet<SlidePart> dirty = new();

    internal SlidePart? PartAt(int slide)
        => slide >= 0 && slide < this.parts.Count ? this.parts[slide] : null;

    /// <summary>The shape tree a slide's own shapes live in.</summary>
    internal ShapeTree? TreeAt(int slide) => this.PartAt(slide)?.Slide?.CommonSlideData?.ShapeTree;

    /// <summary>Re-reads one slide from its (now edited) XML and marks it for saving.</summary>
    internal void Reproject(int slide)
    {
        if (this.PartAt(slide) is not { } part)
            return;

        var reader = new SlideReader(part, this.sink);
        this.slides[slide] = reader.Read(slide + 1);

        this.dirty.Add(part);
        this.contentChanged = true;
        this.MarkDirty();
        this.ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            this.document.Dispose();

        base.Dispose(disposing);
    }
}
