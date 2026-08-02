# SplashScreen for .NET MAUI

## Context

`Shiny.Blazor.Controls` now ships **SplashScreen** (`src/Shiny.Blazor.Controls/Splash/`,
`wwwroot/splash.js`, `wwwroot/css/shiny-splash.css`) — a boot splash that paints before Blazor
starts, driven afterwards by `ISplashScreen` + `<SplashScreenHost />`. This plan brings the same
capability to `Shiny.Maui.Controls`.

MAUI startup has two phases and only one is covered:

| Phase | Covered by | Customizable? |
|---|---|---|
| Process start → first frame | `<MauiSplashScreen>` (native, build-time — see `samples/Sample/Sample.csproj:30`) | One static image + a background colour. No text, no progress, nothing at runtime. |
| First frame → **app actually ready** (DI resolved, DB migrated, token refreshed, first sync) | **nothing** | — |

Phase 2 is what every app hand-rolls: a `LoadingPage` that navigates away, or `IsBusy` on the first
page. Both leak the seam — the native splash vanishes, a flash of empty chrome appears, *then* the
loading UI shows up. That flash is the actual defect, and it is the same one the Blazor control
fixes.

**Framing to keep consistent across both hosts:** the splash is deliberately *not* a drop-in control.
On Blazor because there is no component tree yet; on MAUI because there is no page yet and it must
sit above Shell chrome. Same shape, different reason — which lets one skill doc, one docs page and
one near-identical API cover both.

**This is not `LoadingOverlay`.** `LoadingOverlay` (`src/Shiny.Maui.Controls/Overlay/LoadingOverlay.cs`,
and the free one built into `ShinyContentPage`) is an in-page busy state for a page that already
exists. SplashScreen owns the window before any page is ready and must cover Shell/navigation chrome.
The docs should say so explicitly so the two do not get conflated.

## Public API

Registration follows the existing `ConfigureDialogs` pattern in
`ShinyControlConfiguration` (`src/Shiny.Maui.Controls/MauiAppBuilderExtensions.cs`):

```csharp
builder.UseShinyControls(cfg => cfg.ConfigureSplashScreen(x =>
{
    x.ContinueNativeSplash = true;                       // tier 1 — derive art + colour from <MauiSplashScreen>
    x.Title = "My App";                                  // tier 2 — built-in properties
    x.Subtitle = "by Contoso";
    x.Spinner = SplashSpinner.Ring;                      // Ring | Dots | Bar | Pulse | None
    x.MinimumDuration = TimeSpan.FromMilliseconds(600);  // anti-flicker on warm starts
    x.FadeDuration    = TimeSpan.FromMilliseconds(300);
    x.FailSafe        = TimeSpan.FromSeconds(30);        // TimeSpan.Zero disables
    x.ContentTemplate = null;                            // tier 3 — DataTemplate, fully custom

    x.Until = async (sp, splash) =>
    {
        await splash.SetStatusAsync("Loading accounts…");
        await splash.SetProgressAsync(0.3);
        await sp.GetRequiredService<IAccountService>().LoadAsync();
    };
}));
```

`ISplashScreen` is **byte-identical to the Blazor interface** (`src/Shiny.Blazor.Controls/Splash/ISplashScreen.cs`):

```csharp
public interface ISplashScreen
{
    ValueTask<bool> IsVisibleAsync();
    ValueTask SetStatusAsync(string? text);
    ValueTask SetProgressAsync(double? value);   // 0..1 clamped; null = indeterminate
    ValueTask HideAsync(int? fadeMs = null);     // idempotent
}
```

Keep the `ValueTask` signatures even though MAUI could be synchronous. The parity is the point —
one skill file, one docs page, and code that ports between hosts unchanged. Implementation marshals
to the main thread internally.

### The three customization tiers map 1:1 to Blazor

| Tier | Blazor | MAUI |
|---|---|---|
| 1 — zero config | `data-*` attributes on the host div | `ContinueNativeSplash` (derives from `<MauiSplashScreen>`) |
| 2 — config object | `shinySplash.show({...})` | `SplashScreenOptions` properties |
| 3 — bring your own | arbitrary HTML inside the host div | `ContentTemplate` (`DataTemplate`) |

`x.Until` is the analogue of `<SplashScreenHost Until="…" />`, including dismissing in a `finally`
so a startup exception surfaces instead of being trapped behind the splash forever.

## Rendering strategy

Four candidate mechanisms were considered:

