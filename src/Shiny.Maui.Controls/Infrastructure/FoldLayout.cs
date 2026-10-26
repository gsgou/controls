namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>Which side of a fold a piece of content prefers.</summary>
/// <remarks>
/// One value covers both orientations so a caller states its intent once. Against a vertical (book)
/// fold only <see cref="Leading"/> and <see cref="Trailing"/> are honoured; everything else takes the
/// larger segment. Against a horizontal (tabletop) fold only <see cref="Top"/> and <see cref="Bottom"/>
/// are honoured; everything else takes the larger segment. A dialog asks for <see cref="Top"/> and so
/// lands in the larger half of a book and the upper half of a tabletop; a toast at the bottom asks
/// for <see cref="Bottom"/>.
/// </remarks>
enum FoldPreference
{
    Larger,
    Top,
    Bottom,
    Leading,
    Trailing
}


/// <summary>
/// The fold maths every fold-aware control shares, kept free of views so it can be unit tested:
/// where a fold falls inside a control, which side of it content goes on, and how a two-pane split
/// lines up with it.
/// </summary>
static class FoldLayout
{
    /// <summary>
    /// The part of <paramref name="foldInWindow"/> that divides <paramref name="viewInWindow"/>, in the
    /// view's own coordinates — or null when the fold misses the view, only grazes its edge, or there is
    /// no fold. A fold has to have its centre line strictly inside the view to count: one that only
    /// clips an edge leaves a single segment, which is no division at all.
    /// </summary>
    public static Rect? FoldIn(Rect viewInWindow, Rect? foldInWindow)
    {
        if (foldInWindow is not { } fold || viewInWindow.Width <= 0 || viewInWindow.Height <= 0)
            return null;

        var local = new Rect(fold.X - viewInWindow.X, fold.Y - viewInWindow.Y, fold.Width, fold.Height);
        var w = viewInWindow.Width;
        var h = viewInWindow.Height;

        if (IsVertical(local))
        {
            var centre = local.X + (local.Width / 2);
            if (centre <= 0 || centre >= w)
                return null;

            var top = Math.Max(0, local.Top);
            var bottom = Math.Min(h, local.Bottom);
            if (bottom <= top)
                return null;

            var left = Math.Max(0, local.Left);
            var right = Math.Min(w, local.Right);
            return new Rect(left, top, Math.Max(0, right - left), bottom - top);
        }
        else
        {
            var centre = local.Y + (local.Height / 2);
            if (centre <= 0 || centre >= h)
                return null;

            var left = Math.Max(0, local.Left);
            var right = Math.Min(w, local.Right);
            if (right <= left)
                return null;

            var top = Math.Max(0, local.Top);
            var bottom = Math.Min(h, local.Bottom);
            return new Rect(left, top, right - left, Math.Max(0, bottom - top));
        }
    }


    /// <summary>
    /// A fold that is at least as tall as it is wide runs top to bottom. A zero-width fold spanning the
    /// window is the common Android case and comes out vertical; a zero-height one, horizontal.
    /// </summary>
    public static bool IsVertical(Rect fold) => fold.Height >= fold.Width;


    /// <summary>The two regions either side of <paramref name="fold"/> (already in host coordinates).</summary>
    public static (Rect First, Rect Second)? Segments(Rect host, Rect? fold)
    {
        if (fold is not { } f)
            return null;

        if (IsVertical(f))
        {
            var leftWidth = Math.Max(0, f.Left - host.Left);
            var rightX = Math.Min(host.Right, f.Right);
            return (
                new Rect(host.Left, host.Top, leftWidth, host.Height),
                new Rect(rightX, host.Top, Math.Max(0, host.Right - rightX), host.Height)
            );
        }

        var topHeight = Math.Max(0, f.Top - host.Top);
        var bottomY = Math.Min(host.Bottom, f.Bottom);
        return (
            new Rect(host.Left, host.Top, host.Width, topHeight),
            new Rect(host.Left, bottomY, host.Width, Math.Max(0, host.Bottom - bottomY))
        );
    }


    /// <summary>
    /// The region content should be confined to so it never sits across <paramref name="fold"/>. With
    /// no fold that is the whole host. See <see cref="FoldPreference"/> for which side wins; equal
    /// halves go to the leading side (the right one in right-to-left) or the top.
    /// </summary>
    public static Rect SegmentFor(Rect host, Rect? fold, FoldPreference preference, bool rightToLeft = false)
    {
        if (Segments(host, fold) is not { } segments)
            return host;

        var (first, second) = segments;

        if (IsVertical(fold!.Value))
        {
            var leading = rightToLeft ? second : first;
            var trailing = rightToLeft ? first : second;

            return preference switch
            {
                FoldPreference.Leading => leading,
                FoldPreference.Trailing => trailing,
                _ => Area(trailing) > Area(leading) + 0.5 ? trailing : leading
            };
        }

        return preference switch
        {
            FoldPreference.Top => first,
            FoldPreference.Bottom => second,
            _ => Area(second) > Area(first) + 0.5 ? second : first
        };

        static double Area(Rect r) => r.Width * r.Height;
    }


