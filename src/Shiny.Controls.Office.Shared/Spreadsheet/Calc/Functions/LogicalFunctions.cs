namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class LogicalFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        // IF must not evaluate the untaken branch, which is why it reads arguments lazily rather than
        // taking pre-evaluated values: =IF(A1=0,"",1/A1) exists precisely to avoid #DIV/0!.
        registry.Add("IF", 2, 3, a =>
        {
            var condition = a.Scalar(0);
            if (condition.IsError)
                return CalcValue.From(condition);

            if (!Coercion.TryToBoolean(condition, out var result, out var error))
                return CalcValue.Error(error);

            if (result)
                return a.IsMissing(1) ? CalcValue.From(0d) : a.Value(1);

            if (a.Count < 3 || a.IsMissing(2))
                return CalcValue.From(false);

            return a.Value(2);
        });

        registry.Add("IFERROR", 2, 2, a =>
        {
            var value = a.Value(0);
            return value.IsError ? a.Value(1) : value;
        });

        registry.Add("IFNA", 2, 2, a =>
        {
            var value = a.Value(0);
            var scalar = value.Scalar;
            return scalar.IsError && scalar.AsError() == CellError.NotAvailable ? a.Value(1) : value;
        });

        registry.Add("IFS", 2, CalcFunction.Unlimited, a =>
        {
            for (var i = 0; i + 1 < a.Count; i += 2)
            {
                var condition = a.Scalar(i);
                if (condition.IsError)
                    return CalcValue.From(condition);

                if (!Coercion.TryToBoolean(condition, out var result, out var error))
                    return CalcValue.Error(error);

                if (result)
                    return a.Value(i + 1);
            }

            return CalcValue.Error(CellError.NotAvailable);
        });

        registry.Add("AND", 1, CalcFunction.Unlimited, a => Fold(a, all: true));
        registry.Add("OR", 1, CalcFunction.Unlimited, a => Fold(a, all: false));

        registry.Add("XOR", 1, CalcFunction.Unlimited, a =>
        {
            var trueCount = 0;
            var any = false;
            foreach (var value in a.AllValues())
            {
                if (value.IsError)
                    return CalcValue.From(value);

                if (value.IsBlank)
                    continue;

                if (!Coercion.TryToBoolean(value, out var flag, out _))
                    continue;

                any = true;
                if (flag)
                    trueCount++;
            }

            return any ? CalcValue.From(trueCount % 2 == 1) : CalcValue.Error(CellError.Value);
        });

        registry.Add("NOT", 1, 1, a => CalcValue.From(!a.Boolean(0)));
        registry.Add("TRUE", 0, 0, _ => CalcValue.From(true));
        registry.Add("FALSE", 0, 0, _ => CalcValue.From(false));

        registry.Add("CHOOSE", 2, CalcFunction.Unlimited, a =>
        {
            var index = a.Integer(0);
            if (index < 1 || index >= a.Count)
                return CalcValue.Error(CellError.Value);

            return a.Value(index);
        });

        registry.Add("SWITCH", 3, CalcFunction.Unlimited, a =>
        {
            var subject = a.Checked(0);
            var i = 1;
            for (; i + 1 < a.Count; i += 2)
            {
                if (Coercion.Compare(subject, a.Checked(i)) == 0)
                    return a.Value(i + 1);
            }

            // A trailing odd argument is the default.
            return i < a.Count ? a.Value(i) : CalcValue.Error(CellError.NotAvailable);
        });
    }

    static CalcValue Fold(CalcArguments a, bool all)
    {
        var any = false;
        var accumulated = all;

        foreach (var value in a.AllValues())
        {
            if (value.IsError)
                return CalcValue.From(value);

            // Blanks and text inside a range are ignored rather than treated as FALSE.
            if (value.IsBlank || value.Kind == CellValueKind.Text)
                continue;

            if (!Coercion.TryToBoolean(value, out var flag, out _))
                continue;

            any = true;
            accumulated = all ? accumulated && flag : accumulated || flag;
        }

        return any ? CalcValue.From(accumulated) : CalcValue.Error(CellError.Value);
    }
}
