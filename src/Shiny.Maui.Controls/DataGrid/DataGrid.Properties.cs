using System.Collections;
using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.DataGrid;

public partial class DataGrid
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(DataGrid), null,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).OnItemsSourceChanged();
            }));

    public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
        nameof(SelectionMode), typeof(DataGridSelectionMode), typeof(DataGrid), DataGridSelectionMode.None,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem), typeof(object), typeof(DataGrid), null, BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).OnSelectedItemChanged(n);
            }));

    public static readonly BindableProperty SelectedItemsProperty = BindableProperty.Create(
        nameof(SelectedItems), typeof(IList), typeof(DataGrid), null, BindingMode.TwoWay);

    public static readonly BindableProperty DenseProperty = BindableProperty.Create(
        nameof(Dense), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty StripedProperty = BindableProperty.Create(
        nameof(Striped), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildRows();
            }));

    public static readonly BindableProperty BorderedProperty = BindableProperty.Create(
        nameof(Bordered), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty ShowColumnHeadersProperty = BindableProperty.Create(
        nameof(ShowColumnHeaders), typeof(bool), typeof(DataGrid), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
        nameof(IsLoading), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).UpdateLoading();
            }));

    public static readonly BindableProperty EmptyTextProperty = BindableProperty.Create(
        nameof(EmptyText), typeof(string), typeof(DataGrid), "No records");

    public static readonly BindableProperty RowHeightProperty = BindableProperty.Create(
        nameof(RowHeight), typeof(double), typeof(DataGrid), -1d);

    public static readonly BindableProperty SortModeProperty = BindableProperty.Create(
        nameof(SortMode), typeof(DataGridSortMode), typeof(DataGrid), DataGridSortMode.Single);

    public static readonly BindableProperty FilterModeProperty = BindableProperty.Create(
        nameof(FilterMode), typeof(DataGridFilterMode), typeof(DataGrid), DataGridFilterMode.Menu,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty GroupableProperty = BindableProperty.Create(
        nameof(Groupable), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty PageSizeProperty = BindableProperty.Create(
        nameof(PageSize), typeof(int), typeof(DataGrid), 0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).OnPagingChanged();
            }));

    public static readonly BindableProperty EditModeProperty = BindableProperty.Create(
        nameof(EditMode), typeof(DataGridEditMode), typeof(DataGrid), DataGridEditMode.None);

    public static readonly BindableProperty EditTriggerProperty = BindableProperty.Create(
        nameof(EditTrigger), typeof(DataGridEditTrigger), typeof(DataGrid), DataGridEditTrigger.OnRowClick);

    public static readonly BindableProperty ReadOnlyProperty = BindableProperty.Create(
        nameof(ReadOnly), typeof(bool), typeof(DataGrid), false);

    public static readonly BindableProperty AllowColumnResizeProperty = BindableProperty.Create(
        nameof(AllowColumnResize), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildHeader();
            }));

    public static readonly BindableProperty AllowColumnReorderProperty = BindableProperty.Create(
        nameof(AllowColumnReorder), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildHeader();
            }));

    public static readonly BindableProperty DragDropColumnReorderingProperty = BindableProperty.Create(
        nameof(DragDropColumnReordering), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildHeader();
            }));

    public static readonly BindableProperty HorizontalScrollProperty = BindableProperty.Create(
        nameof(HorizontalScroll), typeof(bool), typeof(DataGrid), false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).ApplyLayoutMode();
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty DefaultColumnWidthProperty = BindableProperty.Create(
        nameof(DefaultColumnWidth), typeof(double), typeof(DataGrid), 150d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty MinColumnWidthProperty = BindableProperty.Create(
        nameof(MinColumnWidth), typeof(double), typeof(DataGrid), 48d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty MaxColumnWidthProperty = BindableProperty.Create(
        nameof(MaxColumnWidth), typeof(double), typeof(DataGrid), 0d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty FrozenColumnsProperty = BindableProperty.Create(
        nameof(FrozenColumns), typeof(int), typeof(DataGrid), 0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty FrozenEndColumnsProperty = BindableProperty.Create(
        nameof(FrozenEndColumns), typeof(int), typeof(DataGrid), 0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(DataGrid), () =>
            {
                ((DataGrid)b).RebuildAll();
            }));

    public static readonly BindableProperty SelectionChangedCommandProperty = BindableProperty.Create(
        nameof(SelectionChangedCommand), typeof(ICommand), typeof(DataGrid), null);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)this.GetValue(ItemsSourceProperty);
        set => this.SetValue(ItemsSourceProperty, value);
    }

    public DataGridSelectionMode SelectionMode
    {
        get => (DataGridSelectionMode)this.GetValue(SelectionModeProperty);
        set => this.SetValue(SelectionModeProperty, value);
    }

    public object? SelectedItem
    {
        get => this.GetValue(SelectedItemProperty);
        set => this.SetValue(SelectedItemProperty, value);
    }

    public IList? SelectedItems
    {
        get => (IList?)this.GetValue(SelectedItemsProperty);
        set => this.SetValue(SelectedItemsProperty, value);
    }

    public bool Dense
    {
        get => (bool)this.GetValue(DenseProperty);
        set => this.SetValue(DenseProperty, value);
    }

    public bool Striped
    {
        get => (bool)this.GetValue(StripedProperty);
        set => this.SetValue(StripedProperty, value);
    }

    public bool Bordered
    {
        get => (bool)this.GetValue(BorderedProperty);
        set => this.SetValue(BorderedProperty, value);
    }

    public bool ShowColumnHeaders
    {
        get => (bool)this.GetValue(ShowColumnHeadersProperty);
        set => this.SetValue(ShowColumnHeadersProperty, value);
    }

    public bool IsLoading
    {
        get => (bool)this.GetValue(IsLoadingProperty);
        set => this.SetValue(IsLoadingProperty, value);
    }

    public string EmptyText
    {
        get => (string)this.GetValue(EmptyTextProperty);
        set => this.SetValue(EmptyTextProperty, value);
    }

    public double RowHeight
    {
        get => (double)this.GetValue(RowHeightProperty);
        set => this.SetValue(RowHeightProperty, value);
    }

    public DataGridSortMode SortMode
    {
        get => (DataGridSortMode)this.GetValue(SortModeProperty);
        set => this.SetValue(SortModeProperty, value);
    }

    /// <summary>Rows per page. 0 disables paging (show all rows).</summary>
    public int PageSize
    {
        get => (int)this.GetValue(PageSizeProperty);
        set => this.SetValue(PageSizeProperty, value);
    }

    public DataGridFilterMode FilterMode
    {
        get => (DataGridFilterMode)this.GetValue(FilterModeProperty);
        set => this.SetValue(FilterModeProperty, value);
    }

    /// <summary>Enables per-column grouping (groupable columns show a group toggle in the header).</summary>
    public bool Groupable
    {
        get => (bool)this.GetValue(GroupableProperty);
        set => this.SetValue(GroupableProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => (ICommand?)this.GetValue(SelectionChangedCommandProperty);
        set => this.SetValue(SelectionChangedCommandProperty, value);
    }

    public DataGridEditMode EditMode
    {
        get => (DataGridEditMode)this.GetValue(EditModeProperty);
        set => this.SetValue(EditModeProperty, value);
    }

    public DataGridEditTrigger EditTrigger
    {
        get => (DataGridEditTrigger)this.GetValue(EditTriggerProperty);
        set => this.SetValue(EditTriggerProperty, value);
    }

    public bool ReadOnly
    {
        get => (bool)this.GetValue(ReadOnlyProperty);
        set => this.SetValue(ReadOnlyProperty, value);
    }

    /// <summary>Show drag handles on column headers to resize columns.</summary>
    public bool AllowColumnResize
    {
        get => (bool)this.GetValue(AllowColumnResizeProperty);
        set => this.SetValue(AllowColumnResizeProperty, value);
    }

    /// <summary>Show reorder arrows on column headers to move columns left/right.</summary>
    public bool AllowColumnReorder
    {
        get => (bool)this.GetValue(AllowColumnReorderProperty);
        set => this.SetValue(AllowColumnReorderProperty, value);
    }

    /// <summary>
    /// Lets a column header be dragged and dropped into a new position. Off by default, and
    /// independent of <see cref="AllowColumnReorder"/> - the arrows are the accessible, no-drag path
    /// to the same thing, so a grid can offer either, both, or neither.
    /// </summary>
    /// <remarks>
    /// Under <see cref="HorizontalScroll"/> this claims sideways gestures that start on a header, so
    /// the grid is scrolled by dragging a row rather than the header. Reordering moves the column in
    /// <see cref="Columns"/> itself, which is what the <see cref="ColumnReordered"/> event reports.
    /// </remarks>
    public bool DragDropColumnReordering
    {
        get => (bool)this.GetValue(DragDropColumnReorderingProperty);
        set => this.SetValue(DragDropColumnReorderingProperty, value);
    }

    /// <summary>
    /// Scrolls the header, rows and footer sideways as one when the columns are wider than the
    /// grid. Star widths cannot survive an unbounded measure, so in this mode every star column
    /// resolves to <see cref="DefaultColumnWidth"/> x its star factor. Required for frozen columns.
    /// </summary>
    public bool HorizontalScroll
    {
        get => (bool)this.GetValue(HorizontalScrollProperty);
        set => this.SetValue(HorizontalScrollProperty, value);
    }

    /// <summary>Width a star/auto column resolves to under <see cref="HorizontalScroll"/> (default 150).</summary>
    public double DefaultColumnWidth
    {
        get => (double)this.GetValue(DefaultColumnWidthProperty);
        set => this.SetValue(DefaultColumnWidthProperty, value);
    }

    /// <summary>
    /// Floor applied to every column that does not set its own <see cref="DataGridColumn.MinWidth"/>
    /// (default 48). Keeps a resize drag from collapsing a column to nothing.
    /// </summary>
    public double MinColumnWidth
    {
        get => (double)this.GetValue(MinColumnWidthProperty);
        set => this.SetValue(MinColumnWidthProperty, value);
    }

    /// <summary>
    /// Ceiling applied to every column that does not set its own <see cref="DataGridColumn.MaxWidth"/>.
    /// <c>0</c> (the default) leaves columns unbounded.
    /// </summary>
    public double MaxColumnWidth
    {
        get => (double)this.GetValue(MaxColumnWidthProperty);
        set => this.SetValue(MaxColumnWidthProperty, value);
    }

    /// <summary>
    /// Freezes the first N visible columns (plus the multi-select checkbox column) to the leading
    /// edge. Overridden upward by any leading columns that set <see cref="DataGridColumn.Frozen"/>.
    /// </summary>
    public int FrozenColumns
    {
        get => (int)this.GetValue(FrozenColumnsProperty);
        set => this.SetValue(FrozenColumnsProperty, value);
    }

    /// <summary>Freezes the last N visible columns to the trailing edge.</summary>
    public int FrozenEndColumns
    {
        get => (int)this.GetValue(FrozenEndColumnsProperty);
        set => this.SetValue(FrozenEndColumnsProperty, value);
    }

    /// <summary>Optional server-side data delegate (paging/sort/filter handled remotely).</summary>
    public Func<DataGridGridState, Task<DataGridGridData>>? ServerData { get; set; }

    public event EventHandler<DataGridSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<object>? StartedEditingItem;
    public event EventHandler<object>? CommittedItemChanges;
    public event EventHandler<object>? CanceledEditingItem;
}

public sealed class DataGridSelectionChangedEventArgs : EventArgs
{
    public DataGridSelectionChangedEventArgs(IReadOnlyList<object> selectedItems)
        => this.SelectedItems = selectedItems;

    public IReadOnlyList<object> SelectedItems { get; }
}
