using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.DataGrid;

/// <summary>
/// Drag-and-drop column reordering: pick a header up and drop it between two others.
/// </summary>
/// <remarks>
/// The drag runs on a <see cref="PanGestureRecognizer"/> rather than Drag/DropGestureRecognizer, for
/// the same reasons TableView's row reorder does: the platform recognizers are broken on Mac Catalyst
/// and missing entirely from the AppKit and GTK4 hosts, and even where they work the event carries no
/// pointer position - so there is no way to tell which side of a column the finger is on. A pan
/// reports a usable delta everywhere.
/// <para>
/// Under <see cref="HorizontalScroll"/> the header lives inside a horizontal ScrollView, which will
/// take a sideways gesture away from its children the moment it crosses the touch slop.
/// <see cref="DragTouchHook"/> holds the scroller off from the raw touch-down, which is the only point
/// early enough to matter - by the time a pan reports Started the scroller has already decided.
/// </para>
/// </remarks>
public partial class DataGrid
{
    sealed class HeaderDragCell
    {
        public required Grid Container { get; init; }
        public required BoxView Leading { get; init; }
        public required BoxView Trailing { get; init; }
        public DragTouchHook? Hook { get; set; }
    }

    readonly Dictionary<string, HeaderDragCell> dragCells = new();
    DataGridColumn? dragColumn;
    int dragTargetIndex = -1;

    /// <summary>Raised after a column has been dropped in a new position.</summary>
    public event EventHandler<DataGridColumnReorderedEventArgs>? ColumnReordered;

    /// <summary>Header cells are rebuilt from scratch, so the old ones must not be measured against.</summary>
    void ClearColumnDragCells()
    {
        this.dragColumn = null;
        this.dragTargetIndex = -1;
        this.dragCells.Clear();
    }

    /// <summary>Adds the drop indicators and the drag gesture to one header cell.</summary>
    void AttachColumnDrag(DataGridColumn column, Grid container)
    {
        var cell = new HeaderDragCell
        {
            Container = container,
            Leading = BuildDropIndicator(LayoutOptions.Start),
            Trailing = BuildDropIndicator(LayoutOptions.End)
        };
        container.Add(cell.Leading);
        container.Add(cell.Trailing);
        this.dragCells[column.Id] = cell;

        // The scroller has to be told to keep its hands off from the touch down, not from the pan -
        // see the remarks above. No-op on the builds where the scroller does not fight its children.
        var hook = new DragTouchHook(container);
        hook.Pressed = () => hook.LockScroller(true);
        hook.Released = () => hook.LockScroller(false);
        cell.Hook = hook;

        var capture = column;
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) =>
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    this.BeginColumnDrag(capture);
                    break;

                case GestureStatus.Running:
                    this.UpdateColumnDrag(e.TotalX);
                    break;

                // Android reports zeroed totals on the final event, so the drop commits from the
                // target the last Running event resolved rather than from anything on this one.
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    this.CompleteColumnDrag();
                    break;
            }
        };
        container.GestureRecognizers.Add(pan);
    }

    static BoxView BuildDropIndicator(LayoutOptions horizontal)
    {
        var indicator = new BoxView
        {
            WidthRequest = 3,
            HorizontalOptions = horizontal,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            IsVisible = false
        };
        // BoxView paints from Color, not Background - a solid Background renders transparent on the
        // AppKit host.
        indicator.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.Primary);
        return indicator;
    }

    void BeginColumnDrag(DataGridColumn column)
    {
        this.dragColumn = column;
        this.dragTargetIndex = IndexOf(this.VisibleColumns, column);
        this.SetDragging(column, true);
    }

    void UpdateColumnDrag(double totalX)
    {
        if (this.dragColumn is null)
            return;

        var cols = this.VisibleColumns;
        var from = IndexOf(cols, this.dragColumn);
        if (from < 0)
            return;

        // Walk outward from the dragged column, consuming a neighbour once the finger has travelled
        // past its midpoint - so a column swaps when it is more than half covered, not the instant
        // the drag touches it.
        var target = from;
        var travelled = Math.Abs(totalX);
        var step = totalX > 0 ? 1 : -1;

        for (var i = from + step; i >= 0 && i < cols.Count; i += step)
        {
            var width = this.HeaderCellWidth(cols[i]);
            if (travelled < width / 2)
                break;

            travelled -= width;
            target = i;
        }

        this.dragTargetIndex = target;
        this.ShowDropIndicator(target == from ? null : cols[target], after: target > from);
    }

    void CompleteColumnDrag()
    {
        var column = this.dragColumn;
        var targetIndex = this.dragTargetIndex;

        this.dragColumn = null;
        this.dragTargetIndex = -1;
        this.HideDropIndicators();

        if (column is null)
            return;

        this.SetDragging(column, false);

        var cols = this.VisibleColumns;
        if (targetIndex < 0 || targetIndex >= cols.Count || ReferenceEquals(cols[targetIndex], column))
            return;

        // Indices are taken against Columns, not VisibleColumns: a hidden column between the two
        // still occupies a slot in the collection being reordered.
        var from = this.Columns.IndexOf(column);
        var to = this.Columns.IndexOf(cols[targetIndex]);
        if (from < 0 || to < 0 || from == to)
            return;

        // Move removes before it inserts, so `to` lands the column after the target when moving right
        // and before it when moving left - which is exactly where the indicator was drawn.
        this.Columns.Move(from, to);
        this.ColumnReordered?.Invoke(this, new DataGridColumnReorderedEventArgs(column, to));
    }

    void SetDragging(DataGridColumn column, bool dragging)
    {
        if (!this.dragCells.TryGetValue(column.Id, out var cell))
            return;

        cell.Container.Opacity = dragging ? 0.5 : 1.0;

        // Never through ZIndex on Android or iOS: MAUI implements it by removing the native child and
        // re-adding it, and removing a view mid-gesture cancels the very drag that raised it.
        DragTouchHook.Raise(cell.Container, dragging);
    }

    void ShowDropIndicator(DataGridColumn? target, bool after)
    {
        foreach (var (id, cell) in this.dragCells)
        {
            var hit = target is not null && id == target.Id;
            cell.Leading.IsVisible = hit && !after;
            cell.Trailing.IsVisible = hit && after;
        }
    }

    void HideDropIndicators()
    {
        foreach (var cell in this.dragCells.Values)
        {
            cell.Leading.IsVisible = false;
            cell.Trailing.IsVisible = false;
        }
    }

    /// <summary>
    /// The measured header width, falling back to the width the column asks for. The measurement is
    /// the one that matters: a star column's declared width is a ratio, not a number of pixels.
    /// </summary>
    double HeaderCellWidth(DataGridColumn column)
    {
        if (this.dragCells.TryGetValue(column.Id, out var cell) && cell.Container.Width > 0)
            return cell.Container.Width;

        var resolved = this.ResolveWidth(column);
        return resolved.IsAbsolute && resolved.Value > 0 ? resolved.Value : this.DefaultColumnWidth;
    }

    static int IndexOf(IReadOnlyList<DataGridColumn> columns, DataGridColumn column)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (ReferenceEquals(columns[i], column))
                return i;
        }
        return -1;
    }
}

/// <summary>Reports a column dropped in a new position by <see cref="DataGrid.ColumnReordered"/>.</summary>
public class DataGridColumnReorderedEventArgs(DataGridColumn column, int newIndex) : EventArgs
{
    /// <summary>The column that moved.</summary>
    public DataGridColumn Column { get; } = column;

    /// <summary>Its new index in <see cref="DataGrid.Columns"/>.</summary>
    public int NewIndex { get; } = newIndex;
}
