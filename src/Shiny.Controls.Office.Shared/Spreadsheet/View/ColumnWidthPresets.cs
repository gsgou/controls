namespace Shiny.Controls.Office.Spreadsheet.View;

/// <summary>
/// The column widths both toolbars offer behind the fit-to-contents button.
/// </summary>
/// <remarks>
/// <para>
/// Excel's answer here is a dialog with a number in it. A control that runs on a phone as well as a
/// desktop has nowhere good to put one, and a hand-typed character count is not what the command is
/// usually about anyway — the ask is "make this column wider", and four steps answer it.
/// </para>
/// <para>
/// Measured in Excel's own unit, characters of the default font's widest digit, so a width set here
/// is the width that is written to the file. <see cref="GridMetrics.DefaultColumnWidthCharacters"/>
/// is one of the four deliberately: it is the only way back to the sheet's own width once a column
/// has been dragged or fitted.
/// </para>
/// </remarks>
public static class ColumnWidthPresets
{
    /// <summary>The four widths, narrowest first, in characters.</summary>
    public static IReadOnlyList<(string Name, double Characters)> All { get; } =
    [
        ("Narrow", 6d),
        ("Default", GridMetrics.DefaultColumnWidthCharacters),
        ("Wide", 16d),
        ("Very wide", 26d)
    ];

    /// <summary>A preset's width in pixels, which is what the controller takes.</summary>
    public static double PixelsOf(double characters) => GridMetrics.WidthToPixels(characters);
}
