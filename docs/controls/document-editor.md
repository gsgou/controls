# Document Editor

[← All Shiny Controls](../../README.md)

> Same packages as the viewers. Two controls: `DocumentEditor` is the lone editing surface;
> `DocumentEditorView` is the same thing plus a formatting toolbar.

```csharp
using var document = await WordDocument.OpenAsync("report.docx", editable: true);
```

```razor
<div style="height:520px">
    <DocumentEditorView Document="document" DocumentChanged="OnChanged" />
</div>
```

```xml
<office:DocumentEditorView Document="{Binding Document}" />
```

Edits are surgical on the OOXML runs: a run is split only where an edit actually needs a boundary and
is never rebuilt, so the language, proofing state and revision marks a run carries survive a
formatting change. An unedited document still saves byte-identical.

**Both toolbars are built from the same pickers.** `FontPickerButton`, `FontSizePickerButton` and
`ColorPickerButton` now exist in the core package on both hosts, and both editors use all three — so
the family list previews each face in its own typeface, and the colour swatch opens the full spectrum
rather than the operating system's own dialog. What still differs is only the bar around them: Blazor
composes `ShinyToolbar`, MAUI has no such control and lays out a scrolling row itself.

**One icon set across both toolbars and both hosts.** Every plain button on the Word and PowerPoint
bars draws from a single monochrome stroked set defined once in `Shiny.Controls.Office.Shared`, on a
24x24 grid at one weight: MAUI paints it onto a `GraphicsView`, Blazor writes it out as inline SVG
stroked in `currentColor`. That replaced a mixture of styled letters, geometric unicode and emoji —
and the emoji were the reason it had to go rather than a matter of taste, since a font paints those in
its own colour, size and weight, so the picture and delete buttons could not be tinted, did not dim
with a disabled button and looked different on every platform. The geometry is stored as drawing
commands rather than an SVG path string, because MAUI's `PathBuilder` drops implicit line-tos and
truncates run-together decimals silently — artwork that looks perfect in a browser can draw a stump on
a device. The **pickers are the deliberate exception**: font, size, text colour and the highlight
swatch have to show what they are currently set to, which is the one thing a monochrome icon cannot
do, so the highlight split button keeps the shared `A`-over-a-bar mark and tints only the bar.

**Icon-only buttons carry a tooltip on desktop and web.** Each one is wrapped in Shiny's own `Tooltip`
naming what it does, rather than the browser's `title` — which is slow to appear, cannot be themed and
is unreachable from a keyboard. On Blazor that is on by default; on MAUI it is on for Windows, Mac
Catalyst, macOS and the GTK head and **off on iOS and Android**, because the tooltip opens on hover
and there is no hover on a touch screen — and a long-press tooltip would compete with the tap the
button exists for. `ShowToolbarTooltips` on `DocumentEditorView` and `SlideEditorView` overrides either
way; turning it off on Blazor falls back to the native `title`. The accessible name is set on the
button regardless, since a tooltip is not what a screen reader reads.

| | Blazor | MAUI |
|---|---|---|
| Typing, IME, dictation, paste | ✅ via `beforeinput` | ✅ via a hidden `Entry` |
| Click, drag-select, toolbar commands | ✅ | ✅ |
| Double-click/tap a word, triple a paragraph | ✅ | ✅ |
| Physical keys (arrows, shortcuts) | ✅ | ⚠️ route through `HandleKey` — MAUI has no portable key-down event |

**Selecting what to format.** Drag across text, **double-click a word**, or **triple-click a
paragraph** — the same gestures on MAUI, where the click count is timed from the taps because
SkiaSharp's touch events do not carry one. Slides get the word gesture too: the first double-click
puts a caret in a shape's text, and a second one selects the word under it. A word stops at
punctuation but keeps its apostrophes, so `don't` selects whole and `end.` does not take the full stop.

**Formatting with nothing selected applies to what you type next**, the way Word does: put the caret
somewhere, pick a font, size, colour or weight, and the next text carries it. The choice shows on the
toolbar while it is pending, is spent by the first insertion, undoes together with the characters it
formatted, and is abandoned if the caret moves off the spot where it was made — so a colour picked and
thought better of cannot resurface in something typed later. Slides do the same thing through
PowerPoint's own mechanism, the paragraph end mark.

**Page margins are settable from both toolbars.** A page-margins button opens Word's own four presets
— Normal, Narrow, Moderate and Wide — as an action sheet on MAUI and a popover on Blazor, with the
preset the document already matches marked. `DocumentEditorController.SetPageMargins` takes a preset,
`PageMargins.FromInches(...)`, or four numbers, and the change is one undo step: the whole `w:pgMar`
element is captured before the write, so a document that never had one goes back to not having one and
anything else Word wrote there — a binding gutter, most of all — survives. Only the paginated
(`Print`) layout can show the result; a reflowed column has no paper to inset from, so the margins are
written and saved but have nowhere to appear until the view is showing pages, exactly like a page
break.

**Spell check uses the platform's own dictionary.** On MAUI nothing has to be registered — referencing
the package installs `UITextChecker` (iOS, Mac Catalyst), `NSSpellChecker` (macOS), Android's
text-services session, or the Windows `ISpellChecker` COM API. It is the *user's* dictionary, so words
they taught the keyboard are already known and **Add to dictionary** writes back to it. Misspellings
get a red wavy underline; right-click or long-press for corrections, Ignore and Add to dictionary, and
applying a correction is one undo step.

The browser has no equivalent API — it spell-checks its own editable elements and exposes neither
results nor suggestions to script — so **Blazor defaults to no checking** and takes one you supply.
Either way it is replaceable, per control or globally:

```razor
<DocumentEditorView Document="document" SpellChecker="myChecker" />
```

```csharp
SpellCheckers.Default = new MyChecker();   // derive from SpellCheckerBase; two methods
```

Checking is per paragraph, cached on the paragraph's text, limited to what is on screen and debounced,
so scrolling re-checks nothing and typing re-checks one paragraph.

**Shapes, pictures and tables insert inline.** Twenty preset geometries — the same
`ShapeGeometry` set the slide editor draws, through the same path builder — plus pictures and tables,
all from the toolbar:

```csharp
c.InsertShape(ShapeGeometry.Ellipse, width: 160, height: 120);
c.InsertImage(bytes, "image/png", width: 240);
c.InsertTable(rows: 3, columns: 4);
```

Inline means a `wp:inline`, never a `wp:anchor`: an object behaves like a very large character, wraps
with its line, and moves as text is typed before it. The document view is a reflow engine with no
float layer, so a floating shape could be written but never drawn where it claimed to be — anchored
drawings in an opened file are read and shown in the flow, with the unsupported note saying so.

Selecting one draws a frame with eight resize handles: a corner keeps the aspect ratio, an edge
changes one dimension, and the whole drag is **one** undo step. An inline object counts as exactly one
character, so an arrow key steps over it and a backspace takes all of it.

**Dragging an image file onto the editor inserts it at the drop point** — Blazor everywhere, and on
MAUI Windows, iOS/iPadOS and Mac Catalyst. Android has no file drag from a file manager and the
AppKit/GTK heads have no drop implementation behind `DropGestureRecognizer`; there the toolbar's
picture button is the gesture. `DropRejected` fires for a file over 32MB or in a format OOXML cannot
store.

**Highlighting** is a split button over a sixteen-swatch palette, shared with the slide editor.
Word's `w:highlight` takes a name from a closed list rather than a colour, so a highlight resolves to
the nearest one it can express; `HighlightPalette` is that list, and every swatch on offer round-trips
exactly.
