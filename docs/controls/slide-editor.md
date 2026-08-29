# Slide Editor

[← All Shiny Controls](../../README.md)

> Same packages as the viewers. Two controls: `SlideEditor` is the lone editing surface;
> `SlideEditorView` is the same thing plus an editing toolbar.

```csharp
using var deck = await SlideDeck.OpenAsync("deck.pptx", editable: true);
```

```razor
<div style="height:560px">
    <SlideEditorView Deck="deck" @bind-SlideIndex="index" DeckChanged="OnChanged" />
</div>
```

```xml
<office:SlideEditorView Deck="{Binding Deck}" />
```

**Two gestures carry the whole design.** A single click selects a shape and draws a **dashed** frame
with eight resize handles — drag the body to move it, a handle to resize it. A double-click puts a
caret inside that shape's text and the frame turns **solid**. That is the split PowerPoint uses, and
the frame is the only thing telling a user where their next keystroke will go.

Only shapes the slide itself owns can be selected. Ones painted from the layout or master are skipped,
because they belong to every slide using that layout — letting a click grab one would drag the company
logo off the whole deck at once.

Edits are surgical on the DrawingML runs, the same way the document editor treats Word's: a run is
split only where an edit needs a boundary and never re-created, so the language, hyperlinks and
theme-derived fills it carries survive a formatting change. A whole drag is **one** undo step, not one
per pointer sample.

The toolbar draws from the **same icon set** as the document editor — see above — adding `Previous`,
`Next`, `BulletList`, `NumberedList`, `Indent`, `Outdent`, `TextBox` and `Delete`, and takes the same
`ShowToolbarTooltips`.

| | Blazor | MAUI |
|---|---|---|
| Select, move, resize, double-click into text | ✅ | ✅ |
| Typing, IME, dictation, paste | ✅ via `beforeinput` | ✅ via a hidden `Entry` |
| Physical keys (arrows, shortcuts) | ✅ | ⚠️ route through `HandleKey` — MAUI has no portable key-down event |

**Bullets and numbers work like the document editor's**, through PowerPoint's own mechanism. The two
toggle buttons write `a:buChar`, `a:buAutoNum` or `a:buNone` into the paragraph's properties, and
typing `- ` or `1. ` at the start of a paragraph does the same — the same detector the Word side uses,
so the two cannot drift. `ListStyle.None` is written **explicitly**: a body placeholder inherits its
bullet from the master, so leaving the element out puts that bullet back instead of taking it away.

Auto-numbered paragraphs show a **real number**. It is a function of the paragraph's position at its
outline level within the shape, counted per text body — two bulleted placeholders on one slide each
start at 1 — and rendered in whatever scheme the file asks for, arabic, alphabetic or roman, with a
period, a trailing paren or both.

<kbd>Tab</kbd> and <kbd>Shift</kbd>+<kbd>Tab</kbd> move outline level, which is what makes a bullet
nest; unlike the document editor there is no "not in a list" case, because every paragraph in a shape
carries a level whether or not it draws a mark. A selection spanning two levels moves each paragraph
relative to its own. Nine levels, and no more — a tenth is a file PowerPoint will not open.

```csharp
c.ToggleBulletList();          // or ToggleNumberedList()
c.SetListStyle(ListStyle.Numbered);
c.ShiftLevel(1);               // nest; -1 to un-nest
c.HandleTab(shift: false);

c.CaretFormat.List;            // ListStyle.None / Bullet / Numbered
c.CaretFormat.Level;           // 0-8
```

**Shapes, pictures and tables** come from the same galleries the document editor offers, placed in
slide coordinates and selected on arrival so the next gesture is a drag of the new object:

```csharp
c.AddShape(ShapeGeometry.Hexagon, x, y, width: 240, height: 180);
c.AddPicture(bytes, "image/png", x, y, width: 400);
c.AddTable(rows: 3, columns: 4, x, y, width: 480, height: 200);
```

