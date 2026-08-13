namespace Shiny.Maui.Controls.Tree;

public partial class TreeView
{
    /// <summary>
    /// Default depth cap for <see cref="ExpandAll(int)"/> / <see cref="ExpandAllAsync(int)"/>.
    /// Stops a self-referencing (or endlessly generated) hierarchy from expanding forever.
    /// </summary>
    public const int DefaultExpandAllMaxDepth = 32;

    /// <summary>
    /// Expand every node, materializing children via <see cref="ChildrenSelector"/>.
    /// Only the branches that genuinely need <see cref="ChildrenLoader"/> (the selector
    /// returned null and no children are cached yet) are skipped — call
    /// <see cref="ExpandAllAsync(int)"/> to expand those too.
    /// </summary>
    /// <param name="maxDepth">Deepest node depth to expand. Guards against cyclic hierarchies.</param>
    public void ExpandAll(int maxDepth = DefaultExpandAllMaxDepth)
    {
        foreach (var n in rootNodes)
            ExpandRecursiveSync(n, maxDepth);
        Rebuild();
    }

    void ExpandRecursiveSync(TreeNode node, int maxDepth)
    {
        if (node.Depth >= maxDepth)
            return;
        if (!HasChildren(node.Item) || !CanExpand(node.Item))
            return;
        if (node.Children == null)
        {
            // Per-node, not per-tree: a tree can mix sync and lazy branches, so only the
            // branches the selector can't supply are left for ExpandAllAsync.
            var kids = ChildrenSelector?.Invoke(node.Item);
            if (kids == null && ChildrenLoader != null)
                return; // skip lazy-loaded subtrees in sync expand

            node.Children = new System.Collections.ObjectModel.ObservableCollection<TreeNode>();
            if (kids != null)
            {
                foreach (var item in kids)
                {
                    if (item != null)
                        node.Children.Add(new TreeNode(item, node, node.Depth + 1));
                }
            }
            node.LoadState = TreeLoadState.Loaded;
        }
        node.IsExpanded = true;
        foreach (var c in node.Children!)
            ExpandRecursiveSync(c, maxDepth);
    }

    /// <summary>
    /// Expand every node, awaiting <see cref="ChildrenLoader"/> as needed.
    /// </summary>
    /// <param name="maxDepth">Deepest node depth to expand. Guards against endlessly lazy-loading hierarchies.</param>
    public async Task ExpandAllAsync(int maxDepth = DefaultExpandAllMaxDepth)
    {
        foreach (var n in rootNodes)
            await ExpandRecursiveAsync(n, maxDepth);
        Rebuild();
    }

    async Task ExpandRecursiveAsync(TreeNode node, int maxDepth)
    {
        if (node.Depth >= maxDepth)
            return;
        if (!HasChildren(node.Item) || !CanExpand(node.Item))
            return;
        if (node.Children == null)
        {
            var syncKids = ChildrenSelector?.Invoke(node.Item);
            if (syncKids != null)
            {
                node.Children = new System.Collections.ObjectModel.ObservableCollection<TreeNode>();
                foreach (var item in syncKids)
                {
                    if (item != null)
                        node.Children.Add(new TreeNode(item, node, node.Depth + 1));
                }
                node.LoadState = TreeLoadState.Loaded;
            }
            else if (ChildrenLoader != null)
            {
                node.LoadState = TreeLoadState.Loading;
                try
                {
                    var children = await ChildrenLoader(node.Item);
                    node.Children = new System.Collections.ObjectModel.ObservableCollection<TreeNode>();
                    foreach (var item in children)
                    {
                        if (item != null)
                            node.Children.Add(new TreeNode(item, node, node.Depth + 1));
                    }
                    node.LoadState = TreeLoadState.Loaded;
                }
                catch (Exception ex)
                {
                    node.LoadError = ex;
                    node.LoadState = TreeLoadState.Error;
                    RaiseLoadFailed(node, ex);
                    return;
                }
            }
            else
            {
                node.Children = new System.Collections.ObjectModel.ObservableCollection<TreeNode>();
                node.LoadState = TreeLoadState.Loaded;
            }
        }
        node.IsExpanded = true;
        foreach (var c in node.Children!)
            await ExpandRecursiveAsync(c, maxDepth);
    }

    public void CollapseAll()
    {
        foreach (var n in rootNodes)
            CollapseRecursive(n);
        Rebuild();
    }

