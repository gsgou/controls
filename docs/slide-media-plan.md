# Slides — video and audio

Playing the media a `.pptx` already contains, and putting new media into one.

**Status:** spec only. Nothing implemented.
**Applies to:** `Shiny.Maui.Controls.Office` + `Shiny.Blazor.Controls.Office`, over
`Shiny.Controls.Office.Shared` and `Shiny.Controls.Office.Skia`. Both hosts, at parity, plus one new
add-on package per host for the playback backend.

## Context

### What exists today

| | Where |
|---|---|
| Picture reading | `SlideReader.ReadPicture` (`SlideReader.cs:480`), reached from the shape switch at `SlideReader.cs:96` |
| Picture model | `SlideShape.Image` — raw encoded bytes (`SlideModel.cs:93`) |
| Picture painting | `SlidePainter.DrawImage` (`SlidePainter.cs:387`), decode cache keyed by a byte-sample hash |
| Fit transform | `SlideController.SinglePlacement()` (`SlideController.cs:116`) → destination rect in viewport coords |
| Slide→viewport mapping | `SlideEditorController.Scale` / `ToViewport` / `BoundsOf` (`SlideEditorController.cs:109`, `:122`, `:131`) — **editor only** |
| Adding a picture | `SlideEditorController.AddPicture` (`:766`) → `SlideDeck.AddImagePart` (`PresentationDocument.cs:200`) → `SlideShapeFactory.Image` (`:129`) |
| MAUI surface | `SlideView` — a bare `SKCanvasView` as `Content` (`SlideView.cs:16`, `:34`) |
| Blazor surface | `SlideView.razor` — `SKCanvasView` inside one `div` (`SlideView.razor:15`) |
| Playback engine | `Shiny.Maui.Controls.MediaElement` / `Shiny.Blazor.Controls.MediaElement`, already shipping |

Nothing anywhere in the Office packages mentions `a:videoFile`, `p14:media`, `MediaDataPart` or
`DataPartReferenceRelationships`. Video is not modelled at any layer.

### What a deck with a video does today

A PowerPoint video **is** a `p:pic`. Its `p:blipFill` holds the **poster frame**, and the media itself
hangs off `p:nvPicPr/p:nvPr`:

```xml
<p:pic>
  <p:nvPicPr>
    <p:cNvPr id="4" name="clip.mp4"/>
    <p:cNvPicPr><a:picLocks noGrp="1" noChangeAspect="1"/></p:cNvPicPr>
    <p:nvPr>
      <a:videoFile r:link="rId3"/>                            <!-- the media relationship -->
      <p:extLst>
        <p:ext uri="{DAA4B4D4-6D71-4841-9C94-3DE7FCFB9230}">
          <p14:media r:embed="rId2"/>                          <!-- embedded: the MediaDataPart -->
        </p:ext>
      </p:extLst>
    </p:nvPr>
  </p:nvPicPr>
  <p:blipFill><a:blip r:embed="rId4"/>…</p:blipFill>          <!-- poster frame, an ImagePart -->
  <p:spPr>…</p:spPr>
</p:pic>
```

`ReadPicture` reads the `blipFill` and nothing else, so **the deck opens and the video renders as a
silent still frame with no play affordance and no report to `IUnsupportedFeatureSink`.** That is
precisely the failure mode the sink was written to prevent (`IUnsupportedFeatureSink.cs:23`) — a file
that opens, looks broadly right, and quietly drops a feature. Part A alone fixes that, and is worth
shipping even if nothing else here ever gets built.

Three flavours have to be told apart, because they need different handling:

| Flavour | XML | Bytes live |
|---|---|---|
| **Embedded** | `p14:media r:embed` + `a:videoFile r:link` | a `MediaDataPart` in the package |
| **Linked** | `a:videoFile r:link` to an external target, no `p14:media` | on the author's filesystem or a URL |
| **Web** (YouTube etc.) | `p:ext` `{Web Video}` with `p14:webVideoPr` holding an embed URL | on someone's server |

