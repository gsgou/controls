# Document & Slide Viewers

[← All Shiny Controls](../../README.md)

> Same packages as the Spreadsheet: `Shiny.Maui.Controls.Office` / `Shiny.Blazor.Controls.Office`.

`DocumentView` renders `.docx` and `SlideView` renders `.pptx`. Both are **read-only** — editing those
two formats is a later phase.

```csharp
using var document = await WordDocument.OpenAsync("report.docx");
using var deck = await SlideDeck.OpenAsync("deck.pptx");
```

```xml
<office:DocumentView Document="{Binding Document}" Zoom="1.0" />
<office:SlideView Deck="{Binding Deck}" Mode="Single" />
```

```razor
<div style="height:520px"><DocumentView Document="document" /></div>
<div style="height:460px"><SlideView Deck="deck" @bind-SlideIndex="index" @bind-Mode="mode" /></div>
```

**Word reflows; it does not paginate.** Content is laid out as one continuous column at the control's
width, so there are no pages, headers or footers. That is deliberate: a viewer without a full
pagination engine puts page breaks in the wrong places, which reads as a bug rather than a gap. It
resolves the whole style chain — document defaults, the named style with its entire `basedOn`
ancestry, then direct formatting — along with list numbering from `numbering.xml`, tables with column
spans, vertical merges and shading, inline images, and an `Outline()` for navigation. List numbers are
derived from document order rather than frozen at read time, so editing inside a list item leaves its
number alone and adding or removing an item renumbers the rest of the list.

**PowerPoint scales; it does not reflow.** Slides are fixed-size artboards, so the view fits and
letterboxes them. Shapes arrive resolved through slide → layout → master, which matters because a
title placeholder typically carries text and nothing else. ~20 preset geometries, solid and gradient
fills, outlines, theme colours with their `lumMod`/`lumOff`/`shade`/`tint` modifiers applied, per-level
text styles, speaker notes, pictures, tables, and a scrolling thumbnail-grid mode.

**Fonts are bundled on Blazor.** `Shiny.Blazor.Controls.Office` ships Carlito and Caladea (SIL OFL
1.1, ~1 MB compressed), metric-compatible with Calibri and Cambria, loaded automatically on first
render. SkiaSharp on WebAssembly has no access to system fonts and returns a wrong-but-non-null
fallback for every request, so without them every document renders in a single monospace face. MAUI
uses the platform's own fonts and needs no bundle.

Both preserve the package exactly — opening and saving is byte-identical — and both report what they
could not draw:

```csharp
var collector = new UnsupportedFeatureCollector();
using var document = await WordDocument.OpenAsync(path, collector);
// charts, SmartArt, footnotes, comments, headers/footers, custom geometry...
```
