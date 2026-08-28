# ProgressBar

[← All Shiny Controls](../../README.md)

A progress bar control with gradient fill and a configurable Vista-style shimmer pulse that sweeps left-to-right across the bar. Supports determinate, indeterminate, text overlay, and timed/value-triggered pulse animations. The fill **slides** to each new value rather than snapping, in both directions - a value that drops drains back at the same rate it filled - configurable via `AnimateProgress`, `ProgressAnimationDuration` and `ProgressAnimationEasing`, and skipped for width changes that come from layout rather than from progress.

```xml
<shiny:ProgressBar Value="{Binding Progress}"
                   TrackHeight="12"
                   CornerRadius="6"
                   UseGradient="True"
                   GradientStartColor="#3B82F6"
                   GradientEndColor="#8B5CF6"
                   PulseEnabled="True"
                   PulseOnValueChange="True"
                   PulseLength="0.4"
                   PulseSpeed="800" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Value | double | 0 | Current value (TwoWay) |
| Minimum | double | 0 | Minimum value |
| Maximum | double | 100 | Maximum value |
| TrackColor | Color/string | #E5E7EB | Background track color |
| BarColor | Color/string | #3B82F6 | Fill bar color (when gradient disabled) |
| TrackHeight | double | 8 | Track height in px |
| CornerRadius | double/string | 4 | Corner radius |
| UseGradient | bool | false | Enable gradient fill |
| GradientStartColor | Color/string | #3B82F6 | Left gradient color |
| GradientEndColor | Color/string | #8B5CF6 | Right gradient color |
| PulseEnabled | bool | false | Enable Vista-style shimmer pulse |
| PulseOnValueChange | bool | true | Trigger pulse on value change |
| PulseInterval | TimeSpan | 0 | Trigger pulse on a timer (e.g. every 2s) |
| PulseColor | Color/string | White | Shimmer highlight color |
| PulseOpacity | double | 0.4 | Peak shimmer opacity (MAUI) |
| PulseLength | double | 0.4 | Width of shimmer as fraction of fill (0.05–1.0) |
| PulseSpeed | int | 800 | Milliseconds for one left-to-right sweep |
| ShowText | bool | false | Show percentage text overlay |
| TextFormat | string | "{0:0}%" | Text format string |
| TextColor | Color/string | White | Text color |
| FontSize | double | 11 | Text font size |
| IsIndeterminate | bool | false | Indeterminate sliding animation |
| AnimateProgress | bool | true | Slide the fill to each new value instead of snapping (both directions) |
| ProgressAnimationDuration | int | 250 | Length of the fill slide in ms; `0` snaps |
| ProgressAnimationEasing | Easing/string | CubicOut / `cubic-bezier(0.33, 1, 0.68, 1)` | Curve the fill slide follows |

Events: `ValueChangedEvent`. Commands: `ValueChangedCommand`.
