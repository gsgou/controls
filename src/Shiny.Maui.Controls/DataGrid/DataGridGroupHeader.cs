namespace Shiny.Maui.Controls.DataGrid;

/// <summary>Internal display item representing a group header row in the flattened item list.</summary>
sealed class DataGridGroupHeader
{
    public DataGridGroupHeader(object? key, string title, int count, bool collapsed, IReadOnlyList<object> items)
    {
        this.Key = key;
        this.Title = title;
        this.Count = count;
        this.Collapsed = collapsed;
        this.Items = items;
    }

    public object? Key { get; }
    public string Title { get; }
    public int Count { get; }
    public bool Collapsed { get; }
    public IReadOnlyList<object> Items { get; }

    public string CaretGlyph => this.Collapsed ? "▸" : "▾";
    public string Display => $"{this.Title}: {this.Key} ({this.Count})";
}

sealed class DataGridItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate RowTemplate { get; set; } = default!;
    public DataTemplate GroupTemplate { get; set; } = default!;
    public DataTemplate EditRowTemplate { get; set; } = default!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is DataGridGroupHeader)
            return this.GroupTemplate;
        if (item is DataGridRow { IsEditing: true })
            return this.EditRowTemplate;
        return this.RowTemplate;
    }
}
