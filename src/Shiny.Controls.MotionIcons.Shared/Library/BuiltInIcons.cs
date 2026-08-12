namespace Shiny.Controls.MotionIcons;

/// <summary>
/// The icons that ship with the library.
/// </summary>
/// <remarks>
/// <para>Everything here is drawn in a 24x24 box at a nominal stroke width of 2 with round caps and
/// joins, so the set sits comfortably beside Feather, Lucide and Material's outlined family. Path
/// data is authored, not generated, because the parts an icon is split into are a motion decision:
/// a bell is a body and a clapper because those two things swing differently, and nothing about the
/// artwork alone would tell a tool that.</para>
/// <para>Each icon's motion is hand-authored to say what the icon means — a trash lid lifts off its
/// hinge, a download arrow falls out of the tray and reappears above it, a lock's shackle pops.
/// Callers who want something plainer can override any of it with a <see cref="MotionPreset"/>.</para>
/// </remarks>
public static partial class BuiltInIcons
{
    /// <summary>Every built-in icon.</summary>
    public static IEnumerable<MotionIconDefinition> All()
    {
        foreach (var icon in Actions())
            yield return icon;

        foreach (var icon in Objects())
            yield return icon;

        foreach (var icon in Indicators())
            yield return icon;
    }

    static MotionIconDefinition Icon(string name, MotionSpec motion, params MotionIconPart[] parts)
        => new(name, parts, motion);

    static MotionIconPart Origin(this MotionIconPart part, float x, float y)
        => part with { Origin = new MotionPoint(x, y) };

    static MotionIconPart Solid(this MotionIconPart part)
        => part with { Fill = IconPaint.Current, Stroke = IconPaint.None };
}
