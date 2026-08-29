namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    /// <summary>
    /// Transport controls and the hardware that plays the sound.
    /// </summary>
    /// <remarks>
    /// A transport bar is the one place a whole row of these icons sits together, so the motion is
    /// kept short and low-amplitude on purpose: <c>play</c>, <c>pause</c> and <c>stop</c> all settle
    /// inside their own box rather than growing into their neighbours.
    /// </remarks>
    static IEnumerable<MotionIconDefinition> Media()
    {
        yield return Icon("stop",
            MotionSpecBuilder.Build(700, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.25d, 0.84d, MotionEase.BackOut)
                    .At(0.75d, 1.04d, MotionEase.QuadOut)
                    .At(1d, 1d))),
            new MotionIconPart("square", "M8.5 7.5H15.5A1 1 0 0 1 16.5 8.5V15.5A1 1 0 0 1 15.5 16.5H8.5A1 1 0 0 1 7.5 15.5V8.5A1 1 0 0 1 8.5 7.5z").Origin(12f, 12f));

        // The wedge runs into the end stop and the bar takes the hit — the squash is what sells it
        // as the track ending rather than the triangle simply sliding right.
        yield return Icon("skip-forward",
            MotionSpecBuilder.Build(900, m => m
                .MoveX("wedge", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.4d, 2.4d, MotionEase.BackOut)
                    .At(1d, 0d))
                .MoveX("bar", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.4d, 0d, MotionEase.QuadOut)
                    .At(0.5d, 1.2d, MotionEase.BackOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))
                .ScaleY("bar", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.4d, 1d, MotionEase.QuadOut)
                    .At(0.5d, 0.8d, MotionEase.BackOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("wedge", "M6 5.5L15 12L6 18.5z"),
            new MotionIconPart("bar", "M18 5.5V18.5").Origin(18f, 12f));

        yield return Icon("skip-back",
            MotionSpecBuilder.Build(900, m => m
                .MoveX("wedge", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.4d, -2.4d, MotionEase.BackOut)
                    .At(1d, 0d))
                .MoveX("bar", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.4d, 0d, MotionEase.QuadOut)
                    .At(0.5d, -1.2d, MotionEase.BackOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))
                .ScaleY("bar", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.4d, 1d, MotionEase.QuadOut)
                    .At(0.5d, 0.8d, MotionEase.BackOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("wedge", "M18 5.5L9 12L18 18.5z"),
            new MotionIconPart("bar", "M6 5.5V18.5").Origin(6f, 12f));

        // Both routes draw at once but from opposite ends, so the crossing point appears last —
        // which is the bit of the glyph that actually means "shuffled".
        yield return Icon("shuffle",
            MotionSpecBuilder.Build(1300, m => m
                .Trim("upper", k => k
                    .At(0d, 0.12d, MotionEase.CubicOut)
                    .At(0.55d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("lower", k => k
                    .At(0d, 0.12d, MotionEase.Linear)
                    .At(0.12d, 0.12d, MotionEase.CubicOut)
                    .At(0.67d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveX("heads", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.6d, 0d, MotionEase.BackOut)
                    .At(0.75d, 1.4d, MotionEase.QuadOut)
                    .At(0.92d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("upper", "M3.5 7H7.5L16.5 17H20.5"),
            new MotionIconPart("lower", "M3.5 17H7.5L16.5 7H20.5"),
            new MotionIconPart("heads", "M17.5 4L20.5 7L17.5 10M17.5 14L20.5 17L17.5 20"));

        yield return Icon("repeat",
            MotionSpecBuilder.Build(1300, m => m
                .Trim("loop", k => k
                    .At(0d, 0.08d, MotionEase.CubicOut)
                    .At(0.6d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Opacity("heads", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.45d, 0.15d, MotionEase.QuadOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))
                .Scale("heads", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.6d, 1d, MotionEase.BackOut)
                    .At(0.75d, 1.2d, MotionEase.QuadOut)
                    .At(0.92d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("loop", "M3.5 12V9A3.5 3.5 0 0 1 7 5.5H17.5M20.5 12V15A3.5 3.5 0 0 1 17 18.5H6.5"),
            new MotionIconPart("heads", "M14.5 2.5L17.5 5.5L14.5 8.5M9.5 15.5L6.5 18.5L9.5 21.5").Origin(12f, 12f));

        yield return Icon("record",
            MotionSpecBuilder.Build(1200, m => m
                .Scale("dot", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.25d, 1.22d, MotionEase.QuadIn)
                    .At(0.55d, 1d)
                    .At(1d, 1d))
                .Opacity("ring", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.35d, 0.3d, MotionEase.QuadIn)
                    .At(0.75d, 1d)
                    .At(1d, 1d))
                .Scale("ring", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.35d, 1.06d, MotionEase.QuadIn)
                    .At(0.75d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("ring", "M20 12A8 8 0 0 1 4 12A8 8 0 0 1 20 12z").Origin(12f, 12f),
            new MotionIconPart("dot", "M15 12A3 3 0 0 1 9 12A3 3 0 0 1 15 12z").Solid().Origin(12f, 12f));

        // The pickup arc draws itself on, which reads as the microphone listening rather than
        // merely existing.
        yield return Icon("microphone",
            MotionSpecBuilder.Build(1400, m => m
                .Scale("capsule", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.45d, 1.08d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .Trim("arc", k => k
                    .At(0d, 0.1d, MotionEase.CubicOut)
                    .At(0.55d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("capsule", "M12 2.5A3 3 0 0 1 15 5.5V11.5A3 3 0 0 1 9 11.5V5.5A3 3 0 0 1 12 2.5z").Origin(12f, 8.5f),
            new MotionIconPart("arc", "M18.5 11A6.5 6.5 0 0 1 5.5 11"),
            new MotionIconPart("stem", "M12 17.5V21.5"));

        yield return Icon("headphones",
            MotionSpecBuilder.Build(1200, m => m
                .Scale("left", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.2d, 1.14d, MotionEase.QuadIn)
                    .At(0.45d, 1d)
                    .At(1d, 1d))
                .Scale("right", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.25d, 1d, MotionEase.QuadOut)
                    .At(0.45d, 1.14d, MotionEase.QuadIn)
                    .At(0.7d, 1d)
                    .At(1d, 1d))
                .ScaleY("band", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.06d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("band", "M4 14.5V12A8 8 0 0 1 20 12V14.5").Origin(12f, 14.5f),
            new MotionIconPart("left", "M4 14H6A1.5 1.5 0 0 1 7.5 15.5V18.5A1.5 1.5 0 0 1 6 20H4z").Origin(5.75f, 17f),
            new MotionIconPart("right", "M20 14H18A1.5 1.5 0 0 0 16.5 15.5V18.5A1.5 1.5 0 0 0 18 20H20z").Origin(18.25f, 17f));

        yield return Icon("video",
            MotionSpecBuilder.Build(1200, m => m
                .Rotate("lens", k => k.Evenly(MotionEase.SinInOut, 0d, -10d, 8d, -4d, 0d))
                .ScaleY("body", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.05d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("body", "M4 6.5H13A1.5 1.5 0 0 1 14.5 8V16A1.5 1.5 0 0 1 13 17.5H4A1.5 1.5 0 0 1 2.5 16V8A1.5 1.5 0 0 1 4 6.5z").Origin(8.5f, 12f),
            new MotionIconPart("lens", "M14.5 10.5L21.5 7V17L14.5 13.5z").Origin(14.5f, 12f));

        yield return Icon("music",
            MotionSpecBuilder.Build(1200, m => m
                .MoveY(null, k => k.Evenly(MotionEase.SinInOut, 0d, -1.2d, 0d, 1d, 0d))
                .Scale("head-left", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.22d, 1.18d, MotionEase.QuadIn)
                    .At(0.5d, 1d)
                    .At(1d, 1d))
                .Scale("head-right", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.28d, 1d, MotionEase.QuadOut)
                    .At(0.5d, 1.18d, MotionEase.QuadIn)
                    .At(0.78d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("beam", "M9 18V5L20 3V16"),
            new MotionIconPart("head-left", "M9 18A2.5 2.5 0 0 1 4 18A2.5 2.5 0 0 1 9 18z").Origin(6.5f, 18f),
            new MotionIconPart("head-right", "M20 16A2.5 2.5 0 0 1 15 16A2.5 2.5 0 0 1 20 16z").Origin(17.5f, 16f));

        yield return Icon("mute",
            MotionSpecBuilder.Build(900, m => m
                .Trim("cross", k => k
                    .At(0d, 0.05d, MotionEase.CubicOut)
                    .At(0.45d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Scale("cone", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0.88d, MotionEase.BackOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("cone", "M11 5.2L6.4 9.2H2.8V14.8H6.4L11 18.8z").Origin(7f, 12f),
            new MotionIconPart("cross", "M15.5 9.5L21 15M21 9.5L15.5 15"));
    }
}
