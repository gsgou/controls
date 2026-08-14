namespace Shiny.Maui.Controls;

/// <summary>Where a bubble ended up, and where its tail has to sit to still point at the target.</summary>
/// <param name="Placement">The side actually used — never <see cref="TooltipPlacement.Auto"/>.</param>
/// <param name="Bubble">The bubble's rect in container space.</param>
/// <param name="TailOffset">
/// Distance from the bubble's leading edge (left for Top/Bottom, top for Left/Right) to the centre of
/// the tail. Zero when no tail is drawn.
/// </param>
/// <param name="Fits">Whether the chosen side had room. False means the bubble was clamped to stay on screen.</param>
public readonly record struct TooltipLayout(
    TooltipPlacement Placement,
    Rect Bubble,
    double TailOffset,
    bool Fits
);


/// <summary>
/// Decides which side of a target a bubble goes on, and where exactly.
/// </summary>
/// <remarks>
/// Deliberately free of MAUI view types: it takes rects and returns rects, so the same rules drive a
/// <see cref="Tooltip"/>, a <see cref="Walkthrough"/> callout, and a unit test. The rules themselves
/// are the boring ones every popover engine converges on — try the preferred side, flip to the
/// opposite if it does not fit, clamp along the cross axis so the bubble stays on screen, then slide
/// the tail to keep pointing at the target even though the bubble moved.
/// </remarks>
public static class TooltipPlacementSolver
{
    /// <summary>Order <see cref="TooltipPlacement.Auto"/> tries sides in when several of them fit.</summary>
    static readonly TooltipPlacement[] AutoOrder =
    [
        TooltipPlacement.Bottom,
        TooltipPlacement.Top,
        TooltipPlacement.Right,
        TooltipPlacement.Left
    ];


    /// <summary>
    /// Places <paramref name="bubbleSize"/> against <paramref name="target"/> inside <paramref name="container"/>.
    /// </summary>
    /// <param name="target">The target's rect in container space.</param>
    /// <param name="bubbleSize">The bubble's desired size, tail included.</param>
    /// <param name="container">The space available — normally the page.</param>
    /// <param name="preferred">The side asked for. <see cref="TooltipPlacement.Auto"/> picks one.</param>
    /// <param name="gap">Space left between the target and the bubble.</param>
    /// <param name="margin">Space kept clear at the container's edges.</param>
    /// <param name="tailInset">
    /// How far from a bubble corner the tail may come. Keeps the tail off the rounded corners, where it
    /// would otherwise detach from the bubble's outline.
    /// </param>
    public static TooltipLayout Solve(
        Rect target,
        Size bubbleSize,
        Size container,
        TooltipPlacement preferred,
        double gap = 8,
        double margin = 12,
        double tailInset = 16
    )
    {
        if (preferred == TooltipPlacement.Center)
            return new TooltipLayout(TooltipPlacement.Center, Centered(bubbleSize, container), 0, true);

        var placement = preferred == TooltipPlacement.Auto
            ? ChooseSide(target, bubbleSize, container, gap, margin)
            : FlipIfNeeded(preferred, target, bubbleSize, container, gap, margin);

        var rect = Place(placement, target, bubbleSize, container, gap, margin);
        var fits = Fits(placement, target, bubbleSize, container, gap, margin);
        var tail = TailOffset(placement, target, rect, tailInset);

        return new TooltipLayout(placement, rect, tail, fits);
    }


    /// <summary>The best of the four sides: the first that fits, or failing that the roomiest.</summary>
    static TooltipPlacement ChooseSide(Rect target, Size bubble, Size container, double gap, double margin)
    {
        foreach (var candidate in AutoOrder)
        {
            if (Fits(candidate, target, bubble, container, gap, margin))
                return candidate;
        }

        // Nothing fits — this is a small screen with a big bubble. Take the most room going, so the
        // clamping below eats as little of the bubble as it can.
        var best = AutoOrder[0];
        var bestSpace = double.NegativeInfinity;

        foreach (var candidate in AutoOrder)
        {
            var space = Available(candidate, target, container, gap, margin);
            if (space > bestSpace)
            {
                bestSpace = space;
                best = candidate;
            }
        }
        return best;
    }


