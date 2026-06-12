# Shiny Controls — Theme Token Reference & Migration Guide

This is the canonical contract that controls consume. Tokens are generated from `/themes/*.json`
by `tools/ShinyThemeGen`. **Blazor** consumes CSS custom properties; **MAUI** consumes
`ResourceDictionary` keys (via `ShinyThemeKeys`) bound with `SetDynamicResource`.

## Golden rules

1. **Always keep a fallback.** Blazor: `var(--shiny-color-x, #originalHex)` — keep the *exact*
   original hex as the fallback so the default look is unchanged when no theme stylesheet is linked.
2. **Don't tokenize content colors.** Never replace colors that represent user data or previews:
   the ColorPicker spectrum/swatch values, ImageEditor pixel/brush sample colors, or any inline
   `style` that paints a user-chosen color. Chrome (borders, backgrounds, toolbars) is fine.
3. **Leave `transparent` / `Colors.Transparent` alone.** They are not theme colors.
4. **Map by role, not by hue.** A gray border → `outline-variant`, not "some gray". See table below.

## Color tokens

Blazor CSS var (`--shiny-color-…`) ↔ MAUI key (`ShinyThemeKeys.Color.…`). Same logical role:

| CSS var | MAUI key | Use for |
|---|---|---|
| `--shiny-color-primary` | `Primary` | Brand/accent fills (FAB, primary buttons, active bar) |
| `--shiny-color-on-primary` | `OnPrimary` | Text/icons on `primary` |
| `--shiny-color-primary-container` | `PrimaryContainer` | Softer accent fills |
| `--shiny-color-on-primary-container` | `OnPrimaryContainer` | Text on `primary-container` |
| `--shiny-color-secondary` / `-container`, `on-…` | `Secondary…` | Secondary accents |
| `--shiny-color-tertiary` / `-container`, `on-…` | `Tertiary…` | Tertiary accents |
| `--shiny-color-error` / `-container`, `on-…` | `Error…` | Validation/error (form fields) |
| `--shiny-color-background` | `Background` | Page background |
| `--shiny-color-on-background` | `OnBackground` | Text on background |
| `--shiny-color-surface` | `Surface` | Component/card/sheet background |
| `--shiny-color-on-surface` | `OnSurface` | Primary text/icons |
| `--shiny-color-surface-variant` | `SurfaceVariant` | Muted surfaces, slider tracks |
| `--shiny-color-on-surface-variant` | `OnSurfaceVariant` | Secondary text, placeholders, muted icons |
| `--shiny-color-surface-container-lowest/-low/-/-high/-highest` | `SurfaceContainerLowest…Highest` | Elevated surfaces, hover rows, skeleton base |
| `--shiny-color-surface-tint` | `SurfaceTint` | Tonal elevation tint |
| `--shiny-color-outline` | `Outline` | Borders, dividers (prominent) |
| `--shiny-color-outline-variant` | `OutlineVariant` | Subtle borders, separators, gridlines |
| `--shiny-color-shadow` | `Shadow` | Shadow color (usually black) |
| `--shiny-color-scrim` | `Scrim` | Modal/overlay backdrops (usually black) |
| `--shiny-color-inverse-surface` | `InverseSurface` | Toast/snackbar background |
| `--shiny-color-inverse-on-surface` | `InverseOnSurface` | Text on inverse surface |
| `--shiny-color-inverse-primary` | `InversePrimary` | Accent on inverse surface |
| `--shiny-color-success` / `-container`, `on-…` | `Success…` | Positive status |
| `--shiny-color-info` / `-container`, `on-…` | `Info…` | Informational status |
| `--shiny-color-warning` / `-container`, `on-…` | `Warning…` | Warning status |
| `--shiny-color-caution` / `-container`, `on-…` | `Caution…` | Caution status |
| `--shiny-color-critical` / `-container`, `on-…` | `Critical…` | Critical/danger status |

Status pattern (Pill/Toast/Badge): **container** = soft fill background, **on-container** = text on
that fill, **role** (e.g. `success`) = the vivid border/accent.

## Shape / elevation / state / type / spacing

| CSS var | MAUI key | Notes |
|---|---|---|
| `--shiny-shape-corner-{none,extra-small,small,medium,large,extra-large,full}` | `Shape.Corner…` | px / double |
| `--shiny-elevation-{1..5}` | `Elevation.Level1..3` (MAUI `Shadow`) | Blazor = `box-shadow` string |
| `--shiny-state-{hover,focus,pressed,dragged}-opacity` | `State.…Opacity` | 0..1 |
| `--shiny-type-{role}-{size,line-height,weight,tracking}` | `Type.{Role}Size` | role = display/headline/title/body/label-large/medium/small |
| `--shiny-spacing-{0..8}` | `Spacing.Space0..8` | 0,4,8,12,16,24,32,48,64 px |

## Common hardcoded value → token cheatsheet

| Original | Token (role) |
|---|---|
| `#FFFFFF`/`white` as a **surface/card bg** | `surface` (or `surface-container-lowest`) |
| `#FFFFFF`/`white` as **text on a colored fill** | the matching `on-*` |
| `#000000`/`black` **text** | `on-surface` |
| `#000000` **overlay/backdrop** | `scrim` |
| `#2196F3` `#3B82F6` `#007AFF` (brand blue) | `primary` |
| `#DC2626` `#EF4444` (red badge/error) | `error` (or `critical`) |
| `#E5E7EB` `#E1E1E1` `#E0E0E0` (light divider/track) | `outline-variant` (border) or `surface-container-highest` (fill) |
| `#D1D5DB` `#CCC` (border) | `outline-variant` |
| `#9CA3AF` `#6B7280` `#888` (muted text/icon) | `on-surface-variant` |
| `#374151` `#1F2937` `#212121` (strong text/track) | `on-surface` |
| `#111827` (near-black text) | `on-surface` |
| `#F3F4F6` `#F9FAFB` (subtle fill/hover) | `surface-container-high` / `surface-container` |
| `#E3F2FD` `#DBEAFE` `#EFF6FF` (selected/highlight) | `secondary-container` or `primary-container` |
| Status sets (success green / info blue / warn yellow / danger red) | matching `*-container` + `on-*-container` + role |
| `rgba(0,0,0,0.x)` hover veil | keep, or `rgba(0,0,0,var(--shiny-state-hover-opacity))` |

## MAUI migration pattern (from `Fab.cs` / `PillView.cs`)

`Color` properties (BackgroundColor, TextColor, label TextColor):
```csharp
view.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
label.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnPrimary);
```

`Brush` properties (Border.Stroke, Background) — drive a `SolidColorBrush`'s Color so theme swaps propagate:
```csharp
var brush = new SolidColorBrush();
brush.SetDynamicResource(SolidColorBrush.ColorProperty, ShinyThemeKeys.Color.Outline);
border.Stroke = brush;
```

Color `BindableProperty` defaults: change the default to `null` and in `propertyChanged`, apply the
explicit `Color` when set, otherwise re-apply the `SetDynamicResource` (so clearing returns to theme).
Add `using Shiny.Maui.Controls.Themes;`. Don't tokenize `Colors.Transparent`.

After editing, the keys live in `src/Shiny.Maui.Controls/Themes/Generated/ShinyThemeKeys.cs`.
