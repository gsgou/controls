namespace Shiny.Controls.Office.Spreadsheet;

public enum CellValueKind
{
    Blank,
    Number,
    Text,
    Boolean,
    Error
}

/// <summary>
/// The Excel error values. These are first-class values, not exceptions — a formula that
/// divides by zero produces <see cref="Div0"/> and calculation carries on.
/// </summary>
public enum CellError
{
    Null,       // #NULL!
    Div0,       // #DIV/0!
    Value,      // #VALUE!
    Ref,        // #REF!
    Name,       // #NAME?
    Num,        // #NUM!
    NotAvailable // #N/A
}

/// <summary>
/// A cell's value as one of Excel's five storage types.
/// </summary>
/// <remarks>
/// Dates are deliberately absent: Excel has no date type. A date is a <see cref="Number"/> wearing a
/// date number format, and conflating the two is how round-tripping starts corrupting values.
/// </remarks>
public readonly struct CellValue : IEquatable<CellValue>
{
    readonly double number;
    readonly string? text;

    CellValue(CellValueKind kind, double number, string? text)
    {
        this.Kind = kind;
        this.number = number;
        this.text = text;
    }

    public CellValueKind Kind { get; }

    public static CellValue Blank => default;

    public static CellValue FromNumber(double value) => new(CellValueKind.Number, value, null);
    public static CellValue FromText(string value) => new(CellValueKind.Text, 0, value ?? string.Empty);
    public static CellValue FromBoolean(bool value) => new(CellValueKind.Boolean, value ? 1 : 0, null);
    public static CellValue FromError(CellError error) => new(CellValueKind.Error, (double)error, null);

    public bool IsBlank => this.Kind == CellValueKind.Blank;
    public bool IsError => this.Kind == CellValueKind.Error;

    public double AsNumber() => this.Kind switch
    {
        CellValueKind.Number or CellValueKind.Boolean => this.number,
        _ => throw new InvalidOperationException($"Cell value of kind {this.Kind} is not a number.")
    };

    public string AsText() => this.Kind == CellValueKind.Text
        ? this.text!
        : throw new InvalidOperationException($"Cell value of kind {this.Kind} is not text.");

    public bool AsBoolean() => this.Kind == CellValueKind.Boolean
        ? this.number != 0
        : throw new InvalidOperationException($"Cell value of kind {this.Kind} is not a boolean.");

    public CellError AsError() => this.Kind == CellValueKind.Error
        ? (CellError)(int)this.number
        : throw new InvalidOperationException($"Cell value of kind {this.Kind} is not an error.");

    public static string ErrorText(CellError error) => error switch
    {
        CellError.Null => "#NULL!",
        CellError.Div0 => "#DIV/0!",
        CellError.Value => "#VALUE!",
        CellError.Ref => "#REF!",
        CellError.Name => "#NAME?",
        CellError.Num => "#NUM!",
        CellError.NotAvailable => "#N/A",
        _ => "#VALUE!"
    };

    public static bool TryParseError(string? text, out CellError error)
    {
        switch (text)
        {
            case "#NULL!": error = CellError.Null; return true;
            case "#DIV/0!": error = CellError.Div0; return true;
            case "#VALUE!": error = CellError.Value; return true;
            case "#REF!": error = CellError.Ref; return true;
            case "#NAME?": error = CellError.Name; return true;
            case "#NUM!": error = CellError.Num; return true;
            case "#N/A": error = CellError.NotAvailable; return true;
            default: error = default; return false;
        }
    }

    public bool Equals(CellValue other)
        => this.Kind == other.Kind && this.number.Equals(other.number) && this.text == other.text;

    public override bool Equals(object? obj) => obj is CellValue other && this.Equals(other);
    public override int GetHashCode() => HashCode.Combine(this.Kind, this.number, this.text);
    public static bool operator ==(CellValue left, CellValue right) => left.Equals(right);
    public static bool operator !=(CellValue left, CellValue right) => !left.Equals(right);

    public override string ToString() => this.Kind switch
    {
        CellValueKind.Blank => string.Empty,
        CellValueKind.Number => this.number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        CellValueKind.Text => this.text!,
        CellValueKind.Boolean => this.number != 0 ? "TRUE" : "FALSE",
        CellValueKind.Error => ErrorText(this.AsError()),
        _ => string.Empty
    };
}
