using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Tree;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// Expand-all has to materialize the sync branches itself — the old implementation bailed on
/// the whole tree as soon as a ChildrenLoader was configured, so it only ever re-expanded the
/// nodes the user had already opened by hand. Multi-select rows carry a checkbox.
/// </summary>
[Collection(ApplicationResourcesCollection.Name)]
public class TreeViewTests
{
    class Node
    {
        public string Name { get; set; } = "";
        public List<Node>? Children { get; set; }
        public bool Lazy { get; set; }
    }

    static TreeView BuildTree(out Node root, out Node child, out Node lazy)
    {
        new Application();

        var grandChild = new Node { Name = "grandchild" };
        child = new Node { Name = "child", Children = new() { grandChild } };
        root = new Node { Name = "root", Children = new() { child } };
        lazy = new Node { Name = "lazy", Lazy = true };

        var tree = new TreeView
        {
            ChildrenSelector = o => ((Node)o).Lazy ? null : ((Node)o).Children?.Cast<object>(),
            HasChildrenSelector = o => ((Node)o).Lazy || ((Node)o).Children?.Count > 0,
            // A loader for the lazy branch must not disable sync expansion everywhere else.
            ChildrenLoader = _ => Task.FromResult<IEnumerable<object>>(new object[] { new Node { Name = "remote" } })
        };
        tree.ItemsSource = new List<Node> { root, lazy };
        return tree;
    }

