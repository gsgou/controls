# FrostedGlassView

[← All Shiny Controls](../../README.md)

A view that applies a native frosted glass (blur) effect behind its content. Place over images or busy backgrounds for a glassmorphism effect.

```xml
<shiny:FrostedGlassView BlurRadius="20"
                        TintColor="#80FFFFFF"
                        TintOpacity="0.6"
                        CornerRadius="16">
    <VerticalStackLayout Padding="20" Spacing="8">
        <Label Text="Glass Card" FontSize="20" FontAttributes="Bold" />
        <Label Text="Content over blurred background." FontSize="14" />
    </VerticalStackLayout>
</shiny:FrostedGlassView>
```

```razor
<!-- Blazor -->
<FrostedGlass BlurRadius="20" TintColor="rgba(255,255,255,0.6)" CornerRadius="16">
    <h3>Glass Card</h3>
    <p>Content over blurred background.</p>
</FrostedGlass>
```

| Property | Type | Default | Description |
|---|---|---|---|
| GlassContent / ChildContent | View / RenderFragment | - | Content rendered on top of the glass |
| BlurRadius | double | 20 | Blur strength in pixels |
| TintColor | Color / string | #80FFFFFF / rgba(255,255,255,0.6) | Glass tint overlay |
| TintOpacity | double | 0.6 | Tint opacity (MAUI only) |
| CornerRadius | double | 0 | Corner radius for clipping |

**Platform implementation:** iOS uses `UIVisualEffectView`, Android 12+ uses `RenderEffect.CreateBlurEffect`, Blazor uses CSS `backdrop-filter: blur()`.
