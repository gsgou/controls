namespace Shiny.Controls.MotionIcons;

/// <summary>
/// Builds the generic <see cref="MotionPreset"/> motion for an icon.
/// </summary>
/// <remarks>
/// Presets drive the icon as a whole rather than its individual parts, which is exactly why they
/// work on artwork the library has never seen — a caller's own path gets the same pulse as a
/// built-in one. <see cref="MotionPreset.Draw"/> is the exception: it has to reach into the parts,
/// because drawing a multi-stroke icon on all at once looks like a mistake rather than an effect.
/// </remarks>
public static class MotionPresets
{
    /// <summary>Builds the motion for a preset.</summary>
    /// <param name="preset">The preset.</param>
    /// <param name="icon">The icon it will be applied to.</param>
    /// <returns>The motion, or null if the preset animates nothing.</returns>
    public static MotionSpec? Build(MotionPreset preset, MotionIconDefinition icon)
    {
        ArgumentNullException.ThrowIfNull(icon);

        return preset switch
        {
            MotionPreset.Default => icon.Motion ?? Build(MotionPreset.Pulse, icon),
            MotionPreset.None => null,

            MotionPreset.Pulse => MotionSpecBuilder.Build(600, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.18d, MotionEase.SinInOut)
                    .At(1d, 1d))),

            MotionPreset.Beat => MotionSpecBuilder.Build(900, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.14d, 1.22d, MotionEase.QuadIn)
                    .At(0.28d, 1d, MotionEase.QuadOut)
                    .At(0.42d, 1.14d, MotionEase.QuadIn)
                    .At(0.56d, 1d)
                    .At(1d, 1d))),

            MotionPreset.Spin => MotionSpecBuilder.Build(900, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(1d, 360d))),

            MotionPreset.Shake => MotionSpecBuilder.Build(600, m => m
                .MoveX(null, k => k.Evenly(MotionEase.SinInOut, 0d, -2d, 2d, -1.6d, 1.6d, -0.8d, 0d))),

            MotionPreset.Wobble => MotionSpecBuilder.Build(800, m => m
                .Rotate(k => k.Evenly(MotionEase.SinInOut, 0d, -12d, 10d, -8d, 5d, -2d, 0d))),

            MotionPreset.Bounce => MotionSpecBuilder.Build(800, m => m
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, -5d, MotionEase.BounceOut)
                    .At(1d, 0d))),

            MotionPreset.Float => MotionSpecBuilder.Build(2200, m => m
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.SinInOut)
                    .At(0.5d, -2d, MotionEase.SinInOut)
                    .At(1d, 0d))),

            MotionPreset.Pop => MotionSpecBuilder.Build(500, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.3d, 0.86d, MotionEase.BackOut)
                    .At(1d, 1d))),

            MotionPreset.Tada => MotionSpecBuilder.Build(1000, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.1d, 0.92d, MotionEase.QuadOut)
                    .At(0.3d, 1.12d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .Rotate(k => k.Evenly(MotionEase.SinInOut, 0d, 0d, -3d, 3d, -3d, 3d, -3d, 3d, -3d, 3d, 0d))),

            MotionPreset.Flip => MotionSpecBuilder.Build(900, m => m
                .ScaleX(null, k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, -1d, MotionEase.SinInOut)
                    .At(1d, 1d))),

            // Hung from the top edge rather than the centre — a swing that pivots through the
            // middle of the artwork reads as a wobble, not a swing.
            MotionPreset.Swing => MotionSpecBuilder.Build(900, m => m
                    .Rotate(k => k.Evenly(MotionEase.SinInOut, 0d, 14d, -10d, 6d, -3d, 0d)))
                with { RootOrigin = new MotionPoint(icon.ViewBox / 2f, 1f) },

            MotionPreset.Blink => MotionSpecBuilder.Build(1200, m => m
                .Opacity(k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.45d, 0.15d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),

            MotionPreset.Draw => BuildDraw(icon),

            MotionPreset.Nudge => MotionSpecBuilder.Build(700, m => m
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.CubicOut)
                    .At(0.4d, 3d, MotionEase.CubicInOut)
                    .At(1d, 0d))),

            MotionPreset.Jiggle => MotionSpecBuilder.Build(500, m => m
                .Rotate(k => k.Evenly(MotionEase.QuadInOut, 0d, -8d, 8d, -6d, 6d, -3d, 0d))),

            _ => null
        };
    }

    /// <summary>
    /// Draws each part on in turn across the cycle, then holds the finished icon.
    /// </summary>
    /// <remarks>
    /// Filled parts are faded in rather than drawn: trimming only means something for a stroke, and
    /// a filled shape given a trim would simply pop into existence at the end of its window.
    /// </remarks>
    static MotionSpec BuildDraw(MotionIconDefinition icon)
    {
        var builder = new MotionSpecBuilder(TimeSpan.FromMilliseconds(200 + 500 * icon.Parts.Count));
        var slice = 1d / icon.Parts.Count;

        for (var i = 0; i < icon.Parts.Count; i++)
        {
            var part = icon.Parts[i];
            var start = i * slice;
            var end = start + slice;

            if (part.Stroke.IsPainted)
            {
                builder.Trim(part.Id, k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(start, 0d, MotionEase.QuadInOut)
                    .At(end, 1d, MotionEase.Linear)
                    .At(1d, 1d));
            }

            if (part.Fill.IsPainted)
            {
                builder.Opacity(part.Id, k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(start, 0d, MotionEase.QuadOut)
                    .At(end, 1d, MotionEase.Linear)
                    .At(1d, 1d));
            }
        }

        return builder.Build();
    }
}
