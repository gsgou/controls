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

        yield return Icon("save",
            MotionSpecBuilder.Build(800, m => m
                .Scale("body", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.25d, 0.92d, MotionEase.BackOut)
                    .At(0.65d, 1.04d, MotionEase.QuadOut)
                    .At(1d, 1d))
                .MoveY("label", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, 1.4d, MotionEase.BackOut)
                    .At(0.75d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("body", "M5 3.5H16.2L20.5 7.8V19A1.5 1.5 0 0 1 19 20.5H5A1.5 1.5 0 0 1 3.5 19V5A1.5 1.5 0 0 1 5 3.5z").Origin(12f, 12f),
            new MotionIconPart("shutter", "M8 3.5V8H15"),
            new MotionIconPart("label", "M8 20.5V13.5H16V20.5"));

        // The head backs up along the arc it is about to be dragged around, so the pair reads as one
        // gesture rather than an arrow and a curve that happen to be animating at the same time.
        yield return Icon("undo",
            MotionSpecBuilder.Build(1000, m => m
                .Trim("arc", k => k
                    .At(0d, 0.1d, MotionEase.CubicOut)
                    .At(0.6d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveX("head", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -2.2d, MotionEase.BackOut)
                    .At(0.75d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("arc", "M4 9.5H14.5A5.5 5.5 0 1 1 14.5 20.5H8"),
            new MotionIconPart("head", "M8.5 5L4 9.5L8.5 14"));

        yield return Icon("redo",
            MotionSpecBuilder.Build(1000, m => m
                .Trim("arc", k => k
                    .At(0d, 0.1d, MotionEase.CubicOut)
                    .At(0.6d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveX("head", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, 2.2d, MotionEase.BackOut)
                    .At(0.75d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("arc", "M20 9.5H9.5A5.5 5.5 0 1 0 9.5 20.5H16"),
            new MotionIconPart("head", "M15.5 5L20 9.5L15.5 14"));

        // Flies off the top right and comes back in from the bottom left while invisible — the same
        // teleport the download arrow uses, on a diagonal.
        yield return Icon("send",
            MotionSpecBuilder.Build(1100, m => m
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.42d, 6d, MotionEase.StepEnd)
                    .At(0.46d, -6d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.42d, -6d, MotionEase.StepEnd)
                    .At(0.46d, 6d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .Opacity(k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.42d, 0d, MotionEase.StepEnd)
                    .At(0.46d, 0d, MotionEase.QuadOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("body", "M21.5 2.5L14.8 21.5L11 13L2.5 9.2z"),
            new MotionIconPart("crease", "M21.5 2.5L11 13"));

        // The two halves pull apart and the bar between them wipes out, which is the one motion that
        // says "link" rather than "two brackets".
        yield return Icon("link",
            MotionSpecBuilder.Build(1000, m => m
                .MoveX("left", k => k
                    .At(0d, 0d, MotionEase.SinInOut)
                    .At(0.35d, -1.8d, MotionEase.BackOut)
                    .At(1d, 0d))
                .MoveX("right", k => k
                    .At(0d, 0d, MotionEase.SinInOut)
                    .At(0.35d, 1.8d, MotionEase.BackOut)
                    .At(1d, 0d))
                .Trim("bar", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.35d, 0.15d, MotionEase.QuadOut)
                    .At(0.85d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("left", "M10 7.5H7.5A4.5 4.5 0 0 0 7.5 16.5H10"),
            new MotionIconPart("right", "M14 7.5H16.5A4.5 4.5 0 0 1 16.5 16.5H14"),
            new MotionIconPart("bar", "M8 12H16"));

        yield return Icon("attach",
            MotionSpecBuilder.Build(1200, m => m
                .Rotate(k => k.Evenly(MotionEase.SinInOut, 0d, -13d, 9d, -4d, 0d))
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1.2d, MotionEase.QuadIn)
                    .At(0.75d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("clip", "M20.4 11.5L12.2 19.7A5.7 5.7 0 0 1 4.4 11.9L12.6 3.7A3.8 3.8 0 0 1 17.8 8.9L9.6 17.1A1.9 1.9 0 0 1 7 14.5L14.6 7").Origin(12f, 3.7f));

        yield return Icon("pin",
            MotionSpecBuilder.Build(1000, m => m
                .Rotate("head", k => k.Evenly(MotionEase.SinInOut, 0d, -13d, 9d, -4d, 0d))
                .Trim("needle", k => k
                    .At(0d, 0.2d, MotionEase.CubicOut)
                    .At(0.55d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("head", "M9 3.5H15V9L17.5 13.5H6.5L9 9z").Origin(12f, 13.5f),
            new MotionIconPart("needle", "M12 13.5V21"));

        // The sheet grows out of the machine rather than sliding, because a printed page appears
        // edge-first and a translate would show its whole outline moving down behind the body.
        yield return Icon("print",
            MotionSpecBuilder.Build(1200, m => m
                .ScaleY("sheet-out", k => k
                    .At(0d, 0.08d, MotionEase.CubicOut)
                    .At(0.55d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveY("sheet-in", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.3d, 1.4d, MotionEase.QuadOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("sheet-in", "M7 8.5V3.5H17V8.5"),
            new MotionIconPart("sheet-out", "M7 17.5H17V21.5H7z").Origin(12f, 17.5f),
            new MotionIconPart("machine", "M6 8.5H18A2.5 2.5 0 0 1 20.5 11V16A1.5 1.5 0 0 1 19 17.5H5A1.5 1.5 0 0 1 3.5 16V11A2.5 2.5 0 0 1 6 8.5z"));

        yield return Icon("zoom-in",
            MotionSpecBuilder.Build(1000, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.4d, 1.12d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .Scale("plus", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 1.25d, MotionEase.QuadIn)
                    .At(0.65d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("lens", "M18 11A7 7 0 0 1 4 11A7 7 0 0 1 18 11z"),
            new MotionIconPart("handle", "M16.2 16.2L21 21"),
            new MotionIconPart("plus", "M11 8V14M8 11H14").Origin(11f, 11f));

        yield return Icon("zoom-out",
            MotionSpecBuilder.Build(1000, m => m
                .Scale(k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.4d, 0.9d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .ScaleX("minus", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 1.3d, MotionEase.QuadIn)
                    .At(0.65d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("lens", "M18 11A7 7 0 0 1 4 11A7 7 0 0 1 18 11z"),
            new MotionIconPart("handle", "M16.2 16.2L21 21"),
            new MotionIconPart("minus", "M8 11H14").Origin(11f, 11f));

        // The rows redraw longest-first, which is the only thing about a static three-line glyph
        // that can suggest it has just been reordered.
        yield return Icon("sort",
            MotionSpecBuilder.Build(1200, m => m
                .Trim("row-top", k => k
                    .At(0d, 0.05d, MotionEase.CubicOut)
                    .At(0.4d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("row-middle", k => k
                    .At(0d, 0.05d, MotionEase.Linear)
                    .At(0.12d, 0.05d, MotionEase.CubicOut)
                    .At(0.52d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("row-bottom", k => k
                    .At(0d, 0.05d, MotionEase.Linear)
                    .At(0.24d, 0.05d, MotionEase.CubicOut)
                    .At(0.64d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveY("arrow", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, 1.6d, MotionEase.BounceOut)
                    .At(1d, 0d))),
            new MotionIconPart("row-top", "M3.5 6.5H15.5"),
            new MotionIconPart("row-middle", "M3.5 12H11.5"),
            new MotionIconPart("row-bottom", "M3.5 17.5H7.5"),
            new MotionIconPart("arrow", "M19 4.5V19.5M15.8 16.3L19 19.5L22.2 16.3"));

        yield return Icon("more",
            MotionSpecBuilder.Build(1200, m => m
                .MoveY("dot-left", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.18d, -2.6d, MotionEase.QuadIn)
                    .At(0.38d, 0d)
                    .At(1d, 0d))
                .MoveY("dot-middle", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.12d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -2.6d, MotionEase.QuadIn)
                    .At(0.5d, 0d)
                    .At(1d, 0d))
                .MoveY("dot-right", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.24d, 0d, MotionEase.QuadOut)
                    .At(0.42d, -2.6d, MotionEase.QuadIn)
                    .At(0.62d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("dot-left", "M7 12A1.6 1.6 0 0 1 3.8 12A1.6 1.6 0 0 1 7 12z").Solid(),
            new MotionIconPart("dot-middle", "M13.6 12A1.6 1.6 0 0 1 10.4 12A1.6 1.6 0 0 1 13.6 12z").Solid(),
            new MotionIconPart("dot-right", "M20.2 12A1.6 1.6 0 0 1 17 12A1.6 1.6 0 0 1 20.2 12z").Solid());

        yield return Icon("logout",
            MotionSpecBuilder.Build(1100, m => m
                .MoveX("head", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.42d, 3.5d, MotionEase.StepEnd)
                    .At(0.46d, -3.5d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .Opacity("head", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.42d, 0d, MotionEase.StepEnd)
                    .At(0.46d, 0d, MotionEase.QuadOut)
                    .At(0.75d, 1d)
                    .At(1d, 1d))
                .Trim("shaft", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.42d, 0.2d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("door", "M14 3.5H6A1.5 1.5 0 0 0 4.5 5V19A1.5 1.5 0 0 0 6 20.5H14"),
            new MotionIconPart("shaft", "M9.5 12H20.5"),
            new MotionIconPart("head", "M16.5 8L20.5 12L16.5 16"));

        // A plus is symmetric every quarter turn, so the badge can spin as it pops and still land
        // exactly on the artwork as drawn.
        yield return Icon("add-user",
            MotionSpecBuilder.Build(1000, m => m
                .Rotate("plus", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(1d, 90d))
                .Scale("plus", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.35d, 1.28d, MotionEase.QuadIn)
                    .At(0.7d, 1d)
                    .At(1d, 1d))
                .MoveY("head", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1d, MotionEase.QuadIn)
                    .At(0.7d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("head", "M13.5 8A4 4 0 0 1 5.5 8A4 4 0 0 1 13.5 8z"),
            new MotionIconPart("body", "M2 20.5V19.5A5.5 5.5 0 0 1 7.5 14H11.5A5.5 5.5 0 0 1 17 19.5V20.5"),
            new MotionIconPart("plus", "M20 7V13M17 10H23").Origin(20f, 10f));
    }
}
