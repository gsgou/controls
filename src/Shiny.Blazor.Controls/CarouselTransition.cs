namespace Shiny.Blazor.Controls;

/// <summary>
/// How the <see cref="Carousel{TItem}"/> moves between items.
/// </summary>
public enum CarouselTransition
{
    /// <summary>Native scroll-snap. Supports swipe, peek, and scroll-driven scaling.</summary>
    Snap,

    /// <summary>One item at a time slides in along the orientation axis.</summary>
    Slide,

    /// <summary>Crossfade between the outgoing and incoming item.</summary>
    Fade,

    /// <summary>Incoming item zooms/fades in (scale + opacity).</summary>
    Scale
}

/// <summary>
/// Layout/scroll axis for the <see cref="Carousel{TItem}"/>.
/// </summary>
public enum CarouselOrientation
{
    Horizontal,
    Vertical
}
