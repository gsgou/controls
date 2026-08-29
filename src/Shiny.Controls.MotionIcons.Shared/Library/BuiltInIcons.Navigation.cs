namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    /// <summary>
    /// Arrows, chevrons and the handful of glyphs that mean "go somewhere".
    /// </summary>
    /// <remarks>
    /// The directional set is deliberately consistent: every arrow travels the way it points and
    /// pulls its shaft in behind the head, and every chevron bounces once in its own direction. An
    /// app that swaps <c>arrow-right</c> for <c>arrow-left</c> in a right-to-left layout gets the
    /// mirrored motion for free rather than a different animation.
    /// </remarks>
    static IEnumerable<MotionIconDefinition> Navigation()
    {
        yield return Icon("arrow-left",
            MotionSpecBuilder.Build(800, m => m
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.CubicIn)
                    .At(0.45d, -4d, MotionEase.CubicOut)
                    .At(1d, 0d))
                .Trim("shaft", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.45d, 0.25d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("shaft", "M20.5 12H6.5"),
            new MotionIconPart("head", "M11 6.5L5.5 12L11 17.5"));

        yield return Icon("arrow-up",
            MotionSpecBuilder.Build(800, m => m
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.CubicIn)
                    .At(0.45d, -4d, MotionEase.CubicOut)
                    .At(1d, 0d))
                .Trim("shaft", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.45d, 0.25d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("shaft", "M12 20.5V6.5"),
            new MotionIconPart("head", "M6.5 11L12 5.5L17.5 11"));

        yield return Icon("arrow-down",
            MotionSpecBuilder.Build(800, m => m
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.CubicIn)
                    .At(0.45d, 4d, MotionEase.CubicOut)
                    .At(1d, 0d))
                .Trim("shaft", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.45d, 0.25d, MotionEase.QuadOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("shaft", "M12 3.5V17.5"),
            new MotionIconPart("head", "M6.5 13L12 18.5L17.5 13"));

        yield return Icon("chevron-up",
            MotionSpecBuilder.Build(800, m => m
                .MoveY(null, k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, -3.2d, MotionEase.BounceOut)
                    .At(1d, 0d))),
            new MotionIconPart("chevron", "M6 14.5L12 8.5L18 14.5"));

        yield return Icon("chevron-left",
            MotionSpecBuilder.Build(800, m => m
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, -3.2d, MotionEase.BounceOut)
                    .At(1d, 0d))),
            new MotionIconPart("chevron", "M14.5 6L8.5 12L14.5 18"));

        yield return Icon("chevron-right",
            MotionSpecBuilder.Build(800, m => m
                .MoveX(null, k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, 3.2d, MotionEase.BounceOut)
                    .At(1d, 0d))),
            new MotionIconPart("chevron", "M9.5 6L15.5 12L9.5 18"));

        // The arrow leaves the frame it is anchored to, which is the whole point of the glyph —
        // so it travels out along its own diagonal rather than bobbing in place.
        yield return Icon("external-link",
            MotionSpecBuilder.Build(1000, m => m
                .MoveX("arrow", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.4d, 3d, MotionEase.StepEnd)
                    .At(0.45d, -3d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .MoveY("arrow", k => k
                    .At(0d, 0d, MotionEase.QuadIn)
                    .At(0.4d, -3d, MotionEase.StepEnd)
                    .At(0.45d, 3d, MotionEase.QuadOut)
                    .At(1d, 0d))
                .Opacity("arrow", k => k
                    .At(0d, 1d, MotionEase.QuadIn)
                    .At(0.4d, 0d, MotionEase.StepEnd)
                    .At(0.45d, 0d, MotionEase.QuadOut)
                    .At(0.75d, 1d)
                    .At(1d, 1d))
                .Scale("frame", k => k
                    .At(0d, 1d, MotionEase.QuadOut)
                    .At(0.4d, 0.94d, MotionEase.BackOut)
                    .At(0.8d, 1d)
                    .At(1d, 1d))),
            new MotionIconPart("frame", "M13.5 4.5H5.5A2 2 0 0 0 3.5 6.5V18.5A2 2 0 0 0 5.5 20.5H17.5A2 2 0 0 0 19.5 18.5V10.5").Origin(11.5f, 12.5f),
            new MotionIconPart("arrow", "M10.5 13.5L20.5 3.5M14.5 3.5H20.5V9.5"));

        yield return Icon("expand",
            MotionSpecBuilder.Build(900, m => m
                .MoveX("top-left", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveY("top-left", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveX("top-right", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveY("top-right", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveX("bottom-left", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveY("bottom-left", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveX("bottom-right", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveY("bottom-right", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))),
            new MotionIconPart("top-left", "M9.5 3.5H3.5V9.5"),
            new MotionIconPart("top-right", "M14.5 3.5H20.5V9.5"),
            new MotionIconPart("bottom-left", "M9.5 20.5H3.5V14.5"),
            new MotionIconPart("bottom-right", "M14.5 20.5H20.5V14.5"));

        yield return Icon("collapse",
            MotionSpecBuilder.Build(900, m => m
                .MoveX("top-left", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveY("top-left", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveX("top-right", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveY("top-right", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveX("bottom-left", k => k.Evenly(MotionEase.BackOut, 0d, 1.8d, 0d))
                .MoveY("bottom-left", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveX("bottom-right", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))
                .MoveY("bottom-right", k => k.Evenly(MotionEase.BackOut, 0d, -1.8d, 0d))),
            new MotionIconPart("top-left", "M3.5 7.5H7.5V3.5"),
            new MotionIconPart("top-right", "M20.5 7.5H16.5V3.5"),
            new MotionIconPart("bottom-left", "M3.5 16.5H7.5V20.5"),
            new MotionIconPart("bottom-right", "M20.5 16.5H16.5V20.5"));

        // A needle settles the way a real one does — a wide first swing, then progressively smaller
        // ones about the same pivot as the dial.
        yield return Icon("compass",
            MotionSpecBuilder.Build(1400, m => m
                .Rotate("needle", k => k.Evenly(MotionEase.SinInOut, 0d, 34d, -24d, 15d, -8d, 3d, 0d))
                .Scale("dial", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.05d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("dial", "M21 12A9 9 0 0 1 3 12A9 9 0 0 1 21 12z").Origin(12f, 12f),
            new MotionIconPart("needle", "M16.2 7.8L13.6 13.6L7.8 16.2L10.4 10.4z").Origin(12f, 12f));
    }
}
