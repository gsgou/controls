# Styling & theming

[← All Shiny Controls](../../README.md)

How implicit styles, theme packs and the *unset* appearance defaults apply to every control in this folder.

**Styling (MAUI)** — every control can be targeted by an implicit or explicit `Style`, so
app-wide theming is set once rather than repeated at each usage site:

```xml
<!-- App.xaml -->
<Style TargetType="shiny:PillView">
    <Setter Property="CornerRadius" Value="10" />
    <Setter Property="FontSize" Value="12" />
</Style>
```

Leave a colour property unset to inherit the active Shiny theme; setting one explicitly
overrides the theme default for that instance — permanently. A `ChatView` with
`MyBubbleColor="#DCF8C6"` keeps that green through every `ShinyThemeManager.SetTheme` call, so omit
the property unless you mean to pin it.

**What visibly changes when you swap theme packs.** A theme is not just a palette. Alongside the
colour seeds it can define its own **shape** (corner geometry), **typography** (family, scale,
weight, tracking), **elevation** (`shadow` / `flat` / `outline` / `glow`, with separate intensity
and softness), **density** (spacing ramp and control metrics) and **border widths** — so a pack
restyles the geometry and type of every control, not only its hue. Terminal is square, monospace
and dense with hairline rings instead of shadows; Ocean is soft, roomy and barely-shadowed; Aurora
glows instead of casting shadows. Colour alone would not carry that: the neutral ramp is nearly
identical between packs because a tone-98 near-white has no room to hold a hue, which is why a
palette-only theme used to leave most controls looking the same.

Numeric appearance properties that a theme owns (`CornerRadius`, `FontSize`, stroke widths)
default to `-1`, meaning *unset — let the theme decide*. A literal default would have been written
to the control at construction and beaten the theme permanently rather than merely by default.
Setting one to a real value still pins it, as before.

**Watch out for implicit `BoxView` styles.** The .NET MAUI project template ships
`<Style TargetType="BoxView">` with a setter for **`BackgroundColor`** — which paints an opaque
rectangle *behind* the shape rather than setting the `BoxView`'s own `Color`. Because it is
implicit it applies app-wide, including to `BoxView`s inside controls, and it turns
`<BoxView Color="Transparent" />` spacers into solid dark bars, puts dark corners behind rounded
shapes, and hides gradient `Background`s. Prefer an empty `<Grid HeightRequest="..." />` for
spacers, and if you want a default separator colour set `Color`, not `BackgroundColor`.
