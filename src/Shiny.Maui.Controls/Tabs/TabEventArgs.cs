namespace Shiny.Maui.Controls;

/// <summary>Raised by <see cref="ShinyTabBar.SelectionChanged"/> after the selected tab has changed.</summary>
public class TabSelectionChangedEventArgs(int oldIndex, int newIndex, ShinyTabItem? oldItem, ShinyTabItem? newItem) : EventArgs
{
    /// <summary>Index of the tab that was selected before, or -1.</summary>
    public int OldIndex { get; } = oldIndex;

    /// <summary>Index of the tab now selected, or -1.</summary>
    public int NewIndex { get; } = newIndex;

    /// <summary>The tab that was selected before, or null.</summary>
    public ShinyTabItem? OldItem { get; } = oldItem;

    /// <summary>The tab now selected, or null.</summary>
    public ShinyTabItem? NewItem { get; } = newItem;
}


/// <summary>
/// Raised by <see cref="ShinyTabBar.TabReselected"/> when the tab that is already selected is
/// tapped again — the gesture apps use to scroll a list back to the top or pop a nested stack.
/// </summary>
public class TabReselectedEventArgs(int index, ShinyTabItem item) : EventArgs
{
    /// <summary>Index of the reselected tab.</summary>
    public int Index { get; } = index;

    /// <summary>The reselected tab.</summary>
    public ShinyTabItem Item { get; } = item;
}


/// <summary>Raised by <see cref="ShinyTabBar.CenterClicked"/>.</summary>
public class TabCenterClickedEventArgs : EventArgs
{
    /// <summary>
    /// Set to true to stop the centre menu opening. Leaves <see cref="TabCenterMode.Action"/>
    /// untouched — there is nothing to cancel there.
    /// </summary>
    public bool Cancel { get; set; }
}


/// <summary>Raised by <see cref="TabAction.Clicked"/> and <see cref="ShinyTabBar.ActionInvoked"/>.</summary>
public class TabActionEventArgs(TabAction action) : EventArgs
{
    /// <summary>The action that was invoked.</summary>
    public TabAction Action { get; } = action;
}