Embedded is the common case and the only one Part B plays. Linked-to-URL is a one-liner on top of it.
Linked-to-local-path and web video are `NotRendered` reports — a path from another machine will not
resolve, and a web video means running an `<iframe>` of a third-party player, which is out of scope
for a control that also has to work in MAUI.

### The one real architectural constraint

Both hosts paint the **entire slide** into a single `SKCanvasView`. Skia cannot decode or play video,
so a video can never be "another shape the painter draws". Playback has to be a **native/DOM overlay
layer positioned over the canvas** using the same fit transform the painter uses. Everything below
follows from that, and it is what makes this more than a reader change.

## Design

### Model

`SlideShape` grows one nullable member, alongside `Image` rather than replacing it — the poster frame
stays an ordinary picture so every existing painting path keeps working untouched:

```csharp
public enum SlideMediaKind { Video, Audio }

public enum SlideMediaSourceKind { Embedded, LinkedUri, LinkedPath, WebVideo }

public sealed record SlideMedia
{
    public required SlideMediaKind Kind { get; init; }
    public required SlideMediaSourceKind SourceKind { get; init; }

    /// <summary>Encoded media bytes, for <see cref="SlideMediaSourceKind.Embedded"/> only.</summary>
    public byte[]? Data { get; init; }

    /// <summary>MIME type from the package's content types, e.g. video/mp4.</summary>
    public string? ContentType { get; init; }

    /// <summary>The external target, for a linked or web source.</summary>
    public Uri? Uri { get; init; }

    /// <summary>True when the slide's timing tree starts this automatically.</summary>
    public bool AutoPlay { get; init; }

    public bool Loop { get; init; }
    public double Volume { get; init; } = 1;
}

// on SlideShape:
public SlideMedia? Media { get; init; }
```

`Image` continues to carry the poster, so a host without a media backend degrades to exactly today's
rendering plus a badge.

**Bytes, not a stream.** Consistent with `SlideShape.Image`, and for the same reason: the deck owns a
`MemoryStream` of the whole package anyway, the parts are already in memory, and a lazily-read stream
would outlive the `SlideDeck` that owns the package. The cost is real for video — see the size cap in
*Risks*.

### Reading

`ReadPicture` gains a media probe before it returns. `p14:media`'s `r:embed` resolves through
`slidePart.DataPartReferenceRelationships` (**not** `GetPartById` — a `MediaDataPart` is a data part,
not an `OpenXmlPart`, and `GetPartById` does not see it):

```csharp
var rel = this.part.DataPartReferenceRelationships.FirstOrDefault(x => x.Id == embedId);
if (rel?.DataPart is MediaDataPart media) { /* media.GetStream(), media.ContentType */ }
```

Every flavour we cannot play gets an `UnsupportedFeature` with `NotRendered` — the file still saves
intact, the user is simply told the poster is all they are getting.

### The playback seam

`Shiny.*.Controls.Office` must **not** take a hard dependency on the MediaElement packages. The MAUI
one drags in AndroidX Media3 / ExoPlayer (`Shiny.Maui.Controls.MediaElement.csproj:79-84`), which is a
large amount of Android baggage for somebody who only wanted to read a `.docx`. So Office declares the
seam and an add-on supplies it:

```csharp
// in Shiny.Controls.Office.Shared — no host types, no media dependency
public interface ISlideMediaPresenter
{
    bool CanPlay(SlideMedia media);
}
```

with a host-typed sub-interface per host (`View`-returning on MAUI, a `RenderFragment`-returning one on
Blazor) declared in each host's Office package. `SlideView` resolves it from DI, and when nothing is
registered it paints the badge and stops. **Blazor registration must be `Scoped`** — a singleton
presenter would share one player across every user of a Blazor Server app.

**Packaging decision: two new packages,** `Shiny.Maui.Controls.Office.Media` and
`Shiny.Blazor.Controls.Office.Media`, each referencing its host's Office + MediaElement packages and
exposing `UseShinyOfficeMedia()` / `AddShinyOfficeMedia()`. This mirrors how `MediaElement.Linux` and
the camera analyzers already attach optional backends, and it keeps the default Office install as light
as it is now.

