# Foldables — fold-aware controls (Android foldables + iPhone Duo)

Make the controls that split, overlay or centre content aware of a fold, so a two-pane layout lines up
with the hinge and a dialog never sits across it.

**Status:** implemented, uncommitted (2026-10-25), all five phases. Core seam (`FoldInfo`, `IFoldPostureSource`,
`UseFoldPosture`, internal `FoldPosture`/`FoldTracker`/`FoldLayout`) plus the new `Shiny.Maui.Controls.Foldables` package
(Android WindowManager; iOS `UIHinge` in a separate `net10.0-ios27.1` build). Unit tests pass (MAUI 1902, Desktop 95,
Blazor 657). Verified on a Pixel Fold AVD (`adb emu posture 2/3`, `rotate`, `fold`/`unfold`: Flyout snaps to 420.57dp and
restores, dialog and toast sit in one segment, FloatingPanel opens to the fold detent and leaves it). iPhone Duo simulator
(iOS 27.1): provider runs and reports no fold on the outer screen; hinge poses could not be driven headlessly, so the iOS fold
path is unverified - simulated folds verified. Blazor CSS verified in headless Chromium with a real display-feature override.
Open questions below were resolved: a public seam in core + an add-on package; `SnapToFold` default; hinge margin = gutter.
**Applies to:** `Shiny.Maui.Controls` (Android, iOS) primarily. `Shiny.Blazor.Controls` gets a CSS-first
slice (Phase 4). Desktop, Linux, macOS and Windows hosts have no fold and get no-ops.
**Depends on:** `IScreenPosture` in Shiny.Core — `~/Desktop/dev/shiny/plans/screen-posture.md`. That plan
has the platform facts (Jetpack WindowManager `FoldingFeature`; iOS 27.1 `UIHinge` and
`GetReservedRegions(.division)`), the shared `ScreenPosture` record, and the test rigs.

## Context

### What exists today

Nothing in either host knows about folds. Searches for `viewport-segment`, `devicePosture`,
`FoldingFeature` and `UIHinge` all come back empty. The closest pieces:

| Piece | Where | Relevance |
|---|---|---|
| Window-inset helper pattern | `Infrastructure/SafeAreaInsets.cs` + `.net.cs` + `Platforms/Android/SafeAreaInsets.Android.cs` + `Platforms/iOS/SafeAreaInsets.iOS.cs` | The template for a posture helper: `static partial` with `Platform*()` partials and a `net10.0` fallback |
| Width-driven push/overlay | `Flyout/FlyoutEnums.cs:36-53` (`FlyoutPresentation.Auto`), `FlyoutPanel.cs:21` (`CompactWidth` = 800), resolved at `FlyoutView.cs:729-735` | The existing two-pane control; already decides layout from the measured width |
| Detents | `FloatingPanel/FloatingPanel.cs:44-66` | Reads available height and the bottom inset; tabletop wants a detent at the fold |
| Centred overlays | `Dialogs/DialogView.cs:62-63`, `Overlay/Overlay.cs:27`, `Overlay/LoadingOverlay.cs:26-53`, toasts | A vertical fold cuts them in half |
| Blazor two-pane | `Layout/AppLayout` + `AppLayoutPanel.razor.cs:34-79`, sizes from a ResizeObserver in `wwwroot/app-layout.js:119` | Blazor's equivalent of Flyout |
| Registration | `MauiAppBuilderExtensions.cs:24` `UseShinyControls`; Android lifecycle hooks at 110–118 | Where an optional posture source is wired |

`Shiny.Maui.Controls` targets `net10.0;net10.0-ios;net10.0-android` (+ Windows). It has no
`Xamarin.AndroidX.Window` and **does not reference Shiny.Core**.

### Design guidance from both platforms (summarised)

- **Don't span-and-split by default.** iPhone Duo has no spanning mode. Apple's guidance is compact
  width on the outer display and regular width on the inner one; the fold only matters while partly
  open. Android behaves the same when fully flat. Layout reacts to **width** first (Flyout already
  does) and to the **fold** only when one separates the window.
