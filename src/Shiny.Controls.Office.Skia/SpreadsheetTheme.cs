using Shiny.Controls.Office.Spreadsheet;

namespace Shiny.Controls.Office.Skia;

/// <summary>
/// Colours and metrics for the grid chrome.
/// </summary>
/// <remarks>
/// Expressed in <see cref="ArgbColor"/> rather than an SKColor so the same theme can be built from MAUI
/// resources or CSS custom properties without either host depending on the other's colour type.
/// </remarks>
public sealed record SpreadsheetTheme
{
    public static readonly SpreadsheetTheme Light = new();

    public static readonly SpreadsheetTheme Dark = new()
    {
        Background = new ArgbColor(255, 0x1E, 0x1E, 0x1E),
        CellText = new ArgbColor(255, 0xE6, 0xE6, 0xE6),
        GridLine = new ArgbColor(255, 0x3A, 0x3A, 0x3A),
        HeaderBackground = new ArgbColor(255, 0x2A, 0x2A, 0x2A),
        HeaderText = new ArgbColor(255, 0xC8, 0xC8, 0xC8),
        HeaderSelectedBackground = new ArgbColor(255, 0x3D, 0x50, 0x43),
        HeaderBorder = new ArgbColor(255, 0x45, 0x45, 0x45),
        SelectionFill = new ArgbColor(38, 0x4C, 0xAF, 0x50),
        SelectionBorder = new ArgbColor(255, 0x4C, 0xAF, 0x50),
        ClipboardBorder = new ArgbColor(255, 0x7E, 0xB6, 0xFF),

        // The ring separates a handle from the cell under it, so it takes the sheet's own ground
        // rather than staying white - a white ring on a dark sheet is a brighter mark than the handle.
        TouchHandleRing = new ArgbColor(255, 0x1E, 0x1E, 0x1E),
        FrozenDivider = new ArgbColor(255, 0x6A, 0x6A, 0x6A)
    };

    public ArgbColor Background { get; init; } = new(255, 0xFF, 0xFF, 0xFF);
    public ArgbColor CellText { get; init; } = new(255, 0x1A, 0x1A, 0x1A);
    public ArgbColor GridLine { get; init; } = new(255, 0xD8, 0xD8, 0xD8);

    public ArgbColor HeaderBackground { get; init; } = new(255, 0xF3, 0xF3, 0xF3);
    public ArgbColor HeaderText { get; init; } = new(255, 0x44, 0x44, 0x44);
    public ArgbColor HeaderSelectedBackground { get; init; } = new(255, 0xD6, 0xE9, 0xD8);
    public ArgbColor HeaderBorder { get; init; } = new(255, 0xC4, 0xC4, 0xC4);

    public ArgbColor SelectionFill { get; init; } = new(30, 0x21, 0x7A, 0x3C);
    public ArgbColor SelectionBorder { get; init; } = new(255, 0x21, 0x7A, 0x3C);

    /// <summary>
    /// The marching-ants border around a pending cut or copy.
    /// </summary>
    /// <remarks>
    /// Deliberately not the selection colour. The two borders are routinely on screen at the same time
    /// — that is the whole shape of a paste, mark the source then move to the destination — and a
    /// dashed border in the same green as the solid one reads as the selection having gone strange
    /// rather than as a second thing.
    /// </remarks>
    public ArgbColor ClipboardBorder { get; init; } = new(255, 0x1A, 0x56, 0xB0);

    public ArgbColor FrozenDivider { get; init; } = new(255, 0x9A, 0x9A, 0x9A);

    public string FontFamily { get; init; } = "Calibri";
    public double FontSize { get; init; } = 11;

    /// <summary>Padding inside a cell, before the text starts.</summary>
    public double CellPadding { get; init; } = 4;

    /// <summary>Width of one indent level, in pixels.</summary>
    public double IndentWidth { get; init; } = 8;

    public double SelectionBorderWidth { get; init; } = 2;

    /// <summary>Radius of the round handles drawn on a touch selection.</summary>
    public double TouchHandleRadius { get; init; } = 7;

    /// <summary>The ring that keeps a handle readable over any cell underneath it.</summary>
    public ArgbColor TouchHandleRing { get; init; } = new(255, 0xFF, 0xFF, 0xFF);

    public double TouchHandleRingWidth { get; init; } = 2;

    public double ClipboardBorderWidth { get; init; } = 2;

    /// <summary>Length of one dash, and of the gap after it, in the marching-ants border.</summary>
    public double ClipboardDashLength { get; init; } = 5;

    /// <summary>Side of the square drag handle on the selection's bottom-right corner.</summary>
    public double FillHandleSize { get; init; } = 6;
}
