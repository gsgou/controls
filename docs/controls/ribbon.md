# Ribbon

[← All Shiny Controls](../../README.md)

The desktop ribbon: a strip of tabs over a body of titled command groups, in the shape Office made the
convention for applications with more commands than a toolbar can hold. Tabs, groups, large and small
buttons, toggles, split and menu buttons, contextual tabs, a quick access row, a collapsing body, and
groups that fold themselves into buttons when the window gets narrow.

It ships in `Shiny.Maui.Controls.Desktop` on MAUI and in the core `Shiny.Blazor.Controls` package on
Blazor — the same split docking uses, and for the same reason: Blazor has no desktop add-on.

```bash
dotnet add package Shiny.Maui.Controls.Desktop     # MAUI  (Windows, macOS, Mac Catalyst, Linux)
dotnet add package Shiny.Blazor.Controls           # Blazor
```

The ribbon needs no registration on either host — it is markup, not a service.

> **A desktop control, and only nominally a cross-platform one.** It wants a pointer to hover with and
> enough width for three rows of small commands. Nothing stops it running on a phone, but a phone
> should have a [`ShinyToolbar` or `ShinyTabBar`](toolbar-tabbar.md) instead.

---

## The shape

Everything is authored declaratively — nested elements on MAUI, nested components on Blazor:

```
Ribbon
└── RibbonTab            "Home", "Insert", "Review" — one row of the strip
    └── RibbonGroup      "Clipboard", "Font" — a titled box of related commands
        └── items        buttons, toggles, split/menu buttons, separators, your own content
```

### MAUI

```xml
<shiny:Ribbon ApplicationButtonText="File"
              ApplicationButtonCommand="{Binding OpenFileMenu}"
              DisplayMode="{Binding DisplayMode}">

    <shiny:Ribbon.QuickAccessItems>
        <shiny:RibbonButton Text="Save" Size="Small" Icon="save.png" Command="{Binding Save}" />
        <shiny:RibbonButton Text="Undo" Size="Small" Icon="undo.png" Command="{Binding Undo}" />
    </shiny:Ribbon.QuickAccessItems>

    <shiny:RibbonTab Title="Home" Key="home">

        <shiny:RibbonGroup Title="Clipboard" Priority="30">
            <shiny:RibbonSplitButton Text="Paste" Icon="paste.png" Command="{Binding Paste}">
                <shiny:RibbonMenuEntry Text="Keep source formatting" Command="{Binding PasteKeep}" />
                <shiny:RibbonMenuEntry Text="Text only" Command="{Binding PasteText}" />
                <shiny:RibbonMenuEntry IsSeparator="True" />
                <shiny:RibbonMenuEntry Text="Paste special…" Command="{Binding PasteSpecial}" />
            </shiny:RibbonSplitButton>

            <shiny:RibbonButton Text="Cut"  Size="Small" Icon="cut.png"  Command="{Binding Cut}" />
            <shiny:RibbonButton Text="Copy" Size="Small" Icon="copy.png" Command="{Binding Copy}" />
        </shiny:RibbonGroup>

        <shiny:RibbonGroup Title="Font" ShowDialogLauncher="True"
                           DialogLauncherCommand="{Binding OpenFontDialog}">
            <shiny:RibbonToggleButton Text="Bold"   Size="Small" Icon="bold.png"   IsChecked="{Binding Bold}" />
            <shiny:RibbonToggleButton Text="Italic" Size="Small" Icon="italic.png" IsChecked="{Binding Italic}" />

            <shiny:RibbonSeparator />

            <!-- any view at all can sit in a group -->
            <shiny:RibbonContentItem Size="Small">
                <shiny:FontSizePicker SelectedFontSize="{Binding FontSize}" WidthRequest="86" />
            </shiny:RibbonContentItem>
        </shiny:RibbonGroup>
    </shiny:RibbonTab>
</shiny:Ribbon>
```

`xmlns:shiny="http://shiny.net/maui/controls"` — the same prefix as the core controls; the ribbon is
mapped onto it from the Desktop assembly so a XAML author never has to know there are two.