- **Book (vertical fold, half open):** two panes, one per side, with the split on the fold.
- **Tabletop (horizontal fold, half open):** content on top, controls or a sheet on the bottom.
- **Overlays:** Apple's system alerts, menus and sheets move off the fold automatically. Ours must do
  the same.
- **Grids:** prefer an even column count so the fold lands between columns.

## Decision 1 — where the posture comes from

| Option | For | Against |
|---|---|---|
| **A. Reference Shiny.Core and use `IScreenPosture`** | One implementation; the WindowManager dependency lives in Core | A new hard dependency for every controls consumer |
| **B. Own implementation in the controls** | No new dependency | Duplicates the Android listener and iOS hinge code; two copies to fix |
| **C. A pluggable source (recommended)** | Controls depend on a tiny internal contract; a one-line bridge uses Shiny.Core when present | One extra seam |

**Option C.** Inside `Shiny.Maui.Controls`:

```csharp
// Infrastructure/FoldPosture.cs — internal, mirrors SafeAreaInsets
static partial class FoldPosture
{
    public static FoldInfo Current { get; }          // FoldInfo: State, Orientation, Rect? Fold (window dp)
    public static event EventHandler? Changed;
    internal static Func<FoldInfo>? Source;          // set by the optional bridge
}
```

- `ShinyControlConfiguration.UseScreenPosture(IScreenPosture)` (or an `AddShinyControlsPosture()`
  extension in a tiny bridge assembly) sets `Source` and forwards `Changed`.
- Without a source, `Current` is "no fold" everywhere, and every control behaves exactly as today.
- `FoldInfo` copies only the fields the controls need. It does not depend on Shiny types.

Rejected: adding `Xamarin.AndroidX.Window` to the core controls package. It drags a Kotlin coroutines
and lifecycle graph into every consumer for a feature most never use.

## Decision 2 — fold geometry in control coordinates

A fold rect is in **window** coordinates. Each control needs it in **its own** coordinates.

- MAUI: an `internal static Rect? FoldIn(this VisualElement view)` helper. It converts with the same
  ancestor walk `ViewGeometry.BoundsIn` uses, then intersects with the view's bounds. It returns null
  when the fold misses the view. A Flyout inside a page that doesn't reach the fold should not react.
- Recompute on `FoldPosture.Changed` **and** on the control's own `SizeChanged` / arrange. A fold that
  doesn't move can still cross a control that moved.

## Phase 1 — FlyoutView / ShinyFlyoutPage: snap to the fold

The highest-value change. Flyout is already our two-pane control.

- New `FlyoutPanel.FoldBehavior` (`FoldBehavior.None` / `SnapToFold`, default `SnapToFold`).
- When a **vertical** fold crosses the `FlyoutView` and `State == Expanded`:
  - Force `Presentation` to resolve to push, whatever `CompactWidth` says.
  - Set the effective expanded width so the panel's inner edge sits on the fold's leading edge, and
    the detail starts at the fold's trailing edge. Neither pane draws under the hinge.
  - **Ignore `MinExpandedWidth` / `MaxExpandedWidth` while snapped.** A fold position is not something
    the user can fix. Document this.
  - Hide the resize grip while snapped (`IsResizable`).
- The End panel mirrors this. With both panels, only the one adjacent to the fold snaps.
- A horizontal (tabletop) fold leaves Flyout as it is.
- Implementation point: the width resolution at `FlyoutView.cs:729-735` and the arrange pass
  (comment at ~370). Snapping is an arrange-time override, not a property write, so turning the fold
  back restores the user's width.

## Phase 2 — overlays move off the fold

`DialogView`, `Overlay`, `LoadingOverlay`, `ToastView` and the QuickEntry prompt:

- A **vertical** fold crossing the overlay's host centres the content in the **larger** segment. With
  equal segments, use the leading one, or the trailing one in RTL.
- A **horizontal** fold (tabletop) puts a dialog in the **top** segment and a toast in the **bottom**
  one.
- One shared helper — `FoldLayout.SegmentFor(Rect host, Rect? fold, FoldPreference)` — so the rule
  lives in one place. It goes in a shared, unit-testable spot; there is no `.Shared` geometry project
  for core controls, so use `Infrastructure/` with tests in `tests/`.

