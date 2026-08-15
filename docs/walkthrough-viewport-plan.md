# Walkthrough — off-screen targets and dimming the app chrome

Two related problems with where a walkthrough step can actually point: **targets that aren't in view**
(scrolling is partial and can hang the tour), and **targets in the navigation bar** (the overlay is
structurally incapable of reaching them).

**Status:** spec only. Nothing implemented.
**Applies to:** `Shiny.Maui.Controls` primarily. Blazor shares Part A in a milder form and has none of
Part B — a `position: fixed` overlay at `z-index: 2147483000` (`Walkthrough.razor.css:17`) already
covers everything a browser has.

## Context

### What exists today

| | MAUI | Blazor |
|---|---|---|
| Scroll-into-view | `ScrollIntoViewAsync` (`Walkthrough.cs:656`) → `ViewGeometry.EnclosingScrollView` + `ScrollToAsync` | `el.scrollIntoView({block:'center'})` (`tooltip.js:243`) |
| Settle wait | `Task.Delay(60)` (`Walkthrough.cs:668`) | `Task.Delay(320)` (`Walkthrough.Run.cs:276`) |
| Measurement | `ViewGeometry.BoundsIn` — walks logical parents, subtracts `ScrollX/Y` | `getBoundingClientRect` |
| Re-measure on viewport change | **none** | `observe` → `OnViewportChangedJs` (`tooltip.js:208`) |
| Scrim | `WalkthroughScrim`, a `GraphicsView` + `ScrimDrawable`, even-odd path punch | CSS |
| Where it draws | inside `page.Content`, via `PageOverlay.GetOrCreateRoot` | `position: fixed`, top of the stacking context |

The last row is the whole of Part B: on MAUI everything the tour paints is a child of the page's
content view, so it is structurally below the Shell / `NavigationPage` nav bar and tab bar. On Blazor
there is nothing outside the DOM to be below.

### The actual gaps

**Scroll (MAUI)**

1. **`ScrollToAsync` has no timeout, and can park the tour forever.** `Walkthrough.cs:664` is a bare
   `await` on a task the platform completes from `SendScrollFinished`. No handler, a target detached
   mid-scroll, or a head that doesn't implement it and the run stops between steps — scrim up, no
   callout, and `Stop()` can't rescue it because the cancellation check sits *after* the await.
   `ScrollToAsync` takes no `CancellationToken`. **This is the one live defect in the list.**
2. **`ScrollView` only.** `ViewGeometry.cs:74` matches nothing else, so a target inside a
   `CollectionView`, `ListView`, `CarouselView`, `ParallaxCollectionView`, `DataGrid` or
   `VirtualizedGrid` never scrolls. That is the container family where "not in view" is the *normal*
   case, not the exception. Shiny's own `TableView` is fine — it wraps a real `ScrollView`.
3. **Nearest ancestor only.** It scrolls the first scrollable ancestor and stops. A horizontal strip
   inside the page's vertical `ScrollView` scrolls the strip and leaves the page where it was.
4. **A failed scroll degrades badly, not gracefully.** `BoundsIn` happily returns bounds for an
   off-screen target — it is laid out, just outside the viewport — so `HoleFor` (`Walkthrough.cs:758`)
   punches a hole off-screen and `PlaceCallout` (`Walkthrough.Chrome.cs:311`) positions the bubble
   against it. Dimmed page, callout half off the edge, no error.
5. **No re-measure after the step lands.** Lower severity than it first appears: `UpdateShields`
   (`Walkthrough.Chrome.cs:348`) covers the whole page outside the hole, so the user cannot free-scroll
   during a step. Drift comes from the keyboard, rotation, dynamic content, and from
   `AllowTargetInteraction` scrolling via the exposed target — real, but narrower.

**Scroll (Blazor)**

6. `scrollIntoView` handles every scrollable ancestor for free, so gaps 2 and 3 don't exist. The fixed
   `320 ms` is a guess a long smooth scroll outruns (measure lands mid-flight) and that
   `prefers-reduced-motion: reduce` wastes on every single step.
7. `resolve()` is `querySelector` — shadow-DOM targets are invisible to it. Document, don't fix.

**Chrome (MAUI)**

8. A step cannot dim, let alone spotlight, the nav bar, the toolbar, the tab bar, or the flyout icon.
   "Tap the back arrow", "your profile lives up here", "switch tabs down there" are all unbuildable.

## Part A — scroll and measurement

Bug work. No new public API except one enum, no platform code, shippable on its own.

### A1. Bound the scroll wait *(fixes gap 1)*

```csharp
var scrollTask = scroll.ScrollToAsync(target, ScrollToPosition.Center, animate);
await Task.WhenAny(scrollTask, Task.Delay(ScrollTimeoutMs, token));
```

