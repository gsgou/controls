# Drag & drop event editing (SchedulerAgendaView)

## Context

`SchedulerAgendaView` renders a 24-hour timeline and positions events absolutely inside it —
`AbsoluteLayout` children on MAUI (`Internal/AgendaTimelinePanel.cs`), `position:absolute` divs
inside `.shiny-agenda-daycol` on Blazor (`Scheduler/SchedulerAgendaView.razor`). Both hosts already
do the geometry: minutes → pixels via `TimeSlotHeight / 60.0`, plus an overlap-clustering pass that
assigns each event a column fraction.

What neither host has is any way to *change* an event. The interaction surface is selection-only and
identical on both sides (`ISchedulerEventProvider`):

```csharp
Task<IReadOnlyList<SchedulerEvent>> GetEvents(DateTimeOffset start, DateTimeOffset end);
void OnEventSelected(SchedulerEvent selectedEvent);
bool CanCalendarSelect(DateOnly selectedDate);
void OnCalendarDateSelected(DateOnly selectedDate);
void OnAgendaTimeSelected(DateTimeOffset selectedTime);
bool CanSelectAgendaTime(DateTimeOffset selectedTime);
```

You can tap an event, or tap an empty slot to create one. You cannot drag an event to a new time,
move it to another day, or drag its edges to change its duration — the interaction every native
calendar app has. This plan adds that to **both hosts at parity**.

**Design stance: off by default, additive, zero change when off.** `AllowEventDrag` and
`AllowEventResize` default to `false`. When both are off, no gesture recognizers are attached (MAUI),
no JS module is imported (Blazor), and the rendered tree is byte-for-byte what it is today. Every new
provider method is a **default interface method**, so existing `ISchedulerEventProvider`
implementations compile and behave unchanged. This mirrors how `AllowZoom` gates the pinch
recognizer today and how `TreeView.EnableDragDrop` gates its drag wiring.

