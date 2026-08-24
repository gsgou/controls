using Shiny.Controls.Office.Spreadsheet;

namespace Shiny.Controls.Office.Text;

public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}

public enum UnderlineStyle
{
    None,
    Single,
    Double
}

/// <summary>
/// Character-level formatting, flattened from whatever style chain produced it.
/// </summary>
/// <remarks>
/// Shared by the Word and PowerPoint readers on purpose: DrawingML and WordprocessingML spell run
/// properties differently but mean the same handful of things, and one struct here means one layout
/// engine and one painter serve both.
/// </remarks>
public readonly record struct TextStyle
{
    public static readonly TextStyle Default = new()
    {
        FontFamily = "Calibri",
        FontSize = 11,
        Color = new ArgbColor(255, 0, 0, 0)
    };

    public string FontFamily { get; init; }
    public double FontSize { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public UnderlineStyle Underline { get; init; }
    public bool Strike { get; init; }
    public ArgbColor Color { get; init; }

    /// <summary>Superscript raises and subscript lowers; expressed as a fraction of the font size.</summary>
    public double BaselineShift { get; init; }

    /// <summary>Scale applied to the glyph size for super/subscript. 1 means normal.</summary>
    public double SizeScale { get; init; }

    public ArgbColor? Highlight { get; init; }

    /// <summary>The destination when this run is part of a hyperlink.</summary>
    public string? Link { get; init; }

    public double EffectiveFontSize => this.FontSize * (this.SizeScale <= 0 ? 1 : this.SizeScale);
}

/// <summary>What a measurer reports about a piece of text in a given style.</summary>
public readonly record struct TextMetrics(double Width, double Ascent, double Descent)
{
    /// <summary>Ascent plus descent — the full glyph box, not the line height.</summary>
    public double Height => this.Ascent + this.Descent;
}

/// <summary>
/// Font metrics, abstracted so the layout engine can be tested without a graphics stack.
/// </summary>
/// <remarks>
/// The Skia implementation lives in the rendering package. Keeping the interface here is what lets
/// hundreds of line-breaking assertions run headlessly against a deterministic fake.
/// </remarks>
public interface ITextMeasurer
{
    TextMetrics Measure(ReadOnlySpan<char> text, TextStyle style);

    /// <summary>Ascent and descent of the font itself, independent of any particular text.</summary>
    TextMetrics LineMetrics(TextStyle style);
}