### Blazor

```razor
@using Shiny.Blazor.Controls

<Ribbon ApplicationButtonText="File"
        ApplicationButtonClicked="OpenFileMenu"
        @bind-DisplayMode="mode"
        @bind-SelectedKey="tab">

    <QuickAccess>
        <RibbonButton Size="RibbonItemSize.Small" Text="Save" Icon="@SaveSvg" Clicked="Save" />
        <RibbonButton Size="RibbonItemSize.Small" Text="Undo" Icon="@UndoSvg" Clicked="Undo" />
    </QuickAccess>

    <ChildContent>
        <RibbonTab Title="Home" Key="home">

            <RibbonGroup Title="Clipboard" Priority="30">
                <RibbonSplitButton Text="Paste" Icon="@PasteSvg" Clicked="Paste" Menu="@pasteMenu" />
                <RibbonButton Size="RibbonItemSize.Small" Text="Cut"  Icon="@CutSvg"  Clicked="Cut" />
                <RibbonButton Size="RibbonItemSize.Small" Text="Copy" Icon="@CopySvg" Clicked="Copy" />
            </RibbonGroup>

            <RibbonGroup Title="Font" ShowDialogLauncher="true" DialogLauncherClicked="OpenFontDialog">
                <RibbonToggleButton Size="RibbonItemSize.Small" Text="Bold"   Icon="@BoldSvg"   @bind-Checked="bold" />
                <RibbonToggleButton Size="RibbonItemSize.Small" Text="Italic" Icon="@ItalicSvg" @bind-Checked="italic" />

                <RibbonSeparator />

                <RibbonContent Size="RibbonItemSize.Small">
                    <select @bind="fontSize">…</select>
                </RibbonContent>
            </RibbonGroup>
        </RibbonTab>
    </ChildContent>
</Ribbon>

@code {
    List<RibbonMenuEntry> pasteMenu = new()
    {
        new() { Text = "Keep source formatting", OnClick = … },
        new() { Text = "Text only", OnClick = … },
        new() { IsSeparator = true },
        new() { Text = "Paste special…", OnClick = … }
    };
}
```

> `<QuickAccess>` and `<ChildContent>` have to be named once you use either — that is Blazor's rule for
> a component with more than one `RenderFragment`, not the ribbon's.

---

## Columns fall out of the sizes

Nothing declares a column. A `Large` item takes one to itself — icon over label, the shape a primary
command wants — and `Small` items stack up to `SmallItemRows` (three, the ribbon convention) deep in a
shared column. A `RibbonSeparator`, or a large item, ends the current column and starts a fresh one.

```
┌──────────┬──────────────────┬─────────┐
│          │ Cut              │         │   Paste = Large  → its own column
│  Paste   │ Copy             │ Bullets │   Cut/Copy/Format = three Small → one column
│    ▾     │ Format painter   │         │   Bullets = Large → the next column
└──────────┴──────────────────┴─────────┘
```

That is the whole of a ribbon's layout language, and it is why reordering a group's items re-flows it
with nothing else touched. On MAUI the columns are built in code; on Blazor they are a CSS grid with
`grid-auto-flow: column`, which places items down a column and only then moves across.

## Item kinds

| Kind | What it is |
|---|---|
| `RibbonButton` | A plain command. `Command`/`CommandParameter` and `Clicked` on MAUI, `Clicked` on Blazor |
| `RibbonToggleButton` | Stays pressed. `IsChecked` (MAUI) / `Checked` (Blazor) is two-way; bind it and skip the handler |
| `RibbonSplitButton` | Face runs the default action, chevron opens the dropdown |
| `RibbonMenuButton` | The whole face opens the dropdown; no default action |
| `RibbonSeparator` | A full-height rule, and a break in the column flow |
| `RibbonContentItem` (MAUI) / `RibbonContent` (Blazor) | Hosts arbitrary content — a picker, a combo, a swatch strip |

