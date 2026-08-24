using System.Globalization;

namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// Routes a bound cell value through <see cref="DataGridColumn.FormatValue"/> so the cell shows
/// exactly the text the quick filter searches and the group headers repeat. Kept as a converter (and
/// not the binding's own <c>StringFormat</c>) because presets, prefix/suffix, the null placeholder
/// and <see cref="DataGridColumn.TextFormatter"/> cannot be expressed as a format string.
/// </summary>
sealed class DataGridCellTextConverter : IValueConverter
{
    readonly DataGridColumn column;

    public DataGridCellTextConverter(DataGridColumn column) => this.column = column;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => this.column.FormatValue(value);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("DataGrid cell text is one-way; inline editing writes through DataGridColumn.SetCellValue.");
}
