namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// The argument list handed to a function implementation.
/// </summary>
/// <remarks>
/// Arguments are evaluated lazily. IF must not evaluate the branch it does not take — otherwise
/// <c>=IF(A1=0,"",1/A1)</c> returns #DIV/0! for exactly the case it was written to guard against.
/// </remarks>
public sealed class CalcArguments(IReadOnlyList<FormulaNode> nodes, FormulaEvaluator evaluator, ICalcContext context)
{
    readonly CalcValue?[] evaluated = new CalcValue?[nodes.Count];

    public int Count => nodes.Count;

    public ICalcContext Context => context;

    public bool IsMissing(int index) => index >= nodes.Count || nodes[index] is MissingArgumentNode;

    /// <summary>Evaluates an argument, caching the result so repeated access does not re-evaluate.</summary>
    public CalcValue Value(int index)
    {
        if (index >= nodes.Count)
            return CalcValue.Blank;

        return this.evaluated[index] ??= evaluator.EvaluateNode(nodes[index], context);
    }

    public CellValue Scalar(int index) => this.Value(index).Scalar;

    /// <summary>Evaluates an argument and throws the Excel error if it is one.</summary>
    public CellValue Checked(int index)
    {
        var value = this.Scalar(index);
        if (value.IsError)
            throw new CalcErrorException(value.AsError());

        return value;
    }

    public double Number(int index)
    {
        var value = this.Checked(index);
        if (!Coercion.TryToNumber(value, out var number, out var error))
            throw new CalcErrorException(error);

        return number;
    }

    public double NumberOrDefault(int index, double fallback)
        => this.IsMissing(index) ? fallback : this.Number(index);

    public int Integer(int index) => (int)Math.Truncate(this.Number(index));

    public int IntegerOrDefault(int index, int fallback)
        => this.IsMissing(index) ? fallback : this.Integer(index);

    public string Text(int index) => Coercion.ToText(this.Checked(index));

    public bool Boolean(int index)
    {
        var value = this.Checked(index);
        if (!Coercion.TryToBoolean(value, out var result, out var error))
            throw new CalcErrorException(error);

        return result;
    }

    public bool BooleanOrDefault(int index, bool fallback)
        => this.IsMissing(index) ? fallback : this.Boolean(index);

    /// <summary>
    /// Every value across every argument, flattened. Used by the aggregate functions, which take any
    /// mix of scalars and ranges.
    /// </summary>
    public IEnumerable<CellValue> AllValues()
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            foreach (var value in this.Value(i).Flatten())
                yield return value;
        }
    }

    /// <summary>
    /// The numbers an aggregate function should use: blanks, text and booleans inside ranges are
    /// skipped, matching SUM and AVERAGE. Errors propagate.
    /// </summary>
    public IEnumerable<double> AggregateNumbers()
    {
        foreach (var value in this.AllValues())
        {
            if (value.IsError)
                throw new CalcErrorException(value.AsError());

            if (value.Kind == CellValueKind.Number)
                yield return value.AsNumber();
        }
    }

    /// <summary>The raw range behind an argument, or null when it is not a reference.</summary>
    public CalcArray? AsArray(int index)
    {
        var value = this.Value(index);
        return value.IsArray ? value.Array : null;
    }

    public FormulaNode Node(int index) => nodes[index];
}