`AddShape` writes a real drawn shape rather than a text box, which is what makes PowerPoint give it
the theme fill. A table is a `p:graphicFrame` with a built-in table style, not a shape.

**Dragging an image file onto a slide** drops it centred on the pointer, sized to at most half the
slide — same platforms, same rejections, same `DropRejected` event as the document editor.

**Highlighting** uses the same palette as the document side. `a:highlight` holds a real colour, so
nothing is approximated here.

⚠️ Not implemented, deliberately: soft line breaks, editing table cells or grouped shapes, adding or
reordering slides, and rotation handles.

## Dark mode

`Theme` is nullable and **unset means follow the host** — the app's light/dark appearance on MAUI,
the page's `color-scheme` on Blazor — and it keeps up live when that flips. Pass `SlideTheme.Light`
or `SlideTheme.Dark` only to pin one regardless of the app around it. See
[Styling & theming](styling.md#dark-mode).

## The toolbar is a Ribbon

The formatting bar is a [Ribbon](ribbon.md) on both hosts, replacing the single scrolling strip of
icons it used to be. Slide, Font, Paragraph and Insert, each titled — slide navigation leads, because which slide you are on is navigation rather than formatting.

Two things the strip could not do:

- **The ad-hoc dropdowns became real ribbon items.** Insert is a hosted menu component in its own group. That deleted a hand-written backdrop
  div, an absolutely-positioned panel and a `bool …Open` field per menu on Blazor, and an action sheet
  per menu on MAUI — along with their dismissal, keyboard and edge-flipping behaviour, which the
  ribbon already has.
- **Commands are grouped and captioned** instead of separated by anonymous hairlines.

Undo and redo sit in the ribbon's quick access row, outside the tabs, so they never move or disappear.

**The tab strip is off by default** (`ShowRibbonTabs`). This is a bar a host drops above a surface, not
an application's whole chrome, and a strip carrying a single "Home" is noise — the groups do the
organising. Turn it on when the editor *is* the application, and you get the tab strip and the
collapse chevron with it.

**Below 600px wide the bar runs in `Simplified` mode** — one dense row, every item small, group titles
dropped. Group collapsing is the wrong answer at phone width: it folds groups into dropdowns
worst-first, which is right when a window is a little too narrow, but on a phone there is room for no
group at all and every command ends up behind a dropdown. See [Ribbon](ribbon.md).

## The toolbar

Two tabs. **Home** is the slide you are on and the text on it — Slide (previous / counter / next), Font
and Paragraph. **Insert** is what goes on it — a text box, a shape, a table, a picture, and, behind a
rule, the way to remove the selected one.

The split is only worth making because the second tab holds a real bar rather than a token button. The
deck has no Layout or Zoom tab for the same reason there is nothing to put on one: a slide is a fixed
artboard that is always scaled to fit the viewport, so unlike a document page it is never clipped and
there is nothing to pan to or zoom in on.

## Inserting a picture

Same as the document editor. On iOS and Android the button asks — **Take Photo**, **Photo Library**,
**Browse Files** — with the camera offered only where the platform reports one. Every desktop head
opens its own file dialog filtered to exactly the formats a deck can embed. See
[Document Editor](document-editor.md#inserting-a-picture).

## Shapes are a tab, not a dropdown

The same **Shapes** tab the document editor has — Rectangles / Basic / Arrows, each button drawn as
the shape it inserts. One gallery, shared by both editors and both hosts. See
[Document Editor](document-editor.md#shapes-are-a-tab-not-a-dropdown).

## Accent

The bar wears PowerPoint red (`#C43E1C`) by default — see
[Document Editor ▸ Accent](document-editor.md#accent).

## Watermarks

`Watermark` draws a picture behind the content, on the viewer as well as the editor. The button picks
one through the same path as inserting a picture. See
[Document Editor ▸ Watermarks](document-editor.md#watermarks) — including why it is a display
watermark rather than one written into the file.
