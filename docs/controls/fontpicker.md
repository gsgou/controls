# FontPicker

[← All Shiny Controls](../../README.md)

Font family and font size picker controls, on both MAUI and Blazor. Includes inline list (`FontPicker`, `FontSizePicker`) and popup button (`FontPickerButton`, `FontSizePickerButton`) variants. Each font is rendered in its own typeface for instant visual preview, and each size at the size it names. The Blazor `FontSizePickerButton` also lets a size be typed, since the list is a set of common sizes rather than the set of legal ones.

```xml
<shiny:FontPickerButton AvailableFonts="{Binding Fonts}"
                        SelectedFont="{Binding SelectedFont, Mode=TwoWay}"
                        Placeholder="Font" />

<shiny:FontSizePickerButton AvailableFontSizes="{Binding Sizes}"
                            SelectedFontSize="{Binding SelectedSize, Mode=TwoWay}" />
```

**FontPicker / FontPickerButton:**

| Property | Type | Default | Description |
|---|---|---|---|
| AvailableFonts | IList\<string\>? | null | Font family names to display |
| SelectedFont | string? | null | Currently selected font (TwoWay) |
| PreviewText | string | "The quick brown fox" | Text rendered in each font row |
| PreviewFontSize | double | 18 | Size of preview text |
| Placeholder | string | "Font" | Button placeholder (button only) |
| CornerRadius | int | 8 | Button corner radius (button only) |
| FontChangedCommand | ICommand? | null | Command on selection (button only) |

**FontSizePicker / FontSizePickerButton:**

| Property | Type | Default | Description |
|---|---|---|---|
| AvailableFontSizes | IList\<double\>? | null | Font sizes to display |
| SelectedFontSize | double | 16 | Currently selected size (TwoWay) |
| PreviewText | string | "Aa" | Text rendered at each size |
| CornerRadius | int | 8 | Button corner radius (button only) |
| FontSizeChangedCommand | ICommand? | null | Command on selection (button only) |

Blazor has the same four, with the parameter names unchanged, taking `IReadOnlyList<T>` where MAUI
takes `IList<T>` and an `EventCallback` where MAUI takes an `ICommand`:

```razor
<FontPickerButton AvailableFonts="fonts" @bind-SelectedFont="family" />
<FontSizePickerButton AvailableFontSizes="sizes" @bind-SelectedFontSize="size" />
```

The Blazor `FontSizePickerButton` puts a text field beside the caret so a size that is not on the list —
13pt, say — can be typed; a value that will not parse, or falls outside `MinimumFontSize`/
`MaximumFontSize`, snaps back rather than being committed.

## Sizing the button

On MAUI the trigger shrink-wraps its label, down to a minimum width, so a button dropped into a stack
does not stretch across the row. Set a `WidthRequest` and the trigger fills it instead — which is what
a toolbar sizing several controls to a common width wants. Without that it would draw at its minimum
inside the width you asked for, and the remainder would show as a gap beside the button.

```xml
<shiny:FontPickerButton WidthRequest="150" />
```

These controls are also integrated into the **ImageEditor** toolbar when `AllowFontSelection` and `AllowFontSizeSelection` are enabled, and into both **Office editor** toolbars on both hosts.