Every item carries `Text`, `Icon`, `Tooltip`, `Description`, `Size`, and enabled/visible flags.

**Dropdown entries** are `RibbonMenuEntry`: `Text`, `Icon`, `IsChecked` (draws a tick), `IsSeparator`,
and nestable `Children` that fly out as a submenu. They are declared as markup children on MAUI and
passed as a `List<RibbonMenuEntry>` on Blazor — the same split `ShinyToolbar` already uses, because
XAML has no comfortable way to write a nested object graph inline and Razor does.

## Icons

- **MAUI** — `Icon` is an `ImageSource`, so a PNG, an SVG, or a `FontImageSource` glyph all work. For
  an icon that is *drawn* rather than loaded, set `IconTemplate` to a `DataTemplate` returning a view;
  it wins over `Icon` and is instantiated per drawn button, so it must not return a shared instance.
- **Blazor** — `Icon` is a string: inline SVG/HTML markup, an image URL, or a glyph. Exactly what a
  `ToolbarItem` takes, so one icon convention covers both controls.

## Enabled and visible

Bind `IsEnabled`/`Disabled` rather than removing an item: a command that disappears when it cannot run
makes the bar move under the pointer. A group can dim its whole contents in one place — `IsEnabled` on
MAUI, `Disabled` on Blazor — without every item having to be bound.

---

## Contextual tabs

Nothing special declares one. Setting `ContextTitle` captions the coloured band above the strip and
marks the tab contextual; binding its visibility to whatever the tab is *about* is what makes it come
and go:

```xml
<shiny:RibbonTab Title="Format" Key="picture"
                 ContextTitle="Picture Tools"
                 IsVisible="{Binding PictureSelected}">
```
```razor
<RibbonTab Title="Format" Key="picture" ContextTitle="Picture Tools" Visible="@pictureSelected">
```

When the showing tab stops being selectable — hidden, disabled or removed — the ribbon falls back to
the nearest tab that still is, so a vanished selection never leaves an empty body. MAUI reports that
as `RibbonTabChangeReason.Fallback` on `TabChanged`; Blazor as the same reason on
`RibbonTabChangedEventArgs`.

