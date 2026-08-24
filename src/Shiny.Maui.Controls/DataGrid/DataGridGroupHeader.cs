namespace Shiny.Maui.Controls.DataGrid;

/// <summary>Internal display item representing a group header row in the flattened item list.</summary>
sealed class DataGridGroupHeader
{
    public DataGridGroupHeader(object? key, string? keyText, string title, int count, bool collapsed, IReadOnlyList<object> items)
    {
        this.Key = key;
        this.KeyText = keyText;
        this.Title = title;
        this.Count = count;
        this.Collapsed = collapsed;
        this.Items = items;
    }

    public object? Key { get; }

    /// <summary>
    /// The key run through the grouped column's formatting, so a group header reads the same as the
    /// cells under it ("Salary: $45,000", not "Salary: 45000"). <c>Key</c> stays the raw value because
    /// collapse state is tracked by it.
    /// </summary>
    public string? KeyText { get; }

    public string Title { get; }
    public int Count { get; }
    public bool Collapsed { get; }
    public IReadOnlyList<object> Items { get; }

    public string CaretGlyph => this.Collapsed ? "▸" : "▾";
    public string Display => $"{this.Title}: {this.KeyText ?? this.Key?.ToString()} ({this.Count})";
}

sealed class DataGridItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate RowTemplate { get; set; } = default!;
    public DataTemplate GroupTemplate { get; set; } = default!;
    public DataTemplate EditRowTemplate { get; set; } = default!;
    public DataTemplate DetailTemplate { get; set; } = default!;
    public DataTemplate DetailLoadingTemplate { get; set; } = default!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is DataGridGroupHeader)
            return this.GroupTemplate;
        if (item is DataGridDetailRow detail)
            return detail.IsLoading ? this.DetailLoadingTemplate : this.DetailTemplate;
        if (item is DataGridRow { IsEditing: true })
            return this.EditRowTemplate;
        return this.RowTemplate;
    }
}
