namespace Shiny.Maui.Controls;

/// <summary>Carries the bar item that was tapped.</summary>
public class NavBarItemEventArgs(ToolbarItem item) : EventArgs
{
    /// <summary>The item. A <see cref="NavBarItem"/> when the page declared one.</summary>
    public ToolbarItem Item { get; } = item;
}

/// <summary>Raised before the back button pops, so the pop can be stopped.</summary>
public class NavBarBackEventArgs : EventArgs
{
    /// <summary>Set to true to keep the page where it is.</summary>
    public bool Cancel { get; set; }
}