**Scope: the agenda timeline only.** `SchedulerCalendarView` (month grid) and
`SchedulerCalendarListView` are out of scope — see [Not in scope](#not-in-scope).

## The one real risk: the drag gesture fights the scroll

This is the crux on both hosts, and it drives most of the design below.

The timeline is `24 × TimeSlotHeight` px tall (1440px at the default) inside a vertically scrolling
container — a `ScrollView` on MAUI, `.shiny-agenda-day { overflow: auto }` on Blazor. A vertical drag
on an event is *pixel-identical* to a scroll gesture at the moment it starts. If the drag wins
immediately, the timeline becomes unscrollable by touch; if the scroll wins, the drag never starts.

Native calendars resolve this with **long-press-to-arm**: hold ~350ms without moving, feel a haptic,
*then* the event follows the finger. That is the model here.

- While arming, the finger hasn't moved past the slop threshold, so the scroller hasn't meaningfully
  moved either — nothing visibly "jumps" when the drag takes over.
- If the finger moves past the slop threshold **before** the delay elapses, the gesture is abandoned
  and left to the scroller. That branch must be cheap and silent.
- Once armed, scrolling is suppressed for the duration of the drag (MAUI: set
  `scrollView.Orientation = ScrollOrientation.Neither`, the same lever `AllowPan` already pulls;
  Blazor: `touch-action: none` on the event element plus `setPointerCapture`).

**Mouse input should not wait.** A mouse drag on a desktop calendar is expected to be instantaneous.
Blazor gets this for free — `PointerEvent.pointerType` is `'mouse' | 'touch' | 'pen'`, so the delay
applies only to touch/pen. MAUI's `PanGestureRecognizer` does not report the input device, so the
control tracks a `hasPointerDevice` flag set by a `PointerGestureRecognizer`'s `PointerEntered`
(which fires for mouse hover and never for touch) and skips the delay when it is set. If that proves
unreliable on a given host, `DragActivationDelay` is public and an app can set it to `TimeSpan.Zero`.

## Public API

### Provider contract (both hosts, mirrored)

Added to the existing `ISchedulerEventProvider` as **default interface methods** so this is not a
breaking change. `Shiny.Maui.Controls.Scheduler` uses `Color`; `Shiny.Blazor.Controls.Scheduler` uses
`string?` — otherwise the two are identical, as they are today.

```csharp
/// <summary>Describes a proposed change to an event's time, produced by a drag or resize.</summary>
public class SchedulerEventChange
{
    public required SchedulerEvent Event { get; init; }

    /// <summary>The event's Start before the gesture began.</summary>
    public required DateTimeOffset OriginalStart { get; init; }

    /// <summary>The event's End before the gesture began.</summary>
    public required DateTimeOffset OriginalEnd { get; init; }

    /// <summary>The proposed new Start (already snapped to <c>DragSnapMinutes</c>).</summary>
    public required DateTimeOffset NewStart { get; init; }

    /// <summary>The proposed new End (already snapped; never closer than <c>MinEventDuration</c>).</summary>
    public required DateTimeOffset NewEnd { get; init; }

    /// <summary>Move (both edges shifted) vs. resize (one edge moved).</summary>
    public required SchedulerEventChangeKind Kind { get; init; }
}

public enum SchedulerEventChangeKind { Move, ResizeStart, ResizeEnd }
```

```csharp
public interface ISchedulerEventProvider
{
    // ... existing members unchanged ...

    /// <summary>
    /// Gates whether this event can be dragged/resized at all. Called once when the gesture arms —
    /// return false to leave the event fixed (e.g. read-only calendars, past events).
    /// </summary>
    bool CanChangeEvent(SchedulerEvent evt) => false;

    /// <summary>
    /// Called continuously as the event is dragged, before the change is committed. Return false to
    /// reject this position — the control shows the rejected state and will not commit there.
    /// Must be cheap: this runs on every snap boundary crossed.
    /// </summary>
    bool CanChangeEventTo(SchedulerEventChange change) => true;

    /// <summary>
    /// Called once when the gesture completes. The control has already applied the change
    /// optimistically. Return true to keep it, false to revert to the original time. Exceptions are
    /// treated as false and surfaced via <c>EventChangeFailed</c>.
    /// </summary>
    Task<bool> OnEventChanged(SchedulerEventChange change) => Task.FromResult(false);
}
```

`CanChangeEvent` defaulting to `false` means a provider that ignores this feature can never have its
events moved even if an app sets `AllowEventDrag="True"` — the opt-in is required on both the view
*and* the provider.

### View properties

MAUI — `BindableProperty` on `SchedulerAgendaView`; Blazor — `[Parameter]` on the same-named
component. Same names, same defaults.

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `AllowEventDrag` | `bool` | `false` | Drag an event to a new time (and, when `DaysToShow > 1`, another day). |
| `AllowEventResize` | `bool` | `false` | Drag the top/bottom edge to change duration. |
| `DragSnapMinutes` | `int` | `15` | Snap granularity. Clamped to 1–60; values that don't divide 60 are allowed but produce uneven guides. |
| `MinEventDuration` | `TimeSpan` | `15 min` | Resize floor. A move never changes duration, so this only gates resize. |
| `DragActivationDelay` | `TimeSpan` | `350 ms` | Long-press arming delay for touch. `Zero` = immediate. Ignored for mouse input. |
| `AllowCrossDayDrag` | `bool` | `true` | Only meaningful when `DaysToShow > 1`. |
| `DragSnapGuideColor` | `Color` / `string` | separator colour @ 60% | The horizontal guide line drawn at the snapped position. |

MAUI additionally reuses the existing `UseFeedback` flag: `FeedbackHelper.Execute(this, "EventDragStarted")`
on arm and `"EventDropped"` on commit, alongside the existing `"EventSelected"` / `"TimeSlotSelected"` keys.

## Shared geometry: pull the math out and test it

Both hosts currently inline the same minutes↔pixels arithmetic in their layout paths. Drag adds an
*inverse* (pixels → snapped `DateTimeOffset`) that is easy to get subtly wrong and impossible to
unit-test where it sits today. Extract it into an internal static class per host —
`Scheduler/Internal/AgendaGeometry.cs` (MAUI) and `Scheduler/AgendaGeometry.cs` (Blazor) — with pure
functions:

```csharp
static class AgendaGeometry
{
    public static double MinutesToY(double minutes, double timeSlotHeight);
    public static double YToMinutes(double y, double timeSlotHeight);
    public static double SnapMinutes(double minutes, int snapMinutes);

    /// <summary>
    /// Converts a snapped local wall-clock minute offset within <paramref name="date"/> into a
    /// DateTimeOffset carrying the local zone's offset *at that instant* (see DST below).
    /// </summary>
    public static DateTimeOffset ToLocal(DateOnly date, double snappedMinutes);

    /// <summary>Applies a move/resize delta, enforcing MinEventDuration and 0..1440 clamping.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) Apply(
        SchedulerEvent evt, SchedulerEventChangeKind kind, double deltaMinutes,
        int dayDelta, TimeSpan minDuration);
}
```

The existing layout code in `AgendaTimelinePanel.Build` and `PositionedEvents` should be refactored
to call `MinutesToY`, so forward and inverse can't drift apart.

### DST is a real correctness trap here

The timeline lays out **local wall-clock** time: 24 rows, always. On a DST-transition day that is not
24 hours. If a move is implemented as `evt.Start + TimeSpan.FromMinutes(delta)`, then dragging an
event across the transition lands it one hour off, because `DateTimeOffset` arithmetic is
absolute-time arithmetic.

`Apply` must therefore work in wall-clock space: take the original event's **local** `DateTime`,
add the wall-clock delta, then rebuild the `DateTimeOffset` using
`TimeZoneInfo.Local.GetUtcOffset(newLocal)` — the offset **at the destination**, not the source. On a
spring-forward day this means the invalid 02:00–03:00 window must be handled:
`TimeZoneInfo.Local.IsInvalidTime(newLocal)` → push forward to the first valid time. On fall-back,
`IsAmbiguousTime` → take the first (pre-transition) offset, which is what a user dragging downward
expects. Cover all three cases in tests.

## MAUI implementation

### Where the drag lives: the view, not the panel

A cross-day drag has to move an event out of one `AgendaTimelinePanel` and into another, and the
panels are siblings in `SchedulerAgendaView.columnsGrid`. So the drag is **owner-coordinated**,
exactly like `TreeView`'s pointer drag: the panel detects and forwards, the view decides. This
mirrors `TreeView.BeginPointerDrag` / `UpdatePointerDrag` / `CompletePointerDrag`.

New file `Scheduler/Internal/AgendaDragController.cs` holds the whole gesture state machine so
`SchedulerAgendaView` (already 638 lines) doesn't absorb it:

```csharp
class AgendaDragController
{
    // owner, panels, scrollView injected
    public void Begin(AgendaTimelinePanel panel, View eventView, SchedulerEvent evt, SchedulerEventChangeKind kind);
    public void Update(double totalX, double totalY);
    public Task CompleteAsync();
    public void Cancel();
    public bool IsDragging { get; }
    public bool ConsumedLastGesture { get; }   // used to swallow the trailing tap
}
```

### Gesture wiring (`AgendaTimelinePanel`)

`Build` currently attaches one `TapGestureRecognizer` per event view. When
`AllowEventDrag || AllowEventResize`, also attach a `PanGestureRecognizer`, plus two thin
`BoxView`-backed resize grips inset at the top and bottom of the event view (16px tall hit targets,
visible only while the event is armed or hovered — a 4px visual bar inside a 16px touch area).

Hit-testing which of the three kinds a gesture is: the grips are separate views with their own pan
recognizers, so `Move` vs `ResizeStart` vs `ResizeEnd` is decided by *which view* got the gesture. No
coordinate math, no ambiguity, and it degrades correctly for short events (when
`h < 3 × gripHeight` the grips are suppressed and the event is move-only).

```csharp
void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
{
    switch (e.StatusType)
    {
        case GestureStatus.Started:  owner.DragController.Arm(this, view, evt, kind); break;
        case GestureStatus.Running:  owner.DragController.Update(e.TotalX, e.TotalY); break;
        case GestureStatus.Completed: _ = owner.DragController.CompleteAsync(); break;
        case GestureStatus.Canceled: owner.DragController.Cancel(); break;
    }
}
```

`PanUpdatedEventArgs` gives `TotalX`/`TotalY` relative to the gesture start, which is exactly the
delta the controller needs — no absolute pointer position required. Note that iOS reports
`Completed` with the *final* totals while Android reports zeros on completion, so the controller must
commit from the **last `Running` values it saw**, not from the completion event. This is a known MAUI
asymmetry and is the single most likely source of an "event snaps back to its original slot on
Android" bug.

### Arming

`Arm` starts a `Dispatcher.CreateTimer` for `DragActivationDelay` (skipped when `hasPointerDevice`).
If `Update` arrives with `|TotalY| > 8 || |TotalX| > 8` before it fires, the drag is abandoned and the
`ScrollView` keeps the gesture. When it fires:

1. `Provider.CanChangeEvent(evt)` — bail if false.
2. `FeedbackHelper.Execute(this, "EventDragStarted")` when `UseFeedback`.
3. `scrollView.Orientation = ScrollOrientation.Neither` (restored on complete/cancel to whatever
   `AllowPan` dictates).
4. `eventView.Opacity = 0.75; eventView.ZIndex = 1;` and snapshot `OriginalStart`/`OriginalEnd`.

### Live update

`Update` recomputes the candidate change and repositions the dragged view directly via
`AbsoluteLayout.SetLayoutBounds` — no rebuild, no reflow of the other events. It also:

- draws/moves a 1px snap guide `BoxView` at the snapped Y in the target panel;
- maps `TotalX` to a day column when `DaysToShow > 1 && AllowCrossDayDrag`, by dividing by the panel
  width and clamping to `0..DaysToShow-1`; crossing a column reparents the view into that panel's
  `eventsLayer`;
- calls `Provider.CanChangeEventTo(change)` and, when it returns false, tints the view (60% opacity,
  red-ish stroke) and marks the position non-committable — the guide still tracks the finger, but
  `CompleteAsync` will revert;
- auto-scrolls when the pointer is within 48px of the `ScrollView`'s top or bottom edge, via a 16ms
  dispatcher timer stepping `ScrollToAsync(0, y ± 12, false)`. Without this you can't drag an event
  from 09:00 to 18:00 on a phone, because the destination is off-screen.

### Commit

```csharp
public async Task CompleteAsync()
{
    // restore scrolling + visuals first so the UI is never stuck if the provider hangs
    var change = BuildChange();
    if (!committable) { Revert(); return; }

    evt.Start = change.NewStart;   // optimistic
    evt.End = change.NewEnd;
    owner.RelayoutDay(...);        // re-runs DetectOverlaps for affected day(s)

    var ok = false;
    try { ok = await provider.OnEventChanged(change); }
    catch (Exception ex) { owner.RaiseChangeFailed(change, ex); }

    if (!ok) { evt.Start = change.OriginalStart; evt.End = change.OriginalEnd; owner.RelayoutDay(...); }
    else if (UseFeedback) FeedbackHelper.Execute(owner, "EventDropped");
}
```

Optimistic-then-revert (rather than await-then-apply) is deliberate: a provider that hits the network
would otherwise leave the event visibly stuck under the finger for the round-trip.

`RelayoutDay` is a new method extracted from the tail of `LoadEvents` — it re-runs `DetectOverlaps`
and `Build` for one or two days using the already-loaded event lists, without re-calling
`Provider.GetEvents`. A move can change the overlap clustering of *both* the source and destination
day, so both must be relaid out on a cross-day drop.

### Swallowing the trailing tap

`TapGestureRecognizer` fires on release after a pan on several platforms, so a completed drag would
also raise `OnEventSelected`. The controller sets `ConsumedLastGesture` on arm and clears it on the
next `Started`; `AgendaTimelinePanel`'s tap handler checks it and returns early. Same guard the
`TreeView` pan path needs.

## Blazor implementation

### Pointer Events, not HTML5 drag-and-drop

`TreeView` uses HTML5 DnD (`dragstart`/`dragover`/`drop`) via `tree-view.js` because a tree drop is a
*discrete* target-and-zone decision. An agenda drag is a *continuous positional* one, and HTML5 DnD
is the wrong primitive for it:

- it gives no reliable pointer coordinates during `dragover` on all browsers;
- it does not fire at all for touch input on mobile Safari or Chrome Android;
- the drag image is a browser-rendered ghost you can't position on a snap grid.

So `scheduler-agenda.js` uses **Pointer Events** (`pointerdown` / `pointermove` / `pointerup` +
`setPointerCapture`), which unify mouse/touch/pen, deliver client coordinates on every move, and let
the element itself be repositioned as the live preview.

### Render-loop discipline

**No `pointermove` may round-trip to .NET.** On WASM a `StateHasChanged` per move frame is a visible
stutter. The entire live preview is done in JS by mutating `style.top` / `style.height` /
`style.transform` on the dragged element and on a guide `<div>`; .NET is invoked exactly twice per
gesture:

```js
// on arm — one call, gates the drag
const allowed = await dotNetRef.invokeMethodAsync('OnDragArm', eventId);

// on pointerup — one call, carries the final snapped values
await dotNetRef.invokeMethodAsync('OnDragCommit', eventId, dayIndex, startMinutes, endMinutes, kind);
```

`CanChangeEventTo` is the exception — it *is* per-position. Rather than invoke per frame, the arm
call returns a small allow-list payload (`{ minMinutes, maxMinutes, blockedRanges }`) that JS
evaluates locally; .NET re-checks authoritatively in `OnDragCommit`. Providers that need genuinely
dynamic per-position validation can opt into per-snap-boundary interop with
`DragValidationMode="PerPosition"` (default `OnCommit`), documented as the slower path.

### Markup and CSS changes

`SchedulerAgendaView.razor`:

- add `@ref="rootElement"` to the root `.shiny-agenda` div and `@inject IJSRuntime JS`;
- add `data-day-index="@i"` to each `.shiny-agenda-daycol`;
- add `data-event-id="@p.Event.Identifier"` and `data-draggable="true"` to each `.shiny-agenda-event`
  when the feature is on;
- render two grip spans inside the event when `AllowEventResize` and the event is tall enough;
- implement `IAsyncDisposable` alongside the existing `IDisposable` to dispose the JS module and the
  `DotNetObjectReference`.

`SchedulerAgendaView.razor.css`:

```css
.shiny-agenda-event[data-draggable="true"] { touch-action: none; cursor: grab; }
.shiny-agenda-event.is-dragging { cursor: grabbing; opacity: .75; z-index: 5; box-shadow: 0 4px 12px rgb(0 0 0 / .25); }
.shiny-agenda-event.is-rejected { opacity: .5; outline: 2px dashed #EF4444; }
.shiny-agenda-grip { position: absolute; left: 0; right: 0; height: 16px; cursor: ns-resize; }
.shiny-agenda-grip--start { top: -6px; }  .shiny-agenda-grip--end { bottom: -6px; }
.shiny-agenda-snapguide { position: absolute; left: 0; right: 0; height: 1px; pointer-events: none; }
```

`touch-action: none` on the event is what stops the browser from claiming the gesture as a scroll —
it is the direct analogue of MAUI's `ScrollOrientation.Neither`, and without it the drag simply will
not work on touch.

### `Identifier` becomes load-bearing

The MAUI side matches the dragged event by object reference. Blazor round-trips through the DOM, so
it matches by `SchedulerEvent.Identifier`. It defaults to a `Guid`, so this is safe by default — but
a consumer who assigns duplicate identifiers will move the wrong event. `OnDragCommit` must resolve
against the loaded `timedEvents` list and no-op (with a debug-level trace) on a miss or ambiguity,
and the skill/docs must state the uniqueness requirement.

### Suppressing the click after a drag

Pointer-event drags still produce a trailing `click` on the element, which would fire the existing
`@onclick="() => OnEventTapped(captured)"`. `scheduler-agenda.js` installs a one-shot capturing
`click` listener on `pointerup` when a drag actually occurred, calling `stopPropagation()` and
removing itself. Same class of problem as the MAUI trailing tap, different mechanism.

### Auto-scroll

In `pointermove`, when the pointer is within 48px of the `.shiny-agenda-day` container's top or
bottom edge, a `requestAnimationFrame` loop steps `scrollTop` by ±8px/frame and recomputes the
snapped position each frame (the pointer is stationary but the content moves under it, so the
position must be derived from `clientY + scrollTop`, not `clientY` alone).

## Edge cases to get right

1. **Multi-day / clipped events.** `AgendaTimelinePanel.Build` clamps `startMinutes` to 0 for events
   that began on a previous day (and `endMinutes` to 1440 for ones ending later). The drag delta must
   be applied to the event's **actual** `Start`/`End`, not to the clamped visual position — otherwise
   dragging a spillover event silently truncates it to start at midnight. `SchedulerEventChange`
   carries the true originals for exactly this reason.

2. **All-day events.** `AllDayEventsSection` (MAUI) and `.shiny-agenda-allday` (Blazor) are not
   draggable in v1, and dragging a timed event into the all-day strip (or out of it) is **not**
   supported — it changes `IsAllDay` semantics and interacts with `GetEvents` range filtering. Grips
   and pan recognizers are simply not attached there. Call this out in the docs so it reads as a
   decision, not an oversight.

3. **Resize past the opposite edge.** Dragging the top grip below the end (or vice versa) must clamp
   at `MinEventDuration`, not flip the event inside out. `AgendaGeometry.Apply` enforces this; test
   both directions.

4. **Day-boundary clamping.** A move that would push the event past 00:00 or 24:00 clamps at the
   boundary rather than spilling — except on a cross-day drag, where the day index changes instead.

5. **A drag in flight when data reloads.** `SelectedDate`, `DaysToShow`, `TimeSlotHeight` and pinch
   zoom all call `Rebuild()`, which clears `columnsGrid` and would orphan the dragged view. Guard:
   `Rebuild` cancels any in-flight drag first (`DragController.Cancel()`), and the pinch handler is
   suppressed while `IsDragging`.

6. **`AllowZoom` + drag.** A two-finger pinch that starts on an event must not arm a drag. The
   arming timer is cancelled if a `PinchGestureRecognizer` reports `Started` on the same view.

7. **Provider throws.** Treated as `false` (revert), surfaced through a new
   `EventChangeFailed` event (MAUI) / `EventChangeFailed` `EventCallback<SchedulerEventChangeFailure>`
   (Blazor). Never swallow silently — a revert with no explanation is the worst failure mode here.

## Not in scope

- `SchedulerCalendarView` (month grid) drag between days, and `SchedulerCalendarListView` reordering.
  Both are plausible follow-ons and would reuse `SchedulerEventChange` and `OnEventChanged` unchanged,
  but the month grid's drop targets are discrete cells — a different gesture model (closer to
  `TreeView`'s) and a separate piece of work.
- Timed ↔ all-day conversion (see edge case 2).
- Multi-select drag.
- Creating an event by dragging on empty space (`OnAgendaTimeSelected` already covers tap-to-create;
  drag-to-create is a natural third phase).

## Phasing

| Phase | Content |
| --- | --- |
| 1 | `AgendaGeometry` + tests (both hosts), `SchedulerEventChange`, provider DIMs. No UI. |
| 2 | MAUI move within a single day: arming, live update, snap guide, optimistic commit/revert. |
| 3 | MAUI resize grips + cross-day drag + auto-scroll. |
| 4 | Blazor `scheduler-agenda.js` + markup/CSS, move → resize → cross-day (same order). |
| 5 | Sample pages, docs, skill, release notes. |

Phases 2 and 4 are independent once phase 1 lands and can proceed in parallel.

## Testing

`tests/Shiny.Maui.Controls.Tests` is the home for the pure math (there is no Blazor test project
today; the Blazor `AgendaGeometry` is a near-line-for-line port, so a shared test corpus of
input/expected tuples is the cheapest way to keep them honest).

- `AgendaGeometryTests` — round-trip `MinutesToY`/`YToMinutes` across `TimeSlotHeight` 20/60/200;
  snapping at 5/15/30/60 including exact-boundary and negative inputs.
- `AgendaDragMathTests` — move preserves duration; resize clamps at `MinEventDuration` from both
  directions; day-boundary clamping; cross-day day-index arithmetic; clipped multi-day events keep
  their true start.
- `AgendaDstTests` — spring-forward invalid time push-forward, fall-back ambiguous time resolution,
  and a drag *across* a transition preserving wall-clock intent. Use a fixed `TimeZoneInfo`
  (`America/New_York`) rather than `TimeZoneInfo.Local` so the tests are machine-independent — this
  requires `AgendaGeometry` to take an optional `TimeZoneInfo` parameter defaulting to `Local`.
- `tests/Sample.UITests` — extend the existing `CalendarTests` pattern with a drag on the agenda
  sample page asserting the committed time, once the sample page below exists.

Gesture arbitration itself (does the scroller or the drag win) is not unit-testable and must be
verified by hand on iOS, Android, and at least one desktop host, plus touch and mouse in the browser.

## Sample

- `samples/Sample/Features/Scheduler/AgendaPage.xaml` — add `AllowEventDrag="True"`
  `AllowEventResize="True"` and switches to toggle them, plus a `DragSnapMinutes` picker
  (5/15/30) so the snap behaviour is demonstrable.
- `samples/Sample/Features/Scheduler/SampleSchedulerProvider.cs` — implement `CanChangeEvent`
  (returns false for one deliberately "locked" event, to demo the gate), `CanChangeEventTo` (reject
  anything before 07:00, to demo live rejection), and `OnEventChanged` with a ~600ms delay and a
  10% random failure, so optimistic-commit-then-revert is visible rather than theoretical.
- `samples/Sample.Blazor/Pages/AgendaPage.razor` — the same options against the same provider shape.

## Docs & required updates (per CLAUDE.md)

1. **`README.md`** — extend the Scheduler section with drag/resize; no new NuGet badge (both packages
   already exist).
2. **`SKILLS/shiny-controls/scheduler.md`** — add the new properties to the `SchedulerAgendaView`
   property tables, the three new provider members to the `ISchedulerEventProvider` section, the
   `SchedulerEventChange` model alongside `SchedulerEvent`, and a worked provider example. Add the
   `Identifier` uniqueness requirement to *Scheduler Important Notes*.
3. **`~/Desktop/dev/documentation`**:
   - `src/content/docs/controls/release-notes.mdx` — new entry.
   - `src/content/docs/controls/scheduler/` — document the feature, including the not-in-scope list.
   - `src/sidebar-topics.mjs` — a child node under the existing Scheduler entry in the `Controls`
     topic (feature, not a new control, so no homepage `<Card>` change and no new top-level node).
4. **Screenshots** — `TODO: capture screenshots for scheduler agenda drag/resize`. Not part of this
   work; on request only.

## Rollout / risk

The feature is inert unless an app sets `AllowEventDrag`/`AllowEventResize` *and* its provider
overrides `CanChangeEvent`. Existing apps see no behavioural change and no compile break — the
provider additions are default interface methods and every view property is additive with a `false`
or existing-behaviour default.

The residual risks, in order:

1. **Gesture arbitration on Android** — the `Completed`-reports-zero-totals asymmetry and
   `ScrollView` interaction are where this will break first. Mitigated by committing from the last
   `Running` values and by manual per-platform verification.
2. **Catalyst / AppKit / GTK4** — the plan deliberately uses `PanGestureRecognizer` (not
   `Drag`/`DropGestureRecognizer`) everywhere, so unlike `TreeView` there is no `#if` split and no
   dependency on the broken Catalyst drag recognizers (dotnet/maui#23627). Desktop gets the same code
   path as mobile with the activation delay elided for mouse.
3. **Blazor WASM frame budget** — addressed by keeping `pointermove` entirely in JS. If
   `DragValidationMode="PerPosition"` is used on WASM, expect visible lag; document it as such.
4. **Overlap recompute cost** — `DetectOverlaps` is O(n²) within a cluster and runs on every commit.
   Fine for typical day loads; if a day carries hundreds of events, the relayout will be felt. Not
   worth optimising until reported.

## File-by-file summary

**MAUI — `src/Shiny.Maui.Controls/Scheduler/`**

| File | Change |
| --- | --- |
| `ISchedulerEventProvider.cs` | + `CanChangeEvent`, `CanChangeEventTo`, `OnEventChanged` (default impls) |
| `Models/SchedulerEventChange.cs` | **new** — change record + `SchedulerEventChangeKind` |
| `Models/SchedulerEventChangeFailure.cs` | **new** — change + exception, for `EventChangeFailed` |
| `Internal/AgendaGeometry.cs` | **new** — pure minutes/pixels/snap/DST math |
| `Internal/AgendaDragController.cs` | **new** — arm/update/commit/cancel state machine, auto-scroll |
| `Internal/AgendaTimelinePanel.cs` | + pan recognizers, resize grips, snap guide, tap suppression; `Build` refactored onto `AgendaGeometry` |
| `SchedulerAgendaView.cs` | + 7 bindable properties, `EventChangeFailed`, `RelayoutDay`, drag-aware `Rebuild`/pinch guards, owns the controller |

**Blazor — `src/Shiny.Blazor.Controls/`**

| File | Change |
| --- | --- |
| `Scheduler/ISchedulerEventProvider.cs` | mirrors the MAUI additions |
| `Scheduler/SchedulerEventChange.cs` | **new** (with `string?` colour on the referenced event) |
| `Scheduler/AgendaGeometry.cs` | **new** — port of the MAUI math |
| `Scheduler/SchedulerAgendaView.razor` | + `@ref`/`@inject`, data attributes, grips, JS module lifecycle, `[JSInvokable] OnDragArm`/`OnDragCommit`, `IAsyncDisposable` |
| `Scheduler/SchedulerAgendaView.razor.css` | + drag/grip/guide styles, `touch-action: none` |
| `wwwroot/scheduler-agenda.js` | **new** — pointer-event drag, snapping, ghost/guide, auto-scroll, click suppression |

**Tests / samples / docs** — as listed in [Testing](#testing), [Sample](#sample) and
[Docs & required updates](#docs--required-updates-per-claudemd).
