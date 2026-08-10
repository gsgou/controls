# Soft Keyboard Service

A global, injectable controller for the on-screen keyboard — observe it, dismiss it, move between
fields, and dock a custom accessory bar to it.

**Status:** spec only. Nothing implemented.
**Supersedes:** the earlier accessory-only draft. The accessory bar is now one surface of a service,
not the whole feature.

> **Scope: MAUI only (iOS + Android).** This is a deliberate, documented platform-only feature, in
> the same category as Desktop being MAUI-only and SheetView/Kiosk being Blazor-specific. See
> [Why Blazor is out of scope](#why-blazor-is-out-of-scope) — Blazor gets one small, unrelated bug
> fix instead, tracked separately below.

## Context

### What each platform allows

| Platform | Observe height/visibility | Show / hide | Custom accessory bar |
|---|---|---|---|
| **iOS / Catalyst** | `UIKeyboard` notifications | `become/resignFirstResponder` | **Yes** — `UIResponder.inputAccessoryView`, a real OS-docked view |
| **Android** | `WindowInsets.Type.ime()` | `InputMethodManager` | **No** — the IME is a separate process; only an IME app can draw in it |
| **Windows** | `InputPane.OccludedRect` | `InputPane.TryShow/TryHide` | No |
| **Blazor (mobile web)** | `visualViewport` / `env(keyboard-inset-height)` | `blur()` reliably; show only inside a user gesture | No — **and iOS Safari already draws its own** (‹ › Done), which we can't remove |
| **AppKit / GTK4 / net10.0** | — | — | No soft keyboard exists |

Only iOS has a true accessory API. So the bar is specified as **"a bar pinned to the top edge of the
keyboard"**, not "a view inside the keyboard": iOS satisfies that with the real
`inputAccessoryView`, everyone else renders our own bar glued to the keyboard inset. Same contract,
different mechanism.

### What MAUI already ships — verified against Microsoft.Maui 10.0.71

This matters more than anything else in the plan, because it decides how much is actually ours to
build. Confirmed present in the shipped assemblies:

| API | What it does |
|---|---|
| `Microsoft.Maui.Platform.SoftInputExtensions` — `IsSoftInputShowing`, `ShowSoftInputAsync`, `HideSoftInputAsync` | Show/hide/query — **but every method takes an `ITextInput`.** You must already hold the control. |
| `Page.HideSoftInputOnTapped` | Tap-outside-to-dismiss, built in |
| `SafeAreaEdges` + `SafeAreaRegions.SoftInput` (.NET 10) | Declarative keyboard avoidance per edge |
| `KeyboardAutoManagerScroll` (iOS) | Scrolls the focused field into view automatically |
| `WindowSoftInputModeAdjust` (Android platform-specific) | `AdjustPan` / `AdjustResize` |

**We are not building any of that.** The service wraps it.

### The actual gaps

1. **No targetless control.** `HideSoftInputAsync` needs the `ITextInput`. A ViewModel that wants to
   dismiss the keyboard after a command completes has no supported way to do it — the usual hack is
   walking the visual tree hunting for the focused `Entry`.
2. **No state, no events.** Nothing public tells you the keyboard's height, whether it's up, or how
   long its animation runs. Every app hand-rolls it — **including us**: `ChatView` observes
   `UIKeyboard.Notifications` directly in `Platforms/iOS/ChatView.iOS.cs`. This is the biggest gap and
   the one with the most downstream value.
3. **No accessory bar**, on any platform, at any level.
4. **No field navigation** — no prev/next across a form. Android has literally nothing here.
5. **No hardware-keyboard signal** — you can't tell an iPad with a Magic Keyboard from a phone.
Blazor shares only gap 2, and for different reasons and with a different fix — see
[Why Blazor is out of scope](#why-blazor-is-out-of-scope).

That's a coherent, well-bounded product: **a service that fills MAUI's observation gap and adds a
targetless command surface, plus the one control (the accessory bar) that needs it.**

### What this is NOT

- **Not `OnScreenKeyboard`** (`Shiny.Maui.Controls.Desktop` / `Shiny.Blazor.Controls`). That
  *draws keys* — a replacement keyboard for kiosks. This service *observes and commands the OS
  keyboard* and never draws a key. Opposite problems, and the naming will confuse people unless the
  docs open by saying so.
- **Not a keyboard-avoidance layout.** .NET 10's `SafeAreaEdges` already does that declaratively, and
  it does it better than we would. We expose state for the cases it can't cover (a custom overlay, a
  `FloatingPanel`, a `Toast`).
- **Not a custom IME.** Shipping an Android keyboard app is an application type, not a control.

## Guiding principle

**Wrap, don't reimplement.** On MAUI, `HideAsync()` resolves the currently focused `ITextInput` and
delegates to `SoftInputExtensions`. We do not write our own `resignFirstResponder` /
`InputMethodManager` paths — that's how you inherit bugs Microsoft already fixed. Our net-new code is
(a) focus tracking, (b) inset observation, (c) the accessory bar, (d) field navigation.

## Package placement

`Shiny.Maui.Controls`, namespace `Shiny.Maui.Controls.Keyboard`, xmlns
`http://shiny.net/maui/controls` (register in `GlobalXmlns.cs`). No new NuGet package, no new
dependencies. Not an add-on: it's infrastructure other controls consume.

Compiles into the `net10.0`, `-ios` and `-android` TFMs the project already targets; the plain
`net10.0` build gets the no-op implementation.

**Naming decision needed.** `ISoftKeyboard` matches the platform vocabulary (`SoftInputExtensions`,
`SoftInputMode`, `softInputMode`) and reads correctly. Risk: it sits one adjective away from the
existing `IOnScreenKeyboard`, and "soft keyboard" / "on-screen keyboard" are synonyms in normal
English. Alternative: `IKeyboardManager`. Recommending `ISoftKeyboard` on vocabulary grounds — flagging
it because it's cheap now and breaking later.

## The service

```csharp
namespace Shiny.Maui.Controls.Keyboard;

public interface ISoftKeyboard
{
    // ---- state ----------------------------------------------------------

    /// <summary>True while the soft keyboard is on screen.</summary>
    bool IsVisible { get; }

    /// <summary>Keyboard height in DIPs/CSS px, excluding any Shiny accessory bar. 0 when hidden.</summary>
    double Height { get; }

    /// <summary>Total occluded height — keyboard + accessory bar. What layout actually cares about.</summary>
    double TotalInset { get; }

    /// <summary>True when a physical keyboard is attached (iPad w/ Magic Keyboard, Android w/ BT keyboard).</summary>
    bool IsHardwareKeyboardAttached { get; }

    /// <summary>The input currently holding focus, if any. Null when focus is elsewhere.</summary>
    ITextInput? FocusedInput { get; }

    event EventHandler<KeyboardStateEventArgs> StateChanged;

    // ---- commands -------------------------------------------------------

    /// <summary>Dismiss the keyboard. No target needed — resolves the focused input internally.
    /// Returns false when nothing was focused. Reliable on every platform.</summary>
    ValueTask<bool> HideAsync(CancellationToken ct = default);

    /// <summary>Focus <paramref name="target"/> and raise the keyboard. Best-effort: Android may
    /// refuse without window focus, and browsers refuse outside a user gesture. Check the result.</summary>
    ValueTask<bool> ShowAsync(ITextInput target, CancellationToken ct = default);

    // ---- focus navigation -----------------------------------------------

    ValueTask<bool> MoveNextAsync(CancellationToken ct = default);
    ValueTask<bool> MovePreviousAsync(CancellationToken ct = default);
    bool CanMoveNext { get; }
    bool CanMovePrevious { get; }
}

public class KeyboardStateEventArgs : EventArgs
{
    public bool IsVisible { get; init; }
    public double Height { get; init; }
    public double TotalInset { get; init; }
    public TimeSpan AnimationDuration { get; init; }   // Zero where the platform doesn't report one
}
```

Design notes that are load-bearing:

- **There is no `Show()` without a target.** "Raise the keyboard" is meaningless without something to
  focus, and pretending otherwise produces an API that silently no-ops. `ShowAsync` takes the target
  and returns `bool`, because on Android and on the web it genuinely can fail for reasons the caller
  can't control.
- **`HideAsync` is the star.** It's the one thing that's reliable everywhere and that MAUI cannot do
  without a control reference. Expect it to be 80% of real usage.
- **`ValueTask` even though most of this could be synchronous** — `SoftInputExtensions` is already
  `Task`-returning, Android's show is genuinely async, and it leaves the door open if a Blazor
  implementation is ever justified. (It currently isn't — see
  [Why Blazor is out of scope](#why-blazor-is-out-of-scope).)
- Registered `TryAddSingleton` so it's replaceable, like `IToaster` / `IDialogService`.
- Marshals to the main thread internally; `StateChanged` always fires on the UI thread.

### Registration

```csharp
builder.UseShinyControls(cfg => cfg.ConfigureKeyboard(x =>
{
    // The killer default: iOS numeric/decimal keyboards have no return key.
    x.AutoAttachAccessory = KeyboardAutoAttach.NumericKeyboardsOnly;  // None | NumericKeyboardsOnly | AllInputs
    x.AutoAttachPreset    = KeyboardAccessoryPreset.Done;
    x.BarHeight           = 44;
    x.ShowOnHardwareKeyboard = true;   // iOS floats the bar with a hardware keyboard; false hides it
}));
```

Follows the existing `ConfigureDialogs` shape in `MauiAppBuilderExtensions.cs`.

## Surface 1 — the accessory bar

### Tier 1: attached property on any input

```xml
<Entry Keyboard="Numeric" shiny:KeyboardAccessory.Items="NavigationAndDone" />
<shiny:TextEntry Placeholder="Notes" shiny:KeyboardAccessory.View="{StaticResource MyBar}" />
```

```csharp
public static class KeyboardAccessory
{
    public static readonly BindableProperty ViewProperty;       // KeyboardAccessoryView
    public static readonly BindableProperty ItemsProperty;      // KeyboardAccessoryPreset
    public static readonly BindableProperty GroupProperty;      // string? — field-nav grouping
    public static readonly BindableProperty OrderProperty;      // int — nav order override
    public static readonly BindableProperty IsEnabledProperty;  // bool — opt out of AutoAttach
}

public enum KeyboardAccessoryPreset { None, Done, Navigation, NavigationAndDone }
```

Attaches to `Entry`, `Editor`, `SearchBar`, and Shiny's `TextEntry` / `BorderlessEntry` /
`AutoCompleteEntry` / `AddressEntry`. **`TextEntry` composes a `BorderlessEntry` internally** — the
attached property must forward to the inner input, not sit on the wrapper. Same for
`AutoCompleteEntry`. Most likely place to get it silently wrong.

### Tier 2: one bar for the page

```xml
<shiny:ShinyContentPage>
    <shiny:ShinyContentPage.KeyboardAccessory>
        <shiny:KeyboardAccessoryView>
            <shiny:KeyboardNavigationItem Direction="Previous" />
            <shiny:KeyboardNavigationItem Direction="Next" />
            <shiny:KeyboardAccessorySpacer />
            <shiny:KeyboardAccessoryItem Text="Done" Command="{Binding DoneCommand}" />
        </shiny:KeyboardAccessoryView>
    </shiny:ShinyContentPage.KeyboardAccessory>
    <!-- page content -->
</shiny:ShinyContentPage>
```

For a plain `ContentPage`, the same via `KeyboardAccessoryHost` in the layout — mirroring how
`OverlayHost` relates to `ShinyContentPage`.

Resolution order for a focused input: attached `View` → attached `Items` → page-level bar → global
`AutoAttachAccessory` policy → nothing.

### Tier 3: bring your own

`KeyboardAccessoryView.Content` takes any `View`, so tier 3 is tier 2 with your own layout. No
`DataTemplate` mechanism needed — unlike `SplashScreen`, there's always a live visual tree.

### The bar and its items

```csharp
public class KeyboardAccessoryView : ContentView
{
    // BarHeight (44), BarBackgroundColor, BarBorderColor, ItemSpacing (4), IsSafeAreaAware (true)
}

public class KeyboardAccessoryItem : ContentView { }        // Icon / Text / Command / Tapped
public class KeyboardNavigationItem : KeyboardAccessoryItem // Direction: Previous | Next
{ }                                                          // auto-disables at group boundaries
public class KeyboardAccessorySpacer : View { }              // flexes
```

Item visuals reuse `TextEntryTool`'s construction shape verbatim — icon + label +
`TapGestureRecognizer`, `StyleGuard.MarkReady` as the last constructor line. Two near-identical
classes is the wrong outcome; extract a shared base if the overlap is total.

## Surface 2 — attached behaviours

Thin conveniences over the service. Each must justify itself against what MAUI already does:

| Behaviour | Ship it? |
|---|---|
| `Keyboard.DismissOnTapOutside` | **No** — `Page.HideSoftInputOnTapped` already exists. Document it instead. |
| `Keyboard.DismissOnScroll` | **Yes** — common on forms, nothing built in |
| `Keyboard.FocusOnAppearing` | **Yes** — with the caveat that it's best-effort on Android |
| `Keyboard.ReturnKeyMovesNext` | **Yes** — wires `Completed` to `MoveNextAsync`; trivial, universally wanted |
| Keyboard-avoiding container | **No** — `SafeAreaEdges` + `SafeAreaRegions.SoftInput` covers it |

## Surface 3 — field navigation

`MoveNextAsync` / `MovePreviousAsync` resolve an ordered list of navigable inputs:

1. Collect `InputView` descendants of the host page.
2. Filter to `IsEnabled && IsVisible && !IsReadOnly`, and to the matching `Group` when set.
3. Sort by attached `Order`, else `VisualElement.TabIndex`, else depth-first visual-tree order.

Keep the resolver **pure and platform-free**. It's the only part of this feature with real logic and
the only part worth unit tests.

**Documented limitation:** virtualized containers (`TableView`, `CollectionView`, `DataGrid`) only
realize visible rows, so navigation can't reach an unrealized field. Ship the limitation documented;
revisit with `ScrollTo`-then-focus if it bites.

## Platform implementation

### Shared inset tracking — build this first

Extract the iOS keyboard-notification logic in `Platforms/iOS/ChatView.iOS.cs` into a single
`KeyboardStateTracker` behind `ISoftKeyboard`, and write the Android half that never existed.

Not refactoring for its own sake. Today:

- **`ChatView.AdjustForKeyboard` works on iOS and silently does nothing on Android** — the
  `HookKeyboard`/`UnhookKeyboard` partials have no Android implementation. That is a live, shipped bug.
- `FloatingPanel` has no keyboard awareness; a panel with an input in it gets covered.
- `DialogService` dialogs have the same problem.

One tracker fixes all three, and it's a prerequisite for the Android accessory bar regardless. **This
phase stands alone and delivers real value even if nothing else ships.**

Sequencing note: fixing ChatView's Android behaviour is a change to a shipped control and needs its
own release-notes entry, not a footnote.

### iOS / Mac Catalyst

State from `UIKeyboard.Notifications.ObserveWillChangeFrame` / `ObserveWillHide` — the shape ChatView
already uses, including `AnimationDuration`, which is what lets consumers animate in lockstep.

Accessory via handler mappers appended in `UseShinyControls`, next to the existing `ShinyBorderless`
mapper (`EntryHandler` / `EditorHandler` / `SearchBarHandler`). Resolve the bar, realize it with
`view.ToPlatform(handler.MauiContext)`, assign to `PlatformView.InputAccessoryView`.

Traps, all of which have bitten people:

- **Zero height.** A `UIView` handed to `inputAccessoryView` gets no MAUI layout pass. You must
  `((IView)bar).Measure(...)` + `Arrange`, set an explicit `Frame`, and set
  `AutoresizingMask = FlexibleWidth`. Skip it and the bar "doesn't appear" — it's there, 0pt tall.
- **Swapping while focused** does nothing until `ReloadInputViews()`.
- **Rotation** needs a re-measure on `TraitCollectionDidChange`.
- **Hardware keyboard:** iOS/iPadOS still floats the accessory at the bottom of the screen — that's
  what `ShowOnHardwareKeyboard` gates. On Catalyst proper it generally won't appear at all.
- **One `UIView`, one superview.** A page-level bar can't be live on two inputs at once. See open
  questions.

### Android

No accessory API exists, so we render a MAUI `View` in our own window and translate it onto the IME.

- Host the bar in the existing `OverlayHost` layer (bottom-anchored) rather than plumbing a native
  view into the Activity decor — keeps theming, `StyleGuard` and layout consistent. That's why tier 2
  lives on `ShinyContentPage`.
- Drive `TranslationY` from `WindowInsetsCompat.Type.Ime()` via `ViewCompat.SetOnApplyWindowInsetsListener`.
- **API 30+: `ViewCompat.SetWindowInsetsAnimationCallback` with `DispatchMode.Stop`** so the bar moves
  in lockstep with the keyboard animation. Without it you get a visible pop. This is the whole
  difference between "native" and "bolted on" — not polish.
- Below API 30, `WindowInsetsAnimationCompat` approximates. Accept a jumpier animation and say so
  rather than shipping a bespoke fallback.
- **Android 15 / API 35 forces edge-to-edge**, where `adjustResize` is effectively ignored. The IME
  inset path is the only correct implementation there — build it that way from the start.
- `HideAsync` delegates to `SoftInputExtensions`; do **not** hand-roll `InputMethodManager`.
- **Never use `ZIndex` to raise the bar.** MAUI implements `ZIndex` by removing and re-adding the
  native child, firing `ACTION_CANCEL` and killing in-flight gestures. Use `BringToFront`.
- **Never swap `Shadow` on focus.** Assigning `VisualElement.Shadow` in a focus handler unfocuses the
  input — exactly how TextEntry typing broke on Android. Build one `Shadow` and toggle `Opacity`.
- Hardware keyboard: `Resources.Configuration.HardKeyboardHidden` / `Keyboard`.

### Windows

`InputPane.GetForCurrentView()` `Showing`/`Hiding` + `OccludedRect` for state; `TryShow`/`TryHide`
exist but `SoftInputExtensions` already covers the commands. Low priority — the touch keyboard is a
narrow scenario. If it slips, degrade to "never visible", don't throw.

### net10.0 / AppKit / GTK4

No soft keyboard. `ISoftKeyboard` resolves to a no-op: `IsVisible == false`, `Height == 0`,
`HideAsync` returns false. Attached properties compile and do nothing.

**Note the alt-heads gap:** AppKit and GTK4 ship their own handler types, so `EntryHandler.Mapper`
hooks never run there. Don't rely on the mapper firing — the no-op service must be the guarantee.

## Why Blazor is out of scope

Taken member by member, the service earns almost nothing on the web:

| Member | Blazor value | Why |
|---|---|---|
| Accessory bar | **Negative** | iOS Safari already draws its own bar above the keyboard and won't let us remove it — ours stacks on top and the user sees two. Chrome Android draws none, so behaviour also diverges across the only two browsers that matter. |
| Field navigation | **Low** | Safari's own bar already does prev/next. `tabindex` covers the rest. |
| `ShowAsync` | **~None** | Gesture-gated; can't be invoked from a command handler, which is the only reason you'd want it. |
| `HideAsync` | **Low** | `document.activeElement.blur()`. One line. Doesn't need DI. |
| `IsHardwareKeyboardAttached` | **None** | Not detectable. |
| **Inset observation** | **Real — but it's a bug fix, not a feature** | See below. |

So there is no `ISoftKeyboard` on Blazor. Building one would be parity theatre.

### The one real Blazor problem — track separately

These Blazor controls are `position: fixed`:

`SheetView`, `DialogHost`, `ToastHost`, `FabMenu`, `Overlay`, `ShinyToolbar`, `ImageViewer`,
`MediaPickerButton`

…and there is **zero `visualViewport` handling anywhere in `src/`**. On iOS Safari the keyboard does
not shrink the layout viewport, so `SheetView.razor.css:2` (`position: fixed; inset: 0`) still spans
the full screen and its bottom-anchored container — including any input inside it — sits behind the
keyboard. **A bottom sheet with a text field is broken on iPhone today.**

The fix is not a service. It is one script that measures the inset and publishes it:

```css
:root { --shiny-keyboard-inset: 0px; }
```

Existing CSS consumes it; no C# API surface, no new component, no parity obligation.

- **iOS Safari:** `inset = window.innerHeight - (visualViewport.height + visualViewport.offsetTop)`.
  Subscribe to `visualViewport` `resize` **and** `scroll` — `position: fixed` drifts during
  keyboard-driven scroll and needs `offsetTop` compensation. Throttle with `requestAnimationFrame`.
- **Chromium / Android:** the viewport resizes by default (`interactiveWidget=resizes-content`), so
  `window.resize` suffices. Optionally honour `navigator.virtualKeyboard.overlaysContent` +
  `env(keyboard-inset-height)` for apps that opt into overlay mode.
- Reports a single `double` over interop — no anonymous types, no array DTOs (both have burned us in
  trimmed/published WASM).
- Testing note: Chrome throttles `setTimeout` to ~1/sec in unfocused tabs, which makes synthetic
  viewport tests in the Blazor sample look frozen. Keep the tab focused.

**This should be its own work item and its own release-notes entry** — a mobile-Safari layout fix to
shipped controls, unrelated to the keyboard service. It does not block, and is not blocked by,
anything below.

**Where it actually pays off:** MAUI Blazor Hybrid. A `BlazorWebView` on iOS/Android *is* the mobile
target — the controls just happen to render in a WebView. Desktop browsers have no soft keyboard and
are unaffected either way.

## Phasing

| Phase | Scope | Value if we stop here |
|---|---|---|
| 1 | `ISoftKeyboard` state + `HideAsync` (iOS + Android), `KeyboardStateTracker`, ChatView migrated onto it | **High.** Fills MAUI's biggest gap and fixes a live Android bug. Shippable alone. |
| 2 | iOS accessory bar via handler mappers, `KeyboardAccessoryView` + items, tier-1 attached properties, `AutoAttach` presets | The headline feature |
| 3 | Android in-window bar with `WindowInsetsAnimation` sync | Parity. Where the schedule risk lives. |
| 4 | Tier-2 page host, field navigation, `Keyboard.*` behaviours | |
| 5 | Windows `InputPane` | Optional |

Phase 1 is the one to commit to. Phases 1–2 are the useful minimum for a release.

**Independent, unblocked, and not part of this feature:** the Blazor `--shiny-keyboard-inset` fix
described in [Why Blazor is out of scope](#why-blazor-is-out-of-scope). Roughly a day. Ship it
whenever — it fixes a live iOS Safari defect and shares nothing with the above but the concept.

## Open questions

1. **Naming.** `ISoftKeyboard` vs `IKeyboardManager`, given `IOnScreenKeyboard` already exists in
   Desktop/Kiosk. Cheap now, breaking later.
2. **Focus tracking mechanism.** How does the service know the focused `ITextInput`? Options: subscribe
   to `Focused`/`Unfocused` on every input via a handler mapper (thorough, touches every control), or
   walk the visual tree on demand (cheap, can be stale). Prefer the mapper; confirm it doesn't
   regress the Android focus issues we've already hit.
3. **Shared bar instances.** Can one `KeyboardAccessoryView` attach to inputs across multiple pages?
   iOS says no. Either document "one bar per page" or realize per-input from a template — decide
   before the API is public.
4. **Hybrid.** In MAUI Blazor Hybrid, does the `BlazorWebView` resize when the keyboard shows? That
   decides whether the Blazor inset fix is needed there at all, or whether the native side already
   covers it. Needs a device test — but it only gates the Blazor fix, not this service.
5. **Bar visibility when the keyboard is down.** On Android edge-to-edge, does the bar sit above the
   gesture bar or vanish? Proposal: visible only while the keyboard is, `IsSafeAreaAware` controls
   padding when it is.
6. **Theming tokens.** New `--shiny-keyboard-bar-*` tokens, or reuse the toolbar tokens? Prefer reuse;
   confirm against `themes/*.json`.

## Docs & release obligations

Per `CLAUDE.md`, when this is built (not now):

- `README.md` — new section under input controls, **marked MAUI-only** the way Desktop is.
- `SKILLS/shiny-controls/keyboard.md`, referenced from `SKILL.md`. Must open by disambiguating from
  `onscreen-keyboard.md`, state plainly which MAUI APIs to use instead for tap-to-dismiss and
  keyboard avoidance, and say up front that there is no Blazor equivalent — otherwise generated code
  will invent one.
- Docs repo (`~/Desktop/dev/documentation`): `src/content/docs/controls/keyboard/`, a
  `sidebar-topics.mjs` node under `Controls`, and a homepage `<Card>` entry under **Input**.
- **Three** separate release-notes entries, not one: the keyboard service; the ChatView Android
  keyboard fix (behaviour change to a shipped control); the Blazor iOS Safari inset fix.
- Samples: `samples/Sample/Features/Input/KeyboardPage.xaml` wired into `AppShell.xaml` and
  `MauiProgram.cs`. Needs a long scrolling form — the only way field navigation is demonstrable. No
  Blazor sample page; the Blazor fix is exercised through the existing `SheetView`/`DialogHost` pages
  on a real iPhone.
- `TODO: capture screenshots for keyboard` — not part of the feature work.
