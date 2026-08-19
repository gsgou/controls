using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Tests;

/// <summary>
/// Detail ("breakdown") rows and tree mode are one mechanism: an expanded-item set plus a flatten step.
/// The flatten is where the hierarchy actually happens — levels, which rows appear, and whether a
/// filtered-out parent still gets to carry its matching child.
/// </summary>
public class DataGridExpansionTests
{
    class Node
    {
        public string Name { get; set; } = "";
        public List<Node> Children { get; } = new();
    }

    /// <summary>The renderer is what normally calls OnParametersSet; there isn't one here.</summary>
    class TestGrid<T> : DataGrid<T>
    {
        public void Sync() => this.OnParametersSet();
    }

    static (TestGrid<Node> Grid, Node Root, Node Child, Node GrandChild, Node Leaf) BuildTree()
    {
        var grandChild = new Node { Name = "grandchild" };
        var child = new Node { Name = "child" };
        child.Children.Add(grandChild);
        var root = new Node { Name = "root" };
        root.Children.Add(child);
        var leaf = new Node { Name = "leaf" };

        var grid = new TestGrid<Node>
        {
            Items = new[] { root, leaf },
            ChildrenSelector = n => n.Children.Count == 0 ? null : n.Children
        };
        grid.Sync();
        return (grid, root, child, grandChild, leaf);
    }

    static IReadOnlyList<string> Shape(TestGrid<Node> grid)
        => grid.GetRenderedRows().Select(r => $"{r.Item.Name}:{r.Level}").ToList();

    [Fact]
    public void CollapsedParentsContributeNoChildRows()
    {
        var (grid, _, _, _, _) = BuildTree();

        Shape(grid).ShouldBe(new[] { "root:0", "leaf:0" });
    }

    [Fact]
    public void ExpandingARowInsertsItsChildrenOneLevelDeeper()
    {
        var (grid, root, child, _, _) = BuildTree();

        grid.ExpandedItems = new[] { root };
        grid.Sync();
        Shape(grid).ShouldBe(new[] { "root:0", "child:1", "leaf:0" });

        grid.ExpandedItems = new[] { root, child };
        grid.Sync();
        Shape(grid).ShouldBe(new[] { "root:0", "child:1", "grandchild:2", "leaf:0" });
    }

    [Fact]
    public void OnlyBranchesCarryACaret()
    {
        var (grid, _, _, _, _) = BuildTree();

        var rows = grid.GetRenderedRows();
        rows.Single(r => r.Item.Name == "root").HasChildren.ShouldBeTrue();
        rows.Single(r => r.Item.Name == "leaf").HasChildren.ShouldBeFalse();
        rows.Single(r => r.Item.Name == "leaf").CaretGlyph.ShouldBe(string.Empty);
    }

    [Fact]
    public void FilteringKeepsTheAncestorsOfAMatchSoTheMatchStaysReachable()
    {
        var (grid, root, child, _, _) = BuildTree();
        grid.ExpandedItems = new[] { root, child };
        grid.QuickFilter = n => n.Name == "grandchild";
        grid.Sync();

        // root and child match nothing themselves - they are kept because the match is under them.
        Shape(grid).ShouldBe(new[] { "root:0", "child:1", "grandchild:2" });
    }

    [Fact]
    public void FilteringStillDropsBranchesWithNoMatchAnywhere()
    {
        var (grid, root, _, _, _) = BuildTree();
        grid.ExpandedItems = new[] { root };
        grid.QuickFilter = n => n.Name == "leaf";
        grid.Sync();

        Shape(grid).ShouldBe(new[] { "leaf:0" });
    }

    [Fact]
    public void ALazyBranchOffersItsCaretBeforeAnythingIsLoaded()
    {
        var grid = new TestGrid<Node>
        {
            Items = new[] { new Node { Name = "root" } },
            ChildrenLoader = _ => Task.FromResult<IEnumerable<Node>>(new[] { new Node { Name = "remote" } })
        };
        grid.Sync();

        grid.GetRenderedRows().Single().HasChildren.ShouldBeTrue();
    }

    /// <summary>
    /// A mixed tree - some branches in memory, some fetched. The loader must only cover what the
    /// selector declined, or it takes over every branch and the in-memory levels never render.
    /// </summary>
    [Fact]
    public void ASyncBranchStillReportsItsOwnChildrenWhenALoaderIsAlsoConfigured()
    {
        var child = new Node { Name = "child" };
        var syncRoot = new Node { Name = "sync-root" };
        syncRoot.Children.Add(child);
        var lazyRoot = new Node { Name = "lazy-root" };

        var grid = new TestGrid<Node>
        {
            Items = new[] { syncRoot, lazyRoot },
            // The lazy node has no children of its own, so the selector passes on it.
            ChildrenSelector = n => n.Name == "lazy-root" ? null : n.Children,
            ChildrenLoader = _ => Task.FromResult<IEnumerable<Node>>(new[] { new Node { Name = "remote" } }),
            ExpandedItems = new[] { syncRoot }
        };
        grid.Sync();

        Shape(grid).ShouldBe(new[] { "sync-root:0", "child:1", "lazy-root:0" });
    }

    // ---- async detail loading ----

    [Fact]
    public void ABusyBranchShowsNoCaretGlyphBecauseTheSpinnerTakesItsPlace()
    {
        var loading = new DataGrid<Node>.RowNode(new Node { Name = "n" }, 0, HasChildren: true, IsExpanded: false, IsLoading: true);
        var idle = loading with { IsLoading = false };

        loading.CaretGlyph.ShouldBe(string.Empty);
        idle.CaretGlyph.ShouldBe("▸");
    }

    [Fact]
    public void AGridWithNothingInFlightIsNotBusy()
    {
        var (grid, root, _, _, _) = BuildTree();

        grid.IsBusy.ShouldBeFalse();
        grid.IsRowBusy(root).ShouldBeFalse();
        grid.IsRowDetailLoading(root).ShouldBeFalse();
    }

    // ---- the detail row's expander column ----

    [Fact]
    public void ADetailTemplateAddsTheExpanderColumnAndWidensEveryFullWidthRow()
    {
        var plain = new TestGrid<Node> { Items = Array.Empty<Node>() };
        plain.Sync();
        var bare = plain.ColSpan;

        var grid = new TestGrid<Node>
        {
            Items = Array.Empty<Node>(),
            RowDetailTemplate = _ => _ => { }
        };
        grid.Sync();

        grid.HasExpanderColumn.ShouldBeTrue();
        grid.LeadColumnCount.ShouldBe(1);
        grid.ColSpan.ShouldBe(bare + 1);
    }

    [Fact]
    public void TreeModeUsesTheInlineCaretRatherThanAnExpanderColumn()
    {
        var (grid, _, _, _, _) = BuildTree();

        grid.TreeEnabled.ShouldBeTrue();
        grid.HasExpanderColumn.ShouldBeFalse();
        grid.LeadColumnCount.ShouldBe(0);
    }

    [Fact]
    public void IsRowExpandableVetoHidesTheDetailCaret()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        var grid = new TestGrid<Node>
        {
            Items = new[] { a, b },
            RowDetailTemplate = _ => _ => { },
            IsRowExpandable = n => n.Name != "a"
        };
        grid.Sync();

        grid.CanShowDetail(a).ShouldBeFalse();
        grid.CanShowDetail(b).ShouldBeTrue();
        grid.CanExpand(a).ShouldBeFalse();
    }
}
