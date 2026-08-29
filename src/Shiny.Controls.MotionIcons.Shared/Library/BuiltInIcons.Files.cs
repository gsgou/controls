namespace Shiny.Controls.MotionIcons;

public static partial class BuiltInIcons
{
    /// <summary>
    /// Documents, folders and the containers things get stored in.
    /// </summary>
    /// <remarks>
    /// Everything here is hinged rather than bounced. A folder tab lifts off the line it meets the
    /// body on, a page's corner fold draws itself along its own crease, and the clipboard's clip
    /// pivots where it is actually clipped — all of which need an <see cref="MotionIconPart.Origin"/>
    /// on the seam, not the middle of the box.
    /// </remarks>
    static IEnumerable<MotionIconDefinition> Files()
    {
        yield return Icon("folder",
            MotionSpecBuilder.Build(1000, m => m
                .Rotate("tab", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.35d, -10d, MotionEase.SinInOut)
                    .At(0.6d, -10d, MotionEase.BackInOut)
                    .At(1d, 0d))
                .MoveY("body", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.35d, 0.6d, MotionEase.QuadInOut)
                    .At(1d, 0d))),
            new MotionIconPart("body", "M21 9.5V18.5A1.5 1.5 0 0 1 19.5 20H4.5A1.5 1.5 0 0 1 3 18.5V9.5"),
            new MotionIconPart("tab", "M3 9.5V6A1.5 1.5 0 0 1 4.5 4.5H9L11.2 7.2H19.5A1.5 1.5 0 0 1 21 8.7V9.5").Origin(3f, 9.5f));

        yield return Icon("folder-open",
            MotionSpecBuilder.Build(1000, m => m
                .Rotate("front", k => k
                    .At(0d, 0d, MotionEase.BackOut)
                    .At(0.4d, -9d, MotionEase.BackInOut)
                    .At(1d, 0d))
                .MoveY("back", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.4d, -0.8d, MotionEase.QuadInOut)
                    .At(1d, 0d))),
            new MotionIconPart("back", "M3.5 18.5V6A1.5 1.5 0 0 1 5 4.5H9.4L11.6 7.2H18A1.5 1.5 0 0 1 19.5 8.7V10.5"),
            new MotionIconPart("front", "M3.5 18.5L6.2 12A1.5 1.5 0 0 1 7.6 11H21A1.5 1.5 0 0 1 22.4 13L20.1 19A1.5 1.5 0 0 1 18.7 20H5A1.5 1.5 0 0 1 3.5 18.5z").Origin(3.5f, 18.5f));

        yield return Icon("file",
            MotionSpecBuilder.Build(1000, m => m
                .Trim("fold", k => k
                    .At(0d, 0.05d, MotionEase.CubicOut)
                    .At(0.5d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .MoveY("sheet", k => k
                    .At(0d, 0d, MotionEase.QuadOut)
                    .At(0.3d, -1d, MotionEase.QuadInOut)
                    .At(0.8d, 0d)
                    .At(1d, 0d))),
            new MotionIconPart("sheet", "M6 3.5H13.5L19.5 9.5V19A1.5 1.5 0 0 1 18 20.5H6A1.5 1.5 0 0 1 4.5 19V5A1.5 1.5 0 0 1 6 3.5z"),
            new MotionIconPart("fold", "M13.5 3.5V9.5H19.5"));

        yield return Icon("file-text",
            MotionSpecBuilder.Build(1400, m => m
                .Trim("line-top", k => k
                    .At(0d, 0.02d, MotionEase.CubicOut)
                    .At(0.35d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("line-bottom", k => k
                    .At(0d, 0.02d, MotionEase.Linear)
                    .At(0.3d, 0.02d, MotionEase.CubicOut)
                    .At(0.65d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .ScaleY("sheet", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.03d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("sheet", "M6 3.5H13.5L19.5 9.5V19A1.5 1.5 0 0 1 18 20.5H6A1.5 1.5 0 0 1 4.5 19V5A1.5 1.5 0 0 1 6 3.5z").Origin(12f, 20.5f),
            new MotionIconPart("fold", "M13.5 3.5V9.5H19.5"),
            new MotionIconPart("line-top", "M8 13.5H16"),
            new MotionIconPart("line-bottom", "M8 17H13.5"));

        yield return Icon("image",
            MotionSpecBuilder.Build(1400, m => m
                .Scale("sun", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.3d, 1.2d, MotionEase.SinInOut)
                    .At(0.6d, 1d)
                    .At(1d, 1d))
                .Trim("hill", k => k
                    .At(0d, 0.05d, MotionEase.CubicOut)
                    .At(0.6d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("frame", "M5 4.5H19A1.5 1.5 0 0 1 20.5 6V18A1.5 1.5 0 0 1 19 19.5H5A1.5 1.5 0 0 1 3.5 18V6A1.5 1.5 0 0 1 5 4.5z"),
            new MotionIconPart("sun", "M11 9.5A2 2 0 0 1 7 9.5A2 2 0 0 1 11 9.5z").Origin(9f, 9.5f),
            new MotionIconPart("hill", "M3.8 17.6L9.2 12.2L13.4 16.4L16.4 13.4L20.3 17.3"));

        yield return Icon("clipboard",
            MotionSpecBuilder.Build(1200, m => m
                .Rotate("clip", k => k.Evenly(MotionEase.SinInOut, 0d, -7d, 5d, -2d, 0d))
                .Trim("line-top", k => k
                    .At(0d, 0.02d, MotionEase.CubicOut)
                    .At(0.45d, 1d, MotionEase.Linear)
                    .At(1d, 1d))
                .Trim("line-bottom", k => k
                    .At(0d, 0.02d, MotionEase.Linear)
                    .At(0.25d, 0.02d, MotionEase.CubicOut)
                    .At(0.7d, 1d, MotionEase.Linear)
                    .At(1d, 1d))),
            new MotionIconPart("board", "M9 4.5H6.5A1.5 1.5 0 0 0 5 6V19.5A1.5 1.5 0 0 0 6.5 21H17.5A1.5 1.5 0 0 0 19 19.5V6A1.5 1.5 0 0 0 17.5 4.5H15"),
            new MotionIconPart("clip", "M9.5 2.5H14.5A1 1 0 0 1 15.5 3.5V5.5A1 1 0 0 1 14.5 6.5H9.5A1 1 0 0 1 8.5 5.5V3.5A1 1 0 0 1 9.5 2.5z").Origin(12f, 6.5f),
            new MotionIconPart("line-top", "M8.5 11H15.5"),
            new MotionIconPart("line-bottom", "M8.5 15H13"));

        // A page turning is a horizontal squash about the spine — the only honest way to fake a
        // fold when there is no morph channel to bend the paper with.
        yield return Icon("book",
            MotionSpecBuilder.Build(1600, m => m
                .ScaleX("right", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.45d, 0.12d, MotionEase.SinInOut)
                    .At(0.9d, 1d)
                    .At(1d, 1d))
                .ScaleY("left", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.03d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("left", "M12 6.5A5 5 0 0 0 7 4.5H3.5V18.5H7.5A5 5 0 0 1 12 21z").Origin(12f, 12f),
            new MotionIconPart("right", "M12 6.5A5 5 0 0 1 17 4.5H20.5V18.5H16.5A5 5 0 0 0 12 21z").Origin(12f, 12f));

        // The rings light bottom-up, which is the direction a query actually travels through a
        // stack of storage.
        yield return Icon("database",
            MotionSpecBuilder.Build(1600, m => m
                .Opacity("bottom", k => k
                    .At(0d, 0.2d, MotionEase.QuadOut)
                    .At(0.15d, 1d)
                    .At(1d, 1d))
                .Opacity("middle", k => k
                    .At(0d, 0.2d, MotionEase.Linear)
                    .At(0.18d, 0.2d, MotionEase.QuadOut)
                    .At(0.35d, 1d)
                    .At(1d, 1d))
                .Opacity("top", k => k
                    .At(0d, 0.2d, MotionEase.Linear)
                    .At(0.38d, 0.2d, MotionEase.QuadOut)
                    .At(0.55d, 1d)
                    .At(1d, 1d))
                .ScaleY("sides", k => k
                    .At(0d, 1d, MotionEase.SinInOut)
                    .At(0.5d, 1.04d, MotionEase.SinInOut)
                    .At(1d, 1d))),
            new MotionIconPart("sides", "M4 6V18M20 6V18").Origin(12f, 12f),
            new MotionIconPart("bottom", "M4 17A8 3 0 0 0 20 17"),
            new MotionIconPart("middle", "M4 11.5A8 3 0 0 0 20 11.5"),
            new MotionIconPart("top", "M20 6A8 3 0 0 1 4 6A8 3 0 0 1 20 6z"));
    }
}
