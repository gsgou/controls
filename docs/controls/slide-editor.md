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
`Next`, `Indent`, `Outdent`, `TextBox` and `Delete`, and takes the same `ShowToolbarTooltips`.

| | Blazor | MAUI |
|---|---|---|
| Select, move, resize, double-click into text | ✅ | ✅ |
| Typing, IME, dictation, paste | ✅ via `beforeinput` | ✅ via a hidden `Entry` |
| Physical keys (arrows, shortcuts) | ✅ | ⚠️ route through `HandleKey` — MAUI has no portable key-down event |

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
