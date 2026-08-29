namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    static IEnumerable<MotionIconDefinition> Indicators()
    {
        yield return Icon("play",
            MotionSpecBuilder.Build(600, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.25d, 0.86d, MotionEase.BackOut)
                    .At(0.8d, 1.04d, MotionEase.QuadOut)
                    .At(1d, 1d))),
            new MotionIconPart("triangle", "M8.5 5.6a1 1 0 0 1 1.5-.86l8.4 5.4a1 1 0 0 1 0 1.72l-8.4 5.4a1 1 0 0 1-1.5-.86z").Origin(12f, 12f));

        yield return Icon("pause",
            MotionSpecBuilder.Build(900, m => m
                .ScaleY("left", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.35d, 0.62d, MotionEase.SinInOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))
                .ScaleY("right", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.15d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 0.62d, MotionEase.SinInOut)
                    .At(0.85d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("left", "M9.5 5v14").Origin(9.5f, 12f),
            new MotionIconPart("right", "M14.5 5v14").Origin(14.5f, 12f));

        // The waves leave the cone in order, which is the only way a static speaker reads as loud.
        yield return Icon("volume",
            MotionSpecBuilder.Build(1200, m => m
                .Opacity("wave-inner", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.15d, 0.25d, MotionEase.QuadIn)
                    .At(0.45d, 1d)
                    .At(1d, 1d))
                .Scale("wave-inner", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.15d, 0.82d, MotionEase.QuadIn)
                    .At(0.45d, 1d)
                    .At(1d, 1d))
                .Opacity("wave-outer", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.12d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0.15d, MotionEase.QuadIn)
                    .At(0.62d, 1d)
                    .At(1d, 1d))
                .Scale("wave-outer", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.12d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0.8d, MotionEase.QuadIn)
                    .At(0.62d, 1d)
                    .At(1d, 1d))
                .Scale("cone", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.15d, 1.06d, MotionEase.SinInOut)
                    .At(0.4d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("cone", "M11 5.2L6.4 9.2H2.8v5.6h3.6l4.6 4z").Origin(7f, 12f),
            new MotionIconPart("wave-inner", "M15.2 9.3a3.8 3.8 0 0 1 0 5.4").Origin(15.2f, 12f),
            new MotionIconPart("wave-outer", "M18 6.4a8 8 0 0 1 0 11.2").Origin(18f, 12f));

        // Signal acquiring: the arcs light from the handset outwards.
        yield return Icon("wifi",
            MotionSpecBuilder.Build(1600, m => m
                .Opacity("dot", k => k
                    .At(0d, 0.15d, MotionEase.QuadOut)
                    .At(0.12d, 1d)
                    .At(1d, 1d))
                .Opacity("arc-inner", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.15d, 0.15d, MotionEase.QuadOut)
                    .At(0.3d, 1d)
                    .At(1d, 1d))
                .Opacity("arc-middle", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.33d, 0.15d, MotionEase.QuadOut)
                    .At(0.48d, 1d)
                    .At(1d, 1d))
                .Opacity("arc-outer", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.51d, 0.15d, MotionEase.QuadOut)
                    .At(0.66d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("arc-outer", "M1.6 8.8a16 16 0 0 1 20.8 0"),
            new MotionIconPart("arc-middle", "M5 12.4a11 11 0 0 1 14 0"),
            new MotionIconPart("arc-inner", "M8.5 16a6 6 0 0 1 7 0"),
            new MotionIconPart("dot", "M13.2 20.4a1.2 1.2 0 1 1-2.4 0 1.2 1.2 0 0 1 2.4 0z").Solid());

        // Grown from the baseline rather than the middle, so the bars rise out of the axis.
        yield return Icon("chart",
            MotionSpecBuilder.Build(1100, m => m
                .ScaleY("bar1", k => k
                    .At(0d, 0.1d, MotionEase.BackOut)
                    .At(0.45d, 1d)
                    .At(1d, 1d))
                .ScaleY("bar2", k => k
                    .At(0d, 0.1d, MotionEase.Linear)
                    .At(0.12d, 0.1d, MotionEase.BackOut)
                    .At(0.57d, 1d)
                    .At(1d, 1d))
                .ScaleY("bar3", k => k
                    .At(0d, 0.1d, MotionEase.Linear)
                    .At(0.24d, 0.1d, MotionEase.BackOut)
                    .At(0.69d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("bar1", "M6 20.5V14").Origin(6f, 20.5f),
            new MotionIconPart("bar2", "M12 20.5V6.5").Origin(12f, 20.5f),
            new MotionIconPart("bar3", "M18 20.5V10.5").Origin(18f, 20.5f));

        // The shaft retracts behind the head as the whole arrow moves off, so it reads as travel
        // rather than as a stretch.
        yield return Icon("arrow-right",
            MotionSpecBuilder.Build(800, m => m
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.CubicIn)
                    .At(0.45d, 4d, MotionEase.CubicOut)
                    .At(1d, 0d))
                .Trim("shaft", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.45d, 0.25d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("shaft", "M3.5 12h14"),
            new MotionIconPart("head", "M13 6.5L18.5 12L13 17.5"));

        yield return Icon("chevron-down",
            MotionSpecBuilder.Build(800, m => m
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, 3.2d, MotionEase.BounceOut)
                    .At(1d, 0d))),
            new MotionIconPart("chevron", "M6 9.5L12 15.5L18 9.5"));

        yield return Icon("thumbs-up",
            MotionSpecBuilder.Build(1000, m => m
                .Rotate("hand", k => k.Evenly(MotionEase.SinInOut, 0d, -16d, 9d, -5d, 2d, 0d))
                .Scale("hand", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.2d, 1.1d, MotionEase.QuadInOut)
                    .At(0.6d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("hand", "M7.5 21.5V10.2l4.6-8.2a2.6 2.6 0 0 1 2.6 2.6v4.6h5.1a2 2 0 0 1 2 2.4l-1.6 7.5a2 2 0 0 1-2 1.6z").Origin(7.5f, 21.5f),
            new MotionIconPart("base", "M7.5 10.2H4a1 1 0 0 0-1 1v9.3a1 1 0 0 0 1 1h3.5z"));

        yield return Icon("warning",
            MotionSpecBuilder.Build(900, m => m
                .MoveX(null, k => k.Evenly(MotionEase.SinInOut, 0d, -1.6d, 1.6d, -1.2d, 1.2d, -0.6d, 0d))
                .Opacity("bang", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.25d, 0.2d, MotionEase.QuadIn)
                    .At(0.5d, 1d)
                    .At(1d, 1d))
                .Opacity("dot", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.25d, 0.2d, MotionEase.QuadIn)
                    .At(0.5d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("triangle", "M10.3 3.9L1.9 18a2 2 0 0 0 1.7 3h16.8a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"),
            new MotionIconPart("bang", "M12 9v4.5"),
            new MotionIconPart("dot", "M13.05 17.45a1.05 1.05 0 1 1-2.1 0 1.05 1.05 0 0 1 2.1 0z").Solid());

        yield return Icon("info",
            MotionSpecBuilder.Build(1000, m => m
                .Scale("circle", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.4d, 1.1d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .MoveY("stem", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1.2d, MotionEase.BounceOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))
                .MoveY("dot", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1.2d, MotionEase.BounceOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("circle", "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z").Origin(12f, 12f),
            new MotionIconPart("stem", "M12 11.5V16.5"),
            new MotionIconPart("dot", "M13.05 8a1.05 1.05 0 1 1-2.1 0 1.05 1.05 0 0 1 2.1 0z").Solid());

        yield return Icon("check-circle",
            MotionSpecBuilder.Build(1000, m => m
                .Trim("ring", k => k
                    .At(0d, 0d, MotionEase.CubicOut)
                    .At(0.5d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("mark", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.4d, 0d, MotionEase.CubicOut)
                    .At(0.75d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.7d, 1d, MotionEase.QuadOut)
                    .At(0.82d, 1.1d, MotionEase.QuadIn)
                    .At(1d, 1d))),
            new MotionIconPart("ring", "M21 12A9 9 0 0 1 3 12A9 9 0 0 1 21 12z"),
            new MotionIconPart("mark", "M8 12.3L11 15.3L16.2 9.3"));

        // The head-shake is deliberately off-centre in time from the strokes drawing, so the icon
        // has finished saying "no" before it has finished drawing itself.
        yield return Icon("x-circle",
            MotionSpecBuilder.Build(1000, m => m
                .Trim("ring", k => k
                    .At(0d, 0d, MotionEase.CubicOut)
                    .At(0.45d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("stroke-a", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.4d, 0d, MotionEase.CubicOut)
                    .At(0.62d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("stroke-b", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.5d, 0d, MotionEase.CubicOut)
                    .At(0.72d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.72d, 0d, MotionEase.SinInOut)
                    .At(0.8d, -1.4d, MotionEase.SinInOut)
                    .At(0.88d, 1.4d, MotionEase.SinInOut)
                    .At(1d, 0d))),
            new MotionIconPart("ring", "M21 12A9 9 0 0 1 3 12A9 9 0 0 1 21 12z"),
            new MotionIconPart("stroke-a", "M9 9L15 15"),
            new MotionIconPart("stroke-b", "M15 9L9 15"));

        yield return Icon("help",
            MotionSpecBuilder.Build(1200, m => m
                .Rotate("hook", k => k.Evenly(MotionEase.SinInOut, 0d, -10d, 8d, -3d, 0d))
                .MoveY("dot", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1.4d, MotionEase.BounceOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))
                .Scale("ring", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.45d, 1.08d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("ring", "M21 12A9 9 0 0 1 3 12A9 9 0 0 1 21 12z").Origin(12f, 12f),
            new MotionIconPart("hook", "M9.2 9.2A2.9 2.9 0 1 1 12 12.9V14.6").Origin(12f, 14.6f),
            new MotionIconPart("dot", "M13.05 17.7A1.05 1.05 0 0 1 10.95 17.7A1.05 1.05 0 0 1 13.05 17.7z").Solid());

        yield return Icon("trending-up",
            MotionSpecBuilder.Build(1200, m => m
                .Trim("line", k => k
                    .At(0d, 0.05d, MotionEase.CubicOut)
                    .At(0.6d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Scale("head", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.6d, 1d, MotionEase.BackOut)
                    .At(0.75d, 1.25d, MotionEase.QuadOut)
                    .At(0.92d, 1d)
                    .At(1d, 1d))
                .Opacity("head", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.55d, 0.15d, MotionEase.QuadOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("line", "M3 17.5L9.5 11L13.5 15L21 7.5"),
            new MotionIconPart("head", "M15.5 7.5H21V13").Origin(19f, 9.5f));

        // A trace, not a shape: the trim sweeping end to end is the entire icon, which is why it is
        // one part and has no other channel on it.
        yield return Icon("activity",
            MotionSpecBuilder.Build(1600, m => m
                .Trim("trace", k => k
                    .At(0d, 0.02d, MotionEase.QuadInOut)
                    .At(0.75d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("trace", "M2.5 12H7L10 5L14 19L17 12H21.5"));

        yield return Icon("signal",
            MotionSpecBuilder.Build(1600, m => m
                .Opacity("bar-1", k => k
                    .At(0d, 0.15d, MotionEase.QuadOut)
                    .At(0.12d, 1d)
                    .At(1d, 1d))
                .Opacity("bar-2", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.15d, 0.15d, MotionEase.QuadOut)
                    .At(0.3d, 1d)
                    .At(1d, 1d))
                .Opacity("bar-3", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.33d, 0.15d, MotionEase.QuadOut)
                    .At(0.48d, 1d)
                    .At(1d, 1d))
                .Opacity("bar-4", k => k
                    .At(0d, 0.15d, MotionEase.Linear)
                    .At(0.51d, 0.15d, MotionEase.QuadOut)
                    .At(0.66d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("bar-1", "M4 20.5V17"),
            new MotionIconPart("bar-2", "M9.3 20.5V13.5"),
            new MotionIconPart("bar-3", "M14.7 20.5V9.5"),
            new MotionIconPart("bar-4", "M20 20.5V5.5"));

        // Pairing rather than merely present: the rune draws itself on, then blinks once the way a
        // radio does when it has actually found something.
        yield return Icon("bluetooth",
            MotionSpecBuilder.Build(1600, m => m
                .Trim("rune", k => k
                    .At(0d, 0.02d, MotionEase.CubicOut)
                    .At(0.55d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Opacity("rune", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.62d, 1d, MotionEase.QuadIn)
                    .At(0.7d, 0.2d, MotionEase.QuadOut)
                    .At(0.78d, 1d, MotionEase.QuadIn)
                    .At(0.86d, 0.2d, MotionEase.QuadOut)
                    .At(0.94d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("rune", "M7 7.5L17 16.5L12 21.5V2.5L17 7.5L7 16.5"));

        // Half a turn puts the frame back on itself, so the glass can keep tipping over forever
        // without the icon ever ending up upside down.
        yield return Icon("hourglass",
            MotionSpecBuilder.Build(2200, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.55d, 0d, MotionEase.BackInOut)
                    .At(1d, 180d))
                .ScaleY("sand", k => k
                    .At(0d, 0.15d, MotionEase.SinInOut)
                    .At(0.5d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("frame", "M6 2.5H18M6 21.5H18"),
            new MotionIconPart("glass", "M7.5 2.5V7L12 12L7.5 17V21.5M16.5 2.5V7L12 12L16.5 17V21.5"),
            new MotionIconPart("sand", "M12 12V19.5").Origin(12f, 19.5f));
    }
}
