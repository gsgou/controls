using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Cells;
using TvTableView = Shiny.Maui.Controls.TableView;

namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// Drag-to-reorder state machine shared by every <see cref="DragSortRow"/> in a TableView.
/// Rows are never re-laid out mid-drag: the dragged row is translated under the finger and
/// the target is resolved from the other rows' frames, so a single re-render at the end is
/// the only layout pass the drag costs.
/// </summary>
sealed class DragSortController
{
    const double AutoScrollZone = 56;
    const double AutoScrollStep = 12;
    static readonly TimeSpan AutoScrollInterval = TimeSpan.FromMilliseconds(16);

    readonly TvTableView owner;
    readonly List<DragSortRow> rows = new();

    IDispatcherTimer? autoScroll;
    DragSortRow? source;
    int sourceIndex = -1;
    int insertIndex = -1;
    double lastTotalY;
    double startScrollY;
    double scrollVelocity;

    public DragSortController(TvTableView owner) => this.owner = owner;


    public void Begin(DragSortRow row)
    {
        Abort();

        if (row.Parent is not Layout parent)
            return;

        foreach (var child in parent)
        {
            if (child is DragSortRow sibling && ReferenceEquals(sibling.Section, row.Section))
                this.rows.Add(sibling);
        }

        this.sourceIndex = this.rows.IndexOf(row);
        if (this.sourceIndex < 0 || this.rows.Count < 2)
        {
            this.rows.Clear();
            this.sourceIndex = -1;
            return;
        }

        this.source = row;
        this.insertIndex = this.sourceIndex;
        this.lastTotalY = 0;
        this.startScrollY = this.owner.ScrollOffsetY;

        row.SetDragging(true);

        if (row.Cell.UseFeedback)
            FeedbackHelper.Execute(this.owner, "DragStarted");
    }


    public void Update(DragSortRow row, double totalY)
    {
        if (!ReferenceEquals(this.source, row))
            return;

        this.lastTotalY = totalY;
        Apply();
    }


    public void Complete(DragSortRow row)
    {
        if (!ReferenceEquals(this.source, row))
        {
            Abort();
            return;
        }

        var section = row.Section;
        var cell = row.Cell;
        var from = this.sourceIndex;
        var to = this.insertIndex;
        var anchor = to >= 0 && to < this.rows.Count ? this.rows[to].Cell : null;
        var moved = to != from && anchor != null;

        // On a no-op the row springs back where it started; on a real move it stays
        // under the finger until the re-render below puts it in its new home.
        Finish(snapBack: !moved);

        if (!moved)
            return;

        MoveCell(section, cell, anchor!);

        if (cell.UseFeedback)
            FeedbackHelper.Execute(this.owner, "ItemDropped");

        // Deferred a tick: re-parenting the cells while the gesture is still unwinding
        // pulls the native views out from under the platform recognizer.
        this.owner.Dispatcher.Dispatch(() =>
        {
            this.owner.RenderSections();
            this.owner.RaiseItemDropped(section, cell, from, to);
        });
    }


    public void Cancel(DragSortRow row)
    {
        if (ReferenceEquals(this.source, row))
            Abort();
    }


    /// <summary>Drops any in-flight drag. Safe to call when nothing is dragging.</summary>
    public void Abort()
    {
        if (this.source == null)
            return;

        Finish(snapBack: true);
    }


    void MoveCell(Sections.TableSection section, CellBase cell, CellBase anchor)
    {
        // ObservableCollection.Move inserts into the post-removal collection, which means
        // the destination index is the anchor's *current* index whether the row is going
        // up (it lands before the anchor) or down (it lands after it).
        var fromIndex = section.Cells.IndexOf(cell);
        var toIndex = section.Cells.IndexOf(anchor);

        // A templated cell isn't in section.Cells at all - there is nothing for the control
        // to reorder, so the drop is reported and the app reorders its own ItemsSource.
        if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
            return;

        this.owner.SuppressRender = true;
        try
        {
            section.Cells.Move(fromIndex, toIndex);
        }
        finally
        {
            this.owner.SuppressRender = false;
        }
    }


