namespace Shiny.Controls.Office.Spreadsheet;

public enum CellHorizontalAlignment
{
    /// <summary>Excel's default: text left, numbers right, booleans and errors centred.</summary>
    General,
    Left,
    Center,
    Right,
    Fill,
    Justify,
    CenterContinuous,
    Distributed
}

public enum CellVerticalAlignment
{
    Top,
    Center,
    Bottom,
    Justify,
    Distributed
}

/// <summary>An ARGB colour. Kept host-agnostic so the kernel never references a UI framework's colour type.</summary>
public readonly record struct ArgbColor(byte A, byte R, byte G, byte B)
{
    public static readonly ArgbColor Transparent = new(0, 0, 0, 0);
    public bool IsTransparent => this.A == 0;

    public uint ToUInt32() => ((uint)this.A << 24) | ((uint)this.R << 16) | ((uint)this.G << 8) | this.B;

    public static ArgbColor FromUInt32(uint value)
        => new((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);

    public override string ToString() => $"#{this.A:X2}{this.R:X2}{this.G:X2}{this.B:X2}";
}

/// <summary>
/// A cell's formatting, flattened from the style chain into something a renderer can use directly.
/// </summary>
public sealed record ResolvedFormat
{
    public static readonly ResolvedFormat Default = new();

    /// <summary>The Excel number format code, e.g. <c>#,##0.00</c>. Empty means General.</summary>
    public string NumberFormatCode { get; init; } = string.Empty;

    public string FontName { get; init; } = "Calibri";
    public double FontSize { get; init; } = 11;
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool Strike { get; init; }
    public ArgbColor Foreground { get; init; } = new(255, 0, 0, 0);

    /// <summary>Cell background. Transparent when no fill is applied.</summary>
    public ArgbColor Background { get; init; } = ArgbColor.Transparent;

    public CellHorizontalAlignment HorizontalAlignment { get; init; } = CellHorizontalAlignment.General;
    public CellVerticalAlignment VerticalAlignment { get; init; } = CellVerticalAlignment.Bottom;
    public bool WrapText { get; init; }
    public int Indent { get; init; }

    /// <summary>Resolves <see cref="CellHorizontalAlignment.General"/> against the value being shown.</summary>
    public CellHorizontalAlignment EffectiveAlignment(CellValueKind kind)
    {
        if (this.HorizontalAlignment != CellHorizontalAlignment.General)
            return this.HorizontalAlignment;

        return kind switch
        {
            CellValueKind.Number => CellHorizontalAlignment.Right,
            CellValueKind.Boolean or CellValueKind.Error => CellHorizontalAlignment.Center,
            _ => CellHorizontalAlignment.Left
        };
    }
}
