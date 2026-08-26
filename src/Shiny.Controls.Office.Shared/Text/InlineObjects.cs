using Shiny.Controls.Office.Shapes;

namespace Shiny.Controls.Office.Text;

/// <summary>
/// Something that sits in the text flow and occupies space without being text.
/// </summary>
/// <remarks>
/// <para>
/// An inline object counts as exactly one character for every purpose the caret cares about: one
/// arrow-key press steps over it, backspace removes the whole thing, and a selection that touches it
/// takes all of it. That is what Word does, and it is also what makes the arithmetic work — a
/// <c>w:drawing</c> lives inside a <c>w:r</c> alongside text runs, so the paragraph's character
/// offsets have to account for it or every caret position after it is wrong.
/// </para>
/// <para>
/// Width and height are the object's own, in pixels. The layout engine treats them as a single
/// unbreakable piece: one that does not fit on the current line moves to the next rather than being
/// scaled down, because silently shrinking a user's picture to fit a margin is not a decision a
/// renderer gets to make.
/// </para>
/// </remarks>
public abstract record InlineObject(double Width, double Height, string? Description);

/// <summary>A picture in the text flow, as bytes in whatever format the package stored.</summary>
public sealed record InlineImage(byte[] Data, double Width, double Height, string? Description = null)
    : InlineObject(Width, Height, Description);

/// <summary>
/// A preset-geometry shape in the text flow.
/// </summary>
/// <remarks>
/// Word's <c>wps:wsp</c> inside a <c>wp:inline</c>. The same <see cref="ShapeGeometry"/> the slide
/// side uses, drawn by the same path builder — a rounded rectangle is a rounded rectangle whichever
/// file it came out of.
/// </remarks>
public sealed record InlineShape(ShapeGeometry Geometry, double Width, double Height, string? Description = null)
    : InlineObject(Width, Height, Description)
{
    public ShapeFill Fill { get; init; } = ShapeFill.None;

    public ShapeOutline? Outline { get; init; }

    /// <summary>Corner radius as a fraction of the smaller side, for rounded rectangles.</summary>
    public double CornerRadius { get; init; } = 0.16;

    /// <summary>
    /// The shape's own text, already styled, or empty when it has none.
    /// </summary>
    /// <remarks>
    /// Laid out and centred inside the shape when painted, but not editable through the document
    /// caret: the text lives in a <c>w:txbxContent</c> that is its own little document body, and
    /// giving it a caret means a second nested editing context rather than another run in this
    /// paragraph. Reading it is still worth doing — a labelled arrow with the label missing is a
    /// worse lie than one drawn slightly wrong.
    /// </remarks>
    public IReadOnlyList<StyledRun> Text { get; init; } = [];

    public TextAlignment TextAlignment { get; init; } = TextAlignment.Center;
}