    void Apply()
    {
        if (this.source == null)
            return;

        // The finger's delta in content space: the pan total plus however far the list
        // has auto-scrolled underneath it since the drag started.
        var delta = this.lastTotalY + (this.owner.ScrollOffsetY - this.startScrollY);
        this.source.TranslationY = delta;

        var frame = this.source.Frame;
        var pointerY = frame.Y + (frame.Height / 2) + delta;

        var index = this.rows.Count;
        for (var i = 0; i < this.rows.Count; i++)
        {
            var f = this.rows[i].Frame;
            if (pointerY < f.Y + (f.Height / 2))
            {
                index = i;
                break;
            }
        }

        // index is a slot in the list that still contains the dragged row; convert it to
        // a slot in the list without it.
        if (index > this.sourceIndex)
            index--;

        if (index != this.insertIndex)
        {
            this.insertIndex = index;
            UpdateIndicators();
        }

        UpdateAutoScroll(pointerY);
    }


    void UpdateIndicators()
    {
        foreach (var row in this.rows)
            row.HideIndicators();

        if (this.insertIndex == this.sourceIndex || this.insertIndex < 0 || this.insertIndex >= this.rows.Count)
            return;

        // Moving up lands the row above the row now occupying that slot; moving down
        // lands it below.
        this.rows[this.insertIndex].ShowIndicator(above: this.insertIndex < this.sourceIndex);
    }


    void Finish(bool snapBack)
    {
        StopAutoScroll();

        var row = this.source;
        var translation = row?.TranslationY ?? 0;

        foreach (var sibling in this.rows)
            sibling.HideIndicators();

        this.rows.Clear();
        this.source = null;
        this.sourceIndex = -1;
        this.insertIndex = -1;

        if (row != null)
        {
            row.SetDragging(false);
            if (!snapBack)
                row.TranslationY = translation;
        }
    }


    void UpdateAutoScroll(double pointerY)
    {
        if (this.source?.Parent is not Element sectionLayout)
        {
            StopAutoScroll();
            return;
        }

        var contentY = OffsetWithin(sectionLayout, this.owner.ScrollContent) + pointerY;
        var viewportY = contentY - this.owner.ScrollOffsetY;
        var viewportHeight = this.owner.ViewportHeight;

        this.scrollVelocity =
            viewportY < AutoScrollZone ? -AutoScrollStep :
            viewportY > viewportHeight - AutoScrollZone ? AutoScrollStep :
            0;

        if (this.scrollVelocity == 0)
            StopAutoScroll();
        else
            StartAutoScroll();
    }


    void StartAutoScroll()
    {
        if (this.autoScroll != null)
            return;

        this.autoScroll = this.owner.Dispatcher.CreateTimer();
        this.autoScroll.Interval = AutoScrollInterval;
        this.autoScroll.Tick += OnAutoScrollTick;
        this.autoScroll.Start();
    }


    void StopAutoScroll()
    {
        this.scrollVelocity = 0;

        if (this.autoScroll == null)
            return;

        this.autoScroll.Stop();
        this.autoScroll.Tick -= OnAutoScrollTick;
        this.autoScroll = null;
    }


    void OnAutoScrollTick(object? sender, EventArgs e)
    {
        if (this.source == null || this.scrollVelocity == 0)
        {
            StopAutoScroll();
            return;
        }

        var max = Math.Max(0, this.owner.ContentHeight - this.owner.ViewportHeight);
        var target = Math.Clamp(this.owner.ScrollOffsetY + this.scrollVelocity, 0, max);

        if (Math.Abs(target - this.owner.ScrollOffsetY) < 0.5)
            return;

        this.owner.ScrollToY(target);

        // ScrollOffsetY catches up asynchronously, so this frame reads one step behind -
        // close enough at 16ms, and it keeps the row glued to the finger while the list moves.
        Apply();
    }


    static double OffsetWithin(Element? element, Element root)
    {
        var y = 0d;
        while (element is VisualElement visual && !ReferenceEquals(visual, root))
        {
            y += visual.Frame.Y;
            element = visual.Parent;
        }
        return y;
    }
}
