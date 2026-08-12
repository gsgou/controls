namespace Shiny.Controls.MotionIcons;

/// <summary>
/// A property a motion track can drive.
/// </summary>
/// <remarks>
/// The set is deliberately small and deliberately excludes path morphing. Every channel here has a
/// native, identically-behaving implementation on both hosts — MAUI drives a scene layer, the
/// browser drives a CSS property — which is the only way the same icon can be guaranteed to look
/// the same in both. Morphing geometry would have meant hand-written fallbacks on the web the
/// moment anyone opened Firefox, so hinged and morphing icons are authored as separate parts moved
/// by transforms instead, exactly as they would be in a design tool.
/// </remarks>
public enum MotionChannel
{
    /// <summary>Opacity, 0 to 1.</summary>
    Opacity,

    /// <summary>Horizontal offset in viewBox units.</summary>
    TranslateX,

    /// <summary>Vertical offset in viewBox units.</summary>
    TranslateY,

    /// <summary>Rotation in degrees about the part's origin, clockwise.</summary>
    Rotate,

    /// <summary>Uniform scale about the part's origin.</summary>
    Scale,

    /// <summary>Horizontal scale about the part's origin.</summary>
    ScaleX,

    /// <summary>Vertical scale about the part's origin.</summary>
    ScaleY,

    /// <summary>Multiplier on the host's stroke width.</summary>
    StrokeWidth,

    /// <summary>
    /// Fraction of the path drawn, measured from its start — 0 draws nothing, 1 draws all of it.
    /// This is the "draw on" channel.
    /// </summary>
    Trim
}

/// <summary>Which paint channel of a part a colour track drives.</summary>
public enum MotionPaintChannel
{
    /// <summary>The part's interior.</summary>
    Fill,

    /// <summary>The part's outline.</summary>
    Stroke
}
