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
/// <para>The set is grouped by what an icon is for rather than by what it looks like — actions,
/// objects, indicators, navigation, media, files and weather — and the groups exist only to keep
/// the artwork files a readable length. Every icon lands in one flat, case-insensitive namespace
/// in <see cref="MotionIconLibrary"/>, so a name has to be unique across all of them.</para>
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

        foreach (var icon in Navigation())
            yield return icon;

        foreach (var icon in Media())
            yield return icon;

        foreach (var icon in Files())
            yield return icon;

        foreach (var icon in Weather())
            yield return icon;
    }

    static MotionIconDefinition Icon(string name, MotionSpec motion, params MotionIconPart[] parts)
        => new(name, parts, motion);

    static MotionIconPart Origin(this MotionIconPart part, float x, float y)
        => part with { Origin = new MotionPoint(x, y) };

    static MotionIconPart Solid(this MotionIconPart part)
        => part with { Fill = IconPaint.Current, Stroke = IconPaint.None };

    // A few pieces are a bar rather than a line — a battery's charge, say — and reading as a bar
    // means being drawn several times heavier than the icon's nominal stroke.
    static MotionIconPart Weight(this MotionIconPart part, float scale)
        => part with { StrokeScale = scale };
}
