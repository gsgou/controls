# Slider

[← All Shiny Controls](../../README.md)

A slider control with a two-color gradient track, blended thumb border, tooltip, and full drag/tap interaction.

```xml
<shiny:Slider Value="{Binding Temperature}"
                      Minimum="0"
                      Maximum="100"
                      ColdColor="#3B82F6"
                      HotColor="#EF4444"
                      ShowTooltip="True" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Value | double | 0 | Current value (TwoWay) |
| Minimum | double | 0 | Minimum value |
| Maximum | double | 100 | Maximum value |
| Step | double | 1 | Snap increment |
| ColdColor | Color/string | #3B82F6 | Left gradient color |
| HotColor | Color/string | #EF4444 | Right gradient color |
| TrackHeight | double | 8 | Track height |
| ThumbSize | double | 24 | Thumb diameter |
| ThumbColor | Color/string | White | Thumb fill color |
| ShowTooltip | bool | true | Show value tooltip |
| TooltipTemplate | DataTemplate/RenderFragment | null | Custom tooltip content |
| ValueFormat | string? | null | Format string for tooltip value |