| | Mechanism | Covers Shell chrome | Platform code | Seamless native handoff | Verdict |
|---|---|---|---|---|---|
| A | Swap `Window.Page` to a splash page, run work, swap to real root | yes | none | no (hard cut) | opt-in |
| A2 | `PushModalAsync(splash, animated: false)`, pop with fade | yes | none | partial | **Phase 1 default** |
| B | Native view added above the MAUI root | yes | Android/iOS/Windows | yes | **Phase 2, opt-in** |
| C | Page-level overlay (reuse `DialogManager`'s leaf-page attach) | **no** | none | no | rejected |

**C is rejected.** `DialogManager.GetOrCreateOverlay` (`src/Shiny.Maui.Controls/Dialogs/DialogManager.cs`)
attaches to the *leaf* `ContentPage`, so it cannot cover Shell tab/nav chrome and cannot be up before
the first page exists. That is `LoadingOverlay` with extra steps.

**A** has an advantage that is easy to miss: the root page is not *constructed* until after the
startup work, so slow `AppShell` construction happens **behind** the splash instead of in front of
it. Its cost is intruding on `CreateWindow` (`samples/Sample/App.xaml.cs`).

**A2** touches nothing in the consumer's startup wiring, which matters for retrofit — but the root is
built first, so Shell construction is still exposed.

**B** is the only mechanism that can genuinely *continue* the native splash (Android 12+
`SplashScreen.SetOnExitAnimationListener`) and it survives a `Window.Page` swap, so Shell can be
built behind it and cross-faded out.

### Phasing

- **Phase 1 — A2 default, A opt-in via `RootPageFactory`.** Pure managed. Ships on every head the
  package targets, including the plain `net10.0` base TFM used by the macOS AppKit and Linux GTK4
  heads. Removes the large majority of the hand-rolled work.
- **Phase 2 — B behind an opt-in flag**, Android/iOS/Windows only, automatically falling back to
  Phase 1 elsewhere. Same public API either way. Buys the last of the seam at roughly the cost of
  Phase 1 again — **decide after Phase 1 is on device**; it may not earn its keep.

## Zero-config native continuation

`src/Shiny.Maui.Controls/buildTransitive/Shiny.Maui.Controls.targets` already exists as a build hook
(currently only doing the XamlC reference swap). Extend it with a target that reads the consumer's
`@(MauiSplashScreen)` item — `Include`, `Color`, `BaseSize` — and emits a small generated constant
class, so with `ContinueNativeSplash = true` the in-app splash **starts as a pixel-match of the
native one** with no configuration at all.

This is what makes the handoff read as one continuous screen rather than two splashes, it is cheap,
and it has no Phase 2 dependency. Include it in Phase 1.

Caveat to verify: Windows packaged apps take their splash from the appx manifest, so
`MauiSplashScreen`-derived art may not match there. Degrade to tier 2 on Windows if so.

## Files to change (Phase 1)

- **New** `src/Shiny.Maui.Controls/Splash/` —
  `ISplashScreen.cs` (mirror of the Blazor interface), `SplashScreenService.cs`,
  `SplashScreenOptions.cs`, `SplashSpinner.cs`, `SplashScreenView.cs` (the default visual — logo /
  title / subtitle / spinner / progress / status, honouring `ContentTemplate`), `SplashScreenPage.cs`
  (the modal/root host), `SplashScreenManager.cs` (window resolution, min-duration, fail-safe timer,
  fade, idempotent hide).
- `src/Shiny.Maui.Controls/MauiAppBuilderExtensions.cs` — `ConfigureSplashScreen` on
  `ShinyControlConfiguration`; register `SplashScreenOptions` + `ISplashScreen` with `TryAddSingleton`
  alongside `IDialogService`/`IToaster`; hook window creation to raise the splash and run `Until`.
- `src/Shiny.Maui.Controls/buildTransitive/Shiny.Maui.Controls.targets` — new target reading
  `@(MauiSplashScreen)` and emitting the generated defaults.
- `src/Shiny.Maui.Controls/Themes/` — splash tokens so the default visual follows the active theme,
  matching how `shiny-splash.css` falls back to `--shiny-color-*`.
- `samples/Sample/` — a `Features/Splash/` demo page (replay with configurable spinner/colours/fade
  driven through `ISplashScreen`, mirroring `samples/Sample.Blazor/Pages/SplashScreenPage.razor`),
  wired into `AppShell.xaml` + `MauiProgram.cs`; add a real `ConfigureSplashScreen` call to
  `MauiProgram.CreateMauiApp` so the sample boots behind it.

### Phase 2 additions

- `src/Shiny.Maui.Controls/Platforms/Android/` — native overlay above the MAUI root;
  `androidx.core.splashscreen` exit-animation handoff on API 31+.
- `src/Shiny.Maui.Controls/Platforms/iOS/` — `UIView` above the key window.
- **New** `src/Shiny.Maui.Controls/Platforms/Windows/` — the project has no Windows platform folder
  today.
- `src/Shiny.Maui.Controls/Shiny.Maui.Controls.csproj` — see risk 2 (Mac Catalyst TFM).

## Docs & sample updates (per CLAUDE.md — keep in sync)

The Blazor pieces already exist; these are **updates, not new files**. Note the retitles:

- **README.md** — extend the `### SplashScreen (Blazor only)` section to cover both hosts and drop
  "(Blazor only)"; update the parity-table row, which currently reads
  `<MauiSplashScreen>` (native, build-time) → the Blazor control.
- **Local skill** `SKILLS/shiny-controls/splash-screen.md` — add the MAUI half (registration,
  `SplashScreenOptions` table, `ContentTemplate`, the `LoadingOverlay` distinction). Add MAUI triggers
  to `SKILLS/shiny-controls/SKILL.md` and update the control summary line, which currently says
  "Blazor only … No MAUI equivalent by design".
- **Docs repo** (`~/Desktop/dev/documentation`):
  - `src/content/docs/controls/splashscreen/index.mdx` — retitle from **"Splash Screen (Blazor Only)"**,
    change `<PlatformSupport frameworks={["blazor"]} />` to include MAUI, drop the "No MAUI equivalent,
    by design" aside, and add the MAUI sections.
  - `src/sidebar-topics.mjs` — retitle the node, currently `'Splash Screen (Blazor Only)'`.
  - `src/content/docs/controls/release-notes.mdx` — new entry.
  - Homepage `src/content/docs/index.mdx` — the "Status & Feedback" card entry already exists; no change.
- **Screenshot TODO only** — do not capture. Leave `TODO: capture screenshots for splashscreen (MAUI)`.
- Blog posts: only if explicitly requested later.

## Verification

- Build: `dotnet build Build.slnf`.
- iOS + Android + Windows: run `samples/Sample/`, cold-start, and confirm there is **no flash of empty
  chrome** between the native splash and the app — the specific defect this exists to remove. Verify
  the status/progress sequence renders, `MinimumDuration` suppresses flicker on a warm start, and the
  splash covers Shell tab/nav chrome.
- Mac Catalyst / macOS AppKit / Linux GTK4: confirm the managed path works via the base `net10.0` TFM.
- Fail-safe: throw deliberately inside `Until` and confirm the splash is dismissed and the exception
  surfaces rather than being trapped.
- `ContinueNativeSplash`: compare the last native frame against the first in-app frame per platform.
- Regression: an app with **no** `ConfigureSplashScreen` call must behave exactly as today — this is
  opt-in, and `UseShinyControls` is on the hot path for every consumer.
- Screenshots/UI inspection via **mauidevflow** (per CLAUDE.md), on request only.
- Unit tests (`tests/`): options defaulting, progress clamping, idempotent hide, min-duration and
  fail-safe timing.

## Open decisions

1. **`CreateWindow` intrusion.** A2 avoids it entirely; A needs a `RootPageFactory` but wins by
   deferring `AppShell` construction until behind the splash. Ship A2 as the default with A opt-in
   (this plan's assumption), or make A the default?
2. **Mac Catalyst has no TFM in this project.** `Shiny.Maui.Controls.csproj` targets
   `$(BaseTargetFramework);-ios;-android` (+`-windows` on Windows only), so the `#if MACCATALYST` in
   `MauiAppBuilderExtensions.cs` is currently dead code and `Platforms/` holds only `iOS` and
   `Android`. Phase 2's native path would need `-maccatalyst` added, or Mac Catalyst rides the managed
   fallback permanently. This is a repo-wide decision, not a splash-specific one.
3. **Phase 2 at all** — defer until Phase 1 has been seen on device.

## Open risks

1. **A2 modal timing.** The modal push must land after Shell is attached but before the first frame is
   visible; the window between them is small and platform-dependent. Verify on real devices, not just
   simulators — this is the single most likely source of the very flash the feature exists to remove.
2. **Android splash handoff (Phase 2).** API 31+ uses the platform `SplashScreen` API with
   `SetOnExitAnimationListener`; below that it is the theme drawable and behaves differently. Two code
   paths, and the pre-12 one cannot be truly seamless.
3. **Windows appx manifest splash** may not match `MauiSplashScreen`-derived art (see above).
4. **AOT** — `IsAotCompatible=true` on this project. `DataTemplate` + no reflection keeps that intact;
   the generated-defaults class must be a plain constant, not reflection over MSBuild items.
5. **Theme timing.** `ShinyThemeManager.EnsureApplied` is deliberately deferred to the app/page handler
   mapper because `Application.Current` does not exist during builder configuration
   (`MauiAppBuilderExtensions.cs`). The splash renders earlier than any normal control, so it must
   either tolerate unresolved tokens or force `EnsureApplied` first — the comment there warns that
   binding tokens too early crashes the Windows stroke mapper.
