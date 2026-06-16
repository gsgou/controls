namespace Shiny.Blazor.Controls;

/// <summary>
/// Scroll-linked visual effect applied to each slide as it moves through the
/// <see cref="Carousel{TItem}"/> viewport. Effects are driven by each slide's
/// distance from the centre, so they animate continuously while dragging.
/// </summary>
public enum CarouselEffect
{
    /// <summary>Plain translate — slides simply move along the axis.</summary>
    None,

    /// <summary>Slides scale down as they move away from the centre.</summary>
    Scale,

    /// <summary>Slides fade out as they move away from the centre.</summary>
    Opacity,

    /// <summary>Slide content shifts within the slide for a depth/parallax effect.</summary>
    Parallax,

    /// <summary>Slides are stacked and crossfade between each other (the container does not translate).</summary>
    Fade
}

/// <summary>
/// Where the active slide settles within the viewport.
/// </summary>
public enum CarouselAlign
{
    Start,
    Center,
    End
}

/// <summary>
/// Layout/scroll axis for the <see cref="Carousel{TItem}"/>.
/// </summary>
public enum CarouselOrientation
{
    Horizontal,
    Vertical
}
