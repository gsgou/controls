using Shiny.Controls.Office.Spreadsheet;

namespace Shiny.Controls.Office.Skia;

/// <summary>
/// The neutral part of an app's theme, so a drawn Office surface can sit on the same ground as the
/// composed chrome around it.
/// </summary>
/// <remarks>
/// <para>
/// The grid, the page and the deck are painted rather than composed from themed views, so their
/// colours have to arrive as values. Those values were a fixed pair of palettes — a neutral grey for
/// dark, white for light — while the toolbar above them followed the app's theme tokens. In any theme
/// whose neutrals carry a tint (the packs here run blue) that put a blue-grey bar directly on top of a
/// flat grey grid, close enough to look like a mistake rather than a choice.
/// </para>
/// <para>
/// Only the neutrals are taken. The selection green, the clipboard marquee's blue and a document's
/// link colour carry meaning rather than surface, and an app's accent is no substitute for any of
/// them — a spreadsheet with a purple selection is not theming, it is a different control.
/// </para>
/// </remarks>
public readonly record struct OfficeSurface(
    ArgbColor Surface,
    ArgbColor OnSurface,
    ArgbColor SurfaceContainer,
    ArgbColor SurfaceContainerLow,
    ArgbColor OnSurfaceVariant,
    ArgbColor Outline,
    ArgbColor OutlineVariant)
{
    /// <summary>
    /// Restates a spreadsheet theme's neutrals in the app's own, leaving everything that means
    /// something alone.
    /// </summary>
    /// <remarks>
    /// The touch handles' ring takes the grid's ground, which is the whole reason it exists: it
    /// separates the handle from whatever cell is under it, so on a themed sheet it has to be that
    /// sheet's colour rather than a fixed white.
    /// </remarks>
    public SpreadsheetTheme Apply(SpreadsheetTheme baseline)
        => baseline with
        {
            Background = this.Surface,
            CellText = this.OnSurface,
            GridLine = this.OutlineVariant,
            HeaderBackground = this.SurfaceContainer,
            HeaderText = this.OnSurfaceVariant,
            HeaderBorder = this.Outline,
            FrozenDivider = this.Outline,
            TouchHandleRing = this.Surface
        };

    /// <summary>
    /// Restates a document theme's surround in the app's own — the page itself stays paper.
    /// </summary>
    /// <remarks>
    /// Deliberately not the page. A document is a picture of a printed sheet, and the whole point of
    /// the surround is to make that sheet read as paper lying on a desk; tinting the paper with the
    /// app's surface would misrepresent what the document actually looks like, which is the same
    /// reason the deck's slides are left alone. The surround, the desk it lies on, is chrome.
    /// </remarks>
    public DocumentTheme Apply(DocumentTheme baseline)
        => baseline with
        {
            SurroundBackground = this.SurfaceContainerLow
        };

    /// <summary>Restates a deck theme's surround in the app's own. The slides are left alone.</summary>
    public SlideTheme Apply(SlideTheme baseline)
        => baseline with
        {
            Surround = this.SurfaceContainerLow
        };
}
