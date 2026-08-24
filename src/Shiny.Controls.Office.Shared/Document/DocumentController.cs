using Shiny.Controls.Office.Text;

namespace Shiny.Controls.Office.Document;

/// <summary>
/// Host-independent state for the document viewer: layout at the current width, scrolling and zoom.
/// </summary>
/// <remarks>
/// Layout is the expensive part and only depends on width, so it is cached and rebuilt on resize or
/// zoom rather than on every scroll or repaint.
/// </remarks>
public class DocumentController
{
    readonly DocumentLayoutEngine engine;
    DocumentLayoutResult? layout;
    double laidOutWidth = -1;
    double zoom = 1.0;

    public DocumentController(WordDocument document, ITextMeasurer measurer)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(measurer);

        this.Document = document;
        this.Measurer = measurer;
        this.engine = new DocumentLayoutEngine(measurer);
        this.Viewport = new DocumentViewport();
    }

    public WordDocument Document { get; }

    public DocumentViewport Viewport { get; }

    /// <summary>Zoom factor. Changing it re-lays-out, because the measure changes with it.</summary>
    public double Zoom
    {
        get => this.zoom;
        set
        {
            var clamped = Math.Clamp(value, 0.25, 4.0);
            if (Math.Abs(clamped - this.zoom) < 0.001)
                return;

            this.zoom = clamped;
            this.laidOutWidth = -1;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Maximum page width in pixels. The page is centred and never stretched past this, so a document
    /// on a wide monitor reads like a page rather than one very long line.
    /// </summary>
    public double MaxPageWidth { get; set; } = 900;

    /// <summary>Horizontal padding inside the page, added to the document's own margins.</summary>
    public double PagePadding { get; set; } = 24;

    public event EventHandler? Changed;

    /// <summary>The measurer, exposed so a subclass can do its own hit-testing.</summary>
    protected ITextMeasurer Measurer { get; }

    /// <summary>Forces a re-layout on the next access, after the document's content has changed.</summary>
    protected void InvalidateLayout() => this.laidOutWidth = -1;

    protected void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);

    public IReadOnlyList<LaidOutBlock> Blocks => this.EnsureLayout().Blocks;

    /// <summary>The page panel's width in viewport coordinates.</summary>
    public double PageWidth => Math.Min(this.MaxPageWidth * this.zoom, Math.Max(100, this.Viewport.Width));

    /// <summary>The page panel's left edge, centring it in the viewport.</summary>
    public double PageX => Math.Max(0, (this.Viewport.Width - this.PageWidth) / 2);

    /// <summary>Width available to content inside the page.</summary>
    public double ContentWidth => Math.Max(50, this.PageWidth - this.PagePadding * 2);

    public void Resize(double width, double height)
    {
        this.Viewport.Width = width;
        this.Viewport.Height = height;
        this.EnsureLayout();
        this.OnViewportChanged();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Scroll(double delta)
    {
        this.EnsureLayout();
        this.Viewport.ScrollBy(delta);
        this.OnViewportChanged();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollTo(double y)
    {
        this.EnsureLayout();
        this.Viewport.ScrollTo(y);
        this.OnViewportChanged();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called after the visible region moves or resizes.
    /// </summary>
    /// <remarks>
    /// A viewer has nothing to do here; the editor uses it to spell-check the paragraphs that have
    /// just come on screen, since work is deliberately confined to what is actually visible.
    /// </remarks>
    protected virtual void OnViewportChanged()
    {
    }

    /// <summary>Scrolls to a heading from <see cref="WordDocument.Outline"/>, by its index in that list.</summary>
    public void ScrollToHeading(int outlineIndex)
    {
        var result = this.EnsureLayout();
        var headings = result.Blocks
            .OfType<LaidOutParagraph>()
            .Where(x => x.Format.OutlineLevel > 0)
            .ToList();

        if (outlineIndex < 0 || outlineIndex >= headings.Count)
            return;

        this.Viewport.ScrollTo(headings[outlineIndex].Y - 8);
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    DocumentLayoutResult EnsureLayout()
    {
        var width = this.ContentWidth;

        if (this.layout is not null && Math.Abs(width - this.laidOutWidth) < 0.5)
            return this.layout;

        this.layout = this.engine.Layout(this.Document.Blocks, width);
        this.laidOutWidth = width;
        this.Viewport.ContentHeight = this.layout.Height;

        // A width change can leave the scroll offset past the new end of the document.
        this.Viewport.ScrollTo(this.Viewport.ScrollY);
        return this.layout;
    }
}