    [Fact]
    public void ExpandAll_ExpandsUntouchedSyncBranches_WhenALoaderIsAlsoConfigured()
    {
        var tree = BuildTree(out var root, out var child, out var lazy);

        tree.ExpandAll();

        tree.FindNode(root)!.IsExpanded.ShouldBeTrue();
        tree.FindNode(child)!.IsExpanded.ShouldBeTrue();
        // The lazy branch needs the async loader, so sync expand-all leaves it alone.
        tree.FindNode(lazy)!.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public async Task ExpandAllAsync_AlsoOpensTheLazyBranch()
    {
        var tree = BuildTree(out var root, out var child, out var lazy);

        await tree.ExpandAllAsync();

        tree.FindNode(root)!.IsExpanded.ShouldBeTrue();
        tree.FindNode(child)!.IsExpanded.ShouldBeTrue();
        tree.FindNode(lazy)!.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void ExpandAll_StopsAtMaxDepth_OnASelfReferencingTree()
    {
        new Application();

        var loop = new Node { Name = "loop" };
        loop.Children = new() { loop };

        var tree = new TreeView
        {
            ChildrenSelector = o => ((Node)o).Children?.Cast<object>(),
            HasChildrenSelector = _ => true
        };
        tree.ItemsSource = new List<Node> { loop };

        tree.ExpandAll(maxDepth: 4);

        // 4 levels expanded (depths 0-3) plus the leaf row the last expand revealed.
        tree.rowLayout.Children.Count.ShouldBe(5);
    }

    [Fact]
    public void MultipleSelection_RendersACheckBoxOnEveryRow()
    {
        var tree = BuildTree(out _, out _, out _);

        CountCheckBoxes(tree).ShouldBe(0);

        tree.SelectionMode = TreeSelectionMode.Multiple;
        CountCheckBoxes(tree).ShouldBe(tree.rowLayout.Children.Count);

        tree.ShowSelectionCheckBoxes = false;
        CountCheckBoxes(tree).ShouldBe(0);
    }

    [Fact]
    public void CheckBox_TracksTheRowSelection()
    {
        var tree = BuildTree(out var root, out _, out _);
        tree.SelectionMode = TreeSelectionMode.Multiple;

        var node = tree.FindNode(root)!;
        FirstCheckBoxIsChecked(tree).ShouldBeFalse();

        tree.HandleRowTapped(node);
        node.IsSelected.ShouldBeTrue();
        FirstCheckBoxIsChecked(tree).ShouldBeTrue();
        tree.SelectedItems!.ShouldContain(root);

        tree.HandleRowTapped(node);
        node.IsSelected.ShouldBeFalse();
        FirstCheckBoxIsChecked(tree).ShouldBeFalse();
        tree.SelectedItems!.ShouldNotContain(root);
    }

    [Fact]
    public void FirstSelectionMode_KeepsAPrePopulatedSelectedItems()
    {
        var tree = BuildTree(out var root, out _, out _);
        var selected = new System.Collections.ObjectModel.ObservableCollection<object> { root };
        tree.SelectedItems = selected;

        tree.SelectionMode = TreeSelectionMode.Multiple;

        selected.ShouldContain(root);
    }

    [Fact]
    public void SwitchingSelectionMode_DropsTheOldSelection()
    {
        var tree = BuildTree(out var root, out _, out _);
        tree.SelectionMode = TreeSelectionMode.Multiple;
        tree.HandleRowTapped(tree.FindNode(root)!);

        tree.SelectionMode = TreeSelectionMode.Single;

        tree.FindNode(root)!.IsSelected.ShouldBeFalse();
        tree.SelectedItem.ShouldBeNull();
        tree.SelectedItems.ShouldBeEmpty();
    }

    [Fact]
    public void SelectAll_ChecksCollapsedBranchesToo()
    {
        var tree = BuildTree(out var root, out var child, out _);
        tree.SelectionMode = TreeSelectionMode.Multiple;
        tree.ExpandAll();
        tree.CollapseAll();

        tree.SelectAll();

        // "child" sits inside a collapsed branch but is materialized, so it counts.
        tree.FindNode(root)!.IsSelected.ShouldBeTrue();
        tree.FindNode(child)!.IsSelected.ShouldBeTrue();
        tree.SelectedItems!.ShouldContain(root);
        tree.SelectedItems!.ShouldContain(child);
    }

    [Fact]
    public void SelectAll_SkipsUnselectableItems()
    {
        var tree = BuildTree(out var root, out var child, out _);
        tree.CanSelectSelector = o => !ReferenceEquals(o, child);
        tree.SelectionMode = TreeSelectionMode.Multiple;
        tree.ExpandAll();

        tree.SelectAll();

        tree.FindNode(root)!.IsSelected.ShouldBeTrue();
        tree.FindNode(child)!.IsSelected.ShouldBeFalse();
        tree.SelectedItems!.ShouldNotContain(child);
    }

    [Fact]
    public void SelectAll_IsANoOp_OutsideMultipleMode()
    {
        var tree = BuildTree(out var root, out _, out _);

        tree.SelectAll();

        tree.FindNode(root)!.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void DeselectAll_ClearsEverySelection()
    {
        var tree = BuildTree(out var root, out _, out _);
        tree.SelectionMode = TreeSelectionMode.Multiple;
        tree.ExpandAll();
        tree.SelectAll();

        tree.DeselectAll();

        tree.EnumerateAllNodes().ShouldAllBe(n => !n.IsSelected);
        tree.SelectedItem.ShouldBeNull();
        tree.SelectedItems.ShouldBeEmpty();
    }

    [Fact]
    public void SetBranchSelected_TogglesTheSubtreeOnly()
    {
        var tree = BuildTree(out var root, out var child, out var lazy);
        tree.SelectionMode = TreeSelectionMode.Multiple;
        tree.ExpandAll();

        tree.SetBranchSelected(child, true);

        tree.FindNode(child)!.IsSelected.ShouldBeTrue();
        tree.FindNode(child)!.Children!.ShouldAllBe(n => n.IsSelected);
        tree.FindNode(root)!.IsSelected.ShouldBeFalse();
        tree.FindNode(lazy)!.IsSelected.ShouldBeFalse();

        tree.SetBranchSelected(child, false);
        tree.SelectedItems.ShouldBeEmpty();
    }

    static int CountCheckBoxes(TreeView tree) =>
        tree.rowLayout.Children.OfType<Tree.Internal.TreeNodeView>().Count(v => FindCheckBox(v) != null);

    /// <summary>The box is drawn (Border + glyph), so "checked" is the visible check mark.</summary>
    static bool FirstCheckBoxIsChecked(TreeView tree) =>
        FindCheckBox((Tree.Internal.TreeNodeView)tree.rowLayout.Children[0])!.Content!.IsVisible;

    static Border? FindCheckBox(IView view) => view switch
    {
        Border b when b.AutomationId == Tree.Internal.TreeNodeView.CheckBoxAutomationId => b,
        Border b when b.Content is not null => FindCheckBox(b.Content),
        ContentView cv when cv.Content is not null => FindCheckBox(cv.Content),
        Layout l => l.Children.Select(FindCheckBox).FirstOrDefault(f => f != null),
        _ => null
    };
}
