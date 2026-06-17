using System.Globalization;

namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// Computes a row's background color from [IsSelected, RowIndex]: selected wins, otherwise an
/// alternating stripe color when striping is enabled, otherwise transparent.
/// </summary>
sealed class SelectionBackgroundConverter : IMultiValueConverter
{
    public Color Selected { get; set; } = Color.FromArgb("#1F7C3AED");
    public Color Stripe { get; set; } = Color.FromArgb("#0A000000");
    public bool StripedEnabled { get; set; }

    public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSelected = values is { Length: > 0 } && values[0] is true;
        if (isSelected)
            return this.Selected;

        if (this.StripedEnabled && values is { Length: > 1 } && values[1] is int index && index % 2 == 1)
            return this.Stripe;

        return Colors.Transparent;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