Ceiling ~800 ms. Never `await` the platform task directly again. The tour continues with whatever
position the target ended up at, which is always better than not continuing.

### A2. Scroll every ancestor, outermost first *(fixes gap 3)*

Replace `EnclosingScrollView` with `ScrollAncestors(Element)` returning the full chain. Walk it
**outermost → innermost**: scrolling the outer container moves the inner one, so the reverse order
re-scrolls stale positions and produces visible jitter. Await each with the A1 ceiling.

### A3. Teach it the `CollectionView` family *(fixes gap 2)*

One `IScrollTarget` shim per container type, resolved by walking up from the target:

| Ancestor | How |
|---|---|
| `ScrollView` | `ScrollToAsync(view, Center, animate)` |
| `ItemsView` (`CollectionView`, `CarouselView`, `ParallaxCollectionView`) | find the ancestor chain element whose `BindingContext` is an item in `ItemsSource`, then `ScrollTo(item, position: Center, animate)` |
| `ListView` | `ScrollTo(item, ScrollToPosition.Center, animate)` |
| `DataGrid` / `VirtualizedGrid` | whatever they expose internally; fall back to the inner `ScrollView` |

`ItemsView.ScrollTo` is fire-and-forget — there is no completion signal at all — so this path needs
the poll-until-stable settle from A4 rather than a fixed delay. `ChatView.Scroll.cs:207` already has
the retry-shaped prior art for exactly this and should be the reference.

**Documented limitation:** a target inside a *virtualized* container that has never been realized has
no `VisualElement` for `x:Reference` to bind to, so `Target` cannot be set at all. `TargetName` has the
same problem. Say so plainly in the skill doc — otherwise generated code will try.

### A4. Settle by measurement, not by clock *(improves 1 and 6, replaces both magic delays)*

Poll the target rect each frame until two consecutive samples match, capped at ~600 ms. Replaces
`Task.Delay(60)` on MAUI and `Task.Delay(320)` on Blazor. Faster than today when the scroll is short
or motion is reduced, correct when it's long.

Blazor can additionally listen for `scrollend` (Chrome/Edge/Firefox, Safari 18+) with the poll as the
fallback — same shape, less polling.

### A5. Fail visibly, not sideways *(fixes gap 4)*

After measuring, if the hole does not intersect the container, treat the step as **targetless**: no
spotlight, centred callout, exactly as a step with no `Target` renders today. Add a
`WalkthroughStep.OffscreenBehavior` — `Center` (default) | `Skip` | `Clamp` — and a `Debug.WriteLine`
naming the step, because silently pointing at nothing is the failure mode people file bugs about a
month later.

### A6. Viewport observer on MAUI *(fixes gap 5)*

Mirror Blazor's `observe`: while a step is showing, subscribe to the resolved chain's `Scrolled`, the
root's `SizeChanged`, and the page's `SizeChanged`; re-measure and reposition hole + callout on a
dispatcher-coalesced tick. No animation on these updates — it must read as "stays put", not as a
second travel animation.

Cheapest correct trigger set; a full per-frame ticker is not warranted.

## Part B — dimming and spotlighting the app chrome

### Why it can't work today

`PageOverlay.GetOrCreateRoot` wraps `page.Content` in a `ShinyOverlayRoot` grid. Everything —
`WalkthroughLayer`, scrim, shields, callout — lives inside that grid, and the grid is the page's
content. The nav bar is the *host's* chrome, outside it on every head. No amount of `ZIndex` reaches
out of a subtree.

### `WindowOverlay` — verified against Microsoft.Maui 10.0.90

Checked by reflecting over the shipped `Microsoft.Maui.dll` in every TFM this repo targets, not from
the docs page:

| Fact | Verified |
|---|---|
| `Microsoft.Maui.WindowOverlay` | present in `net10.0`, `-ios`, `-android`, `-windows` |
| `IWindow.AddOverlay(IWindowOverlay)` / `RemoveOverlay` | present in all four |
| `Initialize()` returns `bool` | yes — **this is the capability probe** |
| `IWindowOverlayElement` | `Contains(Point)`, plus `Draw(ICanvas, RectF)` from `IDrawable` |
| `GraphicsView` property type | `UIKit.UIView` / `Android.Views.View` / `Microsoft.UI.Xaml.FrameworkElement` — and **`System.Object` on plain `net10.0`** |
| Touch | `DisableUITouchEventPassthrough`, `EnableDrawableTouchHandling`, `Tapped` |

Two conclusions fall straight out:

