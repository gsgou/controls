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
    public ArgbColor FrozenDivider { get; init; } = new(255, 0x9A, 0x9A, 0x9A);

    public string FontFamily { get; init; } = "Calibri";
    public double FontSize { get; init; } = 11;

    /// <summary>Padding inside a cell, before the text starts.</summary>
    public double CellPadding { get; init; } = 4;

    /// <summary>Width of one indent level, in pixels.</summary>
    public double IndentWidth { get; init; } = 8;

    public double SelectionBorderWidth { get; init; } = 2;

    /// <summary>Side of the square drag handle on the selection's bottom-right corner.</summary>
    public double FillHandleSize { get; init; } = 6;
}