    /// <summary>
    /// The segment <paramref name="point"/> is in — the one an anchored popup belongs in, so it never
    /// opens across the fold from the thing it points at. A point inside the fold itself goes to the
    /// larger segment. With no fold, the whole host.
    /// </summary>
    public static Rect SegmentContaining(Rect host, Rect? fold, Point point)
    {
        if (Segments(host, fold) is not { } segments)
            return host;

        var (first, second) = segments;
        var vertical = IsVertical(fold!.Value);
        var along = vertical ? point.X : point.Y;

        if (along <= (vertical ? first.Right : first.Bottom))
            return first;

        if (along >= (vertical ? second.Left : second.Top))
            return second;

        return SegmentFor(host, fold, FoldPreference.Larger);
    }


    /// <summary>
    /// The margin that confines a child filling <paramref name="host"/> to <paramref name="segment"/>.
    /// </summary>
    public static Thickness MarginFor(Size host, Rect segment) => new(
        Math.Max(0, segment.Left),
        Math.Max(0, segment.Top),
        Math.Max(0, host.Width - segment.Right),
        Math.Max(0, host.Height - segment.Bottom)
    );


    /// <summary>
    /// <see cref="SegmentFor"/> as a margin — zero with no fold, so adding it to an existing margin is
    /// a no-op on every device that does not fold.
    /// </summary>
    public static Thickness SegmentMargin(Size host, Rect? fold, FoldPreference preference, bool rightToLeft = false)
    {
        if (fold is null || host.Width <= 0 || host.Height <= 0)
            return default;

        var segment = SegmentFor(new Rect(0, 0, host.Width, host.Height), fold, preference, rightToLeft);
        return MarginFor(host, segment);
    }


    /// <summary>
    /// Where a flyout panel lands when it snaps to a vertical fold: its width runs from its own edge to
    /// the near edge of the fold, and the content beside it starts at the far edge. Null when the fold
    /// is not vertical or leaves no room on one side.
    /// </summary>
    /// <param name="hostWidth">The flyout's width.</param>
    /// <param name="fold">The fold, in the flyout's coordinates.</param>
    /// <param name="panelOnLeft">Whether the snapping panel is the physically left one.</param>
    public static (double PanelWidth, double Inset)? FlyoutSnap(double hostWidth, Rect? fold, bool panelOnLeft)
    {
        if (fold is not { } f || !IsVertical(f) || hostWidth <= 0)
            return null;

        var (width, inset) = panelOnLeft
            ? (f.Left, f.Right)
            : (hostWidth - f.Right, hostWidth - f.Left);

        if (width <= 0 || inset >= hostWidth)
            return null;

        return (width, inset);
    }


    /// <summary>
    /// Which physical side of a vertical fold is nearer its own edge — the panel on that side is the one
    /// adjacent to the fold. Ties go left.
    /// </summary>
    public static bool FoldIsNearerLeft(double hostWidth, Rect fold)
        => fold.Center.X <= hostWidth / 2;


    /// <summary>
    /// The detent, as a ratio of <paramref name="hostHeight"/>, that puts a sheet's edge exactly on a
    /// horizontal fold: a bottom sheet then fills the lower half and a top sheet the upper one. Null for
    /// any fold that is not horizontal.
    /// </summary>
    public static double? TabletopDetent(double hostHeight, Rect? fold, bool fromBottom)
    {
        if (fold is not { } f || IsVertical(f) || hostHeight <= 0)
            return null;

        var height = fromBottom ? hostHeight - f.Bottom : f.Top;
        if (height <= 0)
            return null;

        return Math.Clamp(height / hostHeight, 0.05, 1.0);
    }


    /// <summary>
    /// The ratio (0–1) of a two-way split that puts the divider on a fold running the same way, or null
    /// when there is no such fold. <paramref name="horizontalSplit"/> means the panes sit side by side
    /// (a vertical divider).
    /// </summary>
    public static double? SplitRatio(Size host, Rect? fold, bool horizontalSplit)
    {
        if (fold is not { } f)
            return null;

        if (horizontalSplit)
        {
            if (!IsVertical(f) || host.Width <= 0)
                return null;
            return Math.Clamp(f.Center.X / host.Width, 0, 1);
        }

        if (IsVertical(f) || host.Height <= 0)
            return null;
        return Math.Clamp(f.Center.Y / host.Height, 0, 1);
    }


    /// <summary>
    /// How many of <paramref name="count"/> equal-width slots fit before a vertical fold, so a row of
    /// items can be split across the two segments instead of one sitting on the hinge. Null when the
    /// fold is not vertical.
    /// </summary>
    public static int? ItemsBeforeFold(double hostWidth, Rect? fold, int count)
    {
        if (fold is not { } f || !IsVertical(f) || count <= 0 || hostWidth <= 0)
            return null;

        var ratio = f.Center.X / hostWidth;
        return Math.Clamp((int)Math.Round(count * ratio, MidpointRounding.AwayFromZero), 0, count);
    }
}
