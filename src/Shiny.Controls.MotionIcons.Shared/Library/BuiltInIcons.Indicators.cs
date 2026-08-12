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
    }
}
