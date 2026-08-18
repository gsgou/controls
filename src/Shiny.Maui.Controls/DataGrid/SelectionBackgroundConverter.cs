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

    /// <summary>
    /// Flattens the result onto <see cref="Surface"/> instead of returning a translucent tint.
    /// Frozen-column panes need this: they sit over the scrolling cells, so anything less than
    /// opaque lets the content they are supposed to hide read straight through.
    /// </summary>
    public bool Opaque { get; set; }

    public Color Surface { get; set; } = Colors.White;

    public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSelected = values is { Length: > 0 } && values[0] is true;
        if (isSelected)
            return this.Resolve(this.Selected);

        if (this.StripedEnabled && values is { Length: > 1 } && values[1] is int index && index % 2 == 1)
            return this.Resolve(this.Stripe);

        return this.Opaque ? this.Surface : Colors.Transparent;
    }

    Color Resolve(Color tint)
    {
        if (!this.Opaque)
            return tint;

        var a = tint.Alpha;
        return new Color(
            (float)(tint.Red * a + this.Surface.Red * (1 - a)),
            (float)(tint.Green * a + this.Surface.Green * (1 - a)),
            (float)(tint.Blue * a + this.Surface.Blue * (1 - a)),
            1f
        );
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
