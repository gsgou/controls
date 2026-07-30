namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// A column whose cells are rendered entirely by <see cref="DataGridColumn.CellTemplate"/> (and
/// optional <see cref="DataGridColumn.EditTemplate"/>). Not sortable/filterable by default.
/// </summary>
public class DataGridTemplateColumn : DataGridColumn
{
    public DataGridTemplateColumn()
    {
        this.Sortable = false;
        this.Filterable = false;
        this.Groupable = false;
    }

    internal override bool HasValue => false;

    internal override object? GetCellValue(object? item) => null;

    internal override string? GetText(object? item) => null;
}
