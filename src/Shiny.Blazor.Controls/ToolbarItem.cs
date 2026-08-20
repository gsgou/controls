namespace Shiny.Blazor.Controls;

/// <summary>
/// Describes a single action, link, submenu or separator rendered by <see cref="ShinyToolbar"/>.
/// </summary>
public class ToolbarItem
{
    /// <summary>Inline SVG/HTML markup, an image URL, or a glyph/emoji.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional label shown beside (or under) the icon.</summary>
    public string? Text { get; set; }

    /// <summary>Tooltip text. Falls back to <see cref="Text"/> when not set.</summary>
    public string? Tooltip { get; set; }

    /// <summary>When set, the item renders as an anchor navigating to this URL.</summary>
    public string? Href { get; set; }

    /// <summary>Anchor target (e.g. "_blank"). Only used when <see cref="Href"/> is set.</summary>
    public string? Target { get; set; }

    /// <summary>Optional badge text shown on the item (e.g. an unread count).</summary>
    public string? Badge { get; set; }

    /// <summary>Overrides the toolbar's foreground color for this item.</summary>
    public string? IconColor { get; set; }

    /// <summary>When true the item is dimmed and not clickable.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Child items. When set, the item becomes a menu button: clicking it opens a dropdown instead of
    /// raising <c>ItemClicked</c>. Children may have children of their own, which fly out as submenus.
    /// </summary>
    public List<ToolbarItem>? Children { get; set; }

    /// <summary>
    /// Renders as a divider line inside a dropdown (icon, text and children are ignored). A separator that
    /// lands on the bar itself - rather than in a menu - is skipped.
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>Arbitrary payload returned via <c>ShinyToolbar.ItemClicked</c>.</summary>
    public object? Tag { get; set; }

    /// <summary>True when the item opens a dropdown rather than raising a click.</summary>
    public bool HasChildren => !IsSeparator && Children is { Count: > 0 };
}
