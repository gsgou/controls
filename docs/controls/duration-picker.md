# DurationPicker

[← All Shiny Controls](../../README.md)

A standalone duration picker control that opens a FloatingPanel for selection with hour/minute pickers and "hr"/"min" labels. Requires `ShinyContentPage` (or an `OverlayHost` in the visual tree).

```xml
<shiny:DurationPicker Duration="{Binding SelectedDuration, Mode=TwoWay}"
                      MinDuration="0:15:00"
                      MaxDuration="8:00:00"
                      MinuteInterval="5"
                      Placeholder="Choose duration" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `Duration` | `TimeSpan?` | `null` | Selected duration (TwoWay) |
| `MinDuration` | `TimeSpan` | `0:00:00` | Minimum duration |
| `MaxDuration` | `TimeSpan` | `24:00:00` | Maximum duration |
| `MinuteInterval` | `int` | `5` | Minute increment step |
| `Format` | `string` | `@"h\:mm"` | Display format string |
| `Placeholder` | `string` | `"Select duration"` | Text shown when no duration selected |
