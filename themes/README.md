# Shiny Controls — Theming

Shiny Controls ship a central, Material-3-style theming system shared by **MAUI** and **Blazor**.
A theme is a set of design tokens (color roles, shape, elevation, type scale, state, spacing).
The core packages define the token contract and a built-in **Basic** theme; additional themes
(**Ocean**, **Material**) install as separate NuGet packs.

## How it works

- **Single source of truth:** each theme is a small JSON file in `/themes/` describing ~11 seed
  colors. `tools/ShinyThemeGen` expands those into the full Material-3 tonal role set (light + dark)
  and emits the platform assets:
  - **MAUI** → C# `ResourceDictionary` classes (`{Name}LightTheme` / `{Name}DarkTheme` / `{Name}Theme`)
  - **Blazor** → a CSS file of `--shiny-*` custom properties (`:root` light + `.shiny-theme-dark` / `prefers-color-scheme`)
- Controls consume tokens, so a theme restyles the whole control set:
  - **MAUI** controls bind colors with `SetDynamicResource(…, ShinyThemeKeys.Color.X)`.
  - **Blazor** controls reference `var(--shiny-color-x, <fallback>)` — the original value is kept as the
    fallback, so controls look correct even with no theme stylesheet linked.

Regenerate after editing any `/themes/*.json`:

```bash
dotnet run --project tools/ShinyThemeGen
```

See `token-reference.md` for the full token list and the hardcoded-color → token cheatsheet.

## Using a theme — MAUI

Basic is applied automatically by `UseShinyControls()`. To use a pack, install it and select it:

```csharp
// dotnet add package Shiny.Maui.Controls.Themes.Ocean
builder.UseShinyControls(cfg => cfg.UseOceanTheme());   // or .UseMaterialTheme() / .UseBasicTheme()
```

Switch at runtime (light/dark follows the OS automatically and hot-swaps):

```csharp
ShinyThemeManager.SetTheme(new OceanTheme());
Application.Current.UserAppTheme = AppTheme.Dark;       // flips to the dark scheme live
```

Explicitly set control colors (e.g. `Fab.FabBackgroundColor`) still override the theme.

## Using a theme — Blazor

The core Basic stylesheet ships in `Shiny.Blazor.Controls`. Link it in `index.html`:

```html
<link href="_content/Shiny.Blazor.Controls/css/shiny-theme.css" rel="stylesheet" />
```

To use a pack, install it and link its stylesheet **after** the core one (it overrides `:root`):

```html
<!-- dotnet add package Shiny.Blazor.Controls.Themes.Ocean -->
<link href="_content/Shiny.Blazor.Controls.Themes.Ocean/css/shiny-theme-ocean.css" rel="stylesheet" />
```

Dark mode follows the OS by default. Force it by adding `shiny-theme-dark` (or `shiny-theme-light`)
to `<html>` or any container; the tokens cascade to that subtree.

## Authoring a new theme

1. Copy `basic.json` to `myteam.json`, set `name`/`slug`, and tweak the 11 seed colors.
2. Run the generator. New slugs emit to `src/Shiny.{Maui,Blazor}.Controls.Themes.{Name}/`.
3. Add the two project files (model them on the Ocean pack) and register in `Shiny.Controls.slnx` / `Build.slnf`.

A visual theme creator on the docs site (which exports this same JSON) is planned.