A contextual tab underlines in `ContextColor` (the theme's tertiary by default) rather than the accent
the permanent tabs use, so it reads as a different kind of thing rather than the selected one of the
same kind.

## Collapsing the ribbon

`DisplayMode` is two-way on both hosts:

| Mode | |
|---|---|
| `Expanded` | Tab strip plus the open body. The default |
| `Collapsed` | Only the strip. Picking a tab peeks the body back; the next command puts it away again |
| `Simplified` | One dense row — every item drawn small, group titles dropped |

The chevron at the trailing end of the strip toggles Expanded ⇄ Collapsed, and so does a second click
on the tab already showing. `AllowCollapse="false"` removes both; `DisplayMode` still works from code.

## Groups that do not fit

When the showing tab is wider than the bar, groups fold into a single button that opens the whole group
in a popup — lowest `Priority` first, rightmost breaking ties. Items are never dropped individually,
because half a group is worse than a closed one. Raise `Priority` on the groups that should survive
longest, and set `CanCollapse="false"` on one that must stay open. `AllowGroupCollapse="false"` turns
the whole behaviour off and lets the body scroll horizontally instead, which is the better answer when
every group is small.

The decision needs real measured widths, so on Blazor it is made in `ribbon.js` and handed back to the
component. It degrades cleanly: with no JS module — prerendering, a locked-down host — every group
stays open and the body scrolls.

---

## Ribbon reference

| Property | MAUI | Blazor | Description |
|---|---|---|---|
| Tabs | `Tabs` (content property) | `ChildContent` | The `RibbonTab` children |
| Quick access | `QuickAccessItems` | `QuickAccess` fragment | Small icon commands pinned to the strip's trailing end |
| Selection | `SelectedIndex`, `SelectedTab` (two-way), `SelectTab(key)` | `SelectedKey` (two-way) | Which tab is showing |
| `DisplayMode` | two-way | two-way | Expanded / Collapsed / Simplified |
| `AllowCollapse` | ✓ | ✓ | Offer the collapse chevron and the second-click gesture. Default true |
| `AllowGroupCollapse` | ✓ | ✓ | Let groups fold into buttons. Default true |
| `ShowGroupTitles` | ✓ | ✓ | Draw each group's caption. Default true |
| `ShowTabStrip` | ✓ | ✓ | Draw the strip at all — false gives a single-tab ribbon that is really a toolbar |
| `SmallItemRows` | ✓ | ✓ | How deep small items stack before a new column. Default 3 |
| Application button | `ApplicationButtonText`, `ApplicationButtonCommand` | `ApplicationButtonText`, `ApplicationButtonClicked` | The accented "File" button at the head of the strip. Null leaves it out |
| Colours | `AccentColor`, `HeaderBackgroundColor`, `BodyBackgroundColor` | same, as CSS colour strings | Fall back to the theme |
| `ShowTooltips` | ✓ | — | MAUI uses the Shiny `Tooltip` control; Blazor uses the browser's own `title` |

**Events** — MAUI: `TabChanged`, `ItemInvoked`, `GroupDialogLauncherClicked`, `ApplicationButtonClicked`.
Blazor: `TabChanged`, `SelectedKeyChanged`, `DisplayModeChanged`, `MenuEntrySelected`,
`ApplicationButtonClicked`, plus each item's own callback.

**MAUI also has `ribbon.Invoke(item)`** — press an item from code, running its command, flipping a
toggle and raising `ItemInvoked` exactly as a click would. It exists because a keyboard shortcut and
the button it duplicates should go down one path rather than two, and because a `TapGestureRecognizer`
cannot be raised from a test.

### RibbonTab

`Title`, `Key`, visibility (`IsVisible` / `Visible`), enabled (`IsEnabled` / `Enabled`), `ContextTitle`,
`ContextColor`, and the `RibbonGroup` children. `Key` falls back to `Title`.

### RibbonGroup

`Title`, `Priority`, `CanCollapse`, disabled, `ShowDialogLauncher` + its command/callback and tooltip,
`CollapsedIcon`, and the item children. The dialog launcher is the small corner arrow — the convention
for "there is more of this than fits here".

---

## Theming

Both hosts follow the [Shiny theme](styling.md) with no configuration: MAUI resolves
`ShinyThemeKeys.Color.*` through dynamic resources, Blazor through the `--shiny-color-*` custom
properties, and both flip with light/dark. `AccentColor`, `HeaderBackgroundColor` and
`BodyBackgroundColor` override just those three; everything else stays on the theme.

## Platform notes

- **Dropdowns** are drawn above the page rather than inside the bar, which is what stops a menu being
  clipped by a body only three rows tall. On Blazor that is the browser's top layer via the popover
  API, with Escape and a click-away both closing the panel. On MAUI it is the shared page overlay.
- **macOS AppKit (`net10.0-macos`)** gives no native view to a child added after the page has been laid
  out. The ribbon is built for that: every tab's body is created up front and switched with
  `IsVisible`, so tab switching, collapsing and group folding all work. Adding a tab, group or item
  *after* the fact does rebuild, and the dropdown panels are added on demand — those are the two paths
  that head cannot follow. Every other desktop target is unaffected.
- **Collapsed group buttons** fall back to the first item's icon on MAUI; on Blazor set `CollapsedIcon`
  explicitly, since a group's items are components it cannot read an icon out of.
- **Key tips** (the Alt-key letter badges) are not implemented on either host.

## Samples

- MAUI — `samples/Sample/Features/Ribbon/` (the **Ribbon** page in the demo app)
- Blazor — `samples/Sample.Blazor/Pages/RibbonPage.razor` (`/ribbon`)

Both build the same bar — Home / Insert / View plus a contextual Picture Tools tab — with switches for
the display mode, the contextual tab and group collapsing, and a log of every command the bar raises.
