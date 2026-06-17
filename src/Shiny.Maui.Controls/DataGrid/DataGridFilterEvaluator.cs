namespace Shiny.Maui.Controls.DataGrid;

/// <summary>Evaluates a <see cref="DataGridFilterDefinition"/> operator against a cell value.</summary>
static class DataGridFilterEvaluator
{
    public static bool Matches(object? cellValue, DataGridFilterOperator op, object? filterValue)
    {
        switch (op)
        {
            case DataGridFilterOperator.Empty:
                return cellValue is null || string.IsNullOrEmpty(cellValue.ToString());
            case DataGridFilterOperator.NotEmpty:
                return cellValue is not null && !string.IsNullOrEmpty(cellValue.ToString());
        }

        if (filterValue is null || (filterValue is string fs && fs.Length == 0))
            return true;

        switch (op)
        {
            case DataGridFilterOperator.Contains:
                return Text(cellValue).Contains(Text(filterValue), StringComparison.CurrentCultureIgnoreCase);
            case DataGridFilterOperator.NotContains:
                return !Text(cellValue).Contains(Text(filterValue), StringComparison.CurrentCultureIgnoreCase);
            case DataGridFilterOperator.StartsWith:
                return Text(cellValue).StartsWith(Text(filterValue), StringComparison.CurrentCultureIgnoreCase);
            case DataGridFilterOperator.EndsWith:
                return Text(cellValue).EndsWith(Text(filterValue), StringComparison.CurrentCultureIgnoreCase);
            case DataGridFilterOperator.Equals:
            case DataGridFilterOperator.Is:
                return ValuesEqual(cellValue, filterValue);
            case DataGridFilterOperator.NotEquals:
            case DataGridFilterOperator.IsNot:
                return !ValuesEqual(cellValue, filterValue);
            case DataGridFilterOperator.GreaterThan:
                return Compare(cellValue, filterValue) > 0;
            case DataGridFilterOperator.GreaterThanOrEqual:
                return Compare(cellValue, filterValue) >= 0;
            case DataGridFilterOperator.LessThan:
                return Compare(cellValue, filterValue) < 0;
            case DataGridFilterOperator.LessThanOrEqual:
                return Compare(cellValue, filterValue) <= 0;
            default:
                return true;
        }
    }

    static string Text(object? v) => v?.ToString() ?? string.Empty;

    static bool ValuesEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.GetType() == b.GetType()) return a.Equals(b);
        return string.Equals(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }

    static int Compare(object? a, object? b)
    {
        if (a is null || b is null) return DataGridValueComparer.Instance.Compare(a, b);
        if (a.GetType() != b.GetType())
        {
            try { b = System.Convert.ChangeType(b, a.GetType(), System.Globalization.CultureInfo.CurrentCulture); }
            catch { /* compare as-is */ }
        }
        return DataGridValueComparer.Instance.Compare(a, b);
    }
}