## Phase 3 — FloatingPanel / OverlayHost: tabletop detent

- In tabletop posture, add an implicit detent whose top edge sits on the fold's bottom edge, so the
  sheet fills the lower half exactly.
- New `FloatingPanel.FoldDetent` (`bool`, default `true`). When the posture leaves tabletop, the panel
  snaps back to the nearest declared detent.
- Book posture: no change. A bottom sheet across both halves is acceptable, and that is also what the
  system does.

## Phase 4 — Blazor

CSS covers most of it without any C#:

- `AppLayout.razor.css`: under
  `@media (horizontal-viewport-segments: 2)`, size the start panel to
  `env(viewport-segment-width 0 0)` and offset the detail by
  `env(viewport-segment-left 1 0)`. That is Flyout Phase 1 with no JS.
- `ModalView.razor.css` / dialog CSS: same media query; confine the dialog to segment 0.
- `SheetView.razor.css`: under `@media (vertical-viewport-segments: 2)`, a detent at
  `env(viewport-segment-top 0 1)`.
- Only add a `posture.js` + C# service if a component needs the fold in logic, which none of the
  phases above does.
- Chromium-on-Android only. Everything degrades to today's behaviour elsewhere.

## Phase 5 — later

These benefit from the fold but aren't scheduled:

- Docking `DockSplitter` ratio snap (Desktop package).
- Gantt task-list/chart split.
- `DataGrid` frozen columns kept on one segment.
- ImageViewer, CarouselGallery and CoverFlow centring.
- The Walkthrough callout placement.
- `ShinyTabbedPage` / `ShinyTabBar`: on iPhone Duo the system moves tab bars to the side ("vertical
  bars", `UITraitCollection.VerticalBarEdge`). Our tab bar is custom-drawn, so it gets none of that.
  That needs its own plan; this one doesn't cover it.

## Testing

- **Unit:** `FoldLayout.SegmentFor` (vertical, horizontal, RTL, fold outside the host, zero-width
  hinge) and Flyout snapped-width maths. Drive `FoldPosture.Source` with fakes. No device needed.
- **Android:** Pixel Fold AVD. `adb emu posture 2` (tabletop) and a book posture, plus the resizable
  AVD. Use mauidevflow to drive the sample.
- **iOS:** the `iPhone-Duo` simulator, iOS 27.1 runtime. How the simulator changes pose is
  unverified — check this first.
- **Sample:** a `Features/Foldables/FoldablesPage` with a live posture readout, a Flyout, a dialog
  button and a FloatingPanel. Wire it into `AppShell.xaml`, `Catalog.cs` and `MauiProgram.cs` as
  usual. Exclude the folder from `Sample.MacOS.csproj` and `Sample.Linux.csproj` (lines 61–65 / 54–58)
  if it touches platform types.
- **Jon Dick checklist:**
  - A snapped Flyout must not leave a 1–2px seam that scrolls.
  - No double border where the panel edge meets the hinge gap.

## Definition of done (per `CLAUDE.md`)

For each phase as it ships:

- `docs/controls/flyout.md` (and `floating-panel.md` / `dialogs.md`).
- The matching `SKILLS/shiny-controls/*.md`.
- A release note in `~/Desktop/dev/documentation/src/content/docs/controls/release-notes.mdx`.

If the optional bridge is a new package, also: the slnx/`Build.slnf`, README badge, issue-template
dropdowns, and the repo layout bullet.

TODO: capture screenshots for the foldables sample (on request only).

## Open questions

1. **Option C's bridge:** a property on `ShinyControlConfiguration` (needs Shiny.Core in scope at the
   call site, which is fine — it is the app's call), or a separate `Shiny.Maui.Controls.Posture`
   package? A config property is lighter; a package keeps the core free of even the interface.
2. **Should `SnapToFold` be the default?** It changes layout on existing apps the first time they run
   on a foldable, so it is arguably breaking. The alternative is opt-in for one release.
3. **Hinge width:** Android reports a zero-width fold on most current devices (a Pixel Fold's
   `Bounds` width is 0), and the Duo's division region carries `Margins`. Should zero-width folds still
   get a small gutter? Probably the platform margin when there is one, otherwise none.
