using System.Collections;
using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.DataGrid;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Detail ("breakdown") rows and tree mode are one mechanism: an expanded-item set plus a flatten step.
/// These pin what the flatten actually produces — the display list is what the CollectionView renders,
/// so a row that never lands in it is a feature the user cannot see.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class DataGridExpansionTests
{
    class Node
    {
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public List<Node> Children { get; } = new();
    }

    static DataGrid.DataGrid BuildGrid(IEnumerable<Node> items, Action<DataGrid.DataGrid>? configure = null)
    {
        new Application();

        var grid = new DataGrid.DataGrid();
        grid.Columns.Add(new DataGridColumn { Title = "Name", PropertyName = nameof(Node.Name) });
        grid.Columns.Add(new DataGridColumn { Title = "Amount", PropertyName = nameof(Node.Amount) });
        configure?.Invoke(grid);
        grid.ItemsSource = items.ToList();
        return grid;
    }

    static DataTemplate DetailTemplate() => new(() => new Label { Text = "detail" });

    static IReadOnlyList<string> Shape(DataGrid.DataGrid grid)
        => grid.DisplayItems
            .Select(i => i switch
            {
                DataGridDetailRow d => $"detail:{((Node)d.Data).Name}",
                DataGridRow r => $"row:{((Node)r.Data).Name}:{r.Level}",
                _ => "other"
            })
            .ToList();

    // ---------- detail rows ----------

    [Fact]
    public void DetailRowFollowsItsParentOnlyWhileExpanded()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        var grid = BuildGrid(new[] { a, b }, g => g.RowDetailTemplate = DetailTemplate());

        Shape(grid).ShouldBe(new[] { "row:a:0", "row:b:0" });

        grid.ExpandRow(a);
        Shape(grid).ShouldBe(new[] { "row:a:0", "detail:a", "row:b:0" });

        grid.CollapseRow(a);
        Shape(grid).ShouldBe(new[] { "row:a:0", "row:b:0" });
    }

    [Fact]
    public void SingleExpandModeClosesThePreviouslyExpandedRow()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        var grid = BuildGrid(new[] { a, b }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.ExpandMode = DataGridExpandMode.Single;
        });

        grid.ExpandRow(a);
        grid.ExpandRow(b);

        grid.IsRowExpanded(a).ShouldBeFalse();
        grid.IsRowExpanded(b).ShouldBeTrue();
        Shape(grid).ShouldBe(new[] { "row:a:0", "row:b:0", "detail:b" });
    }

    [Fact]
    public void IsRowExpandableVetoKeepsARowShut()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        var grid = BuildGrid(new[] { a, b }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.IsRowExpandable = item => ((Node)item).Name != "a";
        });

        grid.ExpandRow(a);

        grid.IsRowExpanded(a).ShouldBeFalse();
        Shape(grid).ShouldBe(new[] { "row:a:0", "row:b:0" });
    }

    // ---------- async detail loading ----------

    [Fact]
    public void TheDetailRowWaitsOnItsLoaderBeforeTheContentTemplateIsBuilt()
    {
        var a = new Node { Name = "a" };
        var gate = new TaskCompletionSource();
        var grid = BuildGrid(new[] { a }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.RowDetailLoader = _ => gate.Task;
        });

        grid.ExpandRow(a);

        // The detail row is there straight away, but in its loading state - so the caret has
        // somewhere to point and the loaded template never sees half-fetched data.
        grid.DisplayItems.OfType<DataGridDetailRow>().Single().IsLoading.ShouldBeTrue();
        grid.DisplayItems.OfType<DataGridRow>().Single().IsLoadingDetail.ShouldBeTrue();
        grid.IsBusy.ShouldBeTrue();
        grid.IsRowBusy(a).ShouldBeTrue();

        gate.SetResult();

        grid.DisplayItems.OfType<DataGridDetailRow>().Single().IsLoading.ShouldBeFalse();
        grid.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public void TheDetailLoaderRunsOncePerItemUntilItIsInvalidated()
    {
        var a = new Node { Name = "a" };
        var calls = 0;
        var grid = BuildGrid(new[] { a }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.RowDetailLoader = _ => { calls++; return Task.CompletedTask; };
        });

        grid.ExpandRow(a);
        grid.CollapseRow(a);
        grid.ExpandRow(a);
        calls.ShouldBe(1);

        grid.InvalidateRowDetail(a);
        calls.ShouldBe(2);
    }

    [Fact]
    public void ABusyRowSwapsItsCaretForTheSpinner()
    {
        var a = new Node { Name = "a" };
        var gate = new TaskCompletionSource();
        var grid = BuildGrid(new[] { a }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.RowDetailLoader = _ => gate.Task;
        });

        grid.ExpandRow(a);
        var row = grid.DisplayItems.OfType<DataGridRow>().Single();
        row.ShowDetailCaret.ShouldBeFalse();
        row.IsBusy.ShouldBeTrue();

        gate.SetResult();
        grid.DisplayItems.OfType<DataGridRow>().Single().ShowDetailCaret.ShouldBeTrue();
    }

    [Fact]
    public void AFailedDetailLoadCollapsesTheRowAndReportsIt()
    {
        var a = new Node { Name = "a" };
        Exception? reported = null;
        var grid = BuildGrid(new[] { a }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.RowDetailLoader = _ => Task.FromException(new InvalidOperationException("nope"));
        });
        grid.RowDetailLoadFailed += (_, e) => reported = e.Exception;

        grid.ExpandRow(a);

        reported.ShouldBeOfType<InvalidOperationException>();
        grid.IsRowExpanded(a).ShouldBeFalse();
        grid.IsBusy.ShouldBeFalse();
        Shape(grid).ShouldBe(new[] { "row:a:0" });
    }

    [Fact]
    public void IsBusyStaysUpWhileAnyLoadIsStillRunning()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        var gateA = new TaskCompletionSource();
        var gateB = new TaskCompletionSource();
        var flips = new List<bool>();

        var grid = BuildGrid(new[] { a, b }, g =>
        {
            g.RowDetailTemplate = DetailTemplate();
            g.RowDetailLoader = item => ((Node)item).Name == "a" ? gateA.Task : gateB.Task;
        });
        grid.IsBusyChanged += (_, v) => flips.Add(v);

        grid.ExpandRow(a);
        grid.ExpandRow(b);
        gateA.SetResult();

        grid.IsBusy.ShouldBeTrue();   // b is still going

        gateB.SetResult();
        grid.IsBusy.ShouldBeFalse();

        // One flip up, one flip down - not one per load.
        flips.ShouldBe(new[] { true, false });
    }

    // ---------- tree mode ----------

    static (DataGrid.DataGrid Grid, Node Root, Node Child, Node GrandChild) BuildTree()
    {
        var grandChild = new Node { Name = "grandchild", Amount = 1 };
        var child = new Node { Name = "child", Amount = 2 };
        child.Children.Add(grandChild);
        var root = new Node { Name = "root", Amount = 3 };
        root.Children.Add(child);
        var leaf = new Node { Name = "leaf", Amount = 4 };

        var grid = BuildGrid(
            new[] { root, leaf },
            g => g.ChildrenSelector = item => ((Node)item).Children.Count == 0 ? null : ((Node)item).Children);

        return (grid, root, child, grandChild);
    }

    [Fact]
    public void CollapsedParentsContributeNoChildRows()
    {
        var (grid, _, _, _) = BuildTree();

        Shape(grid).ShouldBe(new[] { "row:root:0", "row:leaf:0" });
    }

    [Fact]
    public void ExpandingARowInsertsItsChildrenOneLevelDeeper()
    {
        var (grid, root, child, _) = BuildTree();

        grid.ExpandRow(root);
        Shape(grid).ShouldBe(new[] { "row:root:0", "row:child:1", "row:leaf:0" });

        grid.ExpandRow(child);
        Shape(grid).ShouldBe(new[] { "row:root:0", "row:child:1", "row:grandchild:2", "row:leaf:0" });
    }

    [Fact]
    public void OnlyBranchesCarryACaret()
    {
        var (grid, _, _, _) = BuildTree();

        var rows = grid.DisplayItems.OfType<DataGridRow>().ToList();
        rows.Single(r => ((Node)r.Data).Name == "root").HasChildren.ShouldBeTrue();
        rows.Single(r => ((Node)r.Data).Name == "leaf").HasChildren.ShouldBeFalse();
        rows.Single(r => ((Node)r.Data).Name == "leaf").TreeCaretGlyph.ShouldBe(string.Empty);
    }

    [Fact]
    public void ExpandAllOpensEveryLoadedLevel()
    {
        var (grid, _, _, _) = BuildTree();

        grid.ExpandAll();

        Shape(grid).ShouldBe(new[] { "row:root:0", "row:child:1", "row:grandchild:2", "row:leaf:0" });
    }

    [Fact]
    public void LazyChildrenAreFetchedOnceAndThenCached()
    {
        var root = new Node { Name = "root" };
        var calls = 0;

        var grid = BuildGrid(new[] { root }, g =>
        {
            g.HasChildrenSelector = _ => true;
            g.ChildrenLoader = _ =>
            {
                calls++;
                return Task.FromResult<IEnumerable>(new List<Node> { new() { Name = "remote" } });
            };
        });

        grid.ExpandRow(root);
        Shape(grid).ShouldBe(new[] { "row:root:0", "row:remote:1" });
        calls.ShouldBe(1);

        grid.CollapseRow(root);
        grid.ExpandRow(root);
        calls.ShouldBe(1);
    }

    /// <summary>
    /// A mixed tree - some branches in memory, some fetched. The loader must only cover what the
    /// selector declined, or it takes over every branch and the in-memory levels never render.
    /// </summary>
    [Fact]
    public void TheSelectorGetsFirstRefusalAndTheLoaderOnlyCoversWhatItDeclined()
    {
        var syncChild = new Node { Name = "sync-child" };
        var syncRoot = new Node { Name = "sync-root" };
        syncRoot.Children.Add(syncChild);
        var lazyRoot = new Node { Name = "lazy-root" };
        var loaded = new List<object>();

        var grid = BuildGrid(new[] { syncRoot, lazyRoot }, g =>
        {
            // The lazy node has no children of its own, so the selector passes on it.
            g.ChildrenSelector = item => ((Node)item).Name == "lazy-root" ? null : ((Node)item).Children;
            g.HasChildrenSelector = _ => true;
            g.ChildrenLoader = item =>
            {
                loaded.Add(item);
                return Task.FromResult<IEnumerable>(new List<Node> { new() { Name = "remote" } });
            };
        });

        grid.ExpandRow(syncRoot);
        grid.ExpandRow(lazyRoot);

        Shape(grid).ShouldBe(new[] { "row:sync-root:0", "row:sync-child:1", "row:lazy-root:0", "row:remote:1" });
        loaded.ShouldBe(new object[] { lazyRoot });
    }

    [Fact]
    public void AFailedChildLoadCollapsesTheRowAndReportsIt()
    {
        var root = new Node { Name = "root" };
        Exception? reported = null;

        var grid = BuildGrid(new[] { root }, g =>
        {
            g.HasChildrenSelector = _ => true;
            g.ChildrenLoader = _ => Task.FromException<IEnumerable>(new InvalidOperationException("nope"));
        });
        grid.ChildrenLoadFailed += (_, e) => reported = e.Exception;

        grid.ExpandRow(root);

        reported.ShouldBeOfType<InvalidOperationException>();
        grid.IsRowExpanded(root).ShouldBeFalse();
        Shape(grid).ShouldBe(new[] { "row:root:0" });
    }

    [Fact]
    public void FilteringKeepsTheAncestorsOfAMatchSoTheMatchStaysReachable()
    {
        var (grid, root, child, _) = BuildTree();
        grid.ExpandRow(root);
        grid.ExpandRow(child);

        grid.SetQuickSearch("grandchild");

        // root and child match nothing themselves - they are kept because the match is under them.
        Shape(grid).ShouldBe(new[] { "row:root:0", "row:child:1", "row:grandchild:2" });
    }

    [Fact]
    public void FilteringStillDropsBranchesWithNoMatchAnywhere()
    {
        var (grid, root, child, _) = BuildTree();
        grid.ExpandRow(root);

        grid.SetQuickSearch("leaf");

        Shape(grid).ShouldBe(new[] { "row:leaf:0" });
    }
}
