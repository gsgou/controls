namespace Shiny.Controls.Office.Icons;

/// <summary>The icons the Word and PowerPoint editing toolbars draw.</summary>
public enum OfficeIcon
{
    Bold,
    Italic,
    Underline,
    Strikethrough,
    AlignLeft,
    AlignCenter,
    AlignRight,
    AlignJustify,
    Highlight,
    Shape,
    Table,
    Picture,
    TextBox,
    Delete,
    Indent,
    Outdent,
    Undo,
    Redo,
    Previous,
    Next,

    /// <summary>The chevron on a split button, saying that pressing it opens a gallery.</summary>
    Chevron,

    // The spreadsheet toolbar's own. Everything above is shared with the document and slide bars;
    // these have no meaning outside a grid, which is why they sit apart rather than interleaved.

    /// <summary>Sigma — the AutoSum button.</summary>
    Sum,

    /// <summary>The generic currency sign, not a dollar: the format follows the reader's culture.</summary>
    Currency,
    Percent,
    DecimalIncrease,
    DecimalDecrease,
    WrapText,

    /// <summary>A paint drop — the colour poured into a cell's background.</summary>
    FillColor,

    /// <summary>An eraser: strip the formatting, keep the contents.</summary>
    ClearFormat,

    /// <summary>Two column edges with a measure between them — fit the column to what is in it.</summary>
    ColumnWidth,
    AlignTop,
    AlignMiddle,
    AlignBottom
}


/// <summary>
/// The one icon set behind both Office editing toolbars, on both hosts.
/// </summary>
/// <remarks>
/// <para>
/// Every button on the document and slide toolbars draws from here: one monochrome stroked set on a
/// 24x24 grid, one weight, no colour of its own. What it replaced was a mixture — styled letters for
/// bold and italic, geometric unicode for the alignment and undo controls, and emoji for the picture
/// and delete buttons. Emoji are the reason this exists rather than being a matter of taste: a font
/// paints them in colour, at its own size and its own weight, so those two buttons could not be
/// tinted, did not match the buttons beside them and looked different on every platform. The unicode
/// glyphs had the milder version of the same problem, plus tofu on Android fonts that lack them.
/// </para>
/// <para>
/// The pickers are the deliberate exception. Font, font size and the colour and highlight swatches
/// have to show what they are currently set to, so they stay as they are — a monochrome icon cannot
/// say "Calibri, 11pt, red".
/// </para>
/// </remarks>
public static class OfficeIcons
{
    /// <summary>The grid every icon is drawn on. Hosts scale it to whatever the button offers.</summary>
    public const float Grid = 24f;

    /// <summary>The stroke width on that grid, so the two hosts come out at the same weight.</summary>
    public const float StrokeWidth = 1.9f;


