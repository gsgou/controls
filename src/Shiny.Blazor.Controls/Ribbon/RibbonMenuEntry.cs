using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// One line inside a ribbon dropdown.
/// </summary>
/// <remarks>
/// A plain object rather than a component, matching <see cref="ToolbarItem"/>: a menu line has no
/// layout of its own to author, and a list is what a menu usually comes from — a set of styles, a set
/// of recent files, a set of chart types. The MAUI side declares these in markup instead, because XAML
/// has no comfortable way to write a nested object graph inline.
/// </remarks>
public class RibbonMenuEntry
{
    /// <summary>The line's label.</summary>
    public string? Text { get; set; }

    /// <summary>Inline SVG/HTML markup, an image URL, or a glyph/emoji.</summary>
    public string? Icon { get; set; }

    /// <summary>Draws a tick beside the line, for a menu that is a set of choices rather than actions.</summary>
    public bool IsChecked { get; set; }

    /// <summary>Draws a divider instead of a line. Everything else is ignored.</summary>
    public bool IsSeparator { get; set; }

    public bool IsDisabled { get; set; }

    /// <summary>Runs when the line is picked. Not raised for a line that has <see cref="Children"/>.</summary>
    public EventCallback OnClick { get; set; }

    /// <summary>Nested lines. When any are present the line flies out a submenu instead of acting.</summary>
    public List<RibbonMenuEntry>? Children { get; set; }

    /// <summary>Arbitrary payload, handed back on <see cref="Ribbon.MenuEntrySelected"/>.</summary>
    public object? Tag { get; set; }

    /// <summary>True when picking this line opens a submenu rather than running something.</summary>
    public bool HasChildren => !this.IsSeparator && this.Children is { Count: > 0 };
}
