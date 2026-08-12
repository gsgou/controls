namespace Shiny.Controls.MotionIcons;

/// <summary>Where a part's colour comes from.</summary>
public enum IconPaintKind
{
    /// <summary>The part is not painted at all.</summary>
    None,

    /// <summary>The host's primary icon colour — <c>currentColor</c> on the web.</summary>
    Current,

    /// <summary>The host's secondary colour, for the highlight in a two-tone icon.</summary>
    Accent,

    /// <summary>A fixed colour baked into the artwork.</summary>
    Fixed
}

/// <summary>
/// How one channel (fill or stroke) of a part is painted.
/// </summary>
/// <remarks>
/// Icons overwhelmingly want to inherit the surrounding text colour, so <see cref="Current"/> is
/// the normal answer and a literal colour is the exception. Keeping that a *kind* rather than a
/// resolved value is what lets the same definition render black on a light page, white on a dark
/// one, and themed inside a MAUI app, without the artwork knowing anything about it.
/// </remarks>
public readonly record struct IconPaint(IconPaintKind Kind, string? Value)
{
    /// <summary>Unpainted.</summary>
    public static readonly IconPaint None = new(IconPaintKind.None, null);

    /// <summary>Painted with the host's primary icon colour.</summary>
    public static readonly IconPaint Current = new(IconPaintKind.Current, null);

    /// <summary>Painted with the host's accent colour.</summary>
    public static readonly IconPaint Accent = new(IconPaintKind.Accent, null);

    /// <summary>Painted with a fixed colour.</summary>
    /// <param name="value">Any CSS/MAUI-parseable colour string, normally <c>#rrggbb</c>.</param>
    public static IconPaint Fix(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(IconPaintKind.Fixed, value);
    }

    /// <summary>Whether this channel paints anything.</summary>
    public bool IsPainted => Kind is not IconPaintKind.None;
}
