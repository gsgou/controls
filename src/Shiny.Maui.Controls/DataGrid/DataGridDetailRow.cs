namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// Internal display item for the expanded detail ("breakdown") row that sits under its parent row in
/// the flattened item list. <see cref="DataGrid.RowDetailTemplate"/> content binds to <see cref="Data"/>.
/// </summary>
sealed class DataGridDetailRow
{
    public DataGridDetailRow(object data, bool isLoading)
    {
        this.Data = data;
        this.IsLoading = isLoading;
    }

    /// <summary>The parent row's data item - the detail template's BindingContext.</summary>
    public object Data { get; }

    /// <summary>
    /// True while <see cref="DataGrid.RowDetailLoader"/> is still running for this row. The template
    /// selector reads it, so the loading and loaded states are two different views rather than one
    /// view toggling itself - a detail template is free to assume its data has arrived.
    /// </summary>
    public bool IsLoading { get; }
}