- **`System.Object` on `net10.0` is the whole alt-head story.** The type exists everywhere so the code
  compiles unconditionally; `Initialize()` returning `false` is the runtime signal to fall back to
  today's page-level behaviour. AppKit and GTK4 consume the plain `net10.0` build (this project targets
  `net10.0`, `-ios`, `-android`, and `-windows` only) and get exactly that. No `#if` maze.
- **It draws; it cannot host a view.** The callout is a `TooltipBubble` with labels, buttons, a
  `ContentView` custom host, and theming. None of that can go in a `WindowOverlay`.

### The design: band the scrim, don't punch it

A window-level scrim draws above *everything*, including a page-level callout — so the naive split
dims the callout with its own tour.

**Recommended:** the `WindowOverlay` draws only the regions **outside** the page content rect — the nav
bar band, the tab bar band, the status bar band. The page-level scrim keeps doing exactly what it does
now. Two abutting rects at the same opacity read as one continuous dim.

Why this shape:

- **The existing scrim is already an `IDrawable`.** `WalkthroughScrim` is a `GraphicsView` with a
  `ScrimDrawable` doing an even-odd path punch. `IWindowOverlayElement` wants `Draw(ICanvas, RectF)` +
  `Contains(Point)`. `ScrimDrawable` is reusable nearly verbatim — this is a much smaller job than it
  looks.
- **Zero change to the existing path.** If `Initialize()` fails, or the flag is off, behaviour is
  byte-for-byte what ships today.
- **No seam risk if the rects abut rather than overlap.** Two 0.8-alpha rects overlapping by a pixel
  double-darken a visible line. Compute the bands from the page content's window rect and subtract, do
  not draw a full-window scrim and hope.

Rejected alternatives, for the record:

| Alternative | Why not |
|---|---|
| Full-window scrim, punch a second hole for the callout | Works until a callout has a shadow, a rounded corner mismatch, or any translucency — then the trick is visible. Also couples the punch geometry to the callout's travel animation, every frame. |
| Host the whole chrome in a native window-level container (`ToPlatform()` into `UIWindow` / `android.R.id.content` / root `Panel`) | The *right* answer if callouts must overlay the nav bar too — real views, full coverage. Costs three platform implementations plus hand-driven measure/arrange, versus roughly zero platform code for the banded route. Revisit only if callouts-over-chrome turns out to matter. |
| Hide the nav bar during the tour | Changes the thing the user is being taught about. |

### Touch

`DisableUITouchEventPassthrough` is all-or-nothing for the entire window, so turning it on to block
nav-bar taps also kills the Next button in the page layer. **Leave it off.** The nav bar is dimmed but
still live.

Blocking it properly is separate per-platform work (`Shell.NavBarIsVisible`, or
`userInteractionEnabled = false` on the `UINavigationBar` / disabling the `Toolbar`). If it's wanted,
it's a `BlockChromeInput` property in a later phase, not part of this one.

### Spotlighting a chrome item is a *separate* feature

