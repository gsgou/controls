namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    /// <summary>
    /// Weather glyphs, which is where the library leans hardest on looping motion.
    /// </summary>
    /// <remarks>
    /// Falling precipitation is the awkward case: a drop has to leave the bottom of the box and
    /// reappear at the top, and a translate track is required to finish at rest so the icon settles
    /// back into the artwork as drawn. Both are satisfied by teleporting on a
    /// <see cref="MotionEase.StepEnd"/> segment while the drop is faded out — the same trick the
    /// download arrow uses — and then falling the rest of the way back to zero.
    /// </remarks>
    static IEnumerable<MotionIconDefinition> Weather()
    {
        yield return Icon("moon",
            MotionSpecBuilder.Build(1600, m => m
                .Rotate(k => k.Evenly(MotionEase.SinInOut, 0d, -11d, 8d, -4d, 0d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.06d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("moon", "M21 12.8A9 9 0 1 1 11.2 3A7 7 0 0 0 21 12.8z").Origin(12f, 12f));

        yield return Icon("cloud-rain",
            MotionSpecBuilder.Build(1600, m => m
                .MoveY("drop-left", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.34d, 3.4d, MotionEase.StepEnd)
                    .At(0.36d, -3.4d, MotionEase.QuadIn)
                    .At(0.7d, 0d, MotionEase.Linear)
                    .At(1d, 0d))
                .Opacity("drop-left", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.34d, 0.1d, MotionEase.StepEnd)
                    .At(0.36d, 0.1d, MotionEase.QuadOut)
                    .At(0.7d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveY("drop-middle", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.44d, 3.4d, MotionEase.StepEnd)
                    .At(0.46d, -3.4d, MotionEase.QuadIn)
                    .At(0.8d, 0d, MotionEase.Linear)
                    .At(1d, 0d))
                .Opacity("drop-middle", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.44d, 0.1d, MotionEase.StepEnd)
                    .At(0.46d, 0.1d, MotionEase.QuadOut)
                    .At(0.8d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveY("drop-right", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.54d, 3.4d, MotionEase.StepEnd)
                    .At(0.56d, -3.4d, MotionEase.QuadIn)
                    .At(0.9d, 0d, MotionEase.Linear)
                    .At(1d, 0d))
                .Opacity("drop-right", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.54d, 0.1d, MotionEase.StepEnd)
                    .At(0.56d, 0.1d, MotionEase.QuadOut)
                    .At(0.9d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveY("cloud", k => k.Evenly(MotionEase.SinInOut, 0d, -0.5d, 0.4d, 0d))),
            new MotionIconPart("cloud", "M17.5 16.5H7A4.5 4.5 0 0 1 6.6 7.6A6 6 0 0 1 17.9 9.2A3.65 3.65 0 0 1 17.5 16.5z"),
            new MotionIconPart("drop-left", "M8.4 19V21.4"),
            new MotionIconPart("drop-middle", "M12 19.6V22"),
            new MotionIconPart("drop-right", "M15.6 19V21.4"));

        // A flake is a plus, so it is symmetric every ninety degrees — which is exactly enough
        // rotation to read as tumbling without ever having to come back round.
        yield return Icon("cloud-snow",
            MotionSpecBuilder.Build(2000, m => m
                .Rotate("flake-left", k => k
                    .At(0d, 0d, MotionEase.SinInOut)
                    .At(1d, 90d))
                .Opacity("flake-left", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.3d, 0.15d, MotionEase.QuadOut)
                    .At(0.6d, 1d)
                    .At(1d, 1d))
                .Rotate("flake-middle", k => k
                    .At(0d, 0d, MotionEase.SinInOut)
                    .At(1d, -90d))
                .Opacity("flake-middle", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.15d, 1d, MotionEase.QuadIn)
                    .At(0.45d, 0.15d, MotionEase.QuadOut)
                    .At(0.75d, 1d)
                    .At(1d, 1d))
                .Rotate("flake-right", k => k
                    .At(0d, 0d, MotionEase.SinInOut)
                    .At(1d, 90d))
                .Opacity("flake-right", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.3d, 1d, MotionEase.QuadIn)
                    .At(0.6d, 0.15d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))
                .MoveY("cloud", k => k.Evenly(MotionEase.SinInOut, 0d, -0.5d, 0.4d, 0d))),
            new MotionIconPart("cloud", "M17.5 16.5H7A4.5 4.5 0 0 1 6.6 7.6A6 6 0 0 1 17.9 9.2A3.65 3.65 0 0 1 17.5 16.5z"),
            new MotionIconPart("flake-left", "M7.6 19.2V21.8M6.3 20.5H8.9").Origin(7.6f, 20.5f),
            new MotionIconPart("flake-middle", "M12 19.7V22.3M10.7 21H13.3").Origin(12f, 21f),
            new MotionIconPart("flake-right", "M16.4 19.2V21.8M15.1 20.5H17.7").Origin(16.4f, 20.5f));

        // Two dips rather than one: a single fade reads as a pulse, and a strike does not pulse.
        yield return Icon("lightning",
            MotionSpecBuilder.Build(1400, m => m
                .Opacity("bolt", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.12d, 1d, MotionEase.QuadOut)
                    .At(0.2d, 0.12d, MotionEase.QuadIn)
                    .At(0.3d, 1d, MotionEase.QuadOut)
                    .At(0.38d, 0.2d, MotionEase.QuadIn)
                    .At(0.5d, 1d)
                    .At(1d, 1d))
                .Scale("bolt", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 1.12d, MotionEase.QuadIn)
                    .At(0.6d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("bolt", "M13.5 2.5L4.5 13.5H11L10.5 21.5L19.5 10.5H13z").Origin(12f, 12f));

        yield return Icon("wind",
            MotionSpecBuilder.Build(1800, m => m
                .Trim("gust-top", k => k
                    .At(0d, 0.05d, MotionEase.CubicOut)
                    .At(0.45d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("gust-middle", k => k
                    .At(0d, 0.05d, MotionEase.Linear)
                    .At(0.12d, 0.05d, MotionEase.CubicOut)
                    .At(0.57d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("gust-bottom", k => k
                    .At(0d, 0.05d, MotionEase.Linear)
                    .At(0.24d, 0.05d, MotionEase.CubicOut)
                    .At(0.69d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveX(null, k => k.Evenly(MotionEase.SinInOut, 0d, 0.8d, -0.6d, 0d))),
            new MotionIconPart("gust-top", "M3 8.5H13.5A2.5 2.5 0 1 0 11 6"),
            new MotionIconPart("gust-middle", "M3 12.5H17.5A2.5 2.5 0 1 1 15 15"),
            new MotionIconPart("gust-bottom", "M3 16.5H10.5A2 2 0 1 0 8.5 18.7"));

        yield return Icon("umbrella",
            MotionSpecBuilder.Build(1400, m => m
                .Rotate(k => k.Evenly(MotionEase.SinInOut, 0d, -9d, 7d, -3d, 0d))
                .ScaleY("canopy", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0.86d, MotionEase.BackOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("canopy", "M2.5 12A9.5 9.5 0 0 1 21.5 12z").Origin(12f, 12f),
            new MotionIconPart("handle", "M12 12V18.5A2.5 2.5 0 0 1 7 18.5"));

        yield return Icon("thermometer",
            MotionSpecBuilder.Build(1600, m => m
                .ScaleY("mercury", k => k
                    .At(0d, 0.2d, MotionEase.CubicOut)
                    .At(0.5d, 1d, MotionEase.SinInOut)
                    .At(0.75d, 0.88d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .Scale("tube", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.04d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("tube", "M14.5 13.8V5.5A2.5 2.5 0 0 0 9.5 5.5V13.8A5 5 0 1 0 14.5 13.8z").Origin(12f, 14f),
            new MotionIconPart("mercury", "M12 18.5V9").Origin(12f, 18.5f));

        yield return Icon("droplet",
            MotionSpecBuilder.Build(1200, m => m
                .MoveY(null, k => k
                    .At(0d, -7d, MotionEase.QuadIn)
                    .At(0.45d, 0d, MotionEase.BounceOut)
                    .At(1d, 0d))
                .Opacity(k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.22d, 1d)
                    .At(1d, 1d))
                .ScaleY("drop", k => k
                    .At(0d, 1.15d, MotionEase.Linear)
                    .At(0.45d, 1.15d, MotionEase.QuadOut)
                    .At(0.55d, 0.85d, MotionEase.BackOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("drop", "M12 2.7L17.7 8.4A8 8 0 1 1 6.3 8.4z").Origin(12f, 21f));
    }
}
