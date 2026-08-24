namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// What an expression evaluates to: either a single value, or a rectangle of them.
/// </summary>
/// <remarks>
/// Ranges keep their shape rather than collapsing to a list because functions treat them differently —
/// SUM flattens, INDEX indexes by row and column, and comparison operators broadcast element-wise.
/// </remarks>
public readonly struct CalcValue
{
    readonly CellValue scalar;
    readonly CalcArray? array;

    CalcValue(CellValue scalar)
    {
        this.scalar = scalar;
        this.array = null;
    }

    CalcValue(CalcArray array)
    {
        this.scalar = CellValue.Blank;
        this.array = array;
    }

    public static CalcValue From(CellValue value) => new(value);
    public static CalcValue From(double number) => new(CellValue.FromNumber(number));
    public static CalcValue From(string text) => new(CellValue.FromText(text));
    public static CalcValue From(bool value) => new(CellValue.FromBoolean(value));
    public static CalcValue Error(CellError error) => new(CellValue.FromError(error));
    public static CalcValue From(CalcArray array) => new(array);

    public static readonly CalcValue Blank = new(CellValue.Blank);

    public bool IsArray => this.array is not null;
    public CalcArray Array => this.array ?? throw new InvalidOperationException("Not an array.");

    /// <summary>
    /// Collapses an array to the single value an operator should use. Excel takes the top-left cell,
    /// which is why <c>=A1:A5*2</c> in a non-array context uses A1.
    /// </summary>
    public CellValue Scalar => this.array is null
        ? this.scalar
        : this.array.Count == 0 ? CellValue.FromError(CellError.Value) : this.array[0, 0];

    public bool IsError => this.Scalar.IsError;

    /// <summary>Enumerates every value, row by row. A scalar yields itself once.</summary>
    public IEnumerable<CellValue> Flatten()
    {
        if (this.array is null)
        {
            yield return this.scalar;
            yield break;
        }

        for (var row = 0; row < this.array.RowCount; row++)
            for (var column = 0; column < this.array.ColumnCount; column++)
                yield return this.array[row, column];
    }
}

/// <summary>A rectangle of values, addressed by zero-based row and column within the rectangle.</summary>
public sealed class CalcArray
{
    readonly CellValue[] values;

    public CalcArray(int rowCount, int columnCount)
    {
        this.RowCount = rowCount;
        this.ColumnCount = columnCount;
        this.values = new CellValue[rowCount * columnCount];
    }

    public int RowCount { get; }
    public int ColumnCount { get; }
    public int Count => this.values.Length;

    public CellValue this[int row, int column]
    {
        get => this.values[row * this.ColumnCount + column];
        set => this.values[row * this.ColumnCount + column] = value;
    }
}
