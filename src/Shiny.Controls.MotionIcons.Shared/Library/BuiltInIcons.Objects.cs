namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    static IEnumerable<MotionIconDefinition> Objects()
    {
        // Swung from the crown, with the clapper running a wider arc than the body so it catches up
        // late — the small phase difference is the whole difference between a ring and a wobble.
        yield return Icon("bell",
            MotionSpecBuilder.Build(900, m => m
                .Rotate("body", k => k.Evenly(MotionEase.SinInOut, 0d, 14d, -11d, 8d, -5d, 2d, 0d))
                .Rotate("clapper", k => k.Evenly(MotionEase.SinInOut, 0d, 19d, -15d, 11d, -7d, 3d, 0d))),
            new MotionIconPart("body", "M18 8.5a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9").Origin(12f, 3.5f),
            new MotionIconPart("clapper", "M13.7 21a2 2 0 0 1-3.4 0").Origin(12f, 3.5f));

        // A double thump on the rhythm of a real heartbeat, pivoted on the body of the shape rather
        // than the middle of the box so it swells outwards from its own mass.
        yield return Icon("heart",
            MotionSpecBuilder.Build(900, m => m
                .Scale("heart", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.14d, 1.22d, MotionEase.QuadIn)
                    .At(0.28d, 1d, MotionEase.QuadOut)
                    .At(0.42d, 1.14d, MotionEase.QuadIn)
                    .At(0.56d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("heart", "M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8l1.1 1.1L12 21.2l7.8-7.8 1-1.1a5.5 5.5 0 0 0 0-7.7z").Origin(12f, 12.5f));

        yield return Icon("star",
            MotionSpecBuilder.Build(900, m => m
                .Rotate(k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.35d, -16d, MotionEase.BackInOut)
                    .At(0.7d, 9d, MotionEase.BackOut)
                    .At(1d, 0d))
                .Scale(k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.15d, 0.88d, MotionEase.BackOut)
                    .At(0.55d, 1.14d, MotionEase.QuadInOut)
                    .At(1d, 1d))),
            new MotionIconPart("star", "M12 2.6L15 8.7l6.7 1-4.9 4.7 1.2 6.7-6-3.2-6 3.2 1.2-6.7L2.3 9.7l6.7-1z").Origin(12f, 12f));

        yield return Icon("bookmark",
            MotionSpecBuilder.Build(800, m => m
                .MoveY("mark", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -3.2d, MotionEase.BounceOut)
                    .At(1d, 0d))
                .ScaleY("mark", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.3d, 1.1d, MotionEase.QuadIn)
                    .At(0.6d, 0.94d, MotionEase.BackOut)
                    .At(1d, 1d))),
            new MotionIconPart("mark", "M19 21l-7-4.6L5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z").Origin(12f, 3f));

        // The flap folds up about its own hinge — a negative vertical scale is a fold, not a flip,
        // once the origin sits on the crease.
        yield return Icon("mail",
            MotionSpecBuilder.Build(1000, m => m
                .ScaleY("flap", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.4d, -0.75d, MotionEase.Linear)
                    .At(0.6d, -0.75d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .MoveY("envelope", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.4d, 0.8d, MotionEase.QuadInOut)
                    .At(1d, 0d))),
            new MotionIconPart("envelope", "M4 5.5h16A1.5 1.5 0 0 1 21.5 7v10a1.5 1.5 0 0 1-1.5 1.5H4A1.5 1.5 0 0 1 2.5 17V7A1.5 1.5 0 0 1 4 5.5z"),
            new MotionIconPart("flap", "M2.9 6.4L12 12.9l9.1-6.5").Origin(12f, 6.4f));

        yield return Icon("message",
            MotionSpecBuilder.Build(1300, m => m
                .MoveY("dot1", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.16d, -2.2d, MotionEase.QuadIn)
                    .At(0.34d, 0d)
                    .At(1d, 0d))
                .MoveY("dot2", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.12d, 0d, MotionEase.QuadOut)
                    .At(0.28d, -2.2d, MotionEase.QuadIn)
                    .At(0.46d, 0d)
                    .At(1d, 0d))
                .MoveY("dot3", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.24d, 0d, MotionEase.QuadOut)
                    .At(0.4d, -2.2d, MotionEase.QuadIn)
                    .At(0.58d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("bubble", "M20.5 11.6a8 8 0 0 1-8.6 8 8.5 8.5 0 0 1-3.8-.9L3.5 20.5l1.8-4.6a8.5 8.5 0 0 1-.9-3.8 8 8 0 0 1 8-8.6 8 8 0 0 1 8.1 8.1z"),
            new MotionIconPart("dot1", "M8.5 10.4a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2z").Solid(),
            new MotionIconPart("dot2", "M12 10.4a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2z").Solid(),
            new MotionIconPart("dot3", "M15.5 10.4a1.1 1.1 0 1 0 0 2.2 1.1 1.1 0 0 0 0-2.2z").Solid());

        yield return Icon("user",
            MotionSpecBuilder.Build(900, m => m
                .Rotate("head", k => k.Evenly(MotionEase.SinInOut, 0d, -9d, 8d, -4d, 0d))
                .MoveY("head", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1.2d, MotionEase.QuadIn)
                    .At(0.7d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("head", "M16.2 7.2a4.2 4.2 0 1 1-8.4 0 4.2 4.2 0 0 1 8.4 0z").Origin(12f, 11.4f),
            new MotionIconPart("body", "M4 21v-1.2A5.8 5.8 0 0 1 9.8 14h4.4a5.8 5.8 0 0 1 5.8 5.8V21"));

        yield return Icon("home",
            MotionSpecBuilder.Build(1000, m => m
                .MoveY("roof", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.25d, -2.4d, MotionEase.BounceOut)
                    .At(0.7d, 0d)
                    .At(1d, 0d))
                .Trim("door", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.3d, 0.05d, MotionEase.CubicOut)
                    .At(0.85d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("roof", "M2.8 10.6L12 3l9.2 7.6"),
            new MotionIconPart("walls", "M5.5 9.6V20a1 1 0 0 0 1 1h11a1 1 0 0 0 1-1V9.6"),
            new MotionIconPart("door", "M9.5 21v-6h5v6"));

        yield return Icon("calendar",
            MotionSpecBuilder.Build(900, m => m
                .Rotate("hangers", k => k.Evenly(MotionEase.SinInOut, 0d, -10d, 8d, -4d, 0d))
                .ScaleY("body", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.35d, 0.9d, MotionEase.BackOut)
                    .At(0.75d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("body", "M5 5.5h14A1.5 1.5 0 0 1 20.5 7v13a1.5 1.5 0 0 1-1.5 1.5H5A1.5 1.5 0 0 1 3.5 20V7A1.5 1.5 0 0 1 5 5.5z").Origin(12f, 21.5f),
            new MotionIconPart("rule", "M3.5 10.5h17"),
            new MotionIconPart("hangers", "M8 3v5M16 3v5").Origin(12f, 8f));

        yield return Icon("lock",
            MotionSpecBuilder.Build(900, m => m
                .MoveY("shackle", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.3d, -2.6d, MotionEase.QuadIn)
                    .At(0.62d, 0d, MotionEase.Linear)
                    .At(1d, 0d))
                .MoveX("body", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(0.62d, 0d, MotionEase.SinInOut)
                    .At(0.7d, -1d, MotionEase.SinInOut)
                    .At(0.78d, 1d, MotionEase.SinInOut)
                    .At(0.86d, -0.5d, MotionEase.SinInOut)
                    .At(1d, 0d))),
            new MotionIconPart("shackle", "M7.5 10.5V7a4.5 4.5 0 0 1 9 0v3.5"),
            new MotionIconPart("body", "M6 10.5h12a1.5 1.5 0 0 1 1.5 1.5v7.5a1.5 1.5 0 0 1-1.5 1.5H6a1.5 1.5 0 0 1-1.5-1.5V12A1.5 1.5 0 0 1 6 10.5z"));

        // A blink is a vertical squash of both the lid and the pupil about the same line, held
        // shut for a couple of frames so it reads as deliberate.
        yield return Icon("eye",
            MotionSpecBuilder.Build(1400, m => m
                .ScaleY("outline", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.55d, 1d, MotionEase.QuadIn)
                    .At(0.66d, 0.06d, MotionEase.Linear)
                    .At(0.71d, 0.06d, MotionEase.QuadOut)
                    .At(0.82d, 1d)
                    .At(1d, 1d))
                .ScaleY("pupil", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.55d, 1d, MotionEase.QuadIn)
                    .At(0.66d, 0.06d, MotionEase.Linear)
                    .At(0.71d, 0.06d, MotionEase.QuadOut)
                    .At(0.82d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("outline", "M1.8 12S5.6 5.5 12 5.5 22.2 12 22.2 12 18.4 18.5 12 18.5 1.8 12 1.8 12z").Origin(12f, 12f),
            new MotionIconPart("pupil", "M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0z").Origin(12f, 12f));

        yield return Icon("camera",
            MotionSpecBuilder.Build(800, m => m
                .Scale("lens", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.22d, 0.2d, MotionEase.QuadOut)
                    .At(0.45d, 1.08d, MotionEase.QuadInOut)
                    .At(0.7d, 1d)
                    .At(1d, 1d))
                .Scale("body", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.22d, 1.06d, MotionEase.QuadInOut)
                    .At(0.6d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("body", "M4 7.5h3.2L8.6 5h6.8l1.4 2.5H20A1.5 1.5 0 0 1 21.5 9v9a1.5 1.5 0 0 1-1.5 1.5H4A1.5 1.5 0 0 1 2.5 18V9A1.5 1.5 0 0 1 4 7.5z").Origin(12f, 13.5f),
            new MotionIconPart("lens", "M15.5 13.5a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0z").Origin(12f, 13.5f));

        yield return Icon("location",
            MotionSpecBuilder.Build(900, m => m
                .MoveY(null, k => k
                    .At(0d, -7d, MotionEase.QuadIn)
                    .At(0.45d, 0d, MotionEase.BounceOut)
                    .At(1d, 0d))
                .Opacity(k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.25d, 1d)
                    .At(1d, 1d))
                .ScaleY("pin", k => k
                    .At(0d, 1d, MotionEase.Linear)
                    .At(0.45d, 1d, MotionEase.QuadOut)
                    .At(0.55d, 0.88d, MotionEase.BackOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("pin", "M12 21.5s7.3-6.4 7.3-11.8A7.3 7.3 0 0 0 4.7 9.7c0 5.4 7.3 11.8 7.3 11.8z").Origin(12f, 21.5f),
            new MotionIconPart("dot", "M14.6 9.8a2.6 2.6 0 1 1-5.2 0 2.6 2.6 0 0 1 5.2 0z"));

        // Both hands turn about the dial's centre at a believable ratio — a minute for an hour.
        yield return Icon("clock",
            MotionSpecBuilder.Build(2400, m => m
                .Rotate("minute", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(1d, 360d))
                .Rotate("hour", k => k
                    .At(0d, 0d, MotionEase.Linear)
                    .At(1d, 30d))),
            new MotionIconPart("face", "M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z"),
            new MotionIconPart("hour", "M12 7.4V12").Origin(12f, 12f),
            new MotionIconPart("minute", "M12 12h4.4").Origin(12f, 12f));

        yield return Icon("cloud",
            MotionSpecBuilder.Build(3000, m => m
                .MoveY(null, k => k.Evenly(MotionEase.SinInOut, 0d, -1.4d, 0d, 1d, 0d))
                .MoveX(null, k => k.Evenly(MotionEase.SinInOut, 0d, 1d, 0d, -1d, 0d))),
            new MotionIconPart("cloud", "M17.5 18.5H7A4.5 4.5 0 0 1 6.6 9.6 6 6 0 0 1 17.9 11.2a3.65 3.65 0 0 1-.4 7.3z"));

        // Eight rays at 45 degrees apart, so a 45 degree turn lands the icon exactly on itself.
        yield return Icon("sun",
            MotionSpecBuilder.Build(1600, m => m
                .Rotate("rays", k => k
                    .At(0d, 0d, MotionEase.CubicInOut)
                    .At(1d, 45d))
                .Scale("rays", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.12d, MotionEase.SinInOut)
                    .At(1d, 1d))
                .Scale("core", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.12d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("core", "M17 12a5 5 0 1 1-10 0 5 5 0 0 1 10 0z").Origin(12f, 12f),
            new MotionIconPart("rays", "M12 1.8v2.4M12 19.8v2.4M4.6 4.6l1.7 1.7M17.7 17.7l1.7 1.7M1.8 12h2.4M19.8 12h2.4M4.6 19.4l1.7-1.7M17.7 6.3l1.7-1.7").Origin(12f, 12f));
    }
}
