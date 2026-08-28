# RangeSlider

[← All Shiny Controls](../../README.md)

A two-thumb variant of Slider that selects a lower/upper value pair. It reuses the gradient track, blended thumb borders, and floating tooltips, adding `MinimumRange`/`MaximumRange` gap constraints between the thumbs. The dragged thumb hard-stops at `MinimumRange`; dragging past `MaximumRange` pushes the other thumb along.

```xml
<shiny:RangeSlider LowerValue="{Binding PriceLow}"
                   UpperValue="{Binding PriceHigh}"
                   Minimum="0"
                   Maximum="1000"
                   Step="10"
                   MinimumRange="50"
                   MaximumRange="500"
                   ValueFormat="C0" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| LowerValue | double | 0 | Lower thumb value (TwoWay) |
| UpperValue | double | 100 | Upper thumb value (TwoWay) |
| Minimum | double | 0 | Minimum value |
| Maximum | double | 100 | Maximum value |
| Step | double | 1 | Snap increment |
| MinimumRange | double | 0 | Minimum gap between thumbs (hard stop); 0 = off |
| MaximumRange | double | 0 | Maximum gap between thumbs (pushes the other thumb); 0 = off |
| ColdColor | Color/string | #3B82F6 | Left gradient color |
| HotColor | Color/string | #EF4444 | Right gradient color |
| TrackHeight | double | 8 | Track height |
| ThumbSize | double | 24 | Thumb diameter |
| ThumbColor | Color/string | White | Thumb fill color |
| ShowTooltip | bool | true | Show a value tooltip per thumb |
| TooltipTemplate | DataTemplate/RenderFragment | null | Custom tooltip content (applied to both thumbs) |
| ValueFormat | string? | null | Format string for tooltip values |