*Alternative considered:* no new package, and the app hands Office a `Func<SlideMedia, View>` in
`MauiProgram`. Cheaper (skips the whole new-package checklist in CLAUDE.md) but pushes ~15 lines of
glue onto every consumer, which is not what a controls library is for. The interface is identical
either way, so this can be decided at the end of Part B rather than the start.

## Part A — read it, show it, say so

No overlay, no new package, no host code. Shippable on its own and it removes a silent data loss.

**A1. Model.** `SlideMedia` + `SlideShape.Media` as above.

**A2. Reader.** Probe `p:nvPr` in `ReadPicture` for `a:videoFile` / `a:audioFile` / the `p14:media`
extension / the web-video extension; classify; pull bytes for the embedded case; report everything
else through the sink.

**A3. Painter.** `SlidePainter.PaintShape` (`SlidePainter.cs:227`) draws a play badge over a shape
with `Media` — a translucent dark circle and a white triangle, radius `min(w, h) * 0.18` clamped to
20–48 slide px, centred. Audio gets a speaker glyph in the corner rather than a centred badge. Drawn
in slide coordinates so it scales with the artboard, exactly like the poster it sits on.

The badge must **not** be drawn in the editor's chrome pass — it is content, not chrome, so a
thumbnail and an exported frame both get it.

**A4. Grid mode gets the badge and never a player.** Falls out of A3 for free.

## Part B — playback

The overlay. This is the bulk of the work.

**B1. Promote the coordinate mapping.** `Scale`, `ToViewport`, `ToSlide` and `BoundsOf` currently live
on `SlideEditorController` (`:109`–`:136`) but depend on nothing the editor adds — they are pure
`SinglePlacement()` arithmetic. Move all four down to `SlideController` and leave the editor
inheriting them. The read-only viewer needs `BoundsOf` to place an overlay, and duplicating it is how
the two drift apart.

**B2. `SlideMediaLayer` — shared placement logic.** In `Office.Shared`, host-independent and testable:
given a controller and the current slide, yield `(SlideShape, SlideRect, SlideMedia)` for every media
shape that should be live right now. Rules, all of which are the interesting behaviour and all of
which are unit-testable with no UI:

- **Single mode only.** Grid mode yields nothing — twenty simultaneous players is not a feature.
- Current slide only. A slide change yields a different set and the host tears down the difference.
- Nothing outside the placement rect, and clip to it (a video that overhangs the artboard must be
  clipped by the slide edge, not float over the surround).
- Skip anything whose bounds are degenerate.

**B3. MAUI host.**

- `SlideView`'s content becomes a `Grid` **built in the constructor**, with the `SKCanvasView` at
  index 0 and an `AbsoluteLayout` above it. Not created lazily — on `net10.0-macos` a child added
  after layout is never realized and paints blank (this repo has been bitten by that before, and it
  is the single most likely way to lose a day on this part).
- On every controller change, diff the live players against `SlideMediaLayer`'s set, add/remove, and
  set `AbsoluteLayout` bounds from `BoundsOf`. Shape `Rotation` maps to `View.Rotation`; `FlipVertical`
  / `FlipHorizontal` have no MediaElement equivalent — ignore and report.
- **Embedded bytes must be spilled to a file.** `MediaSource` offers `FromUri` / `FromFile` /
  `FromResource` (`MediaSource.cs:21`–`:33`) and **has no stream or byte-array source**. So the
  presenter writes each embedded part to `FileSystem.CacheDirectory/shiny-office-media/{sha256}.{ext}`
  (content-addressed, so the same clip on ten slides is written once), and hands over
  `MediaSource.FromFile`. Extension comes from the part's content type — a MIME→extension map is
  needed, because Android's ExoPlayer and AVFoundation both sniff by extension in places.
  Cleanup: delete the folder on `SlideView.Dispose`, and best-effort-purge anything older than a day
  on first use, since a crash leaks the lot.

  *Optionally, later:* add a `StreamMediaSource` to the MediaElement package. That is the better fix
  and helps every consumer, but it is a change to a shipped public API on five platform backends — it
  should not be inside this plan's critical path.
