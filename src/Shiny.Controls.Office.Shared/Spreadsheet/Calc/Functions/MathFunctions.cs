namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class MathFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Add("SUM", 1, CalcFunction.Unlimited, a => CalcValue.From(a.AggregateNumbers().Sum()));
        registry.Add("PRODUCT", 1, CalcFunction.Unlimited, a =>
        {
            var any = false;
            var product = 1d;
            foreach (var number in a.AggregateNumbers())
            {
                product *= number;
                any = true;
            }

            return CalcValue.From(any ? product : 0);
        });

        registry.Add("ABS", 1, 1, a => CalcValue.From(Math.Abs(a.Number(0))));
        registry.Add("SIGN", 1, 1, a => CalcValue.From((double)Math.Sign(a.Number(0))));
        registry.Add("INT", 1, 1, a => CalcValue.From(Math.Floor(a.Number(0))));
        registry.Add("TRUNC", 1, 2, a => CalcValue.From(Scale(a.Number(0), a.IntegerOrDefault(1, 0), Math.Truncate)));
        registry.Add("ROUND", 2, 2, a => CalcValue.From(Scale(a.Number(0), a.Integer(1), HalfAwayFromZero)));
        registry.Add("ROUNDUP", 2, 2, a => CalcValue.From(Scale(a.Number(0), a.Integer(1), AwayFromZero)));
        registry.Add("ROUNDDOWN", 2, 2, a => CalcValue.From(Scale(a.Number(0), a.Integer(1), Math.Truncate)));

        registry.Add("MOD", 2, 2, a =>
        {
            var divisor = a.Number(1);
            if (divisor == 0)
                return CalcValue.Error(CellError.Div0);

            // Excel's MOD takes the sign of the divisor, unlike C#'s % which takes the dividend's.
            var result = a.Number(0) - divisor * Math.Floor(a.Number(0) / divisor);
            return CalcValue.From(result);
        });

        registry.Add("POWER", 2, 2, a =>
        {
            var result = Math.Pow(a.Number(0), a.Number(1));
            return double.IsNaN(result) || double.IsInfinity(result) ? CalcValue.Error(CellError.Num) : CalcValue.From(result);
        });

        registry.Add("SQRT", 1, 1, a =>
        {
            var value = a.Number(0);
            return value < 0 ? CalcValue.Error(CellError.Num) : CalcValue.From(Math.Sqrt(value));
        });

        registry.Add("EXP", 1, 1, a => CalcValue.From(Math.Exp(a.Number(0))));
        registry.Add("LN", 1, 1, a => Positive(a.Number(0), Math.Log));
        registry.Add("LOG10", 1, 1, a => Positive(a.Number(0), Math.Log10));
        registry.Add("LOG", 1, 2, a =>
        {
            var value = a.Number(0);
            var logBase = a.NumberOrDefault(1, 10);
            if (value <= 0 || logBase <= 0 || logBase == 1)
                return CalcValue.Error(CellError.Num);

            return CalcValue.From(Math.Log(value, logBase));
        });

        registry.Add("PI", 0, 0, _ => CalcValue.From(Math.PI));
        registry.Add("SIN", 1, 1, a => CalcValue.From(Math.Sin(a.Number(0))));
        registry.Add("COS", 1, 1, a => CalcValue.From(Math.Cos(a.Number(0))));
        registry.Add("TAN", 1, 1, a => CalcValue.From(Math.Tan(a.Number(0))));
        registry.Add("ATAN", 1, 1, a => CalcValue.From(Math.Atan(a.Number(0))));
        registry.Add("ATAN2", 2, 2, a => CalcValue.From(Math.Atan2(a.Number(1), a.Number(0))));
        registry.Add("DEGREES", 1, 1, a => CalcValue.From(a.Number(0) * 180d / Math.PI));
        registry.Add("RADIANS", 1, 1, a => CalcValue.From(a.Number(0) * Math.PI / 180d));

        registry.Add("CEILING", 2, 2, a => Step(a, Math.Ceiling));
        registry.Add("FLOOR", 2, 2, a => Step(a, Math.Floor));

        registry.Add("SUMPRODUCT", 1, CalcFunction.Unlimited, a =>
        {
            var arrays = new List<IReadOnlyList<CellValue>>();
            for (var i = 0; i < a.Count; i++)
                arrays.Add(a.Value(i).Flatten().ToList());

            var length = arrays[0].Count;
            if (arrays.Any(x => x.Count != length))
                return CalcValue.Error(CellError.Value);

            var total = 0d;
            for (var i = 0; i < length; i++)
            {
                var term = 1d;
                foreach (var array in arrays)
                {
                    var value = array[i];
                    if (value.IsError)
                        return CalcValue.From(value);

                    // Non-numeric entries count as zero rather than failing the whole product.
                    term *= value.Kind == CellValueKind.Number ? value.AsNumber() : 0d;
                }

                total += term;
            }

            return CalcValue.From(total);
        });

        registry.Add("SUMIF", 2, 3, a => ConditionalFunctions.SumIf(a));
        registry.Add("SUMIFS", 3, CalcFunction.Unlimited, a => ConditionalFunctions.SumIfs(a));
    }

    static CalcValue Positive(double value, Func<double, double> f)
        => value <= 0 ? CalcValue.Error(CellError.Num) : CalcValue.From(f(value));

    static CalcValue Step(CalcArguments a, Func<double, double> rounder)
    {
        var value = a.Number(0);
        var significance = a.Number(1);
        if (significance == 0)
            return CalcValue.From(0);

        if (value > 0 && significance < 0)
            return CalcValue.Error(CellError.Num);

        return CalcValue.From(rounder(value / significance) * significance);
    }

    /// <summary>Applies a rounding mode at a given number of decimal places, including negative places.</summary>
    static double Scale(double value, int digits, Func<double, double> rounder)
    {
        var factor = Math.Pow(10, digits);
        var scaled = value * factor;

        // Re-round the scaled value first: 2.675*100 is 267.49999999999997 in binary floating point, and
        // rounding that directly gives 2.67 where Excel shows 2.68.
        var corrected = Math.Round(scaled, 10, MidpointRounding.AwayFromZero);
        return rounder(corrected) / factor;
    }

    static double HalfAwayFromZero(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

    static double AwayFromZero(double value) => value < 0 ? Math.Floor(value) : Math.Ceiling(value);
}
