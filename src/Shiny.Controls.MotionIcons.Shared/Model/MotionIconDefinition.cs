namespace Shiny.Controls.MotionIcons;

/// <summary>
/// An icon: its artwork, and optionally the motion authored for it.
/// </summary>
/// <remarks>
/// Artwork and motion are separable on purpose. A definition with no <see cref="Motion"/> is still
/// perfectly usable — every generic preset works on any icon — and an icon that ships its own
/// motion can still be driven by a preset when the caller wants something different.
/// </remarks>
public sealed record MotionIconDefinition
{
    /// <summary>Creates a definition.</summary>
    /// <param name="name">The name it is looked up by. Lower-kebab-case by convention.</param>
    /// <param name="parts">The artwork, drawn in order.</param>
    /// <param name="motion">Motion authored specifically for this icon.</param>
    /// <param name="viewBox">Width and height of the square coordinate space the parts are drawn in.</param>
    public MotionIconDefinition(
        string name,
        IReadOnlyList<MotionIconPart> parts,
        MotionSpec? motion = null,
        float viewBox = 24f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(viewBox, 0f);

        if (parts.Count == 0)
            throw new ArgumentException("An icon needs at least one part.", nameof(parts));

        Name = name;
        Parts = parts;
        Motion = motion;
        ViewBox = viewBox;
    }

    /// <summary>The name it is looked up by.</summary>
    public string Name { get; }

    /// <summary>Width and height of the square coordinate space the parts are drawn in.</summary>
    public float ViewBox { get; }

    /// <summary>The artwork, drawn in order.</summary>
    public IReadOnlyList<MotionIconPart> Parts { get; }

    /// <summary>Motion authored specifically for this icon, if any.</summary>
    public MotionSpec? Motion { get; init; }

    /// <summary>The centre of the viewBox — the default pivot for anything without its own origin.</summary>
    public MotionPoint Center => new(ViewBox / 2f, ViewBox / 2f);

    /// <summary>Resolves a part's pivot, falling back to the icon's centre.</summary>
    public MotionPoint OriginOf(MotionIconPart part)
    {
        ArgumentNullException.ThrowIfNull(part);
        return part.Origin ?? Center;
    }

    /// <summary>Finds a part by id.</summary>
    public MotionIconPart? FindPart(string id)
    {
        foreach (var part in Parts)
        {
            if (string.Equals(part.Id, id, StringComparison.Ordinal))
                return part;
        }

        return null;
    }

    /// <summary>Returns the same artwork with different motion attached.</summary>
    public MotionIconDefinition WithMotion(MotionSpec? motion) => this with { Motion = motion };
}
