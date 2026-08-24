namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class LookupFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Add("VLOOKUP", 3, 4, a => Lookup(a, byColumn: true));
        registry.Add("HLOOKUP", 3, 4, a => Lookup(a, byColumn: false));

        registry.Add("INDEX", 2, 3, a =>
        {
            var array = a.AsArray(0);
            if (array is null)
            {
                // A scalar behaves as a 1x1 array, so INDEX(A1,1,1) is legal.
                var scalar = a.Scalar(0);
                return a.Integer(1) == 1 && a.IntegerOrDefault(2, 1) == 1
                    ? CalcValue.From(scalar)
                    : CalcValue.Error(CellError.Ref);
            }

            var rowArgument = a.Integer(1);
            var columnArgument = a.IntegerOrDefault(2, 0);

            // A single-row or single-column range lets the caller pass one index, and it means whichever
            // axis is not degenerate.
            if (a.IsMissing(2) || a.Count < 3)
            {
                if (array.RowCount == 1)
                {
                    columnArgument = rowArgument;
                    rowArgument = 1;
                }
                else if (array.ColumnCount == 1)
                {
                    columnArgument = 1;
                }
            }

            if (rowArgument == 0 || columnArgument == 0)
                return CalcValue.Error(CellError.Ref);

            if (rowArgument < 1 || rowArgument > array.RowCount || columnArgument < 1 || columnArgument > array.ColumnCount)
                return CalcValue.Error(CellError.Ref);

            return CalcValue.From(array[rowArgument - 1, columnArgument - 1]);
        });

        registry.Add("MATCH", 2, 3, a =>
        {
            var needle = a.Checked(0);
            var haystack = a.Value(1).Flatten().ToList();
            var type = a.IntegerOrDefault(2, 1);

            switch (type)
            {
                case 0:
                    var predicate = ConditionalFunctions.Parse(needle);
                    for (var i = 0; i < haystack.Count; i++)
                    {
                        if (predicate(haystack[i]))
                            return CalcValue.From((double)(i + 1));
                    }

                    return CalcValue.Error(CellError.NotAvailable);

                case 1:
                    // Ascending data: the last value less than or equal to the needle.
                    return Ordered(haystack, needle, (candidate, target) => Coercion.Compare(candidate, target) <= 0);

                default:
                    // Descending data: the last value greater than or equal to the needle.
                    return Ordered(haystack, needle, (candidate, target) => Coercion.Compare(candidate, target) >= 0);
            }
        });

        registry.Add("ROW", 0, 1, a =>
        {
            if (a.Count == 0 || a.IsMissing(0))
                return CalcValue.From((double)(a.Context.CurrentCell.Row + 1));

            return a.Node(0) switch
            {
                ReferenceNode reference => CalcValue.From((double)(reference.Cell.Row + 1)),
                RangeNode range => CalcValue.From((double)(range.Range.Top + 1)),
                _ => CalcValue.Error(CellError.Value)
            };
        });

        registry.Add("COLUMN", 0, 1, a =>
        {
            if (a.Count == 0 || a.IsMissing(0))
                return CalcValue.From((double)(a.Context.CurrentCell.Column + 1));

            return a.Node(0) switch
            {
                ReferenceNode reference => CalcValue.From((double)(reference.Cell.Column + 1)),
                RangeNode range => CalcValue.From((double)(range.Range.Left + 1)),
                _ => CalcValue.Error(CellError.Value)
            };
        });

        registry.Add("ROWS", 1, 1, a =>
        {
            var array = a.AsArray(0);
            return CalcValue.From((double)(array?.RowCount ?? 1));
        });

        registry.Add("COLUMNS", 1, 1, a =>
        {
            var array = a.AsArray(0);
            return CalcValue.From((double)(array?.ColumnCount ?? 1));
        });
    }

    static CalcValue Ordered(List<CellValue> haystack, CellValue needle, Func<CellValue, CellValue, bool> acceptable)
    {
        var best = -1;
        for (var i = 0; i < haystack.Count; i++)
        {
            if (haystack[i].IsBlank)
                continue;

            if (acceptable(haystack[i], needle))
                best = i;
            else
                break;
        }

        return best < 0 ? CalcValue.Error(CellError.NotAvailable) : CalcValue.From((double)(best + 1));
    }

    static CalcValue Lookup(CalcArguments a, bool byColumn)
    {
        var needle = a.Checked(0);
        var table = a.AsArray(1);
        if (table is null)
            return CalcValue.Error(CellError.NotAvailable);

        var index = a.Integer(2);
        var approximate = a.BooleanOrDefault(3, true);

        var limit = byColumn ? table.ColumnCount : table.RowCount;
        if (index < 1 || index > limit)
            return CalcValue.Error(CellError.Ref);

        var length = byColumn ? table.RowCount : table.ColumnCount;

        if (!approximate)
        {
            var predicate = ConditionalFunctions.Parse(needle);
            for (var i = 0; i < length; i++)
            {
                var key = byColumn ? table[i, 0] : table[0, i];
                if (predicate(key))
                    return CalcValue.From(byColumn ? table[i, index - 1] : table[index - 1, i]);
            }

            return CalcValue.Error(CellError.NotAvailable);
        }

        // Approximate match assumes the first column is sorted ascending and takes the last key that is
        // not greater than the needle. On unsorted data this silently returns the wrong row - which is
        // Excel's behaviour too, and the reason FALSE is almost always what you want.
        var best = -1;
        for (var i = 0; i < length; i++)
        {
            var key = byColumn ? table[i, 0] : table[0, i];
            if (key.IsBlank)
                continue;

            if (Coercion.Compare(key, needle) <= 0)
                best = i;
            else
                break;
        }

        if (best < 0)
            return CalcValue.Error(CellError.NotAvailable);

        return CalcValue.From(byColumn ? table[best, index - 1] : table[index - 1, best]);
    }
}
