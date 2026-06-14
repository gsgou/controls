# CLAUDE.md

Guidance for working in the **Shiny Controls** repo (`Shiny.Maui.Controls` + `Shiny.Blazor.Controls` and their add-on packages).

## Repo layout

- `src/` — one project per package per host (e.g. `Shiny.Maui.Controls`, `Shiny.Blazor.Controls`, plus add-ons: `*.Camera`, `*.Barcodes`, `*.Markdown`, `*.MermaidDiagrams`, `*.Desktop`, `*.Kiosk`, camera analyzers `Camera.Barcode/Documents/Face/Motion/Ocr`, themes `Themes.Material/Ocean`).
- `samples/Sample/` — the MAUI + Blazor demo app. Each control has a feature page under `samples/Sample/Features/<Area>/`, wired into `AppShell.xaml` and `MauiProgram.cs`.
- `tests/` — unit tests.
- `SKILLS/shiny-controls/` — the **local skill** (`SKILL.md` + one markdown file per control) that teaches code generation for these controls.
- `README.md` — the package-level overview (top-of-file summary paragraph + per-control sections + NuGet badges).
- `themes/` — M3 theme pack seeds.

## Documentation site

The public docs live in a **separate repo**: `~/Desktop/dev/documentation` (Astro / Starlight).

- Controls docs: `src/content/docs/controls/<control>/`
- Controls release notes: `src/content/docs/controls/release-notes.mdx`
- Main menu (sidebar): `src/sidebar-topics.mjs` (the `Controls` topic, ~line 289). The homepage menu (`HomepageNav.astro`) is **auto-generated from this file** — no separate edit needed.
- Homepage: `src/content/docs/index.mdx` — the **"UI Controls"** `<Card>` (~line 149) lists every control grouped by category (Flagship / Layout & Overlays / Input / Display & Media / Status & Feedback / Desktop).

## Required updates for EVERY fix & feature

With each fix and each new feature, update all of the following so they stay in sync:

1. **README.md** — reflect new/changed behavior; add a NuGet badge + section if it's a new package.
2. **Local skill** (`SKILLS/shiny-controls/`) — update the relevant control's `.md` (or add a new one and reference it in `SKILL.md`) so generated code matches.
3. **Shiny docs** (`~/Desktop/dev/documentation`):
   - **Release notes** — add an entry to `src/content/docs/controls/release-notes.mdx`.
   - **Menu** — for a **new feature**, add the new menu node(s) under the `Controls` topic in `src/sidebar-topics.mjs`. (The homepage menu updates automatically from this.)

### Additionally, if the control itself is NEW

4. Add its docs folder under `src/content/docs/controls/<control>/`.
5. Add it to the **homepage section** — the "UI Controls" `<Card>` in `src/content/docs/index.mdx` (place it in the appropriate category group).
6. Add its top-level node to the **main menu** (`src/sidebar-topics.mjs`) under the `Controls` topic — which also surfaces it in the homepage menu.

## Conventions

- Keep MAUI and Blazor at feature parity where the platform allows; note platform-only features explicitly (Desktop is MAUI-only; SheetView/Kiosk are Blazor-specific, etc.).
- Add/update a demo page in `samples/Sample/Features/` for any new control or notable feature.
- Build with `dotnet build Build.slnf`.