    /// <summary>The figures making up an icon, in draw order.</summary>
    public static IReadOnlyList<OfficeIconShape> Shapes(OfficeIcon icon) => icon switch
    {
        // Two bowls off a shared stem — the letterform, drawn rather than typeset, so it carries the
        // same weight as its neighbours instead of whatever the platform's bold face happens to be.
        OfficeIcon.Bold =>
        [
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(7.5f, 4.5f),
                OfficeIconVertex.LineTo(13f, 4.5f),
                OfficeIconVertex.CurveTo(15.4f, 4.5f, 17f, 6.1f, 17f, 8.35f),
                OfficeIconVertex.CurveTo(17f, 10.6f, 15.4f, 12.2f, 13f, 12.2f),
                OfficeIconVertex.LineTo(7.5f, 12.2f),
                OfficeIconVertex.Close),
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(7.5f, 12.2f),
                OfficeIconVertex.LineTo(13.8f, 12.2f),
                OfficeIconVertex.CurveTo(16.3f, 12.2f, 18f, 13.8f, 18f, 15.85f),
                OfficeIconVertex.CurveTo(18f, 17.9f, 16.3f, 19.5f, 13.8f, 19.5f),
                OfficeIconVertex.LineTo(7.5f, 19.5f),
                OfficeIconVertex.Close)
        ],

        OfficeIcon.Italic =>
        [
            OfficeIconShape.Line(9.5f, 4.8f, 18.5f, 4.8f),
            OfficeIconShape.Line(5.5f, 19.2f, 14.5f, 19.2f),
            OfficeIconShape.Line(14f, 4.8f, 10f, 19.2f)
        ],

        OfficeIcon.Underline =>
        [
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(6.5f, 4f),
                OfficeIconVertex.LineTo(6.5f, 10.5f),
                OfficeIconVertex.CurveTo(6.5f, 13.54f, 8.96f, 16f, 12f, 16f),
                OfficeIconVertex.CurveTo(15.04f, 16f, 17.5f, 13.54f, 17.5f, 10.5f),
                OfficeIconVertex.LineTo(17.5f, 4f)),
            OfficeIconShape.Line(5f, 20f, 19f, 20f)
        ],

        OfficeIcon.Strikethrough =>
        [
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(16.5f, 4.7f),
                OfficeIconVertex.LineTo(9.8f, 4.7f),
                OfficeIconVertex.CurveTo(7.7f, 4.7f, 6.4f, 6.4f, 7.1f, 8.4f)),
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(13.4f, 12f),
                OfficeIconVertex.CurveTo(15.9f, 12.4f, 17.2f, 14.2f, 16.8f, 16.4f),
                OfficeIconVertex.CurveTo(16.4f, 18.4f, 14.6f, 19.4f, 12.3f, 19.4f),
                OfficeIconVertex.LineTo(6.6f, 19.4f)),
            OfficeIconShape.Line(3.5f, 12f, 20.5f, 12f)
        ],

        // The alignment set is four rules with the short ones moved about, which is the only mark
        // that reads at 22px — an arrow says "go left", not "align left".
        OfficeIcon.AlignLeft => Rules(4f, 14.5f),
        OfficeIcon.AlignCenter => Rules(6.75f, 17.25f),
        OfficeIcon.AlignRight => Rules(9.5f, 20f),
        OfficeIcon.AlignJustify => Rules(4f, 20f),

        // Letter over a filled bar: the mark Word and PowerPoint both use, and the one place a
        // toolbar host may tint the bar with the colour it would apply.
        OfficeIcon.Highlight =>
        [
            OfficeIconShape.Polyline(7.3f, 15.6f, 12f, 4.8f, 16.7f, 15.6f),
            OfficeIconShape.Line(9.1f, 12.2f, 14.9f, 12.2f),
            OfficeIconShape.Rectangle(4.5f, 18f, 15f, 3f, 0.8f).Filled()
        ],

        OfficeIcon.Shape =>
        [
            OfficeIconShape.Rectangle(3.3f, 3.3f, 11.4f, 11.4f, 1.6f),
            OfficeIconShape.Circle(15.2f, 15.2f, 5.6f)
        ],

        OfficeIcon.Table =>
        [
            OfficeIconShape.Rectangle(3.4f, 4.6f, 17.2f, 14.8f, 1.6f),
            OfficeIconShape.Line(3.4f, 9.5f, 20.6f, 9.5f),
            OfficeIconShape.Line(3.4f, 14.5f, 20.6f, 14.5f),
            OfficeIconShape.Line(9.2f, 4.6f, 9.2f, 19.4f),
            OfficeIconShape.Line(15f, 4.6f, 15f, 19.4f)
        ],

        OfficeIcon.Picture =>
        [
            OfficeIconShape.Rectangle(3.4f, 4.6f, 17.2f, 14.8f, 2f),
            OfficeIconShape.Circle(8.6f, 9.4f, 1.7f),
            OfficeIconShape.Polyline(4.4f, 17.4f, 9.6f, 12.2f, 13.2f, 15.8f, 16.2f, 12.8f, 20f, 16.6f)
        ],

        OfficeIcon.TextBox =>
        [
            OfficeIconShape.Rectangle(3.4f, 4.6f, 17.2f, 14.8f, 2f),
            OfficeIconShape.Line(8.2f, 9.4f, 15.8f, 9.4f),
            OfficeIconShape.Line(12f, 9.4f, 12f, 15.6f)
        ],

        OfficeIcon.Delete =>
        [
            OfficeIconShape.Line(3.8f, 6.4f, 20.2f, 6.4f),
            OfficeIconShape.Polyline(9.4f, 6.4f, 9.4f, 4.2f, 14.6f, 4.2f, 14.6f, 6.4f),
            OfficeIconShape.Polyline(6.4f, 6.4f, 7.5f, 20.6f, 16.5f, 20.6f, 17.6f, 6.4f),
            OfficeIconShape.Line(10.4f, 10f, 10.4f, 17f),
            OfficeIconShape.Line(13.6f, 10f, 13.6f, 17f)
        ],

        OfficeIcon.Indent =>
        [
            .. Rules(10f, 20f),
            OfficeIconShape.Polyline(3.6f, 9.2f, 7.2f, 12f, 3.6f, 14.8f)
        ],

        OfficeIcon.Outdent =>
        [
            .. Rules(10f, 20f),
            OfficeIconShape.Polyline(7.2f, 9.2f, 3.6f, 12f, 7.2f, 14.8f)
        ],

        // Arrow head plus a half-circle back the way it came, matching the undo pair the image editor
        // already draws — one undo mark across the whole product.
        OfficeIcon.Undo =>
        [
            OfficeIconShape.Polyline(9f, 14f, 4f, 9f, 9f, 4f),
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(4f, 9f),
                OfficeIconVertex.LineTo(13f, 9f),
                OfficeIconVertex.CurveTo(16.31f, 9f, 19f, 11.69f, 19f, 15f),
                OfficeIconVertex.CurveTo(19f, 18.31f, 16.31f, 21f, 13f, 21f),
                OfficeIconVertex.LineTo(9.5f, 21f))
        ],

        OfficeIcon.Redo =>
        [
            OfficeIconShape.Polyline(15f, 14f, 20f, 9f, 15f, 4f),
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(20f, 9f),
                OfficeIconVertex.LineTo(11f, 9f),
                OfficeIconVertex.CurveTo(7.69f, 9f, 5f, 11.69f, 5f, 15f),
                OfficeIconVertex.CurveTo(5f, 18.31f, 7.69f, 21f, 11f, 21f),
                OfficeIconVertex.LineTo(14.5f, 21f))
        ],

        OfficeIcon.Previous => [OfficeIconShape.Polyline(14.5f, 4.5f, 8f, 12f, 14.5f, 19.5f)],
        OfficeIcon.Next => [OfficeIconShape.Polyline(9.5f, 4.5f, 16f, 12f, 9.5f, 19.5f)],
        OfficeIcon.Chevron => [OfficeIconShape.Polyline(7.5f, 10f, 12f, 14.5f, 16.5f, 10f)],

        // ---- the spreadsheet set ----

        OfficeIcon.Sum => [OfficeIconShape.Polyline(16.6f, 4.8f, 7.4f, 4.8f, 12.7f, 12f, 7.4f, 19.2f, 16.6f, 19.2f)],

        // The international currency sign rather than a dollar. The button applies the reader's own
        // currency, and stamping one country's symbol on it would be a promise the format does not keep.
        OfficeIcon.Currency =>
        [
            OfficeIconShape.Circle(12f, 12f, 4.6f),
            OfficeIconShape.Line(8.75f, 8.75f, 6.2f, 6.2f),
            OfficeIconShape.Line(15.25f, 8.75f, 17.8f, 6.2f),
            OfficeIconShape.Line(8.75f, 15.25f, 6.2f, 17.8f),
            OfficeIconShape.Line(15.25f, 15.25f, 17.8f, 17.8f)
        ],

        OfficeIcon.Percent =>
        [
            OfficeIconShape.Circle(8f, 8f, 2.6f),
            OfficeIconShape.Circle(16f, 16f, 2.6f),
            OfficeIconShape.Line(17.8f, 5.4f, 6.2f, 18.6f)
        ],

        // An arrow over the decimal places it moves. Drawn rather than lettered because "0.00" at 20px
        // is illegible, and the direction of the arrow is the whole message anyway.
        OfficeIcon.DecimalIncrease => [.. Decimals(), OfficeIconShape.Line(7f, 10f, 16.2f, 10f), OfficeIconShape.Polyline(13f, 6.8f, 16.2f, 10f, 13f, 13.2f)],
        OfficeIcon.DecimalDecrease => [.. Decimals(), OfficeIconShape.Line(7.8f, 10f, 17f, 10f), OfficeIconShape.Polyline(11f, 6.8f, 7.8f, 10f, 11f, 13.2f)],

        OfficeIcon.WrapText =>
        [
            OfficeIconShape.Line(4f, 5.5f, 20f, 5.5f),
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(4f, 11f),
                OfficeIconVertex.LineTo(15f, 11f),
                OfficeIconVertex.CurveTo(17.6f, 11f, 19.2f, 12.5f, 19.2f, 14.5f),
                OfficeIconVertex.CurveTo(19.2f, 16.5f, 17.6f, 18f, 15f, 18f),
                OfficeIconVertex.LineTo(10.5f, 18f)),
            OfficeIconShape.Polyline(13f, 15.5f, 10.5f, 18f, 13f, 20.5f)
        ],

        OfficeIcon.FillColor =>
        [
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(12f, 4f),
                OfficeIconVertex.CurveTo(12f, 4f, 6.2f, 10.6f, 6.2f, 14.4f),
                OfficeIconVertex.CurveTo(6.2f, 17.6f, 8.8f, 20.2f, 12f, 20.2f),
                OfficeIconVertex.CurveTo(15.2f, 20.2f, 17.8f, 17.6f, 17.8f, 14.4f),
                OfficeIconVertex.CurveTo(17.8f, 10.6f, 12f, 4f, 12f, 4f),
                OfficeIconVertex.Close)
        ],

        OfficeIcon.ClearFormat =>
        [
            OfficeIconShape.Path(
                OfficeIconVertex.MoveTo(5.5f, 15f),
                OfficeIconVertex.LineTo(11.8f, 8.7f),
                OfficeIconVertex.LineTo(18.1f, 15f),
                OfficeIconVertex.LineTo(13.6f, 19.5f),
                OfficeIconVertex.LineTo(10f, 19.5f),
                OfficeIconVertex.Close),
            OfficeIconShape.Line(8.65f, 11.85f, 14.95f, 18.15f),
            OfficeIconShape.Line(4f, 22f, 20f, 22f)
        ],

        OfficeIcon.ColumnWidth =>
        [
            OfficeIconShape.Line(6f, 4.5f, 6f, 19.5f),
            OfficeIconShape.Line(18f, 4.5f, 18f, 19.5f),
            OfficeIconShape.Line(8.2f, 12f, 15.8f, 12f),
            OfficeIconShape.Polyline(10.6f, 9.6f, 8.2f, 12f, 10.6f, 14.4f),
            OfficeIconShape.Polyline(13.4f, 9.6f, 15.8f, 12f, 13.4f, 14.4f)
        ],

        // A full-width rule at the edge the content is pulled to, with the content beside it. The
        // horizontal set says the same thing by moving short rules about; this is its other axis.
        OfficeIcon.AlignTop => [OfficeIconShape.Line(4f, 4.5f, 20f, 4.5f), .. Lines(9.5f, 14f)],
        OfficeIcon.AlignMiddle => [OfficeIconShape.Line(4f, 12f, 20f, 12f), .. Lines(6.5f, 17.5f)],
        OfficeIcon.AlignBottom => [OfficeIconShape.Line(4f, 19.5f, 20f, 19.5f), .. Lines(10f, 14.5f)],

        _ => []
    };


    /// <summary>
    /// The four text rules the alignment and indent icons are built from: two full-width, and the
    /// second and fourth spanning whatever the caller asks for.
    /// </summary>
    static OfficeIconShape[] Rules(float shortLeft, float shortRight) =>
    [
        OfficeIconShape.Line(4f, 5f, 20f, 5f),
        OfficeIconShape.Line(shortLeft, 9.6f, shortRight, 9.6f),
        OfficeIconShape.Line(4f, 14.4f, 20f, 14.4f),
        OfficeIconShape.Line(shortLeft, 19f, shortRight, 19f)
    ];


    /// <summary>Two short rules standing in for cell content, at the given heights.</summary>
    static OfficeIconShape[] Lines(float firstY, float secondY) =>
    [
        OfficeIconShape.Line(7.5f, firstY, 16.5f, firstY),
        OfficeIconShape.Line(7.5f, secondY, 16.5f, secondY)
    ];


    /// <summary>
    /// The three decimal places the decimal buttons move.
    /// </summary>
    /// <remarks>
    /// Stroked circles at a radius smaller than the stroke, so they paint as dots. Filling them would
    /// be the honest way to say it, but the set is stroked throughout so that a host can tint it.
    /// </remarks>
    static OfficeIconShape[] Decimals() =>
    [
        OfficeIconShape.Circle(8f, 17.6f, 0.6f),
        OfficeIconShape.Circle(12f, 17.6f, 0.6f),
        OfficeIconShape.Circle(16f, 17.6f, 0.6f)
    ];
}
