namespace Shiny.Maui.Controls.DataGrid;

/// <summary>Null-safe value comparer used for column sorting (nulls sort first).</summary>
sealed class DataGridValueComparer : IComparer<object?>
{
    public static readonly DataGridValueComparer Instance = new();

    public int Compare(object? x, object? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is IComparable cx && x.GetType() == y.GetType())
            return cx.CompareTo(y);

        if (x is IComparable cx2)
        {
            try { return cx2.CompareTo(y); }
            catch { /* fall through */ }
        }

        return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
    }
}