- `EnableBackgroundPlayback` stays **off** and playback stops on slide change, on `Dispose`, and on
  the page disappearing. A deck that keeps talking after you navigate away is a bug report.

**B4. Blazor host.**

- Wrap the canvas in `position: relative`, add an absolutely-positioned overlay `div` per live media,
  sized from `BoundsOf` and rotated with a CSS `transform`.
- Embedded bytes reach JS as a `DotNetStreamReference` → `new Blob([...])` → `URL.createObjectURL`.
  **Not** base64 in the render tree: it inflates by a third, lands in a diffable attribute, and for a
  20 MB clip that is a stall the user sees. Revoke every object URL on dispose — a leaked one pins the
  whole blob for the life of the tab.
- The `Poster` parameter (`MediaElement.razor.cs:75`) takes the poster frame, so the overlay matches
  the painted badge before playback starts.

**B5. Editor.** In `SlideEditor` / `SlideEditorView`, media is **not** live. A click there selects the
shape to move and resize it; a live player would swallow that click and make the shape unselectable.
The badge is drawn (Part A), and a toolbar toggle can preview the current slide by dropping into the
same viewer path. Decision recorded so this does not get "fixed" later into a broken selection model.

## Part C — putting video in

**C1. `SlideDeck.AddMediaPart`.** Alongside `AddImagePart` (`PresentationDocument.cs:200`), but the
shape is different and that asymmetry is the trap: a media data part is created on the **package**
(`document.CreateMediaDataPart(contentType, extension)`), then referenced from the slide part twice —
`AddMediaReferenceRelationship` for `p14:media`'s `r:embed`, and
`AddVideoReferenceRelationship` for `a:videoFile`'s `r:link`. Both ids are needed, and PowerPoint
rejects the file if either is missing.

**C2. `SlideShapeFactory.Video`.** A `p:pic` with the `p:nvPr` above, plus the poster `blipFill`. If
the caller supplies no poster, generate one — a flat dark rectangle with the play badge, encoded PNG.
A `p:pic` whose `blipFill` has no valid blip is what PowerPoint calls corrupt.

**C3. `p:timing`.** The one that will be found on a device rather than in a test: a `p:pic` with media
relationships and **no matching node in the slide's `<p:timing>` tree** displays in PowerPoint but
will not play. The insert has to append a `p:video` entry into the timing tree's condition list (and
create the tree when the slide has none). Verify by opening the output in real PowerPoint on Windows —
LibreOffice is more forgiving here and will hide the bug.

**C4. `SlideEditorController.AddVideo`**, mirroring `AddPicture` (`:766`), through the same undoable
`AddElement` path, plus a file-picker entry in `OfficeMenus` (MAUI) and `OfficeInsertMenu`
(Blazor) with a media accept-list beside the existing `ImageContentTypes`.

**C5. Size guard.** Refuse over a configurable cap (default ~64 MB) with an `OfficeDropRejected`
rather than silently building a package that cannot be opened or e-mailed.

## Part D — optional, later

- **Audio.** The reader and the seam already cover it (`a:audioFile`); it needs a speaker-icon
  presentation instead of a video surface. Small, once B lands.
- **Autoplay and timing.** Reading whether the timing tree says *on click* or *automatically*, and
  honouring it on slide entry. Until then, everything is click-to-play, which is safe.
- **Trim points and poster offsets** (`p14:trim`) — read, report, ignore.
- **Linked-to-URL playback** — a two-line branch on `MediaSource.FromUri` in B3, gated on whether we
  want a control to fetch remote URLs without the app asking.

## Risks and open questions

1. **Android z-order.** ExoPlayer's `SurfaceView` composites outside the normal view hierarchy and has
   a long history of punching through or hiding under siblings. If the overlay sits wrong over the
   `SKCanvasView`, the fallback is `TextureView` in the Android backend. **Needs a device pass, and it
   is the most likely thing in this plan to be ugly.**
