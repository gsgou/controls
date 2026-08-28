# ColorPicker

[← All Shiny Controls](../../README.md)

A full-featured color picker with spectrum, hue bar, opacity slider, hex input, and preview swatch. Available as both an inline `ColorPicker` control and a `ColorPickerButton` that opens as a popup dialog.

| Button | Picker Dialog |
|:---:|:---:|
| ![Color Picker Button](../../assets/colorpicker1.png) | ![Color Picker Dialog](../../assets/colorpicker2.png) |

```xml
<shiny:ColorPickerButton SelectedColor="{Binding SelectedColor}"
                         Text="Pick Color"
                         ShowOpacity="True" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| SelectedColor | Color | Red | Currently selected color — TwoWay |
| Text | string? | null | Button label text |
| ShowOpacity | bool | false | Show/hide opacity slider |
| CornerRadius | int | 8 | Button corner radius |
| ColorChangedCommand | ICommand? | null | Fires when color changes |

**Event:** `ColorChanged` (EventHandler\<Color\>)

Blazor has the same pair. `ColorPicker` is the panel; `ColorPickerButton` is the swatch that opens it,
which is what goes in a toolbar. The swatch sits on a checkerboard so a translucent colour reads as
translucent rather than as a slightly different flat one.

```razor
<ColorPickerButton Text="Text colour"
                   @bind-SelectedColor="hex"
                   ShowOpacity="false" />
```

`SelectedColor` is a hex string on Blazor rather than a `Color`: `#RRGGBB`, or `#AARRGGBB` when
`ShowOpacity` is on — alpha first, so it drops straight into `ArgbColor.FromUInt32`.
