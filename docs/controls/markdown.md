# Markdown Controls

[← All Shiny Controls](../../README.md)

> Separate NuGet packages: `Shiny.Maui.Controls.Markdown` / `Shiny.Blazor.Controls.Markdown`

Render and edit markdown content using native MAUI controls — no WebView required on MAUI. Auto-resolves Light/Dark theming. Available for both MAUI and Blazor.

| Viewer | Editor |
|:---:|:---:|
| ![Viewer](../../assets/markdown-view.png) | ![Editor](../../assets/markdown-editor.png) |

**MarkdownView** — Read-only markdown renderer:

```xml
<md:MarkdownView Markdown="{Binding DocumentContent}" Padding="16" />
```

| Property | Type | Description |
|---|---|---|
| Markdown | string | Markdown content to render |
| Theme | MarkdownTheme? | Rendering theme (auto Light/Dark if null) |
| IsScrollEnabled | bool | Enable/disable scrolling (default: true) |

Events: `LinkTapped` — fired when a link is tapped; set `Handled = true` to prevent default browser launch.

**MarkdownEditor** — Editor with formatting toolbar and live preview:

```xml
<md:MarkdownEditor Markdown="{Binding NoteContent, Mode=TwoWay}"
                   Placeholder="Start writing..."
                   Padding="8" />
```

| Property | Type | Description |
|---|---|---|
| Markdown | string | Markdown content (TwoWay) |
| Theme | MarkdownTheme? | Preview theme (auto Light/Dark if null) |
| Placeholder | string | Placeholder text |
| ToolbarItems | IReadOnlyList\<MarkdownToolbarItem\>? | Toolbar buttons (default set provided) |
| IsPreviewVisible | bool | Toggle preview pane (TwoWay) |
| ToolbarBackgroundColor | Color? | Toolbar background |
| EditorBackgroundColor | Color? | Editor background |
| ShowToolbarInKeyboard | bool | Repeat `ToolbarItems` on the keyboard accessory bar (default `true`; MAUI iOS/Android) |
| Accessory | KeyboardAccessoryView? | Your own accessory bar instead of the generated one |

**Keyboard toolbar (MAUI, iOS + Android)** — on a phone the toolbar above the editor is covered by the
soft keyboard the moment you start typing, so the same `ToolbarItems` are also rendered as icons on a
`KeyboardAccessoryView` docked to the top of the keyboard: a horizontally scrolling row of the enabled
items (grouped exactly like the toolbar) with a pinned **Done** — the only way to dismiss a multi-line
keyboard, whose return key inserts a newline. On by default; set `ShowToolbarInKeyboard="False"` to opt
out, or set `Accessory` to supply your own bar.

```xml
<md:MarkdownEditor Markdown="{Binding NoteContent, Mode=TwoWay}"
                   ToolbarItems="{Binding MyItems}"
                   ShowToolbarInKeyboard="True" />
```

**Features:**
- Formatting toolbar: bold, italic, headings, lists, code, links, blockquotes
- The same toolbar on the keyboard accessory bar (iOS/Android)
- Live preview toggle
- Auto-growing editor
- Full Markdig support: tables, task lists, strikethrough, fenced code blocks
- Customizable themes with colors, font sizes, and spacing
- Custom toolbar item support

## Dark mode

Left unset, `Theme` resolves to `MarkdownTheme.Themed` — every colour in it is a Shiny theme token,
so the rendered markdown follows the app's scheme *and* its theme pack. `MarkdownTheme.Light` and
`MarkdownTheme.Dark` stay literal palettes, for a preview that has to look the same whatever the app
is doing. See [Styling & theming](styling.md#dark-mode).
