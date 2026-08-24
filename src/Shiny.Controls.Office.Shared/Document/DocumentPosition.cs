namespace Shiny.Controls.Office.Document;

/// <summary>
/// A caret position: a block, and a character offset into that block's text.
/// </summary>
/// <remarks>
/// The offset is into the paragraph's *concatenated* text, not into a run. Runs are a storage detail
/// — Word splits and merges them freely for reasons that have nothing to do with where the caret is —
/// so a position expressed in runs becomes invalid the moment formatting changes underneath it.
/// </remarks>
public readonly record struct DocumentPosition(int Block, int Offset) : IComparable<DocumentPosition>
{
    public static readonly DocumentPosition Start = new(0, 0);

    public int CompareTo(DocumentPosition other)
    {
        var block = this.Block.CompareTo(other.Block);
        return block != 0 ? block : this.Offset.CompareTo(other.Offset);
    }

    public static bool operator <(DocumentPosition a, DocumentPosition b) => a.CompareTo(b) < 0;
    public static bool operator >(DocumentPosition a, DocumentPosition b) => a.CompareTo(b) > 0;
    public static bool operator <=(DocumentPosition a, DocumentPosition b) => a.CompareTo(b) <= 0;
    public static bool operator >=(DocumentPosition a, DocumentPosition b) => a.CompareTo(b) >= 0;

    public override string ToString() => $"{this.Block}:{this.Offset}";
}

/// <summary>An ordered span between two positions.</summary>
public readonly record struct DocumentRange(DocumentPosition Start, DocumentPosition End)
{
    public DocumentRange(DocumentPosition anchor, DocumentPosition focus, bool _)
        : this(anchor <= focus ? anchor : focus, anchor <= focus ? focus : anchor)
    {
    }

    public bool IsEmpty => this.Start == this.End;

    public bool IsWithinOneBlock => this.Start.Block == this.End.Block;

    public bool Contains(DocumentPosition position) => position >= this.Start && position < this.End;
}

/// <summary>
/// The editor's selection: an anchor that stays put and a focus that moves.
/// </summary>
/// <remarks>
/// Kept as anchor/focus rather than a normalised range because direction matters while extending —
/// shift-arrow has to grow from where the selection started, and a range alone has forgotten that.
/// </remarks>
public sealed class DocumentSelection
{
    public DocumentPosition Anchor { get; private set; } = DocumentPosition.Start;

    /// <summary>The moving end. This is where the caret is drawn.</summary>
    public DocumentPosition Focus { get; private set; } = DocumentPosition.Start;

    public DocumentRange Range => new(this.Anchor, this.Focus, true);

    public bool IsEmpty => this.Anchor == this.Focus;

    public event EventHandler? Changed;

    public void MoveTo(DocumentPosition position)
    {
        if (this.Anchor == position && this.Focus == position)
            return;

        this.Anchor = position;
        this.Focus = position;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ExtendTo(DocumentPosition position)
    {
        if (this.Focus == position)
            return;

        this.Focus = position;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Select(DocumentPosition anchor, DocumentPosition focus)
    {
        this.Anchor = anchor;
        this.Focus = focus;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Collapses to one end without moving the caret to somewhere the user did not put it.</summary>
    public void Collapse(bool toStart)
    {
        var range = this.Range;
        this.MoveTo(toStart ? range.Start : range.End);
    }
}
