# Carousel (Blazor)

[← All Shiny Controls](../../README.md)

`Carousel<TItem>` is a Blazor-only, **Embla-style** carousel built on a transform-based drag engine — click-and-drag on desktop, flick with momentum on touch — rather than native scroll-snap. It adds alignment, slides-per-view, scroll-linked effects, looping, autoplay/auto-scroll, and rich chrome on top of the simpler `CarouselGallery`.

```razor
<Carousel TItem="Photo" Items="@photos"
          SlidesPerView="3" Align="CarouselAlign.Start" SlidesToScroll="3"
          Loop="true" DragFree="false"
          Effect="CarouselEffect.Scale" FocusedItemScale="1" UnfocusedItemScale="0.8"
          AutoPlay="true" AutoPlayInterval="3000"
          ShowArrows="true" ShowCounter="true" ShowProgress="true"
          @bind-CurrentPosition="snap">
    <ItemTemplate Context="p">
        <img src="@p.Url" alt="@p.Title" />
    </ItemTemplate>
</Carousel>
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Effect` | `CarouselEffect` | `None` | Scroll-linked effect: `None` / `Scale` / `Opacity` / `Parallax` / `Fade` |
| `Align` | `CarouselAlign` | `Center` | Where the active slide settles: `Start` / `Center` / `End` |
| `SlidesPerView` | `double?` | `null` | Slides visible at once (slides fill the viewport when set) |
| `SlidesToScroll` | `int` | `1` | Slides advanced per arrow/dot step |
| `VariableWidths` | `bool` | `false` | Slides size to their own content |
| `Orientation` | `CarouselOrientation` | `Horizontal` | `Horizontal` / `Vertical` (vertical uses `ViewportHeight`) |
| `Loop` | `bool` | `true` | Seamless wrap-around |
| `Rtl` | `bool` | `false` | Mirrored layout + drag (horizontal) |
| `Draggable` / `DragFree` | `bool` | `true` / `false` | Pointer drag; free-scroll momentum with no snap |
| `AutoPlay` / `AutoPlayInterval` | `bool` / `int` | `false` / `4000` | Discrete timed advance (ms) |
| `AutoScroll` / `AutoScrollSpeed` | `bool` / `double` | `false` / `40` | Continuous marquee scroll (px/s) |
| `PauseOnHover` | `bool` | `true` | Pause auto-motion on hover/focus |
| `ShowArrows` / `ShowIndicators` | `bool` | `true` | Prev/next buttons; dots (one per snap) |
| `ShowCounter` / `ShowProgress` / `ShowThumbnails` | `bool` | `false` | "n / total" readout; scroll-position bar; thumbnail strip |
| `LazyLoad` / `LazyLoadBuffer` | `bool` / `int` | `false` / `1` | Defer item templates outside the buffer |
| `CurrentPosition` | `int` | `0` | Selected snap index (TwoWay) |
| `ItemTemplate` / `ThumbnailTemplate` / `PlaceholderTemplate` / `EmptyTemplate` | `RenderFragment` | — | Item, thumbnail, lazy placeholder, and empty-state content |

**Methods:** `NextAsync()`, `PreviousAsync()`, `GoToAsync(snapIndex)`, `GoToSlideAsync(itemIndex)`. **Events:** `CurrentPositionChanged`, `ItemSelected`.

**Features:**
- Transform-based pointer engine: desktop click-drag + touch flick with friction/momentum
- Alignment, slides-per-view, slides-to-scroll, variable widths, vertical axis, and RTL
- Seamless looping via per-slide repetition (works with variable widths)
- Scroll-linked effects (scale / opacity / parallax / fade) that animate live while dragging
- Autoplay with scroll-position progress bar, or continuous auto-scroll marquee
- Counter, dot indicators (per snap), and a thumbnail navigation strip
- Full keyboard support (arrows / Home / End) and lazy item loading
