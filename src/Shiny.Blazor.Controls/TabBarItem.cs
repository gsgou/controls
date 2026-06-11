namespace Shiny.Blazor.Controls;

/// <summary>
/// Describes a single tab rendered by <see cref="ShinyTabBar"/>.
/// </summary>
public class TabBarItem
{
    /// <summary>Stable identifier used for selection (two-way bound via <c>SelectedKey</c>).</summary>
    public string? Key { get; set; }

    /// <summary>Inline SVG/HTML markup, an image URL, or a glyph/emoji shown when inactive.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional alternate icon shown when the tab is selected (e.g. a filled variant).</summary>
    public string? ActiveIcon { get; set; }

    /// <summary>Label shown under the icon (hidden when <c>ShowLabels</c> is false).</summary>
    public string? Label { get; set; }

    /// <summary>When set, selecting the tab also navigates to this URL.</summary>
    public string? Href { get; set; }

    /// <summary>Optional badge text shown on the tab (e.g. a count). An empty string renders a dot.</summary>
    public string? Badge { get; set; }

    /// <summary>When true the tab is dimmed and not selectable.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Arbitrary payload returned via <c>ShinyTabBar.ItemClicked</c>.</summary>
    public object? Tag { get; set; }
}
