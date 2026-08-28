# Motion Icons

[← All Shiny Controls](../../README.md)

42 hand-drawn animated icons that run **on a timer, on hover, on tap, when they scroll into view, or on command** — a bell that rings from its crown, a hamburger that morphs into a cross, a tick that draws itself on. `MotionIconView` on MAUI, `<MotionIcon>` on Blazor, both in the core packages; the artwork and the motion live in `Shiny.Controls.MotionIcons.Shared` so the two hosts render the same drawing running the same curves.

```xml
<!-- MAUI — the standard shiny xmlns, no extra prefix -->
<shiny:MotionIconView Icon="bell"
                      Trigger="Loop"
                      Interval="0:0:1.5"
                      Color="{AppThemeBinding Light=#2563EB, Dark=#60A5FA}"
                      WidthRequest="32"
                      HeightRequest="32" />
```

```razor
@* Blazor — Color defaults to currentColor, so it inherits the text around it *@
<MotionIcon Icon="bell" Trigger="MotionTrigger.Loop" Interval="TimeSpan.FromSeconds(1.5)" Size="32" />
```

**Triggers** are a `[Flags]` enum and combine: `Loop`, `Hover`, `Press`, `Appear`, or `Manual` for `Play()` / `Stop()` / binding `IsPlaying` to a busy flag. `Interval` rests the icon between cycles.

**Presets** work on any icon, including your own artwork — `Pulse`, `Beat`, `Spin`, `Shake`, `Wobble`, `Bounce`, `Float`, `Pop`, `Tada`, `Flip`, `Swing`, `Blink`, `Draw`, `Nudge`, `Jiggle`. `Default` plays the motion drawn for that icon and falls back to `Pulse` for artwork that has none.

**Bring your own artwork** with `PathData="M12 2 3 20h18z"` for a quick glyph, a `MotionIconDefinition` for something split into moving parts, or `MotionIconLibrary.Register(...)` to replace a built-in across the whole app.

**Notes:**
- The two hosts run the *same* spec through different machinery: on MAUI it compiles to a `KeyframeScene` driven by the [Keyframe](keyframe.md) engine's `Player`, so motion icons and hand-written timelines share one animation engine, one clock per window and one set of easing curves. On Blazor it compiles to `@keyframes` once and the browser composites it, so no C# runs per frame and the animation keeps going while WebAssembly is busy.
- Because nothing touches a platform SDK, the MAUI side works on every head — including AppKit and GTK4.
- Playback never starts before the view is loaded, so an implicit `<Style TargetType="MotionIconView">` that sets `IsPlaying` cannot reach for a dispatcher from inside a constructor.
- Unset `Color` follows the theme pack's on-surface token via `SetDynamicResource`, so icons restyle with a live `SetTheme`.
- Every generic preset works on artwork the library has never seen; only `Draw` needs to know the parts, so it can stagger them.
- `prefers-reduced-motion` is honoured on the web: the icon renders and still responds, it just holds its resting pose.
- **Write path data with explicit `L` commands.** `Microsoft.Maui.Graphics` does not implement SVG's implicit-lineto rule (`M6 6 18 18` becomes two *movetos*, not a line) and cannot read run-together decimals (`l.06.06`). Browsers handle both, so artwork copied from a design tool can look perfect on Blazor and draw nothing on MAUI. Unit tests guard the built-in set against both.
