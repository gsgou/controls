namespace Shiny.Maui.Controls.Desktop.Ribbons;

/// <summary>
/// How much room an item asks for inside its <see cref="RibbonGroup"/>.
/// </summary>
/// <remarks>
/// This is the whole of a ribbon's layout language. A <see cref="Large"/> item takes a column to
/// itself — icon over label, the shape a primary command wants — while <see cref="Small"/> items
/// stack up to <see cref="Ribbon.SmallItemRows"/> deep in a shared column. Nothing declares columns;
/// they fall out of the sizes, which is why a group can be reordered without re-laying it out.
/// </remarks>
public enum RibbonItemSize
{
    /// <summary>Icon above the label, one column to itself. The default.</summary>
    Large,

    /// <summary>Icon beside the label, sharing a column with the small items next to it.</summary>
    Small
}


/// <summary>
/// What the ribbon body does with the room it has.
/// </summary>
public enum RibbonDisplayMode
{
    /// <summary>Tab strip plus the open group body. The normal state.</summary>
    Expanded,

    /// <summary>
    /// Only the tab strip is drawn. The body comes back when a tab is picked, and hides again on the
    /// next command — the "minimize the ribbon" state, for people who want the screen back.
    /// </summary>
    Collapsed,

    /// <summary>
    /// One dense row: every item is drawn small, group titles are dropped and groups that do not fit
    /// fall into the overflow. The shape Office calls the simplified ribbon.
    /// </summary>
    Simplified
}


/// <summary>Why the ribbon changed which tab is showing.</summary>
public enum RibbonTabChangeReason
{
    /// <summary>A caller set <see cref="Ribbon.SelectedIndex"/> or <see cref="Ribbon.SelectedTab"/>.</summary>
    Programmatic,

    /// <summary>The user picked the tab off the strip.</summary>
    User,

    /// <summary>
    /// The tab the ribbon was on stopped being selectable — hidden, disabled or removed — and it
    /// moved to the nearest one that still is.
    /// </summary>
    Fallback
}
