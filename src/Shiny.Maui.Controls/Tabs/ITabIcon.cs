using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls;

/// <summary>
/// The artwork half of anything the tab bar draws an icon for — a tab, a centre button, a row in
/// the centre menu.
/// </summary>
/// <remarks>
/// The four sources are tried in the order they are declared here, matching
/// <see cref="MotionIconView"/>: an explicit <see cref="IconSource"/> beats a
/// <see cref="IconPathData"/> path, which beats a built-in <see cref="Icon"/> name, and
/// <see cref="IconImage"/> is the escape hatch for artwork that is a bitmap or a font glyph rather
/// than a motion icon. The first three animate; the fourth does not.
/// </remarks>
public interface ITabIcon
{
    /// <summary>A built-in motion icon name (<c>home</c>, <c>bell</c>, …). Unknown names draw nothing.</summary>
    string? Icon { get; }

    /// <summary>Explicit motion artwork. Beats <see cref="Icon"/> and <see cref="IconPathData"/>.</summary>
    MotionIconDefinition? IconSource { get; }

    /// <summary>A raw SVG path in a 24x24 box — the quickest way to animate your own glyph.</summary>
    string? IconPathData { get; }

    /// <summary>
    /// A plain image, used when none of the motion sources are set. Nothing about it animates; it is
    /// here so an app with an existing icon set does not have to redraw it to adopt the bar.
    /// </summary>
    ImageSource? IconImage { get; }

    /// <summary>Which motion the icon plays. Defaults to the one drawn for that icon.</summary>
    MotionPreset Motion { get; }
}
