namespace Shiny.Maui.Controls;

/// <summary>Where <see cref="ShinyNavBar"/> puts the title.</summary>
public enum NavBarTitleAlignment
{
    /// <summary>Centred on iOS and Mac Catalyst, leading-aligned everywhere else — the platform convention.</summary>
    Auto,

    /// <summary>Leading edge, immediately after the back button and the left items.</summary>
    Start,

    /// <summary>
    /// Centred in the bar itself, not in the gap between the two item groups — so it stays put when
    /// an item is added to one side. It is inset far enough to clear the wider group and truncates
    /// rather than running underneath one.
    /// </summary>
    Center
}

/// <summary>How <see cref="ShinyNavBar"/> draws the oversized title beneath the bar row.</summary>
public enum LargeTitleDisplay
{
    /// <summary>
    /// Follow the host. On a page it means "whatever the <see cref="ShinyNavigationPage"/> said";
    /// on the navigation page or the bar itself there is nothing above it to ask, so it means
    /// <see cref="None"/>.
    /// </summary>
    /// <remarks>
    /// It exists so a page can turn the large title <em>off</em> against a navigation page that
    /// turned it on. Without a distinct "not answered" value, a page setting <see cref="None"/>
    /// would be indistinguishable from a page that never said anything.
    /// </remarks>
    Inherit = 0,

    /// <summary>No large title. The title is drawn inline, in the bar row.</summary>
    None,

    /// <summary>Always shown, and never collapses. The inline title stays hidden.</summary>
    Always,

    /// <summary>
    /// Shown at rest and collapsed into the inline title as the page scrolls — the iOS behaviour.
    /// Needs something to scroll: see <see cref="ShinyNav.ScrollSourceProperty"/>.
    /// </summary>
    Collapsing
}

/// <summary>What a bar item renders as.</summary>
public enum NavBarItemDisplay
{
    /// <summary>Icon when the item has one, text otherwise. The default.</summary>
    Auto,

    /// <summary>Icon only. An item with no icon draws nothing, so it is only ever set deliberately.</summary>
    Icon,

    /// <summary>Text only, even when the item has an icon.</summary>
    Text,

    /// <summary>Icon with the text beside it.</summary>
    IconAndText
}

/// <summary>Which end of the bar a group of items sits at.</summary>
public enum NavBarSide
{
    /// <summary>The leading end — where the back button is.</summary>
    Left,

    /// <summary>The trailing end.</summary>
    Right
}
