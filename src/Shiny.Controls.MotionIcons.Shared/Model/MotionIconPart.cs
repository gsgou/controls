namespace Shiny.Controls.MotionIcons;

/// <summary>How the ends of an open stroke are drawn.</summary>
public enum MotionLineCap
{
    /// <summary>Rounded ends — the default for this icon style.</summary>
    Round,

    /// <summary>Ends flush with the last point.</summary>
    Butt,

    /// <summary>Square ends extending half a stroke width past the last point.</summary>
    Square
}

/// <summary>How corners between stroke segments are drawn.</summary>
public enum MotionLineJoin
{
    /// <summary>Rounded corners — the default for this icon style.</summary>
    Round,

    /// <summary>Pointed corners.</summary>
    Miter,

    /// <summary>Flattened corners.</summary>
    Bevel
}

/// <summary>
/// One independently animatable piece of an icon.
/// </summary>
/// <remarks>
/// <para>Icons are split into parts for one reason: a part is the unit a motion track can target.
/// A bell is a body, a clapper and a shockwave because those three move differently; a plain
/// chevron is one part because nothing about it moves independently.</para>
/// <para>Every part shares the icon's full viewBox as its coordinate space and its layout box.
/// Nothing is offset or measured per part, which is what makes <see cref="Origin"/> mean the same
/// thing on both hosts: MAUI resolves it against the layer size, the browser against
/// <c>transform-box: view-box</c>, and both land on the same pixel.</para>
/// </remarks>
public sealed record MotionIconPart
{
    /// <summary>Creates a part.</summary>
    /// <param name="id">Identifies the part to motion tracks. Unique within the icon.</param>
    /// <param name="path">SVG path data, authored in the icon's viewBox units.</param>
    public MotionIconPart(string id, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Id = id;
        Path = path;
    }

    /// <summary>Identifies the part to motion tracks.</summary>
    public string Id { get; }

    /// <summary>SVG path data in the icon's viewBox units.</summary>
    public string Path { get; }

    /// <summary>Interior paint. Defaults to unpainted — this is a stroked icon set.</summary>
    public IconPaint Fill { get; init; } = IconPaint.None;

    /// <summary>Outline paint. Defaults to the host's icon colour.</summary>
    public IconPaint Stroke { get; init; } = IconPaint.Current;

    /// <summary>
    /// Multiplier on the host's stroke width, so a hairline detail stays proportionally thinner
    /// when the icon is drawn heavier.
    /// </summary>
    public float StrokeScale { get; init; } = 1f;

    /// <summary>Cap style for open strokes.</summary>
    public MotionLineCap LineCap { get; init; } = MotionLineCap.Round;

    /// <summary>Join style between stroke segments.</summary>
    public MotionLineJoin LineJoin { get; init; } = MotionLineJoin.Round;

    /// <summary>
    /// Pivot for rotation and scale, in viewBox units. Null pivots about the icon's centre, which
    /// is right for a spin and wrong for anything hinged — a bell swings from its crown, a trash
    /// lid from its hinge.
    /// </summary>
    public MotionPoint? Origin { get; init; }

    /// <summary>Creates a stroked part, the common case.</summary>
    public static MotionIconPart Stroked(string id, string path) => new(id, path);

    /// <summary>Creates a filled, unstroked part.</summary>
    public static MotionIconPart Filled(string id, string path, IconPaint? fill = null)
        => new(id, path) { Fill = fill ?? IconPaint.Current, Stroke = IconPaint.None };
}
