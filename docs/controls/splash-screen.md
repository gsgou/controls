# SplashScreen (Blazor only)

[← All Shiny Controls](../../README.md)

A boot splash that is on screen **before Blazor starts**. It cannot be a Razor component — nothing
Blazor renders exists on the first frame — so it ships as static markup you own in `index.html`
plus a classic `splash.js`, with the managed side (`ISplashScreen` + `<SplashScreenHost />`)
owning only status, progress, and the handoff to the app.

MAUI has no equivalent because it does not need one — use the native `MauiSplashScreen`.

```html
<!-- index.html -->
<link href="_content/Shiny.Blazor.Controls/css/shiny-splash.css" rel="stylesheet" />
...
<div id="app">...</div>

<!-- OUTSIDE #app: Blazor clears #app the moment it attaches the root component -->
<div id="shiny-splash"
     data-shiny-splash
     data-title="My App"
     data-logo="img/logo.svg"
     data-spinner="ring"
     data-min-duration="600"></div>

<script src="_content/Shiny.Blazor.Controls/splash.js"></script>
<script src="_framework/blazor.webassembly.js"></script>
```

```csharp
builder.Services.AddShinySplashScreen();
```

```razor
@* in MainLayout / App.razor *@
<SplashScreenHost Until="StartupAsync" />

@code {
    [Inject] ISplashScreen Splash { get; set; } = default!;

    async Task StartupAsync()
    {
        await Splash.SetStatusAsync("Loading accounts…");
        await Splash.SetProgressAsync(0.4);
        await LoadAsync();
    }
}
```

Customization comes in three tiers — data attributes, a `shinySplash.show({...})` config object,
or your own arbitrary HTML inside the host `<div>` (the script then only binds
`[data-shiny-splash-status]`, `[data-shiny-splash-progress-fill]` and
`[data-shiny-splash-percent]` and owns the fade/hide). A `failSafeMs` timer (30s default)
dismisses the splash if the app fails to boot, so a startup exception is never hidden behind it.