`ToolbarItem` derives from `MenuItem`. It is not a `VisualElement` and has no bounds — there is nothing
to measure. Dimming the bar is nearly free; cutting a hole around the back arrow or a named toolbar
item needs native view lookup per platform (walk `UINavigationBar` subviews / find the `Toolbar`'s menu
item view by id / the `CommandBar`'s `AppBarButton`). Phase it separately behind:

```xml
<shiny:WalkthroughStep TargetChrome="Back" Text="Head back to your projects from here." />
<shiny:WalkthroughStep TargetChrome="Toolbar" TargetChromeIndex="0" ... />
<shiny:WalkthroughStep TargetChrome="TabBar" TargetChromeIndex="2" ... />
```

Escape hatch worth shipping in phase 1 regardless: `WalkthroughStep.TargetBounds` (a window-space
`Rect`), so an app that already knows where its thing is can spotlight it without waiting for us.

### Window-space measurement — correction

`Microsoft.Maui.Platform.ViewExtensions.GetBoundingBox` **is `internal`** in 10.0.90 (verified;
overloads for `IView`, `UIView`, `Android.Views.View`). It is not available to us, and an earlier note
saying otherwise was wrong. Roll our own — it's short:

| Platform | Window-space rect |
|---|---|
| iOS / Catalyst | `platformView.ConvertRectToView(platformView.Bounds, null)` — UIKit points are already DIPs |
| Android | `GetLocationInWindow(int[2])` + `Width`/`Height`, all divided by `DisplayMetrics.Density` |
| Windows | `element.TransformToVisual(rootPanel).TransformPoint(new Point(0, 0))` |
| net10.0 / alt heads | fall back to `ViewGeometry.BoundsIn` |

Put it in `Infrastructure/` next to `ViewGeometry`, as `PlatformGeometry.WindowBounds(VisualElement)`
returning `Rect?`. Useful well beyond the walkthrough — `Tooltip` has the same measurement needs.

`ViewGeometry.BoundsIn` stays as the fallback and the alt-head path. It is not deleted.

### API surface

```xml
<shiny:Walkthrough OverlayScope="Window">   <!-- Page (default) | Window -->
```

`Page` is the default and must stay the default: `Window` needs a live `IWindow`, works on three of the
heads, and changes what the dim covers. Opt in.

## Phasing

| Phase | Scope | Value if we stop here |
|---|---|---|
| **1** | A1 scroll timeout, A2 all ancestors, A5 off-screen fallback | **Highest.** Fixes a hang. Pure bug work, no API. Shippable alone. |
| **2** | A3 `CollectionView` family, A4 measured settle (both hosts) | Makes "scroll to a target" actually true as documented |
| **3** | A6 MAUI viewport observer, `PlatformGeometry.WindowBounds` | Parity with Blazor; unblocks phase 4 |
| **4** | `OverlayScope="Window"` banded scrim, `TargetBounds` | The chrome dim. The headline. |
| **5** | `TargetChrome` back/toolbar/tab spotlights, `BlockChromeInput` | Only if phase 4 proves it's wanted |

Phase 1 is the one to commit to. Phases 1–2 are the useful minimum for a release.

## Testing

- `ScrollAncestors` ordering, and `OffscreenBehavior` selection given a hole/container pair — pure
  functions, the only genuinely unit-testable logic here. Test those directly.
- **Use `async Task` `[Fact]` and `TestDispatcherProvider`.** A sync test over a control that awaits
  `ScrollToAsync` hangs xUnit on its sync context forever, with no stack trace. Phase 1 is *entirely*
  about `ScrollToAsync` behaviour, so this will bite otherwise.
- Simulate the A1 timeout with a scroll view whose completion never fires, and assert the run advances.
- `OverlayScope="Window"` needs device verification per head; there is no meaningful unit test for
  "does it draw above the nav bar".
- No `timeout` command on macOS — a `timeout N dotnet test | grep` prints nothing and looks exactly
  like a hang. Build once, then `--no-build`.

## Open questions

1. **Does the `WindowOverlay` sit above the nav bar on every head, or only iOS?** iOS inserts a
   passthrough view over the root view controller, Android adds into the content frame — both should
   clear MAUI's own chrome, neither clears the *system* status/nav bars unless the app is edge-to-edge.
   Windows is untested. Verify on device before phase 4 is committed; the answer may make Windows
   opt-out.
2. **Does `OverlayScope="Window"` survive Shell tab switches and modal pushes?** The overlay is
   window-scoped but the tour is page-scoped, and `OnPageDisappearing` is what tears it down today.
   A modal pushed *over* a running tour is the case to check.
3. **`ItemsView.ScrollTo` with no completion signal** — is poll-until-stable enough, or does the
   virtualized realize/measure cycle need a bounded retry like `ChatView.Scroll.cs` uses?
4. **Should `ScrollToTarget` become an enum?** `bool` can't express "scroll only if off-screen"
   (cheaper, less jarring) versus "always centre it". Cheap now, breaking later.
5. **Theme token for the chrome band** — same `ShinyThemeKeys.Color.Scrim` probe, or does the nav bar
   band want its own? Prefer reuse. Note the probe still needs a parent in the page layer; a colour
   token cannot resolve on an unparented element.

## Docs & release obligations

Per `CLAUDE.md`, when this is built (not now):

- `README.md` — Walkthrough section: the scroll behaviour and its documented limits, `OverlayScope`
  marked MAUI-only.
- `SKILLS/shiny-controls/walkthrough.md` — must state (a) that an unrealized virtualized item cannot be
  a target, (b) that `OverlayScope="Window"` is MAUI-only and opt-in, (c) that `TargetChrome` does not
  exist until phase 5. Generated code invents all three otherwise.
- Docs repo (`~/Desktop/dev/documentation`): `src/content/docs/controls/walkthrough/` updated; a
  `sidebar-topics.mjs` node only if `OverlayScope` earns its own page. No homepage `<Card>` change —
  Walkthrough already ships.
- **Separate release-notes entries**, not one: the scroll hang fix (a defect in a shipped control); the
  scroll-container support; the chrome overlay (a new opt-in capability).
- Samples: `samples/Sample/Features/Walkthrough/WalkthroughPage.xaml` needs a target far enough down its
  `ScrollView` (line 84) to actually require scrolling — today's targets all fit on screen, which is why
  none of this surfaced — plus a `CollectionView` step and a nav-bar step. Blazor sample mirrors the
  scroll case only.
- No screenshot capture as part of this work.