    /// <summary>Honours an explicit side unless it would not fit and the opposite one would.</summary>
    static TooltipPlacement FlipIfNeeded(
        TooltipPlacement preferred,
        Rect target,
        Size bubble,
        Size container,
        double gap,
        double margin
    )
    {
        if (Fits(preferred, target, bubble, container, gap, margin))
            return preferred;

        var opposite = Opposite(preferred);
        return Fits(opposite, target, bubble, container, gap, margin) ? opposite : preferred;
    }


    public static TooltipPlacement Opposite(TooltipPlacement placement) => placement switch
    {
        TooltipPlacement.Top => TooltipPlacement.Bottom,
        TooltipPlacement.Bottom => TooltipPlacement.Top,
        TooltipPlacement.Left => TooltipPlacement.Right,
        TooltipPlacement.Right => TooltipPlacement.Left,
        _ => placement
    };


    /// <summary>Room on one side of the target, once the gap and the container margin are taken out.</summary>
    static double Available(TooltipPlacement placement, Rect target, Size container, double gap, double margin)
        => placement switch
        {
            TooltipPlacement.Top => target.Top - gap - margin,
            TooltipPlacement.Bottom => container.Height - target.Bottom - gap - margin,
            TooltipPlacement.Left => target.Left - gap - margin,
            TooltipPlacement.Right => container.Width - target.Right - gap - margin,
            _ => 0
        };


    static bool Fits(TooltipPlacement placement, Rect target, Size bubble, Size container, double gap, double margin)
    {
        var needed = placement is TooltipPlacement.Top or TooltipPlacement.Bottom
            ? bubble.Height
            : bubble.Width;

        return Available(placement, target, container, gap, margin) >= needed;
    }


    static Rect Place(TooltipPlacement placement, Rect target, Size bubble, Size container, double gap, double margin)
    {
        double x;
        double y;

        switch (placement)
        {
            case TooltipPlacement.Top:
                x = Clamp(target.Center.X - (bubble.Width / 2), margin, container.Width - bubble.Width - margin);
                y = target.Top - gap - bubble.Height;
                break;

            case TooltipPlacement.Bottom:
                x = Clamp(target.Center.X - (bubble.Width / 2), margin, container.Width - bubble.Width - margin);
                y = target.Bottom + gap;
                break;

            case TooltipPlacement.Left:
                x = target.Left - gap - bubble.Width;
                y = Clamp(target.Center.Y - (bubble.Height / 2), margin, container.Height - bubble.Height - margin);
                break;

            case TooltipPlacement.Right:
                x = target.Right + gap;
                y = Clamp(target.Center.Y - (bubble.Height / 2), margin, container.Height - bubble.Height - margin);
                break;

            default:
                return Centered(bubble, container);
        }

        // The main axis gets clamped too. It only bites when the side did not really fit, and a bubble
        // half off the top of the screen is worse than one overlapping its target.
        x = Clamp(x, margin, container.Width - bubble.Width - margin);
        y = Clamp(y, margin, container.Height - bubble.Height - margin);

        return new Rect(x, y, bubble.Width, bubble.Height);
    }


    static Rect Centered(Size bubble, Size container) => new(
        (container.Width - bubble.Width) / 2,
        (container.Height - bubble.Height) / 2,
        bubble.Width,
        bubble.Height
    );


    /// <summary>
    /// Keeps the tail on the target's centre line after the bubble has been clamped, pulled in from the
    /// bubble's corners so it always meets a straight edge.
    /// </summary>
    static double TailOffset(TooltipPlacement placement, Rect target, Rect bubble, double inset)
    {
        var (center, start, length) = placement switch
        {
            TooltipPlacement.Top or TooltipPlacement.Bottom => (target.Center.X, bubble.X, bubble.Width),
            TooltipPlacement.Left or TooltipPlacement.Right => (target.Center.Y, bubble.Y, bubble.Height),
            _ => (0d, 0d, 0d)
        };

        if (length <= 0)
            return 0;

        // A bubble narrower than two insets has no straight edge to speak of; centre the tail.
        if (length <= inset * 2)
            return length / 2;

        return Clamp(center - start, inset, length - inset);
    }


    /// <summary>
    /// Clamp that survives an inverted range. When the bubble is wider than the container the upper
    /// bound lands below the lower one, and <c>Math.Clamp</c> throws rather than picking a side.
    /// </summary>
    static double Clamp(double value, double min, double max)
    {
        if (max < min)
            return min;

        return value < min ? min : (value > max ? max : value);
    }
}