    void CollapseRecursive(TreeNode node)
    {
        node.IsExpanded = false;
        if (node.Children != null)
            foreach (var c in node.Children)
                CollapseRecursive(c);
    }

    /// <summary>
    /// Expand the node that wraps the given source item.
    /// </summary>
    public void Expand(object item)
    {
        var node = FindNode(item);
        if (node == null || node.IsExpanded)
            return;
        ToggleExpand(node);
    }

    public void Collapse(object item)
    {
        var node = FindNode(item);
        if (node == null || !node.IsExpanded)
            return;
        ToggleExpand(node);
    }

    /// <summary>
    /// Drop the cached children for the given item so the next expand re-runs the loader/selector.
    /// </summary>
    public void Refresh(object item)
    {
        var node = FindNode(item);
        if (node == null)
            return;
        var wasExpanded = node.IsExpanded;
        node.IsExpanded = false;
        node.Children = null;
        node.LoadState = TreeLoadState.NotLoaded;
        node.LoadError = null;
        Rebuild();
        if (wasExpanded)
            ToggleExpand(node);
    }

    /// <summary>
    /// Re-run the root loader (or re-bind the ItemsSource). Drops all children caches.
    /// </summary>
    public async Task ReloadAsync()
    {
        if (RootLoader != null)
        {
            rootLoaderInvoked = false;
            await EnsureRootLoadedAsync();
        }
        else
        {
            BuildRootNodes();
            Rebuild();
        }
    }

    /// <summary>
    /// Check every selectable node, whether or not its branch is expanded. Only meaningful in
    /// <see cref="TreeSelectionMode.Multiple"/>; branches that have never been loaded aren't
    /// included (call <see cref="ExpandAllAsync(int)"/> first to materialize them).
    /// </summary>
    public void SelectAll()
    {
        if (SelectionMode != TreeSelectionMode.Multiple)
            return;

        EnsureSelectedItemsCollection();
        foreach (var node in EnumerateAllNodes())
        {
            if (!CanSelect(node.Item))
                continue;
            node.IsSelected = true;
            if (!SelectedItems!.Contains(node.Item))
                SelectedItems.Add(node.Item);
        }
    }

    /// <summary>
    /// Clear the selection in any mode: unchecks every node and resets
    /// <see cref="SelectedItem"/> / <see cref="SelectedItems"/>.
    /// </summary>
    public void DeselectAll()
    {
        foreach (var node in EnumerateAllNodes())
            node.IsSelected = false;

        SelectedItems?.Clear();
        SetValueFromInternal(SelectedItemProperty, null);
    }

    /// <summary>
    /// Check (or uncheck) an item and every materialized descendant beneath it.
    /// Only meaningful in <see cref="TreeSelectionMode.Multiple"/>.
    /// </summary>
    public void SetBranchSelected(object item, bool selected)
    {
        if (SelectionMode != TreeSelectionMode.Multiple)
            return;

        var node = FindNode(item);
        if (node == null)
            return;

        EnsureSelectedItemsCollection();
        foreach (var n in EnumerateNode(node))
        {
            if (!CanSelect(n.Item))
                continue;
            n.IsSelected = selected;
            if (selected && !SelectedItems!.Contains(n.Item))
                SelectedItems.Add(n.Item);
            else if (!selected)
                SelectedItems!.Remove(n.Item);
        }
    }

    /// <summary>Every node the tree has materialized, expanded or not, in visual order.</summary>
    internal IEnumerable<TreeNode> EnumerateAllNodes()
    {
        foreach (var root in rootNodes)
        {
            foreach (var node in EnumerateNode(root))
                yield return node;
        }
    }

    static IEnumerable<TreeNode> EnumerateNode(TreeNode node)
    {
        yield return node;
        if (node.Children == null)
            yield break;
        foreach (var child in node.Children)
        {
            foreach (var descendant in EnumerateNode(child))
                yield return descendant;
        }
    }

    public TreeNode? FindNode(object item)
    {
        foreach (var n in rootNodes)
        {
            var found = FindRecursive(n, item);
            if (found != null)
                return found;
        }
        return null;
    }

    static TreeNode? FindRecursive(TreeNode node, object item)
    {
        if (node.Item == item || Equals(node.Item, item))
            return node;
        if (node.Children == null)
            return null;
        foreach (var c in node.Children)
        {
            var found = FindRecursive(c, item);
            if (found != null)
                return found;
        }
        return null;
    }
}
