namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class StatisticalFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        ConditionalFunctions.Register(registry);

        registry.Add("AVERAGE", 1, CalcFunction.Unlimited, a =>
        {
            var numbers = a.AggregateNumbers().ToList();
            return numbers.Count == 0 ? CalcValue.Error(CellError.Div0) : CalcValue.From(numbers.Average());
        });

        registry.Add("COUNT", 1, CalcFunction.Unlimited, a =>
            CalcValue.From((double)a.AllValues().Count(x => x.Kind == CellValueKind.Number)));

        registry.Add("COUNTA", 1, CalcFunction.Unlimited, a =>
            CalcValue.From((double)a.AllValues().Count(x => !x.IsBlank)));

        registry.Add("COUNTBLANK", 1, 1, a =>
            CalcValue.From((double)a.Value(0).Flatten().Count(x => x.IsBlank)));

        registry.Add("MIN", 1, CalcFunction.Unlimited, a =>
        {
            var numbers = a.AggregateNumbers().ToList();

            // MIN of nothing is 0 in Excel, not an error.
            return CalcValue.From(numbers.Count == 0 ? 0 : numbers.Min());
        });

        registry.Add("MAX", 1, CalcFunction.Unlimited, a =>
        {
            var numbers = a.AggregateNumbers().ToList();
            return CalcValue.From(numbers.Count == 0 ? 0 : numbers.Max());
        });

        registry.Add("MEDIAN", 1, CalcFunction.Unlimited, a =>
        {
            var numbers = a.AggregateNumbers().OrderBy(x => x).ToList();
            if (numbers.Count == 0)
                return CalcValue.Error(CellError.Num);

            var middle = numbers.Count / 2;
            return CalcValue.From(numbers.Count % 2 == 1
                ? numbers[middle]
                : (numbers[middle - 1] + numbers[middle]) / 2d);
        });

        registry.Add("LARGE", 2, 2, a => Nth(a, descending: true));
        registry.Add("SMALL", 2, 2, a => Nth(a, descending: false));

        registry.Add("STDEV", 1, CalcFunction.Unlimited, a => Deviation(a, sample: true));
        registry.Add("STDEVP", 1, CalcFunction.Unlimited, a => Deviation(a, sample: false));
        registry.Add("VAR", 1, CalcFunction.Unlimited, a => Variance(a, sample: true));
        registry.Add("VARP", 1, CalcFunction.Unlimited, a => Variance(a, sample: false));

        registry.Add("RANK", 2, 3, a =>
        {
            var target = a.Number(0);
            var numbers = a.Value(1).Flatten()
                .Where(x => x.Kind == CellValueKind.Number)
                .Select(x => x.AsNumber())
                .ToList();

            if (!numbers.Contains(target))
                return CalcValue.Error(CellError.NotAvailable);

            // A non-zero third argument ranks ascending.
            var ascending = a.NumberOrDefault(2, 0) != 0;
            var better = ascending
                ? numbers.Count(x => x < target)
                : numbers.Count(x => x > target);

            return CalcValue.From((double)(better + 1));
        });
    }

    static CalcValue Nth(CalcArguments a, bool descending)
    {
        var numbers = a.Value(0).Flatten()
            .Where(x => x.Kind == CellValueKind.Number)
            .Select(x => x.AsNumber())
            .ToList();

        var k = a.Integer(1);
        if (k < 1 || k > numbers.Count)
            return CalcValue.Error(CellError.Num);

        numbers.Sort();
        return CalcValue.From(descending ? numbers[^k] : numbers[k - 1]);
    }

    static CalcValue Variance(CalcArguments a, bool sample)
    {
        var numbers = a.AggregateNumbers().ToList();
        var divisor = sample ? numbers.Count - 1 : numbers.Count;
        if (divisor <= 0)
            return CalcValue.Error(CellError.Div0);

        var mean = numbers.Average();
        var sum = numbers.Sum(x => (x - mean) * (x - mean));
        return CalcValue.From(sum / divisor);
    }

    static CalcValue Deviation(CalcArguments a, bool sample)
    {
        var variance = Variance(a, sample);
        return variance.IsError ? variance : CalcValue.From(Math.Sqrt(variance.Scalar.AsNumber()));
    }
}