2. **AppKit realization.** Covered by building the container up front (B3), but the AppKit head of the
   MediaElement package is the least-exercised backend in the repo, and `Sample.MacOS` has its own
   `MauiProgram`/`AppShell` that has to be wired separately or the page renders blank.
3. **Blazor Server bandwidth.** An embedded 40 MB video streams over SignalR to reach the browser.
   Mitigation options: cap it, or serve the media through a minimal-API endpoint from a server-side
   cache instead. Not solved here; the cap ships and the endpoint is a follow-up.
4. **Memory.** `SlideMedia.Data` holds every embedded clip in the deck for the deck's lifetime.
   Above some size, `Data` should be left null and a stream handed out on demand instead. Decide
   during A2 with a measured threshold rather than guessing now.
5. **Save fidelity.** Unchanged for Parts A/B — nothing writes. Part C adds parts, so the deck is
   dirty and re-serialised; that is already documented behaviour (`PresentationDocument.cs`
   `FlushToPackage`) and needs a line in the skill doc, not a fix.
6. **Trim/AOT.** Office is `IsAotCompatible`. The new packages must be too, and Blazor interop here
   must not marshal anonymous types or array-typed DTOs — both are known to survive debug and fail
   only in a trimmed WASM publish. Verify with a Release publish, not `dotnet run`.

## Testing

Headless, in `tests/Shiny.Controls.Office.Tests` (no new test project):

| | Test |
|---|---|
| A2 | `SlideFixture` gains a deck with an embedded `MediaDataPart` → reader returns `SlideMedia` with the right kind, content type and bytes |
| A2 | A linked and a web video each produce exactly one `NotRendered` report and no `Media` |
| A2 | A plain picture still produces `Media == null` — the probe must not misfire on every image |
| A3 | Painter draws the badge for a media shape and not for a picture (assert against the existing painter-test harness in `ViewerPainterTests`) |
| B1 | `BoundsOf` on the base `SlideController` matches the editor's former result for the same deck |
| B2 | `SlideMediaLayer` yields nothing in grid mode, only the current slide's media in single mode, and re-yields after `Index` changes |
| C1–C3 | `AddVideo` round-trips: reopen the saved package and find the media part, both relationships, the poster and the `p:timing` entry |
| C1 | `PackageDiff` shows the added parts and **only** the added parts |
| C5 | Oversize input is rejected without mutating the deck |

Device/browser pass, on request afterwards: iOS + Android via DevFlow, macOS AppKit via `Sample.MacOS`,
Blazor via the browser. Android z-order (risk 1) is the specific thing to look at.

## Required doc and sample updates

Per `CLAUDE.md`, on the way through:

- `README.md` — the Office paragraph, plus NuGet badges for the two new packages.
- `SKILLS/shiny-controls/slide-editor.md` — media section; remove video from *Not implemented*; the
  save-fidelity note from risk 5. Add the viewer side to `document-viewer.md`, which is where slide
  rendering limits are currently listed.
- Docs repo (`~/Desktop/dev/documentation`): release-notes entry; `controls/slide-editor/` content;
  new menu nodes in `src/sidebar-topics.mjs` if the media add-on gets its own page.
- New packages: `Shiny.Controls.slnx`, `Build.slnf`, both dropdowns in
  `.github/ISSUE_TEMPLATE/bug_report.yml` and `feature_request.yml`, and the Repo layout bullet in
  `CLAUDE.md`.
- Samples: a video on the sample deck in `SampleOfficeDocuments.cs`, so `SlideViewerPage` (MAUI) and
  `SlideViewerPage.razor` (Blazor) both demonstrate it; insert-video button on the editor pages.
- `TODO: capture screenshots for slide media` — not part of this work.

## Staging

| Part | Size | Ships alone? |
|---|---|---|
| A — read, badge, report | small | **yes**, and it fixes a silent loss today |
| B — playback overlay | large; the real lift | yes, after A |
| C — insert | medium, OOXML-fiddly | yes, after A |
| D — audio, autoplay, trim | small each | yes |

A first. B and C are independent of each other and can be done in either order — C is more fiddly per
line but has no device risk, B has all of it.
