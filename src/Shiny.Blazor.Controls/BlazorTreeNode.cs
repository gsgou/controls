namespace Shiny.Blazor.Controls;

public enum BlazorTreeLoadState
{
    NotLoaded,
    Loading,
    Loaded,
    Error
}

public class BlazorTreeNode<TItem>
{
    internal BlazorTreeNode(TItem item, BlazorTreeNode<TItem>? parent, int depth)
    {
        Item = item;
        Parent = parent;
        Depth = depth;
    }

    public TItem Item { get; }
    public BlazorTreeNode<TItem>? Parent { get; }
    public int Depth { get; }

    /// <summary>Stable identity used to correlate DOM rows with nodes during drag/drop.</summary>
    internal string Id { get; } = Guid.NewGuid().ToString("N");

    public bool IsExpanded { get; internal set; }
    public bool IsSelected { get; internal set; }
    public BlazorTreeLoadState LoadState { get; internal set; }
    public Exception? LoadError { get; internal set; }
    public List<BlazorTreeNode<TItem>>? Children { get; internal set; }
}

public enum BlazorTreeSelectionMode
{
    None,
    Single,
    Multiple
}

public enum BlazorTreeDropPosition
{
    Above,
    Below,
    Into
}

public class TreeItemEventArgs<TItem>
{
    public TreeItemEventArgs(BlazorTreeNode<TItem> node)
    {
        Node = node;
    }
    public BlazorTreeNode<TItem> Node { get; }
    public TItem Item => Node.Item;
}

public class TreeLoadFailedEventArgs<TItem> : TreeItemEventArgs<TItem>
{
    public TreeLoadFailedEventArgs(BlazorTreeNode<TItem> node, Exception ex) : base(node)
    {
        Exception = ex;
    }
    public Exception Exception { get; }
}

public class TreeItemDroppedEventArgs<TItem>
{
    public TreeItemDroppedEventArgs(BlazorTreeNode<TItem> source, BlazorTreeNode<TItem> target, BlazorTreeDropPosition position)
    {
        Source = source;
        Target = target;
        Position = position;
    }
    public BlazorTreeNode<TItem> Source { get; }
    public BlazorTreeNode<TItem> Target { get; }
    public BlazorTreeDropPosition Position { get; }
    public TItem SourceItem => Source.Item;
    public TItem TargetItem => Target.Item;
}
