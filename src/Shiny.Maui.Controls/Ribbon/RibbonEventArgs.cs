namespace Shiny.Maui.Controls.Ribbons;

/// <summary>The new state of a <see cref="RibbonToggleButton"/>.</summary>
public class RibbonCheckedEventArgs(bool isChecked) : EventArgs
{
    public bool IsChecked { get; } = isChecked;
}


/// <summary>Which tab the ribbon moved to, and what moved it.</summary>
public class RibbonTabEventArgs(RibbonTab? tab, int index, RibbonTabChangeReason reason) : EventArgs
{
    /// <summary>The tab now showing, or null when the ribbon has no selectable tab left.</summary>
    public RibbonTab? Tab { get; } = tab;

    /// <summary>Its index in <see cref="Ribbon.Tabs"/>, or -1 when there is no tab.</summary>
    public int Index { get; } = index;

    public RibbonTabChangeReason Reason { get; } = reason;
}


/// <summary>An item the user pressed, with the group and tab it came from.</summary>
/// <remarks>
/// Raised on top of the item's own <c>Clicked</c> / command, not instead of it. It exists so a host
/// can log, close a collapsed ribbon or drive a status bar from one handler rather than subscribing
/// to every button on the bar.
/// </remarks>
public class RibbonItemEventArgs(RibbonItem item, RibbonGroup? group, RibbonTab? tab) : EventArgs
{
    public RibbonItem Item { get; } = item;

    public RibbonGroup? Group { get; } = group;

    public RibbonTab? Tab { get; } = tab;
}


/// <summary>The group whose dialog launcher was pressed.</summary>
public class RibbonGroupEventArgs(RibbonGroup group) : EventArgs
{
    public RibbonGroup Group { get; } = group;
}
