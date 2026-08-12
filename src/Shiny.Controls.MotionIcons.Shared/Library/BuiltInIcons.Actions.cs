namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    static IEnumerable<MotionIconDefinition> Actions()
    {
        // The tick draws itself on, with a small settle at the end so it lands rather than stops.
        yield return Icon("check",
            MotionSpecBuilder.Build(650, m => m
                .Trim("mark", k => k
                    .At(0d, 0d, MotionEase.CubicOut)
                    .At(0.55d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.1d, 0.92d, MotionEase.BackOut)
                    .At(0.6d, 1.06d, MotionEase.QuadOut)
                    .At(0.85d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("mark", "M4.5 12.8L9.7 18L19.5 6"));

        // A half turn lands the cross back on itself, so the strokes can wipe off and back on
        // underneath it without the icon ever appearing to end up crooked.
        yield return Icon("close",
            MotionSpecBuilder.Build(600, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(1d, 180d))
                .Trim("a", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.3d, 0.1d, MotionEase.QuadOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))
                .Trim("b", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.4d, 0.1d, MotionEase.QuadOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("a", "M6 6L18 18"),
            new MotionIconPart("b", "M18 6L6 18"));

        // A plus is symmetric every 90 degrees, so a quarter turn is free to overshoot and settle.
        yield return Icon("plus",
            MotionSpecBuilder.Build(600, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(1d, 90d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.35d, 1.12d, MotionEase.QuadIn)
                    .At(1d, 1d))),
            new MotionIconPart("v", "M12 5v14"),
            new MotionIconPart("h", "M5 12h14"));

        // The classic hamburger-to-cross morph, and the reason parts exist at all. Each bar spins
        // about its own centre and *then* slides to the middle, which is the order the transform is
        // applied in on both hosts — rotating about the icon's centre instead would swing the bars
        // round the outside rather than crossing them.
        yield return Icon("menu",
            MotionSpecBuilder.Build(900, m => m
                .Rotate("top", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.45d, 45d, MotionEase.Linear)
                    .At(0.6d, 45d, MotionEase.BackOut)
                    .At(1d, 0d))
                .MoveY("top", k => k
                    .At(0d, 0d, MotionEase.QuadInOut)
                    .At(0.45d, 6d, MotionEase.Linear)
                    .At(0.6d, 6d, MotionEase.QuadInOut)
                    .At(1d, 0d))
                .Rotate("bottom", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.45d, -45d, MotionEase.Linear)
                    .At(0.6d, -45d, MotionEase.BackOut)
                    .At(1d, 0d))
                .MoveY("bottom", k => k
                    .At(0d, 0d, MotionEase.QuadInOut)
                    .At(0.45d, -6d, MotionEase.Linear)
                    .At(0.6d, -6d, MotionEase.QuadInOut)
                    .At(1d, 0d))
                .Opacity("middle", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0d, MotionEase.Linear)
                    .At(0.7d, 0d, MotionEase.QuadIn)
                    .At(1d, 1d))
                .ScaleX("middle", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0.2d, MotionEase.Linear)
                    .At(0.7d, 0.2d, MotionEase.QuadIn)
                    .At(1d, 1d))),
            new MotionIconPart("top", "M3.5 6h17").Origin(12f, 6f),
            new MotionIconPart("middle", "M3.5 12h17").Origin(12f, 12f),
            new MotionIconPart("bottom", "M3.5 18h17").Origin(12f, 18f));

        // Sweeps the glass across the surface it is searching rather than just wobbling it.
        yield return Icon("search",
            MotionSpecBuilder.Build(1100, m => m
                .MoveX(null, k => k.Evenly(MotionEase.SinInOut, 0d, -1.8d, 1.8d, 0.9d, 0d))
                .MoveY(null, k => k.Evenly(MotionEase.SinInOut, 0d, 1.2d, -1.2d, 0.6d, 0d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.08d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("lens", "M18 11a7 7 0 1 1-14 0 7 7 0 0 1 14 0"),
            new MotionIconPart("handle", "M16.2 16.2L21 21"));

        // A twist rather than a full turn: the cog is not rotationally symmetric, so anything other
        // than a whole revolution has to come back to where it started.
        yield return Icon("settings",
            MotionSpecBuilder.Build(900, m => m
                .Rotate("cog", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.45d, 40d, MotionEase.BackInOut)
                    .At(1d, 0d))
                .Scale("core", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.45d, 1.15d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("cog", "M19.5 12a7.5 7.5 0 1 1-15 0 7.5 7.5 0 0 1 15 0zM12 4.5V2.4M12 19.5V21.6M19.5 12H21.6M4.5 12H2.4M17.3 6.7L18.8 5.2M6.7 17.3L5.2 18.8M17.3 17.3L18.8 18.8M6.7 6.7L5.2 5.2").Origin(12f, 12f),
            new MotionIconPart("core", "M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0z").Origin(12f, 12f));

        yield return Icon("refresh",
            MotionSpecBuilder.Build(1000, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.CubicInOut)
                    .At(1d, 360d))),
            new MotionIconPart("arcs", "M3.5 9a9 9 0 0 1 14.9-3.4L23 10M1 14l4.6 4.4A9 9 0 0 0 20.5 15").Origin(12f, 12f),
            new MotionIconPart("heads", "M23 4v6h-6M1 20v-6h6").Origin(12f, 12f));

        // Turn plus trim together: the arc appears to chase its own tail, which is what makes an
        // indeterminate spinner read as busy rather than merely rotating. The trim opens back out to
        // the full three-quarter ring at both ends of the cycle, so a spinner that is switched off
        // settles into the arc as drawn instead of snapping wider the instant it stops.
        yield return Icon("loader",
            MotionSpecBuilder.Build(1400, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(1d, 360d))
                .Trim("ring", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 0.35d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("ring", "M12 3a9 9 0 1 0 9 9").Origin(12f, 12f));

        yield return Icon("trash",
            MotionSpecBuilder.Build(900, m => m
                .Rotate("lid", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.35d, -22d, MotionEase.SinInOut)
                    .At(0.65d, -22d, MotionEase.BackInOut)
                    .At(1d, 0d))
                .MoveY("can", k => k.Evenly(MotionEase.SinInOut, 0d, 0.6d, -0.3d, 0.2d, 0d))),
            // Hinged at the left end of the crossbar, so the lid swings up and off rather than
            // rotating about the middle of the bin.
            new MotionIconPart("lid", "M3 6h18M8 6V4.5A1.5 1.5 0 0 1 9.5 3h5A1.5 1.5 0 0 1 16 4.5V6").Origin(3.5f, 6f),
            new MotionIconPart("can", "M18.5 6.5L17.6 20a2 2 0 0 1-2 1.9H8.4a2 2 0 0 1-2-1.9L5.5 6.5"));

        // The arrow falls out of the tray, then jumps back above it while invisible — the step-end
        // easing is what makes the teleport instant instead of a visible drift back up.
        yield return Icon("download",
            MotionSpecBuilder.Build(1000, m => m
                .MoveY("arrow", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.4d, 5d, MotionEase.StepEnd)
                    .At(0.45d, -6d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .Opacity("arrow", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.4d, 0d, MotionEase.StepEnd)
                    .At(0.45d, 0d, MotionEase.QuadOut)
                    .At(0.75d, 1d)
                    .At(1d, 1d))
                .Trim("stem", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.4d, 0.15d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))
                .ScaleY("tray", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.45d, 0.82d, MotionEase.BackOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("tray", "M21 15v3.5a2.5 2.5 0 0 1-2.5 2.5h-13A2.5 2.5 0 0 1 3 18.5V15").Origin(12f, 21f),
            new MotionIconPart("stem", "M12 3v11.5"),
            new MotionIconPart("arrow", "M7.5 10.5L12 15l4.5-4.5"));

        yield return Icon("upload",
            MotionSpecBuilder.Build(1000, m => m
                .MoveY("arrow", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.4d, -5d, MotionEase.StepEnd)
                    .At(0.45d, 6d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .Opacity("arrow", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.4d, 0d, MotionEase.StepEnd)
                    .At(0.45d, 0d, MotionEase.QuadOut)
                    .At(0.75d, 1d)
                    .At(1d, 1d))
                .Trim("stem", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.4d, 0.15d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("tray", "M21 15v3.5a2.5 2.5 0 0 1-2.5 2.5h-13A2.5 2.5 0 0 1 3 18.5V15"),
            new MotionIconPart("stem", "M12 15V3.5"),
            new MotionIconPart("arrow", "M7.5 8L12 3.5L16.5 8"));

        yield return Icon("edit",
            MotionSpecBuilder.Build(1100, m => m
                .MoveX("pen", k => k.Evenly(MotionEase.SinInOut, 0d, -1.6d, 1.2d, -0.6d, 0d))
                .MoveY("pen", k => k.Evenly(MotionEase.SinInOut, 0d, 1.6d, -1.2d, 0.6d, 0d))
                .Trim("line", k => k
                    .At(0d, 0.15d, MotionEase.CubicOut)
                    .At(0.7d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("pen", "M17 3a2.83 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5z"),
            new MotionIconPart("line", "M13.5 6.5L17.5 10.5"));

        // Settles like something poured through it.
        yield return Icon("filter",
            MotionSpecBuilder.Build(800, m => m
                .ScaleY("funnel", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 0.78d, MotionEase.BackOut)
                    .At(0.7d, 1.06d, MotionEase.QuadInOut)
                    .At(1d, 1d))),
            new MotionIconPart("funnel", "M3 5.5h18l-7 8.2v5.6l-4 2.2v-7.8z").Origin(12f, 5.5f));

        // The links draw between the nodes, and each node pops as the link reaches it.
        yield return Icon("share",
            MotionSpecBuilder.Build(1200, m => m
                .Scale("source", k => k
                    .At(0d, 1d, MotionEase.BackOut)
                    .At(0.12d, 1.3d, MotionEase.QuadOut)
                    .At(0.3d, 1d)
                    .At(1d, 1d))
                .Trim("links", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.15d, 0d, MotionEase.CubicOut)
                    .At(0.6d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Scale("out-top", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.5d, 1d, MotionEase.BackOut)
                    .At(0.65d, 1.3d, MotionEase.QuadOut)
                    .At(0.82d, 1d)
                    .At(1d, 1d))
                .Scale("out-bottom", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.58d, 1d, MotionEase.BackOut)
                    .At(0.73d, 1.3d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("links", "M8.6 10.6L15.4 6.4M8.6 13.4l6.8 4.2"),
            new MotionIconPart("source", "M9 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0z").Origin(6f, 12f),
            new MotionIconPart("out-top", "M21 5a3 3 0 1 1-6 0 3 3 0 0 1 6 0z").Origin(18f, 5f),
            new MotionIconPart("out-bottom", "M21 19a3 3 0 1 1-6 0 3 3 0 0 1 6 0z").Origin(18f, 19f));

        yield return Icon("copy",
            MotionSpecBuilder.Build(900, m => m
                .MoveX("front", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.4d, 2.2d, MotionEase.BackInOut)
                    .At(1d, 0d))
                .MoveY("front", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.4d, 2.2d, MotionEase.BackInOut)
                    .At(1d, 0d))),
            new MotionIconPart("back", "M5.5 16H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v.5"),
            new MotionIconPart("front", "M10 8h9a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-9a2 2 0 0 1-2-2v-9a2 2 0 0 1 2-2z"));

        yield return Icon("power",
            MotionSpecBuilder.Build(1000, m => m
                .Trim("stem", k => k
                    .At(0d, 0d, MotionEase.CubicOut)
                    .At(0.3d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("arc", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.3d, 0d, MotionEase.CubicOut)
                    .At(0.8d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.8d, 1d, MotionEase.QuadOut)
                    .At(0.88d, 1.1d, MotionEase.QuadIn)
                    .At(1d, 1d))),
            new MotionIconPart("arc", "M6.3 6.3a8 8 0 1 0 11.4 0"),
            new MotionIconPart("stem", "M12 2.5v9.5"));
    }
}
