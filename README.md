# Shiny Controls

A rich, ready-to-use UI controls library for both **.NET MAUI** and **Blazor**. One package per host covers TableView, DataGrid, TreeView, Scheduler, FloatingPanel/OverlayHost, DurationPicker, FrostedGlassView, Toast, Dialogs (owned, animated alert/confirm/prompt/action-sheet), Fab/FabMenu, ShinyToolbar/ShinyTabBar (Blazor), SplashScreen (Blazor), PillView, BadgeView, SecurityPin, SignaturePad, ShinyImage, ImageViewer, ImageEditor, MediaPickerButton, ChatView, ColorPicker, FontPicker, Slider, ProgressBar, Overlay/LoadingOverlay, SkeletonView, AutoCompleteEntry, CountryPicker, AddressEntry, TextEntry, CarouselGallery, ParallaxCollectionView, StaggeredGrid, VirtualizedGrid, and StateView/Wizard (named branches switched by one string, and a multi-step flow built on them with a pointed progress bar, per-step validity gates and conditional steps). Walkthrough and Tooltip round that out: a guided tour that dims the page and cuts an animated spotlight around one control at a time — steps declared together in order, advancing on a command, on a tap of the highlighted control, or on a timer, with a RememberRunKey so onboarding runs once — and the themed tooltip bubble underneath it, which wraps its target or points at one, auto-flips to whichever side has room, and is drawn above the page so nothing can clip it. Blazor additionally gets layout primitives — `VStack`/`HStack`, a responsive `Grid`/`Row`/`Column`, and an `AppLayout` application shell whose left/right panels collapse to hidden, a toolbar rail or fully shown, drag-resize between a min and max, keep their own scroll regions, and can persist and auto-collapse when the shell gets narrow. Motion Icons — 42 animated icons that run on a timer, on hover, on tap or on command — ship in the core packages on both hosts. Sliders come in single-value (Slider) and two-thumb range (RangeSlider) flavors. Markdown, Mermaid Diagrams, Barcodes (1D + 2D, QR codes), Keyframe animation (declarative XAML timelines with seekable, reversible playback), and a cross-platform CameraView (preview, photo/video capture, a pluggable effects pipeline for colour/comic/sketch/blur looks, face masks and AI stylization, plus a pluggable frame-analysis pipeline for barcode/face/motion/OCR/structured-documents) ship as separate add-on packages per host, and a cross-platform MediaElement (local + remote audio/video with a themed, per-element-toggleable transport bar, background audio with OS lock-screen controls, and Picture-in-Picture) ship as separate add-on packages per host. **Desktop-only** features — system tray / status-bar icon and Visual-Studio-style docking — ship in a separate `Shiny.Maui.Controls.Desktop` add-on (Windows, macOS AppKit, MacCatalyst, and Linux). On the web there is no separate add-on: Blazor docking and the touch / kiosk on-screen keyboard both ship in the main `Shiny.Blazor.Controls` package.

[![MAUI NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.svg?label=Shiny.Maui.Controls)](https://www.nuget.org/packages/Shiny.Maui.Controls)
[![Blazor NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.svg?label=Shiny.Blazor.Controls)](https://www.nuget.org/packages/Shiny.Blazor.Controls)
[![MAUI Markdown NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Markdown.svg?label=Shiny.Maui.Controls.Markdown)](https://www.nuget.org/packages/Shiny.Maui.Controls.Markdown)
[![Blazor Markdown NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Markdown.svg?label=Shiny.Blazor.Controls.Markdown)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Markdown)
[![MAUI Mermaid NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.MermaidDiagrams.svg?label=Shiny.Maui.Controls.MermaidDiagrams)](https://www.nuget.org/packages/Shiny.Maui.Controls.MermaidDiagrams)
[![Blazor Mermaid NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.MermaidDiagrams.svg?label=Shiny.Blazor.Controls.MermaidDiagrams)](https://www.nuget.org/packages/Shiny.Blazor.Controls.MermaidDiagrams)
[![MAUI Barcodes NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Barcodes.svg?label=Shiny.Maui.Controls.Barcodes)](https://www.nuget.org/packages/Shiny.Maui.Controls.Barcodes)
[![Blazor Barcodes NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Barcodes.svg?label=Shiny.Blazor.Controls.Barcodes)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Barcodes)
[![MAUI Camera NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Camera.svg?label=Shiny.Maui.Controls.Camera)](https://www.nuget.org/packages/Shiny.Maui.Controls.Camera)
[![Blazor Camera NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Camera.svg?label=Shiny.Blazor.Controls.Camera)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Camera)
[![MAUI Camera AI NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Camera.Ai.svg?label=Shiny.Maui.Controls.Camera.Ai)](https://www.nuget.org/packages/Shiny.Maui.Controls.Camera.Ai)
[![Blazor Camera AI NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Camera.Ai.svg?label=Shiny.Blazor.Controls.Camera.Ai)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Camera.Ai)
[![MAUI Keyframe NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Keyframe.svg?label=Shiny.Maui.Controls.Keyframe)](https://www.nuget.org/packages/Shiny.Maui.Controls.Keyframe)
[![MAUI MediaElement NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.MediaElement.svg?label=Shiny.Maui.Controls.MediaElement)](https://www.nuget.org/packages/Shiny.Maui.Controls.MediaElement)
[![MAUI MediaElement Linux NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.MediaElement.Linux.svg?label=Shiny.Maui.Controls.MediaElement.Linux)](https://www.nuget.org/packages/Shiny.Maui.Controls.MediaElement.Linux)
[![Blazor MediaElement NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.MediaElement.svg?label=Shiny.Blazor.Controls.MediaElement)](https://www.nuget.org/packages/Shiny.Blazor.Controls.MediaElement)
[![Keyframe Export NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Keyframe.Export.svg?label=Shiny.Maui.Controls.Keyframe.Export)](https://www.nuget.org/packages/Shiny.Maui.Controls.Keyframe.Export)
[![Motion Icons NuGet](https://img.shields.io/nuget/v/Shiny.Controls.MotionIcons.Shared.svg?label=Shiny.Controls.MotionIcons.Shared)](https://www.nuget.org/packages/Shiny.Controls.MotionIcons.Shared)
[![MAUI Terminal Theme NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Themes.Terminal.svg?label=Shiny.Maui.Controls.Themes.Terminal)](https://www.nuget.org/packages/Shiny.Maui.Controls.Themes.Terminal)
[![Blazor Terminal Theme NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Themes.Terminal.svg?label=Shiny.Blazor.Controls.Themes.Terminal)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Themes.Terminal)
[![MAUI Aurora Theme NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Themes.Aurora.svg?label=Shiny.Maui.Controls.Themes.Aurora)](https://www.nuget.org/packages/Shiny.Maui.Controls.Themes.Aurora)
[![Blazor Aurora Theme NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Themes.Aurora.svg?label=Shiny.Blazor.Controls.Themes.Aurora)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Themes.Aurora)

## Getting Started

### .NET MAUI

```bash
dotnet add package Shiny.Maui.Controls
```

Register in your `MauiProgram.cs`:

```csharp
var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .UseShinyControls();
```

Add the XAML namespace:

```xml
xmlns:shiny="http://shiny.net/maui/controls"
```

For Markdown controls (separate package):

```bash
dotnet add package Shiny.Maui.Controls.Markdown
```

```xml
xmlns:md="http://shiny.net/maui/markdown"
```

For Mermaid Diagrams (separate package):

```bash
dotnet add package Shiny.Maui.Controls.MermaidDiagrams
```

```xml
xmlns:diagram="http://shiny.net/maui/diagrams"
```

For Barcodes & QR codes (separate package):

```bash
dotnet add package Shiny.Maui.Controls.Barcodes
```

```xml
xmlns:bc="http://shiny.net/maui/barcodes"
```

```xml
<bc:QRCodeView Value="https://shinylib.net" Size="300" />
<bc:BarcodeView Value="5901234123457" Format="Ean13" />
```

Supported formats: QR Code, Aztec, Data Matrix, PDF417, Code 128/39/93, Codabar, EAN-8/13, UPC-A/E, ITF. Output is rendered as PNG via a pure-managed encoder (no SkiaSharp / System.Drawing dependency). Need an SVG string? Call `BarcodeRenderer.RenderSvg(...)` directly.

For Keyframe animation (separate package):

```bash
dotnet add package Shiny.Maui.Controls.Keyframe
```

```xml
xmlns:kf="http://shiny.net/maui/keyframe"
```

For the CameraView (separate package — iOS, Android, Windows, macOS AppKit, Blazor):

```bash
dotnet add package Shiny.Maui.Controls.Camera
```

```csharp
builder
    .UseShinyControls()
    .UseShinyCamera();
```

```xml
xmlns:cam="http://shiny.net/maui/camera"
```

```xml
<cam:CameraView Facing="Back" Filter="None" />
```

For the MediaElement (separate package — iOS, Android, Windows, macOS AppKit, Linux GTK4, Blazor):

```bash
dotnet add package Shiny.Maui.Controls.MediaElement
```

```csharp
builder
    .UseShinyControls()
    .UseShinyMediaElement();
```

```xml
xmlns:media="http://shiny.net/maui/media"
```

```xml
<media:MediaElement Source="https://example.com/clip.mp4" AutoPlay="True" />
```

Live preview with zoom / torch / lens selection, photo + video capture, and a **pluggable effects pipeline**. `CameraView.Effects` is an ordered, live collection applied to the preview, to captured photos **and to recorded video**, and it takes four kinds of effect because there are four genuinely different mechanisms: `IColorEffect` (a `ColorMatrix4x5` — honoured on every platform and surface), `INativeEffect` (a `NativeEffectDescriptor` carrying a Core Image filter, an AGSL shader, an SVG filter and a managed CPU fallback, so spatial looks that need to see a pixel's neighbours are possible at all), `IDrawEffect` (composite over the frame with a `Microsoft.Maui.Graphics.ICanvas`), and `ICaptureEffect` (slow, async, post-capture). Built-ins ship as `CameraEffects.*`: the twelve colour grades (Mono, Noir, Sepia, Invert, Vivid, Cool, Warm, Fade, Chrome, Instant, Tonal) plus five spatial looks — **Comic**, **Sketch**, **Posterize**, **Pixelate** and **Blur**. The older `CameraView.Filter` enum still works and is now sugar: it is applied first in the chain, so it composes predictably with everything else. Because coverage genuinely differs by platform and surface — Windows has no live-preview hook, Android needs API 33 for spatial shaders, recorded video on Android gets draw effects but not pixel effects — **`CameraView.GetEffectSupport(effect)` reports what will actually happen** (`Full` / `ColorOnly` / `StillOnly` / `Unsupported`) so an app can grey a control out instead of shipping one that silently does nothing. **Blazor now runs the whole built-in set** — the twelve colour grades as CSS `filter` shorthands, and all five spatial looks as generated SVG `<filter>` definitions referenced from the same chain (`Pixelate` included, via an `feTile`/`feMorphology` mosaic), live on the preview and baked into `CapturePhotoAsync` stills. Note that Blazor's `Effects` is an `IReadOnlyList<ICameraEffect>` **component parameter** rather than MAUI's live `IList`, so assign a new list to change the chain rather than mutating in place. **Messenger-style face masks** come from `FaceAnalyzer { DetectLandmarks = true }` (which now populates `DetectedFace.Landmarks` with typed `FaceLandmarks` — eyes, nose, mouth, plus derived `EyeCenter`/`EyeDistance`/`Roll` — via Apple Vision and Android MLKit) feeding a `FaceMaskEffect` that pins an image or your own drawing to every tracked face, scaled from eye distance, rotated to head roll, and smoothed because the analysis pipeline drops frames. Zoom is bindable (`Zoom`, clamped to the device's reported `MinZoom`..`MaxZoom`) and, with **`IsPinchToZoomEnabled="True"`** (MAUI), a two-finger pinch on the preview drives that same property — so a slider or view model bound to `Zoom` stays in step with the gesture and neither can leave the supported range. Assign a **single frame analyzer** at a time via `CameraView.Analyzer` — `Shiny.Maui.Controls.Camera.Barcode`, `.Camera.Face`, `.Camera.Motion`, `.Camera.Ocr`, `.Camera.Documents` — to scan barcodes, detect faces/motion, run OCR, or extract structured documents — every document analyzer hands back a strongly-typed record with nullable fields: `Invoice` (with order lines), `Receipt` (line items, per-tax breakdown, subtotal/tip/total), `BusinessCard` (name/title/company + emails/phones/website/address), `DriversLicense` (AAMVA, incl. Canadian provinces + jurisdiction), `HealthCard` (Canadian province-aware: RAMQ/OHIP/MSP/AHCIP…), `CreditCard`, and `Passport` (MRZ). The analyzer can be declared right in XAML (it's the content property), swapped live, or toggled off via `IsEnabled`, and draws styled bounding boxes via the built-in `CameraOverlayView` (`MotionAnalyzer` draws a box per distinct moving region). Detection events **always deliver an array** — a frame can hold several barcodes/faces — so e.g. `BarcodeAnalyzer.BarcodesDetected` fires once per frame with every code. Set `FrameAnalyzer.ScanWindow` (a normalized rect) to **restrict scanning to a region**: only detections centered inside it are reported, and the overlay dims outside it and frames a viewfinder reticle. For OCR the scan window is now pushed all the way down into the engine as a real **region of interest** — `OcrAnalyzer` crops to it rather than only post-filtering what came back — which together with `MinimumTextHeight` and `MinimumInputHeight` (upscale the crop before recognizing) is what makes **small, distant text** legible at all: the platform engines discard text below a minimum height (Apple Vision's default is 1/32 of the frame) and downscale before recognizing, so a license plate or a far-off sign never survives whole-frame OCR. Reach for `TextRecognizer.RecognizeAsync(frame, TextRecognitionOptions, ct)` directly for a custom analyzer; results always come back in full-frame coordinates, never crop space. Detection uses a **gated "scan trigger"** model — boxes draw continuously, but a result is delivered only after you **arm** the analyzer (`CameraView.Scan()` / `ScanCommand`, e.g. from a Fab); the next confirmed detection fires once (single-shot), and an `OnDetected` (`Func<TArgs, Task<bool>>`) returning `true` keeps scanning — no more event firehose. Document analyzers **accumulate fields across a few frames** (`AccumulationFrames`) and fire **once** with the richest merged record, and `MotionAnalyzer` **debounces** its event (`EnterFrames`/`ExitFrames`). Blazor mirrors this with a typed `Analyzer` (`BarcodeAnalyzer`, incl. `ScanWindow`) plus `await camera.RequestBarcodeAsync()` off an `@ref`. For "scan then freeze", call `CameraView.CaptureAndStopAsync()` inside `OnDetected`. **Burn overlays into recorded video** by setting `VideoRecordingOptions.Overlay` to an `IVideoOverlayRenderer` (or the built-in `DelegateVideoOverlay` / `DrawableVideoOverlay`) — anything you draw with a `Microsoft.Maui.Graphics.ICanvas` (watermark, timestamp, reticle, telemetry) is composited into every recorded frame of the saved MP4/MOV, not just shown live (iOS, Mac Catalyst, macOS, Android; Windows falls back to the raw feed for now). When no overlay is supplied the fast native recorder is used unchanged. **Recording quality is yours to set**: `CameraView.VideoQuality` (`Lowest`/`Low`/`Medium`/`High`/`UltraHigh`/`Highest`, defaulting to **1080p**), plus optional `VideoBitrate` and `VideoFrameRate` hints. These are *session* settings — both AVFoundation and CameraX fix the capture resolution when the session is configured — so set them once or bind them to a preference rather than toggling per clip, and a device that cannot deliver the requested rung falls back to the nearest one it supports instead of failing to start. Frame rate is the cheapest lever on thermals and file size for continuously-recording apps. **The camera now follows the device as it rotates** via `CameraView.Orientation` (`Device` by default, or pin it to `Portrait`/`PortraitUpsideDown`/`LandscapeTopLeft`/`LandscapeTopRight`), and it covers the preview, captured stills, recorded video and the frames a frame analyzer sees — on Apple those all hang off connections on one session, so they cannot disagree. This fixes real bugs rather than only adding a knob: every `AVCaptureConnection` defaults to portrait and only the frame-delivery one was ever oriented, so a landscape-held device previewed sideways, recorded sideways through the native movie path and wrote portrait EXIF onto stills, while on Android the target rotation was simply whatever the display happened to be when the use cases were built. The landscape members are named for where the device's **top edge** points because every platform's own `LandscapeLeft`/`LandscapeRight` pair means something different from the others'. A change is **deferred while a recording is in progress** — transposing the pixel buffer under an encoder configured from its first frame corrupts the file rather than rotating it — so apps doing segmented continuous capture should pin an explicit value instead of leaving `Device`. Apps that relied on the old always-portrait output should set `Orientation = CameraOrientation.Portrait`. **Recording no longer takes the user's audio away on Apple platforms.** An `AVCaptureSession` configures and activates the app's shared audio session by default and never with `MixWithOthers`, so starting a recording interrupted whatever was playing (music over CarPlay or Bluetooth, a podcast, a navigation app) and — the direction that is much harder to diagnose — anything starting playback afterwards interrupted the *capture session*, which stops video as well as audio. The handler now owns that configuration: `PlayAndRecord` + `MixWithOthers`, applied only when `VideoRecordingOptions.IncludeAudio` is set, so a video-only recording never touches the audio session at all. `CameraView.MixWithOtherAudio = false` restores the exclusive behaviour for the case where a clean audio track matters more than the user's playback. **A session that iOS takes away now comes back.** AVFoundation suspends a capture session — a phone call, another app claiming the camera, a second foreground app in Split View, or the system throttling under thermal/power pressure — and raises nothing to the app: the preview holds its last frame and a recording silently stops producing any. `CameraView.CameraError` now reports the interruption (backgrounding excepted, since that is ordinary lifecycle) and the session is restarted when the interruption ends or after a recoverable `AVCaptureSession` runtime error, provided `IsActive` is still true. **Frame analysis now runs while video is recording on every platform** — a dash-cam-style app can read signs or plates off its own live feed, and **draw what it found into the recorded frames**: analyzer geometry is normalized upright coordinates and the overlay draws in encoded-frame pixel space, so boxing a detection in the saved file is a straight multiply by `context.Width`/`Height`. That mapping holds because both share a field of view — the same sample buffer on Apple platforms, and a shared CameraX **`ViewPort`** on Android (bound only when analysis and recording run together, so no existing setup's recorded FOV moves). On Android that combination (Preview + VideoCapture + ImageAnalysis) is a guaranteed CameraX combination at LIMITED hardware level, but a fourth use case is not, so `ImageCapture` is dropped for the duration of a recording that has an analyzer attached; `CapturePhotoAsync` says so plainly and photo capture returns when the recording stops. **`Shiny.Maui.Controls.Camera.Ai`** (and **`Shiny.Blazor.Controls.Camera.Ai`**) add two AI features. The **AI document scanner**: an `AiDocumentAnalyzer<TDocument>` (MAUI) / `AiDocumentScanner<TDocument>` (Blazor) that cheaply detects a document is *present* (native Apple Vision / managed edge detection on MAUI; an in-browser heuristic on Blazor — **no OCR**), then ships **just that one frame** to a **Microsoft.Extensions.AI `IChatClient`** for structured extraction — so the model runs only on real documents, not every frame. Strongly-typed via MEAI structured output (or the built-in schema-free `AiDocument`), provider-agnostic (Azure OpenAI / OpenAI / Ollama / …). And the **AI photo stylizer**: `AiPhotoStylizer` is an `ICaptureEffect` that redraws a captured still through a Microsoft.Extensions.AI **`IImageGenerator`** — the "turn my selfie into a comic" flow the photo-toy apps ship. Drop it in `CameraView.Effects` and `CapturePhotoAsync` returns the stylized image; the prompt is yours (comic by default, but watercolour, pixel art and the rest are a string away). It is deliberately a *capture* effect rather than a live one — a model round-trip is seconds of latency and a per-image cost, so it belongs on the shutter, never on a frame loop — and it pairs naturally with the procedural `CameraEffects.Comic` for a matching live viewfinder that costs nothing. On failure it raises `Error` and hands back the original photo, so a model outage never costs the user their capture. Note that `IImageGenerator` is still evaluation-only in MEAI, so consuming apps need `<NoWarn>$(NoWarn);MEAI001</NoWarn>`. See the [CameraView docs](https://shinylib.net/controls/cameraview/).

### Blazor

```bash
dotnet add package Shiny.Blazor.Controls
dotnet add package Shiny.Blazor.Controls.Markdown       # optional
dotnet add package Shiny.Blazor.Controls.MermaidDiagrams # optional
dotnet add package Shiny.Blazor.Controls.Barcodes       # optional
```

Add the `@using` directives — typically in `_Imports.razor`:

```razor
@using Shiny.Blazor.Controls
@using Shiny.Blazor.Controls.Cells
@using Shiny.Blazor.Controls.Sections
@using Shiny.Blazor.Controls.Scheduler
@using Shiny.Blazor.Controls.Markdown
@using Shiny.Blazor.Controls.MermaidDiagrams
@using Shiny.Blazor.Controls.Barcodes
@using Shiny.Controls.Barcodes
```

Most controls need no DI registration at all — drop the component into any `.razor` page and its
scoped CSS and JS module come along with it. A handful are driven by a service (Toast, Dialogs, the
splash screen, the walkthrough store, Docking and the on-screen keyboard), and one call covers all
of them, mirroring MAUI's `UseShinyControls()`:

```csharp
using Shiny.Blazor.Controls;

builder.Services.AddShinyControls();
```

With optional configuration, again shaped like the MAUI side:

```csharp
builder.Services.AddShinyControls(cfg => cfg
    .ConfigureDialogs(o => o.DefaultAnimation = DialogAnimation.Zoom)
    .ConfigureKeyboard(o => o.HeightPx = 320)
    .UseHttpImageDownloader()                    // ShinyImage through your HttpClient (optional)
    .AddDockPanel<ExplorerPanel>("explorer", "Explorer", "📁")
);
```

Every individual `AddShinyToast()` / `AddShinyDialogs()` / `AddShinySplashScreen()` /
`AddShinyWalkthrough()` / `AddShinyDocking()` / `AddShinyOnScreenKeyboard()` call still exists, and
all registrations are `TryAdd`, so the two styles compose in either order. Register à la carte when
you want to keep the WASM payload tight; to replace an implementation, use a `SetCustom*` method or
register your own first — first registration wins.

> All of these services are **scoped**, not singleton: they hold per-user UI state. Under WebAssembly
> the two lifetimes are identical, but on Blazor Server a singleton would show one user's toast,
> dialog or keyboard to every connected user.

#### MAUI → Blazor quick reference

| MAUI (XAML) | Blazor (Razor) |
|---|---|
| `<shiny:TableView>` with `<shiny:TableRoot>` | `<TableView>` (no `TableRoot` wrapper) |
| `<shiny:TreeView>` — `ExpandedIcon`/`CollapsedIcon` are `ImageSource` | `<TreeView TItem="…">` — icons are `RenderFragment` slots; adds keyboard navigation |
| `<shiny:PillView>` | `<Pill>` |
| `<shiny:BadgeView Text="…">` (wraps `Content`) | `<BadgeView Text="…">` (wraps `ChildContent`) |
| `<shiny:FloatingPanel>` in `<shiny:OverlayHost>` | `<SheetView>` with `<SheetContent>` child (Blazor uses CSS overlay) |
| `Value="{Binding Pin}"` (TwoWay) | `@bind-Value="pin"` |
| `IsOpen="{Binding IsOpen, Mode=TwoWay}"` | `@bind-IsOpen="isOpen"` |
| `Command="{Binding DoCommand}"` | `OnClick="DoAsync"` / `Clicked="DoAsync"` |
| `Color` type (e.g. `Colors.Blue`) | CSS color string (e.g. `"#2196F3"`) |
| `Fab.Icon="add.png"` (ImageSource) | `<Fab Icon="+">` (inline text/SVG string) |
| `shiny:CarouselGallery` | `<CarouselGallery>` — `PeekAreaInsets` → `PeekAmount`; adds `ShowIndicators` |
| `shiny:ParallaxCollectionView` | `<ParallaxList>` — `HeaderTemplate` → `HeroTemplate`; Blazor uses a JS scroll listener for the transform |
| `shiny:StaggeredGrid` | `<StaggeredGrid>` — `ItemSelectedCommand` → `ItemSelected` EventCallback |
| `shiny:VirtualizedGrid` | `<VirtualizedGrid>` — `CellPadding` → individual padding props; adds `EnableVirtualization`, `GroupedItems` |
| `ItemTemplate` as `DataTemplate` | `ItemTemplate` as `RenderFragment<object>` |
| `IToaster.ShowAsync(text, cfg => {})` (DI) | `IToastService.ShowAsync(text, cfg => {})` (DI + `<ToastHost />`) |
| `IDialogService.Confirm(...)` (DI; auto-attaches) | `IDialogService.Confirm(...)` (DI + `<DialogHost />`) |
| `<shiny:DataGrid>` + `<shiny:DataGridColumn PropertyName="..."/>` (items as `object`) | `<DataGrid TItem="T">` + `<PropertyColumn Property="x => x..."/>` (generic, `RenderFragment` templates) |
| `<shiny:TextEntry>` | `<TextEntry>` |
| `<shiny:Overlay>` in `<shiny:ShinyContentPage.Panels>` | `<Overlay>` (wraps ChildContent; custom content in `<OverlayContent>` slot) |
| `<shiny:LoadingOverlay>` in `<shiny:ShinyContentPage.Panels>` | `<LoadingOverlay>` (wraps ChildContent) |
| `<shiny:ProgressBar>` | `<ProgressBar>` |
| `<MauiSplashScreen>` (native, build-time) | `SplashScreen` — `index.html` markup + `splash.js`, driven by `ISplashScreen` / `<SplashScreenHost />` |

`ISchedulerEventProvider` is identical across both hosts.

## Controls

> **Styling (MAUI)** — every control can be targeted by an implicit or explicit `Style`, so
> app-wide theming is set once rather than repeated at each usage site:
>
> ```xml
> <!-- App.xaml -->
> <Style TargetType="shiny:PillView">
>     <Setter Property="CornerRadius" Value="10" />
>     <Setter Property="FontSize" Value="12" />
> </Style>
> ```
>
> Leave a colour property unset to inherit the active Shiny theme; setting one explicitly
> overrides the theme default for that instance — permanently. A `ChatView` with
> `MyBubbleColor="#DCF8C6"` keeps that green through every `ShinyThemeManager.SetTheme` call, so omit
> the property unless you mean to pin it.
>
> **What visibly changes when you swap theme packs.** A theme is not just a palette. Alongside the
> colour seeds it can define its own **shape** (corner geometry), **typography** (family, scale,
> weight, tracking), **elevation** (`shadow` / `flat` / `outline` / `glow`, with separate intensity
> and softness), **density** (spacing ramp and control metrics) and **border widths** — so a pack
> restyles the geometry and type of every control, not only its hue. Terminal is square, monospace
> and dense with hairline rings instead of shadows; Ocean is soft, roomy and barely-shadowed; Aurora
> glows instead of casting shadows. Colour alone would not carry that: the neutral ramp is nearly
> identical between packs because a tone-98 near-white has no room to hold a hue, which is why a
> palette-only theme used to leave most controls looking the same.
>
> Numeric appearance properties that a theme owns (`CornerRadius`, `FontSize`, stroke widths)
> default to `-1`, meaning *unset — let the theme decide*. A literal default would have been written
> to the control at construction and beaten the theme permanently rather than merely by default.
> Setting one to a real value still pins it, as before.
>
> **Watch out for implicit `BoxView` styles.** The .NET MAUI project template ships
> `<Style TargetType="BoxView">` with a setter for **`BackgroundColor`** — which paints an opaque
> rectangle *behind* the shape rather than setting the `BoxView`'s own `Color`. Because it is
> implicit it applies app-wide, including to `BoxView`s inside controls, and it turns
> `<BoxView Color="Transparent" />` spacers into solid dark bars, puts dark corners behind rounded
> shapes, and hides gradient `Background`s. Prefer an empty `<Grid HeightRequest="..." />` for
> spacers, and if you want a default separator colour set `Color`, not `BackgroundColor`.

### Scheduler

Calendar and agenda views for displaying events and appointments, powered by `ISchedulerEventProvider`.

| Calendar | Agenda | Event List |
|:---:|:---:|:---:|
| ![Calendar](assets/scheduler1.png) | ![Agenda](assets/scheduler2.png) | ![Event List](assets/scheduler3.png) |

**SchedulerCalendarView** - Month calendar grid with event indicators, swipe navigation, and date selection.

```xml
<shiny:SchedulerCalendarView
    Provider="{Binding Provider}"
    SelectedDate="{Binding SelectedDate}"
    DisplayMonth="{Binding DisplayMonth}" />
```

**SchedulerAgendaView** - Day/multi-day timeline with time slots, overlapping event layout, current time marker, optional timezone columns, and switchable date picker modes (carousel, calendar sheet, or none).

```xml
<shiny:SchedulerAgendaView
    Provider="{Binding Provider}"
    SelectedDate="{Binding SelectedDate}"
    DaysToShow="{Binding DaysToShow}"
    DatePickerMode="Calendar"
    ShowAdditionalTimezones="{Binding ShowAdditionalTimezones}" />
```

**DatePickerMode** options: `Carousel` (default horizontal day picker), `Calendar` (collapsible month calendar with pull-to-expand), `None` (no picker).

**Drag & drop event editing** (agenda timeline only) - drag an event to a new time, across day columns when `DaysToShow > 1`, or drag its top/bottom grip to change its duration. Off by default and additive: with `AllowEventDrag`/`AllowEventResize` unset, no gesture recognizers are attached (MAUI), no JS module is imported (Blazor), and the rendered tree is unchanged.

```xml
<shiny:SchedulerAgendaView
    Provider="{Binding Provider}"
    AllowEventDrag="True"
    AllowEventResize="True"
    DragSnapMinutes="15"
    MinEventDuration="00:15:00"
    AllowCrossDayDrag="True" />
```

| Property | Default | Notes |
| --- | --- | --- |
| `AllowEventDrag` | `false` | Move an event to a new time (and, when `DaysToShow > 1`, another day). |
| `AllowEventResize` | `false` | Drag the top/bottom edge to change duration. |
| `DragSnapMinutes` | `15` | Snap granularity, clamped to 1-60. |
| `MinEventDuration` | 15 min | Resize floor. A move never changes duration. |
| `DragActivationDelay` | 350 ms | Long-press arming delay for touch; mouse never waits. `Zero` arms immediately. |
| `AllowCrossDayDrag` | `true` | Only meaningful when `DaysToShow > 1`. |
| `DragSnapGuideColor` | separator colour | The guide line drawn at the snapped position. |

On touch the drag arms on a long press, so a vertical swipe still scrolls the timeline; with a mouse it starts immediately. The long press is measured from the touch itself, and arming disables the enclosing scroller natively for that one gesture — a press that arms and then never moves is still a tap, and still selects the event. The change is committed optimistically and reverted if the provider says no. All-day events are not draggable, and timed ↔ all-day conversion is not supported.

**SchedulerCalendarListView** - Scrollable event list grouped by day with infinite scroll loading and sticky day headers (`StickyDayHeaders`, on by default, pins the current day's header to the top while scrolling).

```xml
<shiny:SchedulerCalendarListView
    Provider="{Binding Provider}"
    SelectedDate="{Binding SelectedDate}" />
```

The Blazor `SchedulerAgendaView` has the same feature set — `DaysToShow` (1–7 day columns), `DatePickerMode` (`Carousel` / `Calendar` / `None`), `ShowAdditionalTimezones` + `AdditionalTimezones` side-by-side timezone columns, overlap-aware event layout, and an auto-updating current time marker — using CSS color strings instead of `Color`.

**ISchedulerEventProvider** - Implement this interface to supply event data:

```csharp
public class MyEventProvider : ISchedulerEventProvider
{
    public Task<IReadOnlyList<SchedulerEvent>> GetEvents(DateTimeOffset start, DateTimeOffset end) { ... }
    public void OnEventSelected(SchedulerEvent selectedEvent) { ... }
    public bool CanCalendarSelect(DateOnly selectedDate) => true;
    public void OnCalendarDateSelected(DateOnly selectedDate) { }
    public bool CanSelectAgendaTime(DateTimeOffset selectedTime) => true;
    public void OnAgendaTimeSelected(DateTimeOffset selectedTime) { }

    // drag/drop - all three are default interface methods, so existing providers still compile
    public bool CanChangeEvent(SchedulerEvent evt) => true;                    // defaults to false
    public bool CanChangeEventTo(SchedulerEventChange change) => true;
    public async Task<bool> OnEventChanged(SchedulerEventChange change) { ... } // defaults to false
}
```

`CanChangeEvent` defaults to `false`, so a provider that ignores drag/drop can never have its events moved even if an app sets `AllowEventDrag` - the opt-in is required on both the view and the provider. `SchedulerEventChange` carries the event, its original `Start`/`End`, the proposed (already snapped) `NewStart`/`NewEnd`, and a `Kind` of `Move` / `ResizeStart` / `ResizeEnd`. Returning `false` from `OnEventChanged` reverts; throwing reverts and raises `EventChangeFailed` on the view.

On Blazor, events are matched across the JS boundary by `SchedulerEvent.Identifier` (a `Guid` by default) - duplicate identifiers make a drag a no-op rather than move the wrong event. Blazor also has `DragValidationMode`: `OnCommit` (default, no interop while the pointer moves) or `PerPosition` (`CanChangeEventTo` per snap boundary, which is visibly slower on WASM).

### FloatingPanel + OverlayHost

A floating panel overlay system for MAUI. Panels slide in from the bottom or top of the screen with configurable snap positions (detents), optional header peek when closed, backdrop dimming, and feedback. Multiple panels can coexist on the same page without blocking touches on content underneath.

**OverlayHost** is a transparent Grid layer that manages backdrop and touch passthrough for overlay clients (`FloatingPanel`, `Overlay`, `LoadingOverlay`). **ShinyContentPage** is a convenience ContentPage with a built-in OverlayHost.

> **Blazor equivalent — `SheetView`.** Blazor sheets lay their content out inside the band the detent actually puts on screen, so a footer, an action row or a `ChatView` input bar stays reachable at every detent instead of being pushed below the fold. Give sheet content `height: 100%` to fill that band; anything taller scrolls inside the sheet. A full-bleed `HeaderTemplate` is clipped to the sheet's rounded corners.

| Closed | Open | Header (Closed) | Header (Open) | Top (Closed) | Top (Open) |
|:---:|:---:|:---:|:---:|:---:|:---:|
| ![Closed](assets/sheet1.png) | ![Open](assets/sheet2.png) | ![Header Closed](assets/sheet3.png) | ![Header Open](assets/sheet4.png) | ![Top Closed](assets/sheet5.png) | ![Top Open](assets/sheet6.png) |

```xml
<!-- Using ShinyContentPage (recommended) -->
<shiny:ShinyContentPage xmlns:shiny="http://shiny.net/maui/controls">
    <shiny:ShinyContentPage.PageContent>
        <!-- Your page content here -->
    </shiny:ShinyContentPage.PageContent>
    <shiny:ShinyContentPage.Panels>
        <shiny:FloatingPanel
            IsOpen="{Binding IsSheetOpen}"
            Position="Bottom"
            HasBackdrop="True"
            CloseOnBackdropTap="True"
            PanelCornerRadius="16">
            <shiny:FloatingPanel.Detents>
                <shiny:DetentValue Value="Quarter" />
                <shiny:DetentValue Value="Half" />
                <shiny:DetentValue Value="Full" />
            </shiny:FloatingPanel.Detents>
            <!-- Your panel content here -->
        </shiny:FloatingPanel>
    </shiny:ShinyContentPage.Panels>
</shiny:ShinyContentPage>
```

**FloatingPanel Properties:**

| Property | Type | Description |
|---|---|---|
| IsOpen | bool | Show/hide the panel (TwoWay) |
| Position | FloatingPanelPosition | `Bottom`, `BottomTabs`, or `Top` — which edge the panel slides from. Use `BottomTabs` when inside a Shell TabBar to clip above the tab bar |
| Detents | ObservableCollection\<DetentValue\> | Snap positions (Quarter, Half, Full) |
| PanelContent | View | Content displayed in the panel (`[ContentProperty]`) |
| HeaderTemplate | View | Optional header view at the screen edge; shown as a peek bar when closed |
| ShowHeaderWhenClosed | bool | When true, the header peeks from the edge when the panel is closed |
| HasBackdrop | bool | Fade backdrop behind panel |
| CloseOnBackdropTap | bool | Close when backdrop tapped |
| PanelCornerRadius | double | Corner radius |
| HandleColor | Color | Drag handle color |
| ShowHandle | bool | Show/hide the drag handle bar |
| PanelBackgroundColor | Color | Panel background color |
| AnimationDuration | double | Animation speed (ms) |
| ExpandOnInputFocus | bool | Auto-expand when input focused |
| IsLocked | bool | Prevents drag dismiss; code-only control |
| FitContent | bool | Auto-computes detent from content size |
| IsContentScrollEnabled | bool | Wraps content in a ScrollView (default true). Set **false** when content scrolls itself (a `TableView`/`CollectionView`) — nested scroll-views collapse the inner one to near-zero height |
| UseFeedback | bool | Feedback on open, close, and detent snap (default: true) |

**OverlayHost Properties:**

| Property | Type | Description |
|---|---|---|
| BackdropColor | Color | Backdrop color (default: Black) |
| BackdropMaxOpacity | double | Maximum backdrop opacity (default: 0.5) |

**ShinyContentPage Properties:**

| Property | Type | Description |
|---|---|---|
| PageContent | View | Main page content |
| Panels | IList\<IView\> | Collection of FloatingPanel, Overlay, and LoadingOverlay instances |
| BackdropColor | Color | Forwarded to internal OverlayHost |
| BackdropMaxOpacity | double | Forwarded to internal OverlayHost |

Every `ShinyContentPage` also has a **built-in `LoadingOverlay`** — no need to add one to `Panels`. Just bind `IsLoading`; it's brought to the front when shown and never dismisses on a backdrop tap. Customize it with the `Loading*` passthroughs (including a `LoadingContentTemplate` to fully replace the spinner content):

```xml
<shiny:ShinyContentPage IsLoading="{Binding IsBusy}"
                        LoadingMessage="Working on it…"
                        LoadingBlurRadius="8">
    ...
    <!-- optional: fully custom loading content -->
    <shiny:ShinyContentPage.LoadingContentTemplate>
        <DataTemplate>
            <Label Text="Please wait…" TextColor="White" />
        </DataTemplate>
    </shiny:ShinyContentPage.LoadingContentTemplate>
</shiny:ShinyContentPage>
```

| Built-in loading property | Type | Description |
|---|---|---|
| IsLoading | bool | Show/hide the built-in loading overlay (TwoWay) |
| LoadingMessage | string? | Message under the spinner/progress bar |
| LoadingIsIndeterminate | bool | Spinner (true, default) vs determinate progress bar |
| LoadingProgress | double | Progress 0–100 when determinate (TwoWay) |
| LoadingSpinnerColor | Color? | Accent color override |
| LoadingBlurRadius | double | Frosted-glass blur for the loading backdrop |
| LoadingContentTemplate | DataTemplate? | Replaces the default spinner/progress content |
| LoadingOverlay | LoadingOverlay | The underlying overlay instance for advanced use |

`Overlay` also gained `CloseOnBackdropTap` (default `true`) — set `false` to keep an overlay up until dismissed in code.

### DurationPicker

A standalone duration picker control that opens a FloatingPanel for selection with hour/minute pickers and "hr"/"min" labels. Requires `ShinyContentPage` (or an `OverlayHost` in the visual tree).

```xml
<shiny:DurationPicker Duration="{Binding SelectedDuration, Mode=TwoWay}"
                      MinDuration="0:15:00"
                      MaxDuration="8:00:00"
                      MinuteInterval="5"
                      Placeholder="Choose duration" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `Duration` | `TimeSpan?` | `null` | Selected duration (TwoWay) |
| `MinDuration` | `TimeSpan` | `0:00:00` | Minimum duration |
| `MaxDuration` | `TimeSpan` | `24:00:00` | Maximum duration |
| `MinuteInterval` | `int` | `5` | Minute increment step |
| `Format` | `string` | `@"h\:mm"` | Display format string |
| `Placeholder` | `string` | `"Select duration"` | Text shown when no duration selected |

### ShinyImage

A remote image that always shows *something* — placeholder artwork, a loading ring, the image, or
error artwork — with the download itself under your control on MAUI.

<!-- TODO: capture screenshots for shiny-image -->

```xml
<shiny:ShinyImage Uri="{Binding PhotoUrl}"
                  PlaceholderImage="placeholder.png"
                  ErrorImage="broken_image.png"
                  Aspect="AspectFill"
                  HeightRequest="220" />
```

```razor
<ShinyImage Uri="@PhotoUrl"
            PlaceholderUri="/images/placeholder.svg"
            Alt="Profile photo"
            ObjectFit="cover" />
```

**The loading ring picks its own mode.** When the response carries a `Content-Length` the ring fills
to a real percentage; when it does not — a chunked response, or a request still waiting for a
download slot — the same ring spins instead. There is nothing to configure: `Percent` is null in
exactly the cases where a percentage would be a lie.

| Property | Type | Description |
|---|---|---|
| Uri | string? | The image to load. `http`/`https` goes through `IImageService`; anything else loads as a local file |
| Source | ImageSource? | An explicit source (MAUI). Takes precedence over `Uri` and skips the service entirely |
| PlaceholderImage / PlaceholderUri | ImageSource? / string? | Artwork shown before and during the load, **behind** the ring |
| ErrorImage / ErrorUri | ImageSource? / string? | Artwork shown when the load fails |
| LoadingTemplate / LoadingContent | DataTemplate? / RenderFragment&lt;ImageLoadProgress&gt;? | Replaces the ring entirely; context is the live progress |
| ErrorTemplate / ErrorContent | DataTemplate? / RenderFragment&lt;ImageLoadProgress&gt;? | Replaces the error artwork |
| Aspect / ObjectFit | Aspect / string | How the image scales (default `AspectFit` / `contain`) |
| FadeInDuration | uint / int | Fade-in milliseconds once loaded (default 150) |
| RingSize, RingColor, RingTrackColor, ProgressTextColor, ShowProgressText | — | Ring appearance; colours fall back to theme tokens |
| CacheEnabled, CacheDuration | bool, TimeSpan? | Per-image cache participation and expiry override (MAUI) |
| DisableProgress | bool | Blazor: skip the streamed fetch and let the browser load the URL directly |
| State, Progress, IsLoading, LoadError | read-only | The live load state |
| ImageLoaded / ImageFailed | event / EventCallback | Completion callbacks (plus `ImageLoadedCommand` / `ImageFailedCommand` on MAUI) |
| ReloadAsync() | method | Re-fetch, skipping the cache |

#### ImageService (MAUI)

Downloads go through `IImageService`, which caches to memory and disk, caps concurrency, and
collapses concurrent requests for the same URI into a single download — the difference between a
scrolling list issuing one request per unique image and one per visible cell.

```csharp
builder.UseShinyControls(cfg => cfg
    .ConfigureImages(o =>
    {
        o.MaxConcurrentDownloads = 4;                 // requests past this report Queued
        o.DiskCacheDuration      = TimeSpan.FromDays(7);
        o.MaxDiskCacheBytes      = 100 * 1024 * 1024; // LRU-trimmed
        o.MemoryCacheEnabled     = true;
        o.MaxMemoryCacheBytes    = 32 * 1024 * 1024;
        o.MaxMemoryItemBytes     = 2 * 1024 * 1024;   // bigger images stay disk-only
    })
);
```

```csharp
// Clear cached images — e.g. from a "free up space" setting
await imageService.ClearCacheAsync();
await imageService.ClearCacheAsync(oneUrl);
var bytes = await imageService.GetCacheSizeAsync();
await imageService.PrefetchAsync(nextPageUrls);
```

**Bring your own `HttpClient`.** For authenticated images — the one thing a plain `Image` or `<img>`
genuinely cannot do — replace `IImageDownloader`, not the whole service. Caching, queueing and
de-duplication stay where they are:

```csharp
class AuthenticatedDownloader(HttpClient client, ITokenStore tokens) : IImageDownloader
{
    public async Task<ImageDownloadResult> DownloadAsync(ImageRequest request, CancellationToken ct)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, request.Uri);
        msg.Headers.Authorization = new("Bearer", await tokens.GetAsync(ct));

        var response = await client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        return new ImageDownloadResult(
            await response.Content.ReadAsStreamAsync(ct),
            response.Content.Headers.ContentLength   // this is what makes the ring determinate
        );
    }
}

// builder.UseShinyControls(cfg => cfg.SetCustomImageDownloader<AuthenticatedDownloader>());
// cfg.SetCustomImageService<T>() replaces the whole pipeline, caching included.
```

The memory tier caches **encoded bytes, not `ImageSource` objects** — a stream-backed `ImageSource`
is consumed once and platform image handles are bound to the view that realized them, so sharing one
across cells renders blank. Each control builds a fresh `ImageSource.FromStream` from the cached
bytes, which is free; the network round-trip is what was skipped.

#### Blazor

There is no cache layer on Blazor and that is deliberate — the browser already has a well-tuned HTTP
cache, shared across tabs and persisted between sessions. What Blazor *does* add is progress:
`ShinyImage` streams remote images through `fetch` and a `ReadableStream` reader so the ring can show
a genuine percentage, which no `<img>` element can report.

> **CORS caveat.** A plain `<img>` may load a cross-origin image with no server cooperation; `fetch`
> may not. When the streamed load is blocked, the component silently falls back to letting the
> browser load the URL directly — the image still appears, the ring just stays indeterminate. Set
> `DisableProgress="true"` to take that path deliberately.

For authenticated images, register a downloader:

```csharp
builder.Services.AddShinyImages();                          // uses the registered HttpClient
builder.Services.AddShinyImages<AuthenticatedDownloader>(); // or your own
```

### ImageViewer

A full-screen image overlay with pinch-to-zoom, pan, double-tap zoom, and animated open/close transitions.

| Gallery | Viewer |
|:---:|:---:|
| ![Gallery](assets/imageviewer1.png) | ![Viewer](assets/imageviewer2.png) |

```xml
<Grid>
    <!-- Page content with tappable images -->
    <ScrollView>
        <VerticalStackLayout>
            <Image Source="photo.png">
                <Image.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding OpenViewerCommand}"
                                          CommandParameter="photo.png" />
                </Image.GestureRecognizers>
            </Image>
        </VerticalStackLayout>
    </ScrollView>

    <!-- ImageViewer overlays on top -->
    <shiny:ImageViewer Source="{Binding SelectedImage}"
                       IsOpen="{Binding IsViewerOpen}" />
</Grid>
```

**MAUI: remote images.** Both the thumbnail and the full-screen overlay are a `ShinyImage`, so setting `Uri` instead of `Source` brings the whole loading pipeline with it — placeholder artwork, a loading ring that fills to a real percentage, error artwork, and `IImageService` memory + disk caching. Opening the viewer resolves off the cache the thumbnail already filled rather than downloading the picture a second time.

```xml
<shiny:ImageViewer Uri="{Binding PhotoUrl}"
                   PlaceholderImage="blur_thumb.png"
                   ErrorImage="broken_image.png"
                   Aspect="AspectFill"
                   RingSize="32"
                   HeightRequest="180" />
```

| Property | Type | Description |
|---|---|---|
| Uri | string? | Remote or local image to load. `http`/`https` goes through `IImageService` (MAUI only) |
| Source | ImageSource? | An explicit source. Takes precedence over `Uri` and skips the service |
| IsOpen | bool | Show/hide the viewer (TwoWay) |
| Aspect | Aspect | Thumbnail aspect ratio mode (default: AspectFit) |
| OverlayAspect | Aspect | Aspect ratio mode inside the overlay (default: AspectFit) |
| MaxZoom | double | Maximum zoom scale (default: 5.0) |
| PlaceholderImage | ImageSource? | Artwork shown during the load, behind the ring |
| ErrorImage | ImageSource? | Artwork shown when the load fails |
| LoadingTemplate | DataTemplate? | Replaces the ring; binding context is the live `ImageLoadProgress` |
| ErrorTemplate | DataTemplate? | Replaces the error artwork |
| FadeInDuration | uint | Fade-in milliseconds once loaded (default: 150) |
| RingSize / RingColor / RingTrackColor / ProgressTextColor / ShowProgressText | — | Loading-ring styling |
| CacheEnabled / CacheDuration | bool / TimeSpan? | Per-image cache participation and expiry |
| State / Progress / IsLoading / LoadError | — | Read-only load state, mirrored from the thumbnail |
| ImageLoaded / ImageFailed | event | Raised once per load (thumbnail only, so one image is not announced twice) |
| CloseButtonTemplate | DataTemplate? | Custom close button (tapping closes viewer) |
| HeaderTemplate | DataTemplate? | Custom header overlay |
| FooterTemplate | DataTemplate? | Custom footer overlay |
| OpenViewerOnTap | bool | Whether tapping the thumbnail opens the overlay (default: true) |
| UseFeedback | bool | Enable/disable feedback on double-tap zoom (default: true) |

Method: `ReloadAsync()` re-fetches, skipping both cache tiers.

**Features:**
- Pinch-to-zoom with origin tracking
- Pan when zoomed (clamped to image bounds)
- Double-tap to zoom in (2.5x) / reset
- Animated fade open/close with backdrop
- Close button overlay
- Remote loading with caching, progress ring and placeholder/error artwork (MAUI)

On Blazor, `Source` is a URL string handed straight to `<img>` — the loading pipeline is MAUI-only for now.

### ImageEditor

An inline image editor with cropping, rotation, freehand drawing, line and arrow drawing, text annotations with font family and font size selection, and **zoom/pan that stays live in every tool** — pinch (or wheel / zoom buttons) to magnify up to 8x and draw, crop or place text with pixel accuracy, then two-finger drag to pan without leaving the tool. Includes a built-in undo/redo stack, reset-to-original, and export to PNG/JPEG/WEBP at configurable resolutions. Every feature can be toggled on/off, and the default toolbar can be replaced with a custom template.

The default toolbar is a floating rounded bar with vector (not glyph-font) icons, a horizontally scrollable tool row that never clips on narrow screens, a contextual options row for the active tool (colour swatch, pen weights, font pickers), and an action row with undo/redo/reset, a zoom cluster, and save.

| Editor | Crop Mode |
|:---:|:---:|
| ![Image Editor](assets/imageeditor1.png) | ![Crop Mode](assets/imageeditor2.png) |

```xml
<shiny:ImageEditor Source="{Binding ImageSource}"
                   CurrentToolMode="{Binding ToolMode}"
                   AllowCrop="True"
                   AllowRotate="True"
                   AllowDraw="True"
                   AllowTextAnnotation="True"
                   DrawStrokeColor="Red"
                   DrawStrokeWidth="3" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Source | ImageSource? | null | Image to edit (supports file, stream, URI) |
| CurrentToolMode | ImageEditorToolMode | Move | Active tool (Move, Crop, Draw, Text, Line, Arrow) — TwoWay |
| AllowCrop | bool | true | Enable/disable crop tool |
| AllowRotate | bool | true | Enable/disable rotate action |
| AllowDraw | bool | true | Enable/disable freehand drawing |
| AllowTextAnnotation | bool | true | Enable/disable text annotation |
| AllowLine | bool | true | Enable/disable line drawing tool |
| AllowFontSelection | bool | false | Show font picker button in text mode |
| AllowFontSizeSelection | bool | false | Show font size picker button in text mode |
| AllowZoom | bool | true | Enable/disable zoom & pan |
| ZoomLevel | double | 1 | Current zoom factor where 1.0 is fit-to-view — TwoWay |
| MinZoom | double | 1 | Lower zoom bound |
| MaxZoom | double | 8 | Upper zoom bound |
| ShowZoomControls | bool | true | Show the zoom out / % / zoom in / fit cluster in the toolbar |
| ShowToolLabels | bool | true | Show captions under the tool icons |
| ShowStrokeWidthPicker | bool | true | Show pen-weight presets next to the colour swatch |
| StrokeWidthPresets | IList\<double\> | 2, 4, 8 | Pen weights offered by the stroke-width picker |
| ToolbarBackgroundColor | Color | dark scrim | Background of the default toolbar |
| CanUndo | bool | false | Whether undo is available (OneWayToSource) |
| CanRedo | bool | false | Whether redo is available (OneWayToSource) |
| DrawStrokeColor | Color | White | Drawing stroke color — TwoWay |
| DrawStrokeWidth | double | 3 | Drawing stroke width — TwoWay |
| TextFontSize | double | 16 | Text annotation font size |
| TextFontFamily | string? | null | Font family for text annotations (TwoWay) |
| AnnotationTextColor | Color | White | Text annotation color |
| AvailableFonts | IList\<string\>? | null | Font families shown in font picker |
| AvailableFontSizes | IList\<double\>? | null | Font sizes shown in font size picker |
| SaveCommand | ICommand? | null | Invoked with `EditedImage` parameter on save |
| SaveText | string | "Save" | Save button label |
| CropApplyText | string | "Apply" | Crop apply button label |
| CropCancelText | string | "Cancel" | Crop cancel button label |
| ToolbarTemplate | DataTemplate? | null | Custom toolbar (replaces default) |
| ToolbarPosition | ToolbarPosition | Bottom | Toolbar placement (Top or Bottom) |
| UseFeedback | bool | true | Feedback on actions |

**Features:**
- Zoom and pan in **every** tool: pinch anywhere (two fingers), two-finger drag to pan, double-tap to toggle, plus toolbar zoom buttons and a live zoom % readout. Blazor adds mouse-wheel zoom about the cursor and middle-button pan. Crop chrome and hit targets keep a constant on-screen size at any zoom.
- Crop with drag handles, rule-of-thirds grid, dimmed overlay, and dedicated Apply/Cancel toolbar
- 90° rotation (or arbitrary angles)
- Freehand drawing with configurable color and stroke width (constrained to image bounds)
- Line and arrow drawing between two points with configurable color and width
- Inline text annotations placed by tapping the image with optional font family and size selection
- Integrated color picker for draw color
- Font picker and font size picker integration (when `AllowFontSelection`/`AllowFontSizeSelection` enabled)
- Undo/redo for every edit action
- Reset to original image
- Save via `SaveCommand` with `EditedImage` — call `ToStreamAsync(format)` to get PNG, JPEG, or WEBP
- Image border showing the drawable surface area
- Strokes, lines and text record the on-screen image size they were drawn at, so annotations made on a small preview (or while zoomed in) keep their proportions when exported at full resolution

**Commands:** `UndoCommand`, `RedoCommand`, `RotateCommand`, `ResetCommand`, `CropCommand`, `DrawCommand`, `TextCommand`, `LineCommand`, `SaveCommand`, `ZoomInCommand`, `ZoomOutCommand`, `ZoomToFitCommand`

**Methods:** `Undo()`, `Redo()`, `Rotate(float)`, `Reset()`, `ApplyCrop()`, `GetEditedImage()`, `ZoomIn()`, `ZoomOut()`, `ZoomToFit()`

**Events:** `ZoomChanged`

On Blazor the equivalents are `ZoomInAsync()`, `ZoomOutAsync()`, `ZoomToFitAsync()`, `SetZoomAsync(double)`, the `ZoomLevel` property and the `ZoomLevelChanged` callback, plus a `ToolbarActions` render fragment for host-supplied buttons at the trailing edge of the bar.

### MediaPickerButton

A button that adds photos from the gallery and/or camera, compresses/re-encodes each to PNG or JPEG at a chosen quality (with optional max-dimension downscale), caps the count with `MaxPhotos` (added one at a time), and shows the collected photos inline as a tappable carousel (opening the **ImageViewer**, with an optional **Edit** button that reuses the **ImageEditor**) or a compact pinch/zoom overlay. Ships in the base packages (no extra package). MAUI uses the built-in `MediaPicker`; Blazor uses a hidden `<input type="file">` (with `capture` for the camera) and compresses on an offscreen canvas.

<!-- TODO: capture screenshots for media-picker-button -->

```xml
<shiny:MediaPickerButton Photos="{Binding Photos}"
                         AllowGallery="True"
                         AllowCamera="True"
                         AllowPhotoEdit="True"
                         ShowAsCarouselInView="True"
                         MaxPhotos="5"
                         CompressionQuality="85"
                         OutputFormat="Jpeg"
                         PermissionDeniedText="Photo access was denied — enable it in Settings." />
```

| Property | Type | Default | Description |
|---|---|---|---|
| AllowGallery | bool | true | Offer "choose from gallery" |
| AllowCamera | bool | true | Offer "take photo" (chooser shown when both are enabled) |
| AllowPhotoEdit | bool | false | Show an Edit button that opens the ImageEditor |
| PermissionDeniedText | string | "Permission denied…" | Shown when camera/gallery access is denied |
| NoImagesTemplate | DataTemplate? | null | Shown when there are no photos yet |
| ShowAsCarouselInView | bool | true | Inline carousel (true) vs compact preview + pinch/zoom overlay (false) |
| MaxPhotos | int | 1 | Maximum photos (added one at a time) |
| CompressionQuality | int | 92 | Encoder quality percentage (1–100) |
| MaxImageDimension | int | 0 | If > 0, longest edge is downscaled to this many pixels |
| OutputFormat | ImageExportFormat | Jpeg | Output encoding (Png or Jpeg) |
| Photos | IList\<MediaPickerItem\> | empty | Collected photos (TwoWay) |

**Events:** `PhotoAdded`, `PhotoRemoved`, `PhotosChanged` (+ `PhotosChangedCommand`), `PermissionDenied`

On Blazor the equivalent `Shiny.Blazor.Controls.MediaPickerButton` mirrors these as `[Parameter]`s (`OutputFormat` is `"jpeg"`/`"png"`), with `@bind-Photos` over `MediaPickerItem` (each exposing a `DataUri` for `<img src>`).

### MediaElement

> Separate packages: `Shiny.Maui.Controls.MediaElement` (+ `.Linux` for GTK4) and `Shiny.Blazor.Controls.MediaElement`.

Plays local and remote **audio and video** on iOS, Android, Windows, macOS AppKit, Linux GTK4 and Blazor, behind one API. Backed by AVPlayer (Apple), Media3/ExoPlayer (Android), `Windows.Media.Playback` (Windows), GtkMediaFile (Linux) and HTML5 media (Blazor).

```xml
<media:MediaElement Source="https://example.com/clip.mp4"
                    AutoPlay="True"
                    Aspect="AspectFit"
                    ShowVolumeControl="False"
                    EnableBackgroundPlayback="True" />
```

**The transport bar is drawn by Shiny, not the platform.** That is the whole reason each piece toggles on its own: native transport UI is all-or-nothing everywhere except Windows (iOS `AVPlayerViewController` has a single `showsPlaybackControls`, HTML5's `controlsList` only subtracts download/fullscreen/cast, and GTK's `GtkMediaControls` has no knobs at all). Drawing it also means one look across all six targets, themed from your Shiny theme pack — the scrubber picks up `Shiny.Color.Primary` unless you set `SeekBarColor`. Toggle `ShowTransportBar`, `ShowPlayPauseButton`, `ShowSeekBar`, `ShowVolumeControl`, `ShowFullScreenButton`, `ShowTimeLabels` and `ShowPictureInPictureButton` independently; `AutoHideTransportBar` fades the bar after `TransportBarAutoHideDelay` **while playing only**, so a paused frame and an audio-only track keep their controls reachable.

**Commands on MAUI, methods on Blazor.** `PlayCommand`, `PauseCommand`, `StopCommand`, `TogglePlayPauseCommand`, `SeekCommand`, `MuteCommand`, `ToggleFullScreenCommand`, `PictureInPictureCommand` (whose `CanExecute` is false where the platform can't), plus `Play()`, `Pause()`, `Stop()`, `SeekAsync()`, `ToggleMute()`. `SeekCommand` takes a `TimeSpan`, a number of seconds, or a string of either — `CommandParameter="30"` is thirty *seconds*, and `"00:01:30"` is ninety.

**The player outlives the view.** An `IMediaPlayerBackend` owns the platform player and the view is pushed into it, which is what makes the two hard parts work: entering fullscreen hands the same player to a second surface on a modal page (no re-buffering, and your layout is left alone), and backgrounding detaches the video surface entirely while audio keeps running. It is also the extension point — assign `MediaPlayerBackends.Factory` to substitute a fake in tests or plug in your own player.

**Background playback** (`EnableBackgroundPlayback` + `Metadata`) keeps audio going with the device locked and publishes now-playing information to the OS: `MPNowPlayingInfoCenter` + `MPRemoteCommandCenter` on Apple, a Media3 `MediaSession` behind a `mediaPlayback` foreground service on Android, SMTC on Windows, and `navigator.mediaSession` on Blazor. The library contributes the Android service and permissions to your merged manifest, but two opt-ins are the **app's** to make: iOS/Catalyst need the `audio` entry in `UIBackgroundModes`, and Android 13+ needs the `POST_NOTIFICATIONS` runtime grant for the notification to appear.

**Picture-in-Picture** (`TryEnterPictureInPictureAsync()`) is how video stays visible while backgrounded: `AVPictureInPictureController` on iOS/Catalyst, `EnterPictureInPictureMode` on Android 8+, and `requestPictureInPicture()` on Blazor. Android also needs `SupportsPictureInPicture = true` on your activity, and should forward `OnPictureInPictureModeChanged` to `AndroidMediaIntegration.NotifyPictureInPictureModeChanged` so the control learns when the user collapses the window.

**Ask before you offer.** Support genuinely differs, so `Capabilities` (a `MediaPlaybackCapabilities` flags enum: `BackgroundAudio`, `PictureInPicture`, `PlaybackRate`, `Volume`, `BufferProgress`) reports what the current backend will actually honour, and the transport bar hides what it can't. Windows has no per-element PiP API; GTK has no playback-rate control and no buffered-ahead figure; iOS Safari refuses programmatic volume, which the Blazor backend detects by writing a value and reading it back.

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| Source | MediaSource | null | URI, filesystem path, or a `Resources/Raw` file — a bare string in XAML is classified for you |
| AutoPlay | bool | false | Play as soon as the source opens |
| IsLooping | bool | false | Suppresses `MediaEnded` |
| Volume | double | 1 | Clamped 0..1 |
| IsMuted | bool | false | Independent of `Volume` |
| PlaybackRate | double | 1 | Clamped 0.25..4 |
| Position | TimeSpan | 0 | Two-way; read back every `PositionUpdateInterval`, assigning it seeks |
| Duration | TimeSpan | 0 | Read-only; zero until opened, and for live streams |
| CurrentState | MediaElementState | None | None / Opening / Buffering / Playing / Paused / Stopped / Failed |
| BufferedProgress | double | 0 | 0..1, drawn as the scrubber's secondary track |
| Aspect | MediaAspect | AspectFit | AspectFit / AspectFill / Fill |
| KeepScreenOn | bool | false | Inhibits display sleep while playing |
| IsFullScreen | bool | false | Two-way; pushes/pops the fullscreen page |
| EnableBackgroundPlayback | bool | false | See the manifest opt-ins above |
| Metadata | MediaMetadata | null | Title / Artist / Album / ArtworkUri for the OS transport UI |
| Capabilities | MediaPlaybackCapabilities | None | Read-only; what this backend honours |

**Events:** `StateChanged`, `MediaOpened`, `MediaEnded`, `MediaFailed`, `PositionChanged`, `SeekCompleted`, `FullScreenChanged`, `PictureInPictureChanged`.

Blazor mirrors all of it as `[Parameter]`s with `On*` `EventCallback`s, and adds `Poster`. **Linux** ships separately as `Shiny.Maui.Controls.MediaElement.Linux` — there is no Linux target framework, so the GTK4 backend can't live in the main package — and is registered with `UseShinyMediaElementGtk()` instead of `UseShinyMediaElement()`; decoding needs `gtk4-media-gstreamer` (Fedora/Arch) or `libgtk-4-media-gstreamer` (Debian/Ubuntu) installed.

### ChatView

> **v1 beta** — the API may still change.

A modern, **provider-driven** chat UI control with message bubbles, typing indicators, cursor-based load-more paging, reactions, read receipts, a markdown composition toolbar, image attachments, and custom message templates. The control is *styles + layout only* — all data, lifecycle, permissions, and real-time behavior live behind an `IChatSessionProvider` you implement (the same integration pattern as the Scheduler control). You give the control a `Provider` and a `SessionId`; it resolves an `IChatSession`, subscribes to its events, and renders.

![ChatView](assets/chat1.png)

```xml
<shiny:ChatView Provider="{Binding Provider}"
                SessionId="{Binding SessionId}"
                MyBubbleColor="#DCF8C6"
                OtherBubbleColor="White"
                PlaceholderText="Type a message..." />
```

```csharp
public interface IChatSessionProvider
{
    Task<IChatSession> CreateSessionAsync(string[] userIds, CancellationToken ct = default);
    Task<IChatSession> GetSessionAsync(string sessionId, CancellationToken ct = default); // throws ChatSessionException
}

// IChatSession (IAsyncDisposable) exposes: Info, CurrentUserId, GetMessagesAsync (cursor paging),
// SendMessageAsync/ResendMessageAsync/EditMessageAsync/DeleteMessageAsync, ReactToMessageAsync,
// MarkReadAsync, ToggleTypingAsync, InviteUserAsync, LeaveAsync, RenameAsync, and live events
// (MessageReceived, MessageUpdated, MessageDeleted, UserTyping, UserJoined/Left, SessionUpdated, ConnectionStateChanged).
```

| Property | Type | Default | Description |
|---|---|---|---|
| Provider | IChatSessionProvider? | null | The integration provider |
| SessionId | string? | null | Session to resolve via `GetSessionAsync` |
| PageSize | int | 30 | Messages fetched per page |
| OpenImagesInViewer | bool | true | Tapping an image bubble opens the built-in ImageViewer |
| MyBubbleColor | Color | #DCF8C6 | Local user bubble color |
| MyTextColor | Color | Black | Local user text color |
| OtherBubbleColor | Color | White | Default other-user bubble color (overridden by user's BubbleColor) |
| OtherTextColor | Color | Black | Other-user text color |
| ChatBackgroundColor | Color? | null | Background color for the messages area |
| BubbleFontSize | double | 15 | Font size for bubble text (MAUI) |
| BubbleFontFamily | string? | null | Font family for bubble text (MAUI) |
| TimestampFontSize | double | 11 | Font size for timestamps (MAUI) |
| BubbleCornerRadius | double | 18 | Corner radius for bubbles (tail stays at 4) (MAUI) |
| PlaceholderText | string | "Type a message..." | Input placeholder |
| SendButtonText | string | "Send" | Send button label |
| InputBar | ChatEntryView | built-in | The hosted composer — assign your own to replace it, or read it to tweak (`InputBar.MaxLines = 3`) (MAUI only) |
| InputBarBackgroundColor | Color? | theme | Area behind the composer |
| InputBarBorderColor | Color? | theme | Outline of the rounded composer |
| MaxInputRows | int | 6 | How tall the composer grows before it scrolls (Blazor only) |
| InputTemplate | RenderFragment? | null | Replaces the built-in composer entirely (Blazor only) |
| InputLeftToolbar | RenderFragment? | null | Markup added to the left of the composer's control row (Blazor only) |
| InputRightToolbar | RenderFragment? | null | Markup added right of the control row, before send (Blazor only) |
| IsInputBarVisible | bool | true | Show/hide the input bar (set false for read-only chats) |
| ShowTypingIndicator | bool | true | Enable typing indicators |
| ScrollToFirstUnread | bool | false | Anchor initial scroll at the first unread instead of the end |
| InputActions | IList\<ChatInputAction\> | [] | Custom input-bar actions (MAUI only) |
| CustomBubbleActions | IList\<ChatBubbleAction\> | [] | Custom bubble actions appended to the permission-driven set (MAUI only) |
| MessageTemplate | DataTemplate? | null | Single template for all message content (MAUI only) |
| MessageTemplateSelector | DataTemplateSelector? | null | Per-type template selector (MAUI only) |
| UseFeedback | bool | true | Haptic feedback on interactions (MAUI only) |
| AdjustForKeyboard | bool | true | iOS keyboard padding. Leave on inside a `FloatingPanel` — the panel's `ExpandOnInputFocus` raises the sheet, but only this padding lifts the composer clear of the keyboard once the panel is at its top detent. Set false only when something else already handles the overlap (MAUI only) |

**Methods (MAUI):** `ScrollToEnd(bool animate)`, `ScrollToMessage(string messageId, bool animate)`, `SubmitEntry()`, `EntryText` (get/set), `MessageTapped` event (non-image bubble taps).

#### The composer — `ChatEntryView` (MAUI) / `ChatEntry` (Blazor)

The message composer is its own control, laid out as a single rounded card in the AI-chat idiom —
the formatting toolbar sits along the top, the **multiline** auto-growing entry spans the full width
beneath it, and every other control sits on a row below that:

```
┌────────────────────────────────────────────┐
│  B  I  U  S  </>  🔗                       │   ← formatting (only if permitted)
│  How can I help you today?                 │
│  +  [Chat]                Model  🎤   ↑    │   ← LeftToolbar … RightToolbar + send
└────────────────────────────────────────────┘
```

`ChatView` builds and hosts one automatically, so nothing changes for the common case — supply your
own only when you want a different shape, or use it standalone (an AI prompt box, a comment field)
and handle `SendRequested` yourself. It knows nothing about `IChatSessionProvider`: `ChatView`
remains the only thing that talks to the session, pushing state in (`SetBodyPermissions`,
`ShowAttachButton`, `SetInputEnabled`) and listening to events out.

`LeftToolbar` and `RightToolbar` are the slots on that control row — drop a mode picker, a model
label or a mic button into either side of the send button:

```xml
<shiny:ChatView Provider="{Binding Provider}" SessionId="{Binding SessionId}">
    <shiny:ChatView.InputBar>
        <shiny:ChatEntryView PlaceholderText="How can I help you today?"
                             SendButtonText="↑"
                             MaxLines="5">
            <shiny:ChatEntryView.LeftToolbar>
                <Border StrokeShape="RoundRectangle 14" Padding="10,4">
                    <Label Text="Chat" FontSize="13" />
                </Border>
            </shiny:ChatEntryView.LeftToolbar>
            <shiny:ChatEntryView.RightToolbar>
                <Label Text="Model" FontSize="13" VerticalOptions="Center" />
            </shiny:ChatEntryView.RightToolbar>
        </shiny:ChatEntryView>
    </shiny:ChatView.InputBar>
</shiny:ChatView>
```

`ChatEntryView` properties: `Text`, `PlaceholderText`, `MaxLines` (6), `FontSize`, `FontFamily`,
`SendButtonText`/`SendButtonBackgroundColor`/`SendButtonTextColor`, `BarBackgroundColor`,
`ComposerBackgroundColor`, `BorderColor`, `BorderThickness`, `CornerRadius` (24), `ShowAttachButton`,
`ShowActionsButton`, `LeftToolbar`/`RightToolbar` (`IList<IView>`). Events: `SendRequested`,
`AttachRequested`, `ActionsRequested`, `LinkRequested`, `EditCancelled`, `TextChanged`. Methods:
`Submit()`, `ClearText()`, `FocusInput()`, `SetInputEnabled(bool)`,
`EnterEditMode(string)`/`ExitEditMode()`, `SetBodyPermissions(...)`, `ApplyWrap(...)`,
`InsertLink(...)`.

Blazor's `ChatEntry` mirrors it as parameters — `@bind-Text`, `Placeholder`, `SendButtonText`,
`IsEnabled`, `ShowAttach`, `BodyPermissions`, `MaxRows` (6), `SendOnEnter` (true; Shift+Enter inserts
a newline), `LeftToolbar`/`RightToolbar` (`RenderFragment`), plus `OnSend`, `OnAttach` and
`OnTyping`. `ChatView` surfaces the two slots directly as `InputLeftToolbar` / `InputRightToolbar`,
or drop a whole `ChatEntry` into `ChatView.InputTemplate` to replace the built-in composer.

On MAUI the entry is an `Editor`, so **Enter inserts a newline** and sending is the button's job —
matching how AI chat composers behave. There is no hairline rule between the message list and the
composer; the rounded outline is the edge.

**Permissions:** every action affordance is derived from `ChatSessionPermissions` on `ChatSessionInfo` + ownership — `CanSendMessages`, `CanEditMessages`, `CanDeleteMessages`, `CanReactToMessages`, `CanInviteUsers`, `CanLeaveSession`, `CanChangeSessionName`, `CanSendImages`. `MessageBodyPermissions` drives the markdown composition toolbar (Bold/Italics/Underline/Strikethrough/Codeblocks/Links).

**Send results:** sends are optimistic. A transient failure → `MessageStatus.Failed` + retry (`ResendMessageAsync`); a provider rejection (`ChatSendRejectedException`) → `MessageStatus.Rejected` + reason, no retry. Validation (size, image count, content policy) lives in the provider, not the control.

**Custom actions (MAUI):** the old `ChatEntryTool`/`ChatBubbleTool` FAB tool tree is replaced by permission-driven built-in actions (react/edit/delete/copy) plus two lightweight hooks — `ChatInputAction` (input bar) and `ChatBubbleAction` (bubble menu). `SpeechToTextTool : ChatInputAction` and `TextToSpeechBubbleTool : ChatBubbleAction` ship in `Shiny.Maui.Controls.SpeechAddins`.

```xml
<shiny:ChatView Provider="{Binding Provider}" SessionId="{Binding SessionId}">
    <shiny:ChatView.InputActions>
        <speech:SpeechToTextTool AutoSend="False" SilenceTimeout="00:00:03" />
    </shiny:ChatView.InputActions>
    <shiny:ChatView.CustomBubbleActions>
        <speech:TextToSpeechBubbleTool />
    </shiny:ChatView.CustomBubbleActions>
</shiny:ChatView>
```

**Features:**
- Provider-driven: bind a `Provider` + `SessionId`, implement the rest server-side
- Chat bubbles with left/right alignment (by `CurrentUserId`) and per-user colors/avatars
- Visual grouping by sender and minute; timestamps on last message in each group
- Typing indicators with animated dots and a scroll-aware toast pill (debounced + auto-expiring)
- Reactions (emoji badges grouped by glyph), gated by `CanReactToMessages` + `PermittedEmojis`
- Per-user read receipts; per-message edit/delete gated by permission + ownership
- Optimistic send with `Sending`/`Failed`/`Rejected` states and retry
- Markdown composition toolbar + inline bubble rendering (self-contained, no Markdown-package dependency)
- Image attachments from gallery or camera (camera shown only where the platform supports capture); tap an image to open the ImageViewer
- Cursor-based load-more paging (stable under live inserts)
- Connection banner that disables input while offline/reconnecting
- Custom message templates via `Identifier`/`Metadata` discriminator
- Entire input bar can be hidden for read-only use

<!-- TODO: capture screenshots for chatview (provider, markdown toolbar, attachment picker) -->


### ColorPicker

A full-featured color picker with spectrum, hue bar, opacity slider, hex input, and preview swatch. Available as both an inline `ColorPicker` control and a `ColorPickerButton` that opens as a popup dialog.

| Button | Picker Dialog |
|:---:|:---:|
| ![Color Picker Button](assets/colorpicker1.png) | ![Color Picker Dialog](assets/colorpicker2.png) |

```xml
<shiny:ColorPickerButton SelectedColor="{Binding SelectedColor}"
                         Text="Pick Color"
                         ShowOpacity="True" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| SelectedColor | Color | Red | Currently selected color — TwoWay |
| Text | string? | null | Button label text |
| ShowOpacity | bool | false | Show/hide opacity slider |
| CornerRadius | int | 8 | Button corner radius |
| ColorChangedCommand | ICommand? | null | Fires when color changes |

**Event:** `ColorChanged` (EventHandler\<Color\>)

### FontPicker

Font family and font size picker controls for MAUI. Includes inline list (`FontPicker`, `FontSizePicker`) and popup button (`FontPickerButton`, `FontSizePickerButton`) variants. Each font is rendered in its own typeface for instant visual preview.

```xml
<shiny:FontPickerButton AvailableFonts="{Binding Fonts}"
                        SelectedFont="{Binding SelectedFont, Mode=TwoWay}"
                        Placeholder="Font" />

<shiny:FontSizePickerButton AvailableFontSizes="{Binding Sizes}"
                            SelectedFontSize="{Binding SelectedSize, Mode=TwoWay}" />
```

**FontPicker / FontPickerButton:**

| Property | Type | Default | Description |
|---|---|---|---|
| AvailableFonts | IList\<string\>? | null | Font family names to display |
| SelectedFont | string? | null | Currently selected font (TwoWay) |
| PreviewText | string | "The quick brown fox" | Text rendered in each font row |
| PreviewFontSize | double | 18 | Size of preview text |
| Placeholder | string | "Font" | Button placeholder (button only) |
| CornerRadius | int | 8 | Button corner radius (button only) |
| FontChangedCommand | ICommand? | null | Command on selection (button only) |

**FontSizePicker / FontSizePickerButton:**

| Property | Type | Default | Description |
|---|---|---|---|
| AvailableFontSizes | IList\<double\>? | null | Font sizes to display |
| SelectedFontSize | double | 16 | Currently selected size (TwoWay) |
| PreviewText | string | "Aa" | Text rendered at each size |
| CornerRadius | int | 8 | Button corner radius (button only) |
| FontSizeChangedCommand | ICommand? | null | Command on selection (button only) |

These controls are also integrated into the **ImageEditor** toolbar when `AllowFontSelection` and `AllowFontSizeSelection` are enabled.

### TextEntry

A text entry control with a Material 3 floating label, customizable border, left/right tool slots, hint text for validation errors, character count display, input masking, an autofill/autocorrect opt-out, and — on iOS and Android — a bar docked to the top of the soft keyboard.

`Variant="Floating"` is the **M3 outlined notch**: the label rides up onto the top border stroke and sits in a gap cut out of the outline, so it never overlaps the text being typed. Tools are **inline** by default — a tinted glyph on the field with no grey block and no separator; `ToolStyle="Addon"` brings back the Bootstrap input-group look.

```xml
<shiny:TextEntry Placeholder="Email"
                 Text="{Binding Email, Mode=TwoWay}"
                 Keyboard="Email"
                 HasError="{Binding HasEmailError}"
                 HintText="{Binding EmailError}">
    <shiny:ClearButtonTool />
</shiny:TextEntry>
```

| Property | Type | Default | Description |
|---|---|---|---|
| Text | string | "" | Current text value (TwoWay). When Mask is set, contains raw digits only |
| Placeholder | string | "" | Placeholder / floating label |
| Variant | TextEntryVariant | Classic | `Classic` (native placeholder) or `Floating` (M3 notched outline) |
| ToolStyle | TextEntryToolStyle | Inline | `Inline` (glyph on the field) or `Addon` (filled block + separator) |
| PlaceholderColor | Color | Grey | Placeholder color unfocused |
| FocusedPlaceholderColor | Color | #007AFF | Placeholder color focused |
| BorderColor | Color | #CCCCCC | Border color unfocused |
| FocusedBorderColor | Color | #007AFF | Border color focused |
| BorderThickness | double | 1 | Unfocused border thickness |
| FocusedBorderThickness | double | 2 | Focused border thickness |
| CornerRadius | CornerRadius | 8 | Corner radius |
| EntryBackgroundColor | Color | Transparent | Background fill |
| IsReadOnly | bool | false | Read-only mode |
| IsPassword | bool | false | Password masking |
| Keyboard | Keyboard | Default | Keyboard type (auto-set to Numeric when Mask is active) |
| MaxLength | int | unlimited | Character limit |
| Mask | string? | null | Input mask pattern (`#` = digit slot, other chars are auto-inserted literals) |
| FormattedText | string | "" | Read-only display value with mask applied |
| HintText | string? | null | Hint/error text below field |
| HasError | bool | false | Error state |
| ErrorColor | Color | #DC3545 | Error color |
| ShowCharacterCount | bool | false | Show counter |
| IsAutoCompleteEnabled | bool | true | False switches off autofill, autocorrect, predictive text and spell check together |
| IsSpellCheckEnabled | bool | true | Spell check (forced off when IsAutoCompleteEnabled is false) |
| IsTextPredictionEnabled | bool | true | Suggestion strip (forced off when IsAutoCompleteEnabled is false) |
| Accessory | KeyboardAccessoryView? | null | Bar docked to the top of the soft keyboard (iOS + Android) |
| AccessoryPreset | KeyboardAccessoryPreset | None | Stock bar: `Done`, `Navigation`, `NavigationAndDone` |
| FieldGroup | string? | null | Groups fields for accessory prev/next navigation |
| LeftTools | IList&lt;TextEntryTool&gt; | empty | Left tool slot |
| RightTools | IList&lt;TextEntryTool&gt; | empty | Right tool slot (ContentProperty) |

**Input Masking:**

```xml
<shiny:TextEntry Placeholder="Phone Number" Mask="(###) ###-####" Text="{Binding Phone}" />
<shiny:TextEntry Placeholder="Credit Card" Mask="#### #### #### ####" Text="{Binding Card}" />
<shiny:TextEntry Placeholder="Date" Mask="##/##/####" Text="{Binding DateStr}" />
```

When `Mask` is set, `Text` always contains raw digits (e.g., `"5551234567"`), while the user sees formatted text (e.g., `"(555) 123-4567"`). Keyboard auto-sets to Numeric and literal characters are inserted automatically as the user types.

**Built-in tools:** `ClearButtonTool` (auto-shows ✕ when text present), `TextEntryStepperTool` (increment/decrement numeric values), `TextEntrySpeechToTextTool` (voice input, in SpeechAddins package).

**Stepper Tool:**

```xml
<shiny:TextEntry Placeholder="Quantity"
                 Text="{Binding Quantity, Mode=TwoWay}"
                 Keyboard="Numeric">
    <shiny:TextEntry.LeftTools>
        <shiny:TextEntryStepperTool Step="-1" />
    </shiny:TextEntry.LeftTools>
    <shiny:TextEntryStepperTool Step="1" />
</shiny:TextEntry>
```

`TextEntryStepperTool` increments or decrements the numeric text value by `Step` on each tap. If `Text` is not set, it auto-displays the step value with sign (e.g. "+1", "-5").

**No autocomplete:**

```xml
<shiny:TextEntry Placeholder="Serial number" Text="{Binding Serial}" IsAutoCompleteEnabled="False" />
```

Turns off autofill (iOS `TextContentType`, Android autofill hints), autocorrect, predictive text and spell check in one switch — the combination that otherwise rewrites serials, coupon codes and SKUs mid-entry.

**Keyboard accessory (MAUI, iOS + Android only):**

A bar docked to the **top edge of the soft keyboard** while the field has focus — it belongs to the keyboard, not to the entry, and comes and goes with it. The reason it exists: the iOS numeric keypad has no return key, so without a Done button there is no way to dismiss it.

```xml
<shiny:TextEntry Placeholder="Amount" Keyboard="Numeric"
                 Text="{Binding Amount}"
                 AccessoryPreset="NavigationAndDone" />

<shiny:TextEntry Placeholder="Notes" Text="{Binding Notes}">
    <shiny:TextEntry.Accessory>
        <shiny:KeyboardAccessoryView>
            <shiny:KeyboardNavigationItem Direction="Previous" />
            <shiny:KeyboardNavigationItem Direction="Next" />
            <shiny:KeyboardAccessorySpacer />
            <shiny:KeyboardAccessoryItem Text="#tag" Command="{Binding InsertTagCommand}" />
            <shiny:KeyboardDismissItem />
        </shiny:KeyboardAccessoryView>
    </shiny:TextEntry.Accessory>
</shiny:TextEntry>
```

iOS uses the real `UIResponder.InputAccessoryView`, so it rides the keyboard animation exactly. Android has no accessory API at all (the IME is a separate process), so the same bar is rendered in the activity's content view and driven by the IME window insets — frame-synced on API 30+, and shown only while the IME is genuinely up, so a hardware keyboard correctly shows no bar. Windows, macOS, Linux and Blazor have no soft keyboard to decorate; the property compiles and does nothing. This is *not* the [on-screen keyboard](#on-screen-keyboard) — that one draws keys; this one decorates the OS keyboard.

### Slider

A slider control with a two-color gradient track, blended thumb border, tooltip, and full drag/tap interaction.

```xml
<shiny:Slider Value="{Binding Temperature}"
                      Minimum="0"
                      Maximum="100"
                      ColdColor="#3B82F6"
                      HotColor="#EF4444"
                      ShowTooltip="True" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Value | double | 0 | Current value (TwoWay) |
| Minimum | double | 0 | Minimum value |
| Maximum | double | 100 | Maximum value |
| Step | double | 1 | Snap increment |
| ColdColor | Color/string | #3B82F6 | Left gradient color |
| HotColor | Color/string | #EF4444 | Right gradient color |
| TrackHeight | double | 8 | Track height |
| ThumbSize | double | 24 | Thumb diameter |
| ThumbColor | Color/string | White | Thumb fill color |
| ShowTooltip | bool | true | Show value tooltip |
| TooltipTemplate | DataTemplate/RenderFragment | null | Custom tooltip content |
| ValueFormat | string? | null | Format string for tooltip value |

### RangeSlider

A two-thumb variant of Slider that selects a lower/upper value pair. It reuses the gradient track, blended thumb borders, and floating tooltips, adding `MinimumRange`/`MaximumRange` gap constraints between the thumbs. The dragged thumb hard-stops at `MinimumRange`; dragging past `MaximumRange` pushes the other thumb along.

```xml
<shiny:RangeSlider LowerValue="{Binding PriceLow}"
                   UpperValue="{Binding PriceHigh}"
                   Minimum="0"
                   Maximum="1000"
                   Step="10"
                   MinimumRange="50"
                   MaximumRange="500"
                   ValueFormat="C0" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| LowerValue | double | 0 | Lower thumb value (TwoWay) |
| UpperValue | double | 100 | Upper thumb value (TwoWay) |
| Minimum | double | 0 | Minimum value |
| Maximum | double | 100 | Maximum value |
| Step | double | 1 | Snap increment |
| MinimumRange | double | 0 | Minimum gap between thumbs (hard stop); 0 = off |
| MaximumRange | double | 0 | Maximum gap between thumbs (pushes the other thumb); 0 = off |
| ColdColor | Color/string | #3B82F6 | Left gradient color |
| HotColor | Color/string | #EF4444 | Right gradient color |
| TrackHeight | double | 8 | Track height |
| ThumbSize | double | 24 | Thumb diameter |
| ThumbColor | Color/string | White | Thumb fill color |
| ShowTooltip | bool | true | Show a value tooltip per thumb |
| TooltipTemplate | DataTemplate/RenderFragment | null | Custom tooltip content (applied to both thumbs) |
| ValueFormat | string? | null | Format string for tooltip values |

### ProgressBar

A progress bar control with gradient fill and a configurable Vista-style shimmer pulse that sweeps left-to-right across the bar. Supports determinate, indeterminate, text overlay, and timed/value-triggered pulse animations.

```xml
<shiny:ProgressBar Value="{Binding Progress}"
                   TrackHeight="12"
                   CornerRadius="6"
                   UseGradient="True"
                   GradientStartColor="#3B82F6"
                   GradientEndColor="#8B5CF6"
                   PulseEnabled="True"
                   PulseOnValueChange="True"
                   PulseLength="0.4"
                   PulseSpeed="800" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Value | double | 0 | Current value (TwoWay) |
| Minimum | double | 0 | Minimum value |
| Maximum | double | 100 | Maximum value |
| TrackColor | Color/string | #E5E7EB | Background track color |
| BarColor | Color/string | #3B82F6 | Fill bar color (when gradient disabled) |
| TrackHeight | double | 8 | Track height in px |
| CornerRadius | double/string | 4 | Corner radius |
| UseGradient | bool | false | Enable gradient fill |
| GradientStartColor | Color/string | #3B82F6 | Left gradient color |
| GradientEndColor | Color/string | #8B5CF6 | Right gradient color |
| PulseEnabled | bool | false | Enable Vista-style shimmer pulse |
| PulseOnValueChange | bool | true | Trigger pulse on value change |
| PulseInterval | TimeSpan | 0 | Trigger pulse on a timer (e.g. every 2s) |
| PulseColor | Color/string | White | Shimmer highlight color |
| PulseOpacity | double | 0.4 | Peak shimmer opacity (MAUI) |
| PulseLength | double | 0.4 | Width of shimmer as fraction of fill (0.05–1.0) |
| PulseSpeed | int | 800 | Milliseconds for one left-to-right sweep |
| ShowText | bool | false | Show percentage text overlay |
| TextFormat | string | "{0:0}%" | Text format string |
| TextColor | Color/string | White | Text color |
| FontSize | double | 11 | Text font size |
| IsIndeterminate | bool | false | Indeterminate sliding animation |

Events: `ValueChangedEvent`. Commands: `ValueChangedCommand`.

### Overlay & LoadingOverlay

Full-screen overlay controls. On MAUI, integrates with `OverlayHost`/`ShinyContentPage` (same backdrop system as FloatingPanel). On Blazor, wraps content with a CSS-based overlay. Supports optional frosted glass blur effect.

**MAUI (placed in ShinyContentPage.Panels):**

```xml
<shiny:ShinyContentPage ...>
    <ScrollView>...</ScrollView>

    <shiny:ShinyContentPage.Panels>
        <shiny:Overlay IsShown="{Binding IsOverlayVisible}" BlurRadius="10">
            <shiny:Overlay.OverlayContentTemplate>
                <DataTemplate>
                    <Label Text="Custom content" TextColor="White" />
                </DataTemplate>
            </shiny:Overlay.OverlayContentTemplate>
        </shiny:Overlay>

        <shiny:LoadingOverlay IsShown="{Binding IsBusy}"
                              Message="Loading..." />
    </shiny:ShinyContentPage.Panels>
</shiny:ShinyContentPage>
```

| Property | Type | Default | Description |
|---|---|---|---|
| IsShown | bool | false | Show/hide overlay (TwoWay) |
| AnimationDuration | uint | 250 | Fade animation duration in ms (MAUI) |
| BlurRadius | double | 0 | When > 0, applies a frosted glass blur behind the backdrop (MAUI uses FrostedGlassView; Blazor uses CSS backdrop-filter) |
| OverlayContentTemplate | DataTemplate | null | Custom overlay content (MAUI) |
| OverlayContent | RenderFragment | null | Custom overlay content (Blazor) |

MAUI backdrop color/opacity are controlled by `ShinyContentPage.BackdropColor` / `BackdropMaxOpacity`.

**LoadingOverlay additional properties:**

| Property | Type | Default | Description |
|---|---|---|---|
| IsIndeterminate | bool | true | Spinner mode (true) or progress bar mode (false) |
| Progress | double | 0 | Progress value 0–100 (when determinate) |
| Message | string? | null | Text displayed below spinner/progress bar |
| SpinnerColor | Color/string | White | Spinner color |

**Blazor (wrapper pattern):**

```razor
<LoadingOverlay IsShown="@isBusy" BlurRadius="8" IsIndeterminate="false" Progress="@progress" Message="Loading...">
    <p>Your page content here — gets overlaid when IsShown=true</p>
</LoadingOverlay>
```

### SplashScreen (Blazor only)

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

### AutoCompleteEntry

A text input with debounced search, dropdown suggestions, busy indicator, and custom item templates. Supports both local filtering and remote search via a command/callback. Available on both MAUI and Blazor with full styling control.

![AutoCompleteEntry](assets/autocomplete1.png)

```xml
<shiny:AutoCompleteEntry
    Text="{Binding SearchText}"
    Placeholder="Search..."
    ItemsSource="{Binding Results}"
    SelectedItem="{Binding SelectedResult}"
    SearchCommand="{Binding SearchCommand}"
    TextMemberPath="Name"
    DebounceInterval="300"
    Threshold="2"
    MaxDropDownHeight="250"
    FontSize="16"
    TextColor="Black"
    DropDownBackgroundColor="White"
    DropDownBorderColor="LightGray"
    CornerRadius="8" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Text | string | "" | Current text value (TwoWay) |
| Placeholder | string? | null | Placeholder text |
| PlaceholderColor | Color/string | null | Placeholder text color |
| ItemsSource | IList | null | Suggestion items |
| SelectedItem | object? | null | Currently selected item (TwoWay) |
| SearchCommand | ICommand / EventCallback\<string\> | null | Remote search command |
| TextMemberPath | string? | null | Property name to display from items |
| ItemTemplate | DataTemplate / RenderFragment\<object\> | null | Custom dropdown item template |
| IsBusy | bool | false | Show/hide the loading spinner (TwoWay) |
| DebounceInterval | int | 300 | Debounce delay (ms) |
| Threshold | int | 1 | Minimum characters before searching |
| MaxDropDownHeight | double | 200 | Maximum dropdown height (px) |
| TextColor | Color/string | null | Input text color |
| FontSize | double | 14 | Input font size |
| FontFamily | string? | null | Input font family (MAUI only) |
| FontAttributes | FontAttributes | None | Bold/italic (MAUI only) |
| DropDownBackgroundColor | Color/string | White | Dropdown background |
| DropDownBorderColor | Color/string | LightGray | Dropdown border color |
| CornerRadius | double | 4 | Dropdown border radius (MAUI only) |
| SpinnerColor | Color/string | Grey | Loading spinner color |
| CssClass | string? | null | Root CSS class (Blazor only) |
| InputClass | string? | null | Input element CSS class (Blazor only) |
| DropDownClass | string? | null | Dropdown CSS class (Blazor only) |
| AdditionalAttributes | IDictionary | null | Unmatched HTML attributes (Blazor only) |

Events: `ItemSelected` fires when a suggestion is chosen.

**Blazor CSS Custom Properties** — Override these on a parent element or the component itself to theme without parameters:

| Variable | Default | Controls |
|---|---|---|
| `--shiny-ac-text` | inherit | Input text color |
| `--shiny-ac-ph` | #9CA3AF | Placeholder color |
| `--shiny-ac-dd-bg` | #fff | Dropdown background |
| `--shiny-ac-dd-border` | #D1D5DB | Dropdown border |
| `--shiny-ac-spinner` | #9CA3AF | Spinner color |
| `--shiny-ac-font-size` | inherit | Input font size |
| `--shiny-ac-dd-max-h` | 200px | Dropdown max height |

### CountryPicker

A country search control built on AutoCompleteEntry with flag emoji display, country name, and dial code. Searches all ISO 3166-1 countries.

| Empty | With Selection |
|:---:|:---:|
| ![Country & Address](assets/countryaddress1.png) | ![Country Selected](assets/countryaddress2.png) |

```xml
<shiny:CountryPicker SelectedCountry="{Binding Country}"
                     Placeholder="Select country..."
                     FontSize="16"
                     TextColor="Black" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| SelectedCountry | Country | null | Selected country (TwoWay) |
| Placeholder | string | "Search countries..." | Placeholder text |
| MaxDropDownHeight | double | 200 | Max dropdown height |
| TextColor | Color/string | null | Text color |
| PlaceholderColor | Color/string | null | Placeholder color |
| DropDownBackgroundColor | Color/string | null | Dropdown background |
| DropDownBorderColor | Color/string | null | Dropdown border color |
| FontSize | double | 14 | Font size |
| FontFamily | string? | null | Font family (MAUI only) |
| CornerRadius | double | 4 | Dropdown corner radius (MAUI only) |
| InputClass | string? | null | Input CSS class (Blazor only) |
| DropDownClass | string? | null | Dropdown CSS class (Blazor only) |

Events: `CountrySelected` fires when a country is chosen.

The `Country` model provides: `Name`, `Iso2`, `Iso3`, `DialCode`, `FlagEmoji`.

### AddressEntry

An address search control built on AutoCompleteEntry that queries a geocoding provider (Nominatim/OpenStreetMap by default). Returns structured address data with coordinates.

```xml
<shiny:AddressEntry SelectedAddress="{Binding Address}"
                    Placeholder="Search address..."
                    CountryCodes="us,ca"
                    FontSize="16" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| SelectedAddress | Address | null | Selected address (TwoWay) |
| SearchProvider | IAddressSearchProvider? | null | Custom search provider (defaults to Nominatim) |
| CountryCodes | string? | null | Comma-separated ISO country codes to filter results |
| Placeholder | string | "Search address..." | Placeholder text |
| MaxDropDownHeight | double | 250 | Max dropdown height |
| TextColor | Color/string | null | Text color |
| PlaceholderColor | Color/string | null | Placeholder color |
| DropDownBackgroundColor | Color/string | null | Dropdown background |
| DropDownBorderColor | Color/string | null | Dropdown border color |
| FontSize | double | 14 | Font size |
| FontFamily | string? | null | Font family (MAUI only) |
| CornerRadius | double | 4 | Dropdown corner radius (MAUI only) |
| InputClass | string? | null | Input CSS class (Blazor only) |
| DropDownClass | string? | null | Dropdown CSS class (Blazor only) |

Events: `AddressSelected` fires when an address is chosen.

The `Address` record provides: `DisplayName`, `HouseNumber`, `Street`, `City`, `State`, `PostalCode`, `Country`, `CountryCode`, `Latitude`, `Longitude`.

Implement `IAddressSearchProvider` for custom geocoding:

```csharp
public class MyGeoProvider : IAddressSearchProvider
{
    public Task<IList<Address>> SearchAsync(string query, string? countryCodes, CancellationToken ct)
    {
        // call your preferred geocoding API
    }
}
```

### PillView

Pill/chip/tag elements for displaying categories, filters, or status indicators with predefined or custom color schemes.

![Pills](assets/pills.png)

```xml
<shiny:PillView Text="Success" Type="Success" />
<shiny:PillView Text="Warning" Type="Warning" />
<shiny:PillView Text="Custom" PillColor="Purple" PillTextColor="White" />
```

| Pill Type | Description |
|---|---|
| None | Default/neutral |
| Success | Green |
| Info | Blue |
| Warning | Yellow |
| Caution | Orange |
| Critical | Red |

Each `PillType` maps to a well-known style key (e.g. `ShinyPillSuccessStyle`) that can be overridden in your app's `ResourceDictionary` to customize the preset themes.

### BadgeView

Wraps a single content view and overlays a small notification badge at any of the four corners. Available on both MAUI and Blazor. Setting `Text` to an empty string (and leaving `IsDot` false) hides the badge — bind your unread/cart/count value directly and it shows/clears itself.

```xml
<shiny:BadgeView Text="{Binding UnreadCount}"
                 Position="TopRight"
                 MaxCount="99"
                 BadgeColor="#DC2626"
                 BadgeTextColor="White"
                 BadgeBorderColor="White">
    <Border Stroke="#E5E7EB" StrokeThickness="1" Padding="14,10"
            StrokeShape="RoundRectangle 10">
        <Label Text="📬 Inbox" FontSize="16" />
    </Border>
</shiny:BadgeView>
```

```razor
<BadgeView Text="@unreadCount" Position="BadgePosition.TopRight" MaxCount="99"
           BadgeColor="#DC2626" BadgeTextColor="#FFFFFF" BadgeBorderColor="#FFFFFF">
    <div class="inbox-card">📬 Inbox</div>
</BadgeView>
```

| Property | Type (MAUI / Blazor) | Default | Description |
|---|---|---|---|
| Content / ChildContent | View / RenderFragment | null | The wrapped view the badge overlays |
| Text | string | "" | Badge text. Empty hides the badge unless `IsDot` is true |
| Position | BadgePosition | TopRight | Corner anchor: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` |
| BadgeColor | Color / string | #DC2626 | Badge fill color |
| BadgeTextColor | Color / string | White | Badge text color |
| BadgeBorderColor | Color / string | White | Border color (creates a clean ring around the badge) |
| BadgeBorderThickness | double | 1.5 | Border thickness |
| FontSize | double | 10 | Badge text font size |
| FontAttributes / FontWeight | FontAttributes / string | Bold / "700" | Font weight |
| CornerRadius | double | 999 | Badge corner radius (default fully rounded pill) |
| BadgePadding | Thickness / string | 6,2 / "2px 6px" | Inner padding |
| OffsetX | double | 4 | Horizontal nudge from the corner (positive = outward) |
| OffsetY | double | -4 | Vertical nudge from the corner (negative = upward) |
| IsDot | bool | false | When true, renders a small dot (text is ignored) — for "has new" indicators |
| DotSize | double | 10 | Dot diameter (when `IsDot` is true) |
| MaxCount | int | 0 | When > 0 and `Text` parses as a number above this limit, displays `"{MaxCount}+"` (e.g. `99+`) |
| IsAnimated | bool | true | When true, the badge scale/fades in and out as it appears or disappears |
| IsPulsing | bool | false | When true, the badge continuously pulses to draw attention |

**Features:**
- Four-corner positioning with per-corner offset nudge
- Auto-hide when `Text` is empty (just bind your count and let the control show/hide itself)
- Dot mode for simple notification indicators
- `MaxCount` overflow ("99+" style) for numeric counts
- Configurable show/hide scale animation and optional continuous pulse for attention-grabbing badges
- Blazor honors `prefers-reduced-motion` and disables both animations when set

### ShinyButton

A button that knows what it is doing: a leading and a trailing icon slot, a real working state, and
success/error states, all wired to the theme and — on MAUI — to its `Command`.

`Microsoft.Maui.Controls.Button` renders text and one image and nothing else, so "submit, spin, tick"
ends up hand-assembled on every page out of a `Grid`, an `ActivityIndicator` and a swapped label.
This is that assembly, done once and at parity on both hosts.

- **State machine** — `ButtonState.Normal` / `Busy` / `Success` / `Error`, with `BusyText` /
  `SuccessText` / `ErrorText` standing in for `Text`, per-state icons, and a `StateRevertDelay` that
  drops Success and Error back to Normal on their own (`TimeSpan.Zero` holds).
- **`IsBusy` shorthand** for the common view model that only has an `IsSaving` flag. Clearing it
  unwinds Busy but will *not* cut a Success or Error short — a view model clearing its flag in a
  `finally` is exactly when the outcome is on screen.
- **Three busy modes** — `ReplaceLeftIcon` (the default; the spinner and the icon are the same size,
  so the button cannot change width), `ReplaceContent` (content fades but keeps its layout space, so
  the button holds the width it had), and `KeepContent`.
- **Icon slots, three ways each** — `LeftIcon` (`ImageSource`), `LeftMotionIcon` (the name of a
  [motion icon](#motion-icons), coloured and played by the button), or `LeftIconView` for any `View`
  at all. Same trio on the right.
- **Command state (MAUI)** — the button follows its command's `CanExecuteChanged`, and if the command
  is an async one (`AsyncRelayCommand` and friends) it drives its own `Busy` for exactly as long as
  the command runs. Nothing binds `IsBusy`. It does this through MAUI's own `IsEnabledCore`, so a
  button you explicitly set `IsEnabled="False"` on **stays** disabled when the command becomes
  executable again.
- **Appearance × Type** — `Filled` / `Tonal` / `Outlined` / `Text` / `Elevated` crossed with
  `Primary` / `Secondary` / `Success` / `Warning` / `Critical` / `Info`, all resolved from the theme
  tokens. Any explicit colour property wins over both.

```xml
<!-- The whole point: nothing here binds IsBusy. SaveCommand is an AsyncRelayCommand. -->
<shiny:ShinyButton Text="Save"
                   BusyText="Saving..."
                   LeftMotionIcon="download"
                   Command="{Binding SaveCommand}" />

<!-- Submit, spin, tick. The command sets State=Success itself; the button respects it. -->
<shiny:ShinyButton Text="Submit"
                   State="{Binding SubmitState}"
                   BusyText="Submitting..."
                   SuccessText="Submitted"
                   StateRevertDelay="0:0:2"
                   Command="{Binding SubmitCommand}" />

<!-- Appearance and type are orthogonal; explicit colours still win -->
<shiny:ShinyButton Text="Delete" Appearance="Outlined" Type="Critical" LeftMotionIcon="trash" />
<shiny:ShinyButton Text="Cancel" Appearance="Text" />
<shiny:ShinyButton Text="Brand"  ButtonBackgroundColor="#E91E63" TextColor="White" />
```

Blazor mirrors the parameters one-for-one. There is no `ICommand` on the web, so the command-state
integration is MAUI-only; its equivalent is that `Clicked` is awaited — an `async` handler holds the
button busy for as long as it runs, and a synchronous one never flickers.

```razor
<ShinyButton Text="Save" BusyText="Saving..." LeftMotionIcon="download" Clicked="SaveAsync" />

<ShinyButton Text="Delete" Appearance="ButtonAppearance.Outlined"
             Type="ButtonType.Critical" LeftMotionIcon="trash" Clicked="DeleteAsync" />

@code {
    async Task SaveAsync() => await http.PostAsJsonAsync("/api/save", model);
}
```

Motion icons in the slots play a cycle on tap and take their colour from the button's foreground, so
a disabled or hovered button carries its icons with it. The button owns that playback on both hosts —
the icons sit on the `Manual` trigger and the button plays them from its own tap — so a tap anywhere
on the button animates them, not only one that lands on the glyph. Clearing `BusyMotionIcon` falls
back to a platform `ActivityIndicator` on MAUI and a CSS spinner on Blazor.

### Fab & FabMenu

A Material Design-style floating action button, plus an expanding multi-action menu that animates up from the main FAB.

Menu items render as **pills**: the label lives *inside* one capsule with a tinted circular icon chip on
the edge nearest the main FAB, so the whole row is a single tap target instead of a detached label chip
plus a circle. Every chip is inset so its centre lands on the main FAB's vertical axis — the items read as
one column. An item with no `Text` collapses to a plain circle of `Size`. Items fade, rise and scale in
from that axis with a staggered spring, and the main FAB spins 45° (`IconRotation`) while the menu is open
— the classic "+" turning into an "×" — unless it carries a `Text` label, where a rotated word would just
read as broken.

| Closed | Menu Open |
|:---:|:---:|
| ![FAB Closed](assets/fab-closed.png) | ![FAB Menu Open](assets/fab-open.png) |

```xml
<!-- Single Fab -->
<shiny:Fab Icon="add.png"
           Text="Add Item"
           FabBackgroundColor="#4CAF50"
           TextColor="White"
           Command="{Binding AddCommand}"
           HorizontalOptions="End"
           VerticalOptions="End"
           Margin="24" />

<!-- FabMenu with child items -->
<shiny:FabMenu IsOpen="{Binding IsMenuOpen}"
               Icon="plus.png"
               FabBackgroundColor="#2196F3"
               HorizontalOptions="End"
               VerticalOptions="End"
               Margin="24">
    <shiny:FabMenuItem Icon="share.png"  Text="Share"  Command="{Binding ShareCommand}" />
    <shiny:FabMenuItem Icon="edit.png"   Text="Edit"   Command="{Binding EditCommand}" />
    <shiny:FabMenuItem Icon="delete.png" Text="Delete" Command="{Binding DeleteCommand}" />
</shiny:FabMenu>
```

**Fab** properties:

| Property | Type | Default | Description |
|---|---|---|---|
| Icon | ImageSource? | null | Button icon |
| Text | string? | null | Optional label; when null the Fab is a perfect circle. A short label (e.g. `+`) still renders circular; the Fab stretches into a pill only when the label needs more than Size |
| Command | ICommand? | null | Invoked when the Fab is tapped |
| CommandParameter | object? | null | Parameter passed to the Command |
| FabBackgroundColor | Color | #2196F3 | Fill color |
| BorderColor | Color? | null | Outline stroke color |
| BorderThickness | double | 0 | Outline stroke thickness |
| TextColor | Color | White | Label color |
| FontSize | double | 14 | Label font size |
| FontAttributes | FontAttributes | None | Label font attributes |
| Size | double | 56 | Height of the Fab (diameter when circular) |
| IconSize | double | 24 | Icon image size |
| HasShadow | bool | true | Show drop shadow |
| UseFeedback | bool | true | Feedback on tap |

Events: `Clicked`.

**FabMenu** properties (plus all main-Fab pass-throughs above):

| Property | Type | Default | Description |
|---|---|---|---|
| IsOpen | bool | false | Two-way bindable; opens/closes the menu with animation |
| Items | `IList<FabMenuItem>` | empty | Menu items (content property — place items directly inside the FabMenu) |
| FabSize | double | 56 | Main FAB button size (diameter) |
| HasShadow | bool | true | Drop shadow on the main FAB |
| MenuAlignment | LayoutOptions | End | Horizontal alignment of the menu stack (Start for left-aligned, End for right-aligned) |
| HasBackdrop | bool | true | Show a dim backdrop while open |
| BackdropColor | Color | Black | Backdrop color |
| BackdropOpacity | double | 0.4 | Backdrop peak opacity |
| CloseOnBackdropTap | bool | true | Close when backdrop is tapped |
| CloseOnItemTap | bool | true | Close after any item is tapped |
| AnimationDuration | uint | 200 | Open/close animation duration (ms) |
| IconRotation | double | 45 | Degrees the main FAB rotates while open (0 disables; ignored when the main FAB has `Text`) |
| UseFeedback | bool | true | Feedback on toggle |

Events: `ItemTapped` — fires the `FabMenuItem` that was tapped.

Methods: `Open()`, `Close()`, `Toggle()`.

**FabMenuItem** properties:

| Property | Type | Default | Description |
|---|---|---|---|
| Icon | ImageSource? | null | Icon rendered in the circular chip |
| Text | string? | null | Label inside the pill; when null the item collapses to a plain circle |
| Command | ICommand? | null | Invoked when tapped |
| CommandParameter | object? | null | Parameter for the Command |
| FabBackgroundColor | Color | theme Primary | Icon chip fill — and the whole pill's fill when the item has no `Text` |
| BorderColor | Color? | theme OutlineVariant | Pill outline stroke |
| BorderThickness | double | 1 | Pill outline thickness (0 for a borderless pill) |
| TextColor | Color | theme OnSurface | Label text color |
| LabelBackgroundColor | Color | theme SurfaceContainerHigh | Pill body fill behind the label |
| FontSize | double | 13 | Label font size |
| FontAttributes | FontAttributes | None | Label font attributes |
| Size | double | 44 | Pill height (diameter when the item has no `Text`) |
| IconSize | double | 20 | Icon image size |
| HasShadow | bool | true | Drop shadow on the pill |
| UseFeedback | bool | true | Feedback on tap |

**Placement tip**: `FabMenu` should live in a `Grid` that fills the page (the same placement pattern as `ImageViewer`) so the backdrop can cover the page content. Alternatively, use `ShinyContentPage` with `OverlayHost` for easier overlay management.

**Blazor** matches the MAUI look and API. `Items` is a `List<FabMenuItem>` of plain data objects
(`Icon` is an inline emoji / SVG string or an image URL), and the same knobs are parameters: `FabSize`,
`HasShadow`, `IconRotation`, and `MenuAlignment` (`"end"` default, `"start"` to grow from the left).
Colors default to the theme CSS variables (`--shiny-color-primary`, `--shiny-color-surface-container-high`,
`--shiny-color-on-surface`, `--shiny-color-outline-variant`), the open backdrop adds a 2px blur, and
`prefers-reduced-motion` collapses every transition.

```razor
<FabMenu Items="items" Icon="+" ItemTapped="OnItemTapped" />

@code {
    readonly List<FabMenuItem> items = new()
    {
        new FabMenuItem { Text = "New Note",  Icon = "📝", FabBackgroundColor = "#10B981" },
        new FabMenuItem { Text = "New Photo", Icon = "📷", FabBackgroundColor = "#F59E0B" },
    };
}
```

### StateView & Wizard

Two related controls on both hosts. **`StateView`** shows exactly one of several named branches, chosen by
a string — the declarative form of the `IsVisible` (MAUI) / `@if/else` (Blazor) ladder every app grows.
**`Wizard`** builds on it: the same named branches, plus an order, a progress indicator, a Back/Next bar
that knows where it is, and a gate on leaving a step.

**StateView** — bind `CurrentState` and the matching `StateViewState` is what is on screen. An unmatched
name falls back to `DefaultState`, then to the first declared state, so a typo shows something rather than
a blank rectangle. Content declared inline is built with the rest of the markup; content declared as a
`ContentTemplate` (MAUI) is built the first time the branch is reached and then cached — turn `CacheContent`
off to rebuild, and reset, on every visit. On Blazor the branches are lazy by construction: a
`StateViewState` renders nothing itself and hands its `ChildContent` to the state view.

`Transition` animates the swap — `None`, `Fade`, `Slide` (direction taken from the move, so a later state
enters from the right and an earlier one from the left), `SlideLeft`/`SlideRight`/`SlideUp`/`SlideDown`, or
`Scale`.

```xml
<shiny:StateView CurrentState="{Binding CurrentState}" Transition="Slide">
    <shiny:StateViewState Name="Loading">
        <ActivityIndicator IsRunning="True" />
    </shiny:StateViewState>
    <shiny:StateViewState Name="Loaded">
        <shiny:StateViewState.ContentTemplate>
            <DataTemplate><local:ReportView /></DataTemplate>
        </shiny:StateViewState.ContentTemplate>
    </shiny:StateViewState>
    <shiny:StateViewState Name="Error">
        <Label Text="Something went wrong" />
    </shiny:StateViewState>
</shiny:StateView>
```

```razor
<StateView @bind-CurrentState="state" Transition="StateTransition.Slide">
    <States>
        <StateViewState Name="Loading"><Spinner /></StateViewState>
        <StateViewState Name="Loaded"><Report /></StateViewState>
        <StateViewState Name="Error"><p>Something went wrong</p></StateViewState>
    </States>
</StateView>
```

**Wizard** — steps are `WizardStep`s (a `StateViewState` with a title and a few rules). The default
progress indicator is the pointed breadcrumb: one chevron per step, completed / current / upcoming taken
from the theme, with `ProgressStyle="Dots"` and `"Bar"` as alternatives and `Progress` to replace it with
your own view entirely. On MAUI it is drawn on a `GraphicsView`, so it renders identically on every head
including AppKit and GTK4; on Blazor the same shape is a `clip-path`.

What the wizard adds beyond switching views:

- **Validity gates.** `WizardStep.IsValid` blocks Next; `IsOptional` bypasses it. `ValidateCommand` (MAUI)
  runs *before* `IsValid` is read, so a view-model that validates inside the command and sets the flag is
  enough — no event wiring. Blazor's `Validate` is an `async Func<Task<bool>>`, so a server round-trip is a
  first-class validator. `StepChanging` is cancellable for anything neither can express.
- **Conditional branches.** `IsVisible="False"` takes a step out of the run entirely: skipped by Next/Back,
  dropped from the progress bar, and excluded from `StepCount`. Bind it and the wizard reshapes itself.
  `IsEnabled="False"` keeps the step on the indicator but unreachable.
- **Built-in commands.** `GoNextCommand`, `GoBackCommand`, `FinishCommand`, `CancelCommand` and
  `GoToStepCommand` are on the wizard, so a button inside a step reaches them with `x:Reference` rather
  than the view-model re-implementing navigation. `CanGoBack`/`CanGoNext` remain yours — they are ANDed
  with the wizard's own boundary and validity checks.
- **Position, read-only and bindable.** `StepNumber`, `StepCount`, `IsFirstStep`, `IsLastStep` and
  `ProgressFraction`, plus two-way `CurrentStep` and `CurrentStepIndex`. Assigning an unknown or disabled
  step is reverted rather than blanking the wizard.
- **Review without skipping ahead.** `AllowStepSelection` makes the indicator clickable;
  `LinearNavigation` (on by default) limits that to steps already completed.
- **Finish that can fail.** `Finishing` is cancellable, so a submit rejected server-side leaves the user on
  the last step with their input intact.

```xml
<shiny:Wizard x:Name="Checkout"
              CurrentStep="{Binding CurrentStep}"
              ShowCancel="True"
              AllowStepSelection="True"
              FinishedCommand="{Binding SubmitCommand}">

    <shiny:WizardStep Name="Account" Title="Account" IsValid="{Binding EmailIsValid}">
        <shiny:TextEntry Text="{Binding Email}" Placeholder="you@example.com" />
    </shiny:WizardStep>

    <!-- Turn delivery off and this step leaves the run entirely -->
    <shiny:WizardStep Name="Delivery" Title="Delivery" IsVisible="{Binding WantsDelivery}">
        <shiny:TextEntry Text="{Binding Address}" />
    </shiny:WizardStep>

    <shiny:WizardStep Name="Review" Title="Review" NextText="Place order">
        <Button Text="Start over"
                Command="{Binding Source={x:Reference Checkout}, Path=GoToStepCommand}"
                CommandParameter="Account" />
    </shiny:WizardStep>
</shiny:Wizard>
```

```razor
<Wizard @bind-CurrentStep="step" ShowCancel="true" Finished="SubmitAsync">
    <Steps>
        <WizardStep Name="Account" Title="Account" IsValid="@emailIsValid">…</WizardStep>
        <WizardStep Name="Delivery" Title="Delivery" IsVisible="@wantsDelivery">…</WizardStep>
        <WizardStep Name="Review" Title="Review" NextText="Place order" Validate="ConfirmAsync">…</WizardStep>
    </Steps>
    <Progress>
        <!-- optional: replaces the built-in pointed progress bar -->
    </Progress>
</Wizard>
```

Turn `ShowNavigationBar` off when each step carries its own buttons; `NavigationBar` (MAUI) /
`<NavigationBar>` (Blazor) replaces the built-in bar while keeping the wizard's own navigation logic.

### Walkthrough

A guided tour of a page on both hosts: dim everything, cut an animated spotlight around one control at a
time, and say what it does. Onboarding, feature announcements, and workflows people only do once a quarter.

The steps are declared **together on the walkthrough, in order** — not attached to the controls they
describe. That is the point of the control. On a real screen (nested layouts, templated cells, a control
that is only sometimes there) attached ordering scatters the sequence across the markup where nothing can
see it as a whole: reordering means hunting, and a step whose control is conditionally hidden derails the
rest silently. Here reordering is moving a line, and `IsVisible="False"` takes a step out of the run and
re-numbers the counter.

The tour paints into a layer above the page's content, so a target inside a scroll view or a card is
highlighted **where it actually is** rather than clipped by its container.

A step advances **three ways**, which compose per step:

1. **The Next command** — the built-in nav row, or `NextCommand` / `NextAsync()` on your own button.
2. **Using the highlighted control** — `AdvanceOnTargetTap` (MAUI) / `AdvanceOnTargetClick` (Blazor). This
   is "tap Save to continue"; it implies target interaction, since the tap has to reach the control.
3. **A timer** — the step's `Duration` in milliseconds. Zero (the default) waits for the user.

Four displays: **`Popover`** (card, tail, counter and Back/Next/Skip — the default), **`Tooltip`**
(compact, no buttons), **`Inline`** (card without a tail, beside the target), and **`Spotlight`** (no card
at all — the text sits on the dim and the cut-out does the pointing). Or replace the body entirely with
`Content` / `ContentTemplate`.

`RememberRunKey` is what makes onboarding onboarding: the tour runs once per user and then stays out of the
way. It is backed by a replaceable `IWalkthroughStore` — `Preferences` on MAUI, `localStorage` on Blazor —
so the flag can live with the rest of your user state instead. `Restart()` clears it and runs again, which
is the "show me the tour again" menu item.

```xml
<shiny:Walkthrough x:Name="Tour"
                   RememberRunKey="home-v1"
                   AutoStart="True"
                   UseOverlay="True"
                   OverlayOpacity="0.8">

    <!-- No target: a centred welcome card, no cut-out. -->
    <shiny:WalkthroughStep Title="Welcome" Text="Here is what is new." AnimationIn="Pop" />

    <shiny:WalkthroughStep Target="{x:Reference SearchBox}"
                           Title="Find anything"
                           Text="Search across every project you can see."
                           Placement="Bottom" />

    <!-- No card; the cut-out does the pointing. -->
    <shiny:WalkthroughStep Target="{x:Reference Avatar}"
                           Title="Your profile"
                           Display="Spotlight" Highlight="Circle" />

    <!-- Live control: the tap reaches it through the hole, and using it advances. -->
    <shiny:WalkthroughStep Target="{x:Reference SaveButton}"
                           Text="Press Save to finish."
                           AllowTargetInteraction="True"
                           AdvanceOnTargetTap="True" />
</shiny:Walkthrough>
```

```razor
<Walkthrough @ref="tour" RememberRunKey="home-v1" AutoStart="true">
    <Steps>
        <WalkthroughStep Title="Welcome" Text="Here is what is new." />
        <WalkthroughStep Target="#search" Title="Find anything" Text="Search everything."
                         Placement="TooltipPlacement.Bottom" />
        <WalkthroughStep Target="#avatar" Title="Your profile"
                         Display="WalkthroughDisplay.Spotlight"
                         Highlight="WalkthroughHighlight.Circle" />
        <WalkthroughStep Target="#save" Text="Press Save to finish."
                         AllowTargetInteraction="true" AdvanceOnTargetClick="true" />
    </Steps>
</Walkthrough>
```

Targets are `{x:Reference}` on MAUI — prefer it, because it is checked when the XAML compiles, so a renamed
control breaks the build instead of quietly producing a tour that highlights nothing — or a CSS selector on
Blazor. `Walkthrough` renders nothing where it sits, so put it anywhere on the page. Blazor adds keyboard
navigation (arrows and Enter move, Escape leaves) and a scroll lock, both on by default; register the
`localStorage` store with `builder.Services.AddShinyWalkthrough()`.

`AllowTargetInteraction` is implemented by fencing the backdrop with four transparent panels *around* the
cut-out rather than one full-screen catcher — hit testing has no notion of a hole, so the hole has to be a
gap between panels.

### Tooltip

The bubble the walkthrough is built on, usable on its own. It either **wraps** the thing it describes or
**points at** something else, and it is drawn in a page-level layer (MAUI) or the browser's top layer
(Blazor) — so it is never clipped by the scroll view, card or grid cell its target lives in, and never
loses a z-index argument.

```xml
<!-- Wrapping: no reference needed, and the wrapper does not disturb the layout. -->
<shiny:Tooltip Text="Saves without closing" Placement="Top" Trigger="LongPress">
    <Button Text="Apply" />
</shiny:Tooltip>

<!-- Anchored and bound: it does not have to sit near its target in the markup. -->
<shiny:Tooltip Target="{x:Reference SaveButton}"
               Title="Why is this disabled?"
               Text="Make a change first."
               Placement="Bottom"
               ShowTail="True"
               IsOpen="{Binding ShowSaveHint}"
               Command="{Binding DismissHint}" />

<!-- The attached shorthand, for places an element does not fit. -->
<Button Text="Sync" shiny:TooltipProperties.Text="Pushes local changes to the server" />
```

```razor
<Tooltip Text="Saves without closing" Placement="TooltipPlacement.Top">
    <ShinyButton Text="Apply" />
</Tooltip>

<Tooltip Target="#save" Title="Why is this disabled?" Text="Make a change first."
         Trigger="TooltipTrigger.Manual" @bind-IsOpen="showHint" />
```

Bind **`IsOpen`**, never `IsVisible` — that one is `VisualElement.IsVisible`, and setting it would hide the
anchor the tooltip is wrapping rather than the bubble.

`Placement` is a preference, not a promise: a side with no room flips to its opposite, then to the roomiest
of the four; the bubble is clamped to stay inside `ScreenMargin`; and the tail slides along the bubble's
edge to keep pointing at the target it was clamped away from, pulled in from the corners so it always meets
a straight edge. So `Placement="Left"` on a control hard against the left edge gives you a bubble on the
right, deliberately.

Triggers are `Manual` / `Tap` / `LongPress` / `Hover` / `Focus` on MAUI, and `Manual` / `Hover` / `Click` /
`Focus` / `HoverOrFocus` (the default) / `LongPress` on Blazor — `HoverOrFocus` being the accessible one,
since a hover-only tooltip is unreachable by keyboard.

### ShinyToolbar & ShinyTabBar (Blazor)

Two screen-docked navigation chromes for Blazor. **`ShinyToolbar`** docks to the top or bottom of its
scroll container as an action bar (icons with links/actions, title, custom slots). **`ShinyTabBar`** is a
mobile-style tab bar pinned to the bottom of the viewport with a selected state and badges. Both support a
**frosted-glass** toggle (`Frosted`) backed by `backdrop-filter`.

The top toolbar uses `position: sticky`, so it reserves its own height (content never starts *underneath*
it) yet page content scrolls *under* it as you scroll — the classic translucent-header effect. The tab bar
uses `position: fixed` so it stays pinned regardless of scroll.

```razor
@using Shiny.Blazor.Controls

<!-- Frosted top toolbar: content scrolls under it -->
<ShinyToolbar Dock="ToolbarDock.Top"
              Frosted="true"
              Title="Inbox"
              Items="@toolbarItems"
              ItemClicked="OnItemClicked" />

<!-- Bottom tab bar with two-way selection and a badge -->
<ShinyTabBar Items="@tabs"
             @bind-SelectedKey="selectedKey"
             ActiveColor="#7C3AED"
             Frosted="true" />

@code {
    string? selectedKey = "home";

    List<ToolbarItem> toolbarItems = new()
    {
        new() { Icon = "<svg>…search…</svg>", Text = "Search" },
        new() { Icon = "<svg>…bell…</svg>", Text = "Alerts", Badge = "3" },
        new() { Icon = "compose.png", Text = "Compose", Href = "/compose" }
    };

    List<TabBarItem> tabs = new()
    {
        new() { Key = "home",   Label = "Home",   Icon = "<svg>…</svg>", ActiveIcon = "<svg>…filled…</svg>" },
        new() { Key = "chat",   Label = "Chat",   Icon = "<svg>…</svg>", Badge = "5" },
        new() { Key = "me",     Label = "Profile",Icon = "<svg>…</svg>", Href = "/profile" }
    };

    void OnItemClicked(ToolbarItem item) { /* … */ }
}
```

> Icons accept inline SVG/HTML markup, an emoji/glyph, or an image URL (`.png`/`.svg`/`http…`/`/…`).

**ShinyToolbar** parameters:

| Property | Type | Default | Description |
|---|---|---|---|
| Dock | ToolbarDock | Top | Docks to the `Top` or `Bottom` edge |
| Sticky | bool | true | `position:sticky` (content scrolls under); set false for a normal in-flow bar |
| Title | string? | null | Convenience leading title text (used when `StartContent` is not set) |
| Items | `List<ToolbarItem>?` | null | Trailing action/link items (used when `EndContent` is not set) |
| StartContent / ChildContent / EndContent | RenderFragment? | null | Custom leading / center / trailing content |
| BackgroundColor | string | #FFFFFF | Solid fill (ignored when `Frosted`) |
| TextColor | string | #1F2937 | Foreground color |
| Height | double | 56 | Bar height (min-height) |
| IconSize | double | 22 | Item icon size |
| ShowItemLabels | bool | false | Show each item's `Text` under its icon |
| Frosted | bool | false | Frosted glass via `backdrop-filter` |
| BlurRadius | double | 20 | Blur amount when `Frosted` |
| TintColor | string | rgba(255,255,255,0.7) | Translucent fill when `Frosted` |
| HasShadow | bool | true | Edge shadow (direction follows `Dock`) |
| BorderColor / BorderThickness | string? / double | null / 0 | Hairline on the docked edge |
| SafeArea | bool | true | Adds `env(safe-area-inset-*)` padding on the docked edge |
| ZIndex | int | 100 | Stacking order |
| CssClass / Style | string? | null | Extra root class / inline style |

Events: `ItemClicked` — fires the `ToolbarItem` that was tapped (items with an `Href` also navigate).

**ToolbarItem** properties: `Icon`, `Text`, `Href`, `Target`, `Badge`, `IconColor`, `IsDisabled`, `Tag`.

**ShinyTabBar** parameters:

| Property | Type | Default | Description |
|---|---|---|---|
| Items | `List<TabBarItem>?` | null | The tabs |
| SelectedKey | string? | null | Two-way bindable active tab `Key` |
| Dock | ToolbarDock | Bottom | Docks to the `Bottom` (default) or `Top` edge |
| Fixed | bool | true | `position:fixed` (always pinned); set false to use `sticky` inside a container |
| BackgroundColor | string | #FFFFFF | Solid fill (ignored when `Frosted`) |
| ActiveColor | string | #2196F3 | Selected tab color |
| InactiveColor | string | #9CA3AF | Unselected tab color |
| ShowLabels | bool | true | Show each tab's `Label` under its icon |
| Height | double | 56 | Bar height (min-height) |
| IconSize | double | 24 | Tab icon size |
| Frosted / BlurRadius / TintColor | bool / double / string | false / 20 / rgba(255,255,255,0.7) | Frosted glass options |
| HasShadow / BorderColor / BorderThickness | bool / string? / double | true / null / 0 | Edge chrome |
| SafeArea | bool | true | Adds `env(safe-area-inset-bottom)` padding (home-indicator clearance) |
| ZIndex | int | 100 | Stacking order |
| CssClass / Style | string? | null | Extra root class / inline style |

Events: `SelectedKeyChanged` (two-way bind via `@bind-SelectedKey`), `ItemClicked` — fires the tapped `TabBarItem`.

**TabBarItem** properties: `Key`, `Icon`, `ActiveIcon` (optional filled variant shown when selected), `Label`, `Href` (selecting also navigates), `Badge` (empty string `""` renders a dot), `IsDisabled`, `Tag`.

**Placement tip**: `position:sticky` sticks relative to the nearest scroll container, and any ancestor with
`overflow: hidden` silently breaks it — use `overflow: clip` if you must clip. For app-wide chrome, place
`ShinyToolbar` as the first element of your page/layout scroll area and drop `ShinyTabBar` anywhere (it's
`Fixed`). The Blazor sample wires both into `MainLayout` — a frosted top header plus a bottom tab bar that
appears on narrow viewports.

### SecurityPin

A PIN entry control with individually rendered cells that captures input through a hidden Entry. Digits remain visible by default and can optionally be masked with any character.

![SecurityPin](assets/securitypin.png)

```xml
<shiny:SecurityPin Length="4"
                   HideCharacter="*"
                   Value="{Binding Pin}"
                   Keyboard="Numeric"
                   Completed="OnPinCompleted" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Length | int | 4 | Number of PIN cells |
| Value | string | "" | Current PIN value (TwoWay) |
| Keyboard | Keyboard | Numeric | Keyboard type for input |
| HideCharacter | string? | null | When set, masks entered characters; when null/empty, shows actual values |
| CellSize | double | 50 | Width/height of each cell |
| CellSpacing | double | 8 | Space between cells |
| CellCornerRadius | double | 8 | Border corner radius |
| CellBorderColor | Color? | null | Cell border color |
| CellFocusedBorderColor | Color? | null | Border color for the active cell |
| CellBackgroundColor | Color? | null | Cell fill color |
| CellTextColor | Color? | null | Entered character color |
| FontSize | double | 24 | Character font size |

| UseFeedback | bool | Enable/disable feedback on digit entry (click) and completion (long press) (default: true) |

Events: `Completed` fires with a `SecurityPinCompletedEventArgs` once the entered value reaches `Length`.

Methods: `Focus()`, `Unfocus()`, `Clear()`.

### SignaturePad

A signature capture control that opens in a FloatingPanel overlay (MAUI) or SheetView (Blazor). Users draw on a canvas and tap Sign to export the signature as a PNG. The Sign button is disabled until the user actually draws something.

**Important:** Like FloatingPanel, SignaturePad must be placed inside an `OverlayHost` or `ShinyContentPage` on MAUI — it uses a FloatingPanel internally.

While the pad is open the control automatically suppresses system navigation gestures that would otherwise steal edge-started strokes (restoring them on close): on iOS, the navigation controller's interactive "swipe back" pop; on Android, the system back edge-swipe (API 29+) and — when the pad is hosted inside a `TabbedPage` — the swipe-between-tabs gesture. So strokes that begin near the screen edges are drawn instead of navigating away.

```xml
<!-- MAUI — must be inside ShinyContentPage.Panels or OverlayHost -->
<shiny:ShinyContentPage xmlns:shiny="http://shiny.net/maui/controls">
    <shiny:ShinyContentPage.PageContent>
        <VerticalStackLayout Padding="20" Spacing="10">
            <Button Text="Capture Signature" Command="{Binding OpenSignatureCommand}" />
            <Image Source="{Binding SignatureImage}" HeightRequest="150" Aspect="AspectFit" />
        </VerticalStackLayout>
    </shiny:ShinyContentPage.PageContent>
    <shiny:ShinyContentPage.Panels>
        <shiny:SignaturePad IsOpen="{Binding IsSignatureOpen}"
                            StrokeColor="Black"
                            SignatureBackgroundColor="#F8F8F8"
                            StrokeWidth="3"
                            SignButtonColor="#6C63FF"
                            CancelButtonColor="#94A3B8"
                            SignCommand="{Binding HandleSignedCommand}"
                            CancelCommand="{Binding HandleCancelledCommand}" />
    </shiny:ShinyContentPage.Panels>
</shiny:ShinyContentPage>
```

```razor
<!-- Blazor -->
<SignaturePad @bind-IsOpen="isOpen"
              StrokeColor="#000000"
              SignatureBackgroundColor="#F8F8F8"
              StrokeWidth="3"
              SignButtonColor="#6C63FF"
              CancelButtonColor="#94A3B8"
              Signed="OnSigned"
              Cancelled="OnCancelled" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| IsOpen | bool | false | Opens/closes the signature panel (TwoWay) |
| Position | FloatingPanelPosition | Bottom | Panel slide direction (Bottom, BottomTabs, Top) |
| IsLocked | bool | true | Prevents drag dismiss |
| Detent | DetentValue | Half | Panel snap position |
| StrokeColor | Color | Black | Drawing stroke color |
| SignatureBackgroundColor | Color | White | Canvas background |
| StrokeWidth | double | 3.0 | Drawing stroke width |
| SignButtonText | string | "Sign" | Sign button label |
| CancelButtonText | string | "Cancel" | Cancel button label |
| SignButtonColor | Color | Blue | Sign button background |
| CancelButtonColor | Color | Gray | Cancel button background |
| ShowCancelButton | bool | true | Show/hide cancel button |
| PanelBackgroundColor | Color | White | Panel background |
| PanelCornerRadius | double | 16 | Panel corner radius |
| HasBackdrop | bool | true | Backdrop behind panel |
| ExportWidth | int | 600 | Exported PNG width |
| ExportHeight | int | 200 | Exported PNG height |
| SignCommand | ICommand? | null | Invoked on sign with `SignatureImageEventArgs` |
| CancelCommand | ICommand? | null | Invoked on cancel |

Blazor uses CSS color strings instead of `Color`, `SheetDirection` instead of `FloatingPanelPosition`, and `Signed` is `EventCallback<byte[]>` (raw PNG bytes).

Events: `Signed` fires with `SignatureImageEventArgs` (MAUI) or `byte[]` (Blazor). `Cancelled` fires on cancel.

### FrostedGlassView

A view that applies a native frosted glass (blur) effect behind its content. Place over images or busy backgrounds for a glassmorphism effect.

```xml
<shiny:FrostedGlassView BlurRadius="20"
                        TintColor="#80FFFFFF"
                        TintOpacity="0.6"
                        CornerRadius="16">
    <VerticalStackLayout Padding="20" Spacing="8">
        <Label Text="Glass Card" FontSize="20" FontAttributes="Bold" />
        <Label Text="Content over blurred background." FontSize="14" />
    </VerticalStackLayout>
</shiny:FrostedGlassView>
```

```razor
<!-- Blazor -->
<FrostedGlass BlurRadius="20" TintColor="rgba(255,255,255,0.6)" CornerRadius="16">
    <h3>Glass Card</h3>
    <p>Content over blurred background.</p>
</FrostedGlass>
```

| Property | Type | Default | Description |
|---|---|---|---|
| GlassContent / ChildContent | View / RenderFragment | - | Content rendered on top of the glass |
| BlurRadius | double | 20 | Blur strength in pixels |
| TintColor | Color / string | #80FFFFFF / rgba(255,255,255,0.6) | Glass tint overlay |
| TintOpacity | double | 0.6 | Tint opacity (MAUI only) |
| CornerRadius | double | 0 | Corner radius for clipping |

**Platform implementation:** iOS uses `UIVisualEffectView`, Android 12+ uses `RenderEffect.CreateBlurEffect`, Blazor uses CSS `backdrop-filter: blur()`.

### Toast

A service-first toast notification system — inject `IToaster` (registered by `UseShinyControls()`) and call from code. No XAML or OverlayHost required. The overlay auto-attaches to the current page on first use.

```csharp
using Shiny.Maui.Controls.Toast;

public class MyViewModel(IToaster toaster)
{
    // Simple
    await toaster.ShowAsync("Item saved!");

    // With spinner + manual dismiss
    IDisposable toast = await toaster.ShowAsync("Uploading...", cfg =>
    {
        cfg.Spinner = ToastSpinnerPosition.Left;
        cfg.Duration = TimeSpan.Zero;
    });
    // Later: toast.Dispose();
}
```

**Themed methods** — colors from MAUI Styles or built-in defaults:

```csharp
await toaster.InfoAsync("Update available");        // Blue
await toaster.SuccessAsync("File saved");           // Green
await toaster.WarningAsync("Storage almost full");  // Amber
await toaster.DangerAsync("Save failed");           // Orange
await toaster.CriticalAsync("System error");        // Red
```

```razor
<!-- Blazor: register AddShinyToast() in DI, place <ToastHost /> in layout -->
@inject IToastService ToastService

await ToastService.ShowAsync("Saved!", cfg =>
{
    cfg.Duration = TimeSpan.FromSeconds(3);
    cfg.ShowProgressBar = true;
});

// Blazor themed methods also available:
await ToastService.InfoAsync("Update available");
await ToastService.SuccessAsync("File saved");
```

| Property | Type | Default | Description |
|---|---|---|---|
| Text | string | (required) | Toast message |
| Duration | TimeSpan | 3s | Auto-dismiss. Zero = manual only |
| Position | ToastPosition | Bottom | Top or Bottom |
| DisplayMode | ToastDisplayMode | Pill | Pill (rounded) or FillHorizontal (full width) |
| DismissOnTap | bool | true | Tap to dismiss |
| QueueMode | ToastQueueMode | Queue | Queue (sequential) or Stack (multiple visible) |
| Spinner | ToastSpinnerPosition | None | None, Left, or Right |
| ShowProgressBar | bool | false | Countdown drain bar |
| Icon | ImageSource? | null | Optional icon (MAUI) |
| TapCommand | ICommand? | null | Tap action (MAUI) |
| UseFeedback | bool | true | Feedback on show/dismiss |
| BackgroundColor | Color? | dark gray | Background fill |
| TextColor | Color? | white | Text color |
| BorderColor | Color? | null | Border stroke |
| CornerRadius | double | 20 | Corner radius (pill mode) |
| TextOverflow | ToastTextOverflow | Ellipsis | Ellipsis, MultiLine, or Marquee |
| MarqueeSpeedPixelsPerSecond | double | 40 | Scroll speed for marquee mode |

**Text Overflow modes:**
- `Ellipsis` — truncates long text with "…" (default)
- `MultiLine` — wraps text to multiple lines
- `Marquee` — scrolling ticker animation (configure speed via `MarqueeSpeedPixelsPerSecond`)

### Dialogs

A service-first dialog system that emulates the classic `alert`, `confirm`, `prompt`, and `action sheet` primitives — with **owned (non-native), animated, themeable** dialogs on **both MAUI and Blazor**. Inject `IDialogService` and `await` a result — no markup per call. Calls are queued, so awaiting several in a row shows them one at a time.

- **MAUI**: registered by `UseShinyControls()`. The overlay auto-attaches to whichever page is current **at the time of each call** (no XAML or OverlayHost required), so dialogs keep working across navigation.
- **Blazor**: register `AddShinyDialogs()` in DI and place a single `<DialogHost />` in your layout.

```csharp
// MAUI — inject IDialogService (e.g. into a ViewModel)
public class MyViewModel(IDialogService dialogs)
{
    await dialogs.Alert("Heads up", "Your changes have been saved.", "Got it");

    var ok = await dialogs.Confirm("Delete item?", "This cannot be undone.", okText: "Delete", cancelText: "Cancel");

    var result = await dialogs.Prompt("What's your name?", "We'll personalize your experience.", placeholder: "e.g. Allan");
    if (result.Ok)
        Console.WriteLine(result.Value);
}
```

```razor
@* Blazor — same surface *@
@inject IDialogService Dialogs

await Dialogs.Alert("Heads up", "Your changes have been saved.", "Got it");
var ok = await Dialogs.Confirm("Delete item?", "This cannot be undone.", okText: "Delete", cancelText: "Cancel");
var result = await Dialogs.Prompt("Your name?", "Personalize things.", placeholder: "e.g. Allan");

// action sheet — returns the chosen option's text (or null if cancelled); mark one option destructive (red)
var choice = await Dialogs.ActionSheet("Photo", ["Take Photo", "Choose from Library", "Delete Photo"], destructive: "Delete Photo");
```

| Method | Returns | Buttons |
|---|---|---|
| `Alert(title, message, okText, configure?)` | `Task` | OK |
| `Confirm(title, message, okText, cancelText, configure?)` | `Task<bool>` | confirm + cancel |
| `Prompt(title, message, placeholder, okText, cancelText, initialValue?, maxLength?, keyboard?/inputType?, configure?)` | `Task<PromptResult>` | confirm + cancel + text field |
| `ActionSheet(title, options, cancelText, destructive?, configure?)` | `Task<string?>` | one button per option + cancel (returns the chosen option, or `null` if cancelled) |

`Prompt` forwards `initialValue`, `maxLength`, and the keyboard directly (MAUI takes a `Keyboard`; Blazor takes an HTML `inputType` string). Pass `cancelText: null` to `Prompt` or `ActionSheet` to **hide the cancel button** entirely (the ActionSheet otherwise always renders one).

**Animations** — every call takes an optional `configure` delegate to set the entry/exit animation and styling. `DialogAnimation` values: `None`, `Fade`, `SlideTop`, `SlideBottom`, `SlideLeft`, `SlideRight`, `Zoom`, `Pop` (default).

```csharp
await dialogs.Confirm("Delete?", "This cannot be undone.", configure: c =>
{
    c.Animation = DialogAnimation.SlideBottom;
    c.BackgroundColor = Color.FromArgb("#312E81");   // MAUI Color (Blazor: CSS string)
    c.OkButtonColor = Color.FromArgb("#22D3EE");
    c.CornerRadius = 24;
});
```

**Customization**
- **Per-call**: the `configure` delegate (animation, colors, corner radius, backdrop opacity, dismiss behavior).
- **Global defaults**: MAUI `UseShinyControls(c => c.ConfigureDialogs(o => o.DefaultAnimation = DialogAnimation.Zoom))`; Blazor `AddShinyDialogs(o => o.DefaultAnimation = DialogAnimation.Zoom)`.
- **Full template override**: MAUI `DialogOptions.ContentTemplate` (a `DataTemplate` bound to `DialogContext`); Blazor `<DialogHost Template="...">` (a `RenderFragment<DialogContext>`). The host still supplies the dimmed backdrop and animation.
- **Replace the service**: MAUI `c.SetCustomDialogs<T>()`.

Tapping the backdrop or pressing `Escape` (Blazor) cancels (`Confirm` → `false`, `Prompt` → `Cancelled`); `Enter` confirms. Colors follow the theme tokens (`--shiny-color-surface` / `Shiny.Color.Surface`, `Primary`, …) so dialogs match light/dark automatically.

### DataGrid

A feature-rich data grid for both hosts, modeled on MudBlazor's DataGrid. Blazor renders a semantic
HTML `<table>` (generic `DataGrid<TItem>`); MAUI is a pure cross-platform composite (a `Grid` header
over a virtualized `CollectionView`, no native handlers). Same feature surface on both: typed
`PropertyColumn` + `TemplateColumn`, sorting (single + multi), column **filtering** (menu / row /
toolbar quick-search), **grouping** with expandable groups, footer/group **aggregates**
(Count/Sum/Average/Min/Max/Custom), single/multi **selection** with checkboxes, inline **editing**
(cell + form), **paging**, **virtualization**, column **resize/reorder**, sticky header, loading +
empty states, a `ServerData` delegate for server-side data, and density/striped/bordered/hover styling.
Colors follow the theme tokens.

```razor
@* Blazor *@
<DataGrid TItem="Person" Items="people" MultiSelection="true"
          SortMode="DataGridSortMode.Multiple" FilterMode="DataGridFilterMode.Menu"
          Groupable="true" EditMode="DataGridEditMode.Form"
          Dense="true" Striped="true" Hover="true" FixedHeader="true" Height="420px">
    <Columns>
        <PropertyColumn Property="x => x.FirstName" Title="First" />
        <PropertyColumn Property="x => x.Age" Format="N0" />
        <PropertyColumn Property="x => x.Salary" Format="C0" />
        <TemplateColumn Title="Status" Sortable="false">
            <CellTemplate><Pill Text="@(context.Item.Active ? "Active" : "Inactive")" /></CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent><DataGridPager TItem="Person" /></PagerContent>
</DataGrid>
```

```xml
<!-- MAUI -->
<shiny:DataGrid ItemsSource="{Binding People}" SelectionMode="Multiple"
                SortMode="Multiple" FilterMode="Menu" Groupable="True"
                PageSize="20" EditMode="Form" AllowColumnResize="True" AllowColumnReorder="True"
                Striped="True" Bordered="True">
    <shiny:DataGridColumn Title="First" PropertyName="FirstName" Width="*" />
    <shiny:DataGridColumn Title="Age" PropertyName="Age" Width="Auto" />
    <shiny:DataGridColumn Title="Salary" PropertyName="Salary" StringFormat="{}{0:C0}" Width="*">
        <shiny:DataGridColumn.Aggregate>
            <shiny:DataGridAggregateDefinition Type="Sum" Format="C0" />
        </shiny:DataGridColumn.Aggregate>
    </shiny:DataGridColumn>
    <shiny:DataGridTemplateColumn Title="Status" Width="Auto" Editable="False">
        <shiny:DataGridTemplateColumn.CellTemplate>
            <DataTemplate><shiny:PillView Text="{Binding StatusText}" /></DataTemplate>
        </shiny:DataGridTemplateColumn.CellTemplate>
    </shiny:DataGridTemplateColumn>
</shiny:DataGrid>
```

Reflection-based string-path columns are annotated for trimming; set a column's `ValueGetter`/
`ValueSetter` (MAUI) for fully reflection-free AOT.

Header titles ellipsize and clip to their own column, so budget columns to the width you have: a
phone-width grid fits roughly **3–4** columns, fewer once `AllowColumnResize`, `AllowColumnReorder`,
`Groupable`, or `FilterMode="Menu"` add their glyphs to each header. Columns with no `Width` are `*`
and split whatever the `Auto` columns leave behind.

### TableView

A settings-style table view with 14+ built-in cell types, section grouping, drag-to-reorder, and dynamic data binding.

| Basic | Dynamic | Drag & Sort | Pickers | Styling |
|:---:|:---:|:---:|:---:|:---:|
| ![Basic](assets/tableview-basic.png) | ![Dynamic](assets/tableview-dynamic.png) | ![Drag & Sort](assets/tableview-dragsort.png) | ![Pickers](assets/tableview-picker.png) | ![Styling](assets/tableview-styling.png) |

```xml
<shiny:TableView>
    <shiny:TableRoot>
        <shiny:TableSection Title="General">
            <shiny:SwitchCell Title="Wi-Fi" On="{Binding WifiEnabled}" />
            <shiny:EntryCell Title="Username" Text="{Binding Username}" />
            <shiny:PickerCell Title="Theme" ItemsSource="{Binding Themes}" SelectedItem="{Binding SelectedTheme}" />
        </shiny:TableSection>
    </shiny:TableRoot>
</shiny:TableView>
```

**Cell Types:**

| Cell | Description |
|---|---|
| SwitchCell | Toggle switch |
| EntryCell | Text input field — with TextEntry's input masking, keyboard accessory bar (iOS/Android) and autocomplete opt-out |
| CheckboxCell | Checkbox with accent color |
| RadioCell | Radio button with section-level grouping |
| CommandCell | Tappable row with optional arrow indicator |
| ButtonCell | Command-bound button |
| LabelCell | Read-only text display |
| PickerCell | Single or multi-select picker |
| TextPickerCell | String list picker |
| DatePickerCell | Date selection with min/max bounds |
| TimePickerCell | Time selection with 24-hour mode and minute interval |
| DurationPickerCell | TimeSpan picker with min/max |
| NumberPickerCell | Integer picker with min/max/unit |
| SimpleCheckCell | Checkmark indicator |
| CustomCell | Custom view content with drag-reorder support |

**EntryCell input features** — `EntryCell` shares `TextEntry`'s input behaviour without any of its chrome (no tools, no floating label, no hint — the cell already has those):

```xml
<shiny:TableSection Title="Payment">
    <shiny:EntryCell Title="Phone" Mask="(###) ###-####"
                     ValueText="{Binding Phone, Mode=TwoWay}"
                     FieldGroup="payment" AccessoryPreset="NavigationAndDone" />
    <shiny:EntryCell Title="Card" Mask="#### #### #### ####"
                     ValueText="{Binding Card, Mode=TwoWay}"
                     FieldGroup="payment" AccessoryPreset="NavigationAndDone" />
</shiny:TableSection>
```

| Property | Type | Default | Description |
|---|---|---|---|
| Mask | string? | null | Input mask (`#` = digit slot). `ValueText` stays raw; `FormattedValueText` is what's displayed |
| FormattedValueText | string | "" | Read-only masked display value |
| Accessory | KeyboardAccessoryView? | null | Bar docked to the top of the soft keyboard (iOS + Android) |
| AccessoryPreset | KeyboardAccessoryPreset | None | `Done`, `Navigation`, `NavigationAndDone` |
| FieldGroup | string? | null | Scopes accessory prev/next to a subset of fields |
| IsAutoCompleteEnabled | bool | true | False switches off autofill, autocorrect, prediction and spell check |

`TableView` is not virtualized, so accessory prev/next reaches every cell on the page. Blazor supports `Mask` and `IsAutoCompleteEnabled`; the accessory bar is MAUI-only.

**Dynamic Sections** - Bind to a collection to generate sections from data:

```xml
<shiny:TableView ItemsSource="{Binding Items}" ItemTemplate="{StaticResource SectionTemplate}" />
```

**Drag to reorder** - `UseDragSort="True"` puts a drag handle on every row in a section. Dragging a
handle lifts the row under the finger, draws an insertion line at the drop position, and auto-scrolls
when the drag reaches the top or bottom edge; touches anywhere else still scroll the table. Rows
reorder within their own section only.

```xml
<shiny:TableView ItemDropped="OnItemDropped">
    <shiny:TableRoot>
        <shiny:TableSection Title="Reorder" UseDragSort="True">
            <shiny:LabelCell Title="First" ValueText="1" />
            <shiny:LabelCell Title="Second" ValueText="2" />
        </shiny:TableSection>
    </shiny:TableRoot>
</shiny:TableView>
```

`ItemDropped` / `ItemDroppedCommand` report `Section`, `Cell`, `Item`, `FromIndex`, and `ToIndex`.
Cells declared in XAML are reordered by the control; rows generated from a section's `ItemsSource`
are not - their order lives in your collection, so move `Item` to `ToIndex` yourself in the handler.
The gesture is pan-driven on every platform (the platform `DragGestureRecognizer` is broken on Mac
Catalyst and absent from the AppKit and GTK4 hosts, and reports no pointer position where it does
work), with native hooks on iOS and Android that stop the enclosing scroller from stealing the drag.

### TreeView

Hierarchical tree control with lazy-loaded branches, configurable expand/collapse icons, single or multi-selection (checkbox per row), per-item `CanExpand`/`CanSelect` predicates, retry on load failure, optional guide lines, and drag/drop reorder. Available on both MAUI and Blazor.

| Initial | Expanded | Multi-level | Lazy loading | Multi-select |
|:---:|:---:|:---:|:---:|:---:|
| ![Initial](assets/treeview-initial.png) | ![Expanded](assets/treeview-expanded.png) | ![Multi-level](assets/treeview-deep.png) | ![Lazy load](assets/treeview-loading.png) | ![Multi-select](assets/treeview-multiselect.png) |

```xml
<shiny:TreeView x:Name="Tree"
                IndentSize="22"
                ShowGuideLines="True"
                SelectionMode="Single"
                SelectedItem="{Binding Selected, Mode=TwoWay}"
                ItemSelected="OnSelected"
                ItemExpanded="OnExpanded"
                LoadFailed="OnLoadFailed">
    <shiny:TreeView.ItemTemplate>
        <DataTemplate x:DataType="local:FileNode">
            <HorizontalStackLayout Spacing="8">
                <Label Text="{Binding Icon}" />
                <Label Text="{Binding Name}" VerticalTextAlignment="Center" />
            </HorizontalStackLayout>
        </DataTemplate>
    </shiny:TreeView.ItemTemplate>
</shiny:TreeView>
```

```csharp
// Delegates aren't bindable from XAML — wire in code-behind
Tree.ItemsSource         = roots;
Tree.ChildrenSelector    = item => (item is FileNode f && !f.LazyLoad) ? f.Children : null;
Tree.ChildrenLoader      = LoadRemoteChildrenAsync;            // covers lazy branches
Tree.HasChildrenSelector = item => item is FileNode { IsFolder: true };
Tree.CanSelectSelector   = item => item is FileNode f && !f.IsLocked;
```

**Key Properties:**

| Property | Type | Description |
|---|---|---|
| ItemsSource | IEnumerable | Root items (ignored when `RootLoader` is set) |
| RootLoader | `Func<Task<IEnumerable<object>>>` | Async loader for roots; shows a centered spinner |
| ChildrenSelector | `Func<object, IEnumerable<object>?>` | Sync children getter (return `null` to defer to loader) |
| ChildrenLoader | `Func<object, Task<IEnumerable<object>>>` | Async children loader; cached on first expand |
| HasChildrenSelector | `Func<object, bool>` | Render chevron only when true |
| CanExpandSelector | `Func<object, bool>` | Gate expand gesture (dimmed chevron when false) |
| CanSelectSelector | `Func<object, bool>` | Gate selection per item |
| SelectionMode | TreeSelectionMode | `None` / `Single` / `Multiple` (switching modes clears the current selection) |
| ShowSelectionCheckBoxes | bool | Checkbox on every row while `SelectionMode` is `Multiple` (default true) |
| CheckBoxColor | Color? | Checkbox tint (MAUI); Blazor uses the `--shiny-color-primary` token |
| SelectedItem | object? | Two-way (Single mode) |
| SelectedItems | IList\<object\>? | Two-way (Multiple mode) |
| ExpandedIcon / CollapsedIcon / RetryIcon | ImageSource? | Fall back to ▼ / ▶ / ↻ glyphs |
| IndentSize | double | Pixels of indent per depth level (default 20) |
| ShowGuideLines | bool | Vertical connector lines between parent and children |
| EnableDragDrop | bool | Drag/drop with above/below/into drop positions and visual drop indicators; event-only, never mutates data |

**Events + Commands (MAUI):** `ItemSelected` / `ItemExpanded` / `ItemCollapsed` / `LoadFailed` / `ItemDropped` each have a matching `*Command` bindable property.

`ItemDropped` reports `Source`, `Target`, and `Position` (`Above` / `Below` reorder among siblings, `Into` drops into a folder) — your handler moves the data, then rebinds `ItemsSource` (MAUI) or calls `ReloadAsync()` (Blazor, which preserves expansion/selection state). Blazor drag/drop runs on native HTML5 drag events via a small JS module (required for Safari/Firefox `dataTransfer` support); MAUI uses platform drag gestures with a pan-gesture fallback on Mac Catalyst, AppKit, and GTK4 where those are broken or missing.

**Multi-select:** `SelectionMode="Multiple"` puts a checkbox on every row. The whole row is the hit target — tapping the row or its checkbox toggles it — and rows failing `CanSelectSelector` show a disabled box. Set `ShowSelectionCheckBoxes="False"` for the older highlight-only look.

**Public methods:** `ExpandAll(maxDepth)`, `ExpandAllAsync(maxDepth)`, `CollapseAll`, `Expand(item)`, `Collapse(item)`, `SelectAll()`, `DeselectAll()`, `SetBranchSelected(item, selected)`, `Refresh(item)`, `ReloadAsync`, `FindNode(item)` — Blazor mirrors these as `ExpandAsync` / `CollapseAsync` / `ExpandAllAsync` / `CollapseAll` / `SelectAllAsync` / `DeselectAllAsync` / `SetBranchSelectedAsync` / `RefreshAsync` / `ReloadAsync` / `FindNode`.

`ExpandAll` materializes everything `ChildrenSelector` can supply — it only leaves branches that need `ChildrenLoader`, which `ExpandAllAsync` awaits. Both stop at `maxDepth` (default 32) so a self-referencing or endlessly generated hierarchy can't expand forever. `SelectAll` / `SetBranchSelected` cover collapsed branches too, but only nodes that have been loaded — call `ExpandAllAsync()` first to check a lazy tree in full.

### Markdown Controls

> Separate NuGet packages: `Shiny.Maui.Controls.Markdown` / `Shiny.Blazor.Controls.Markdown`

Render and edit markdown content using native MAUI controls — no WebView required on MAUI. Auto-resolves Light/Dark theming. Available for both MAUI and Blazor.

| Viewer | Editor |
|:---:|:---:|
| ![Viewer](assets/markdown-view.png) | ![Editor](assets/markdown-editor.png) |

**MarkdownView** — Read-only markdown renderer:

```xml
<md:MarkdownView Markdown="{Binding DocumentContent}" Padding="16" />
```

| Property | Type | Description |
|---|---|---|
| Markdown | string | Markdown content to render |
| Theme | MarkdownTheme? | Rendering theme (auto Light/Dark if null) |
| IsScrollEnabled | bool | Enable/disable scrolling (default: true) |

Events: `LinkTapped` — fired when a link is tapped; set `Handled = true` to prevent default browser launch.

**MarkdownEditor** — Editor with formatting toolbar and live preview:

```xml
<md:MarkdownEditor Markdown="{Binding NoteContent, Mode=TwoWay}"
                   Placeholder="Start writing..."
                   Padding="8" />
```

| Property | Type | Description |
|---|---|---|
| Markdown | string | Markdown content (TwoWay) |
| Theme | MarkdownTheme? | Preview theme (auto Light/Dark if null) |
| Placeholder | string | Placeholder text |
| ToolbarItems | IReadOnlyList\<MarkdownToolbarItem\>? | Toolbar buttons (default set provided) |
| IsPreviewVisible | bool | Toggle preview pane (TwoWay) |
| ToolbarBackgroundColor | Color? | Toolbar background |
| EditorBackgroundColor | Color? | Editor background |

**Features:**
- Formatting toolbar: bold, italic, headings, lists, code, links, blockquotes
- Live preview toggle
- Auto-growing editor
- Full Markdig support: tables, task lists, strikethrough, fenced code blocks
- Customizable themes with colors, font sizes, and spacing
- Custom toolbar item support

### MermaidDiagramControl

> Separate NuGet packages: `Shiny.Maui.Controls.MermaidDiagrams` / `Shiny.Blazor.Controls.MermaidDiagrams`

Native Mermaid flowchart renderer — no WebView, no SkiaSharp, AOT compatible on MAUI. Parses Mermaid syntax and renders interactive diagrams with pan and zoom support. Available for both MAUI and Blazor.

| Flowchart | Editor | Themes | Subgraphs |
|:---:|:---:|:---:|:---:|
| ![Flowchart](assets/mermaid-flowchart.png) | ![Editor](assets/mermaid-editor.png) | ![Themes](assets/mermaid-themes.png) | ![Subgraphs](assets/mermaid-subgraphs.png) |

```bash
dotnet add package Shiny.Maui.Controls.MermaidDiagrams
```

```xml
xmlns:diagram="http://shiny.net/maui/diagrams"
```

```xml
<diagram:MermaidDiagramControl
    DiagramText="graph TD&#10;    A[Start] --> B{Decision}&#10;    B -->|Yes| C[Do Something]&#10;    B -->|No| D[Do Other]&#10;    C --> E[End]&#10;    D --> E"
    HorizontalOptions="Fill"
    VerticalOptions="Fill" />
```

**Features:**
- Mermaid `graph` / `flowchart` syntax (TD, LR, BT, RL directions)
- 6 node shapes: Rectangle, RoundedRectangle, Stadium, Circle, Diamond, Hexagon
- 6 edge styles: Solid, Open, Dotted, DottedOpen, Thick, ThickOpen
- Subgraph support with nested grouping
- 4 built-in themes: Default, Dark, Forest, Neutral
- Pan and pinch-to-zoom gestures
- Sugiyama layered graph layout algorithm

### Barcodes & QR Codes

> Separate NuGet packages: `Shiny.Maui.Controls.Barcodes` / `Shiny.Blazor.Controls.Barcodes`

Pure-managed 1D and 2D barcode renderer powered by ZXing.Net. No SkiaSharp, no `System.Drawing`, AOT-safe on every TFM. MAUI renders to PNG bytes via a built-in PNG encoder and feeds an `Image`. Blazor renders inline SVG by default (crisp at any size) or a PNG `data:` URI. Need raw bytes or markup? Call the static `BarcodeRenderer` directly.

**Supported formats:** QR Code, Aztec, Data Matrix, PDF417, Code 128, Code 39, Code 93, Codabar, EAN-8, EAN-13, UPC-A, UPC-E, ITF.

```xml
xmlns:bc="http://shiny.net/maui/barcodes"
```

```xml
<!-- Any supported 1D/2D barcode -->
<bc:BarcodeView Value="5901234123457"
                Format="Ean13"
                PixelWidth="400"
                PixelHeight="150"
                ForegroundColor="Black"
                BarcodeBackgroundColor="White" />

<!-- QR code shortcut with error correction -->
<bc:QRCodeView Value="https://shinylib.net"
               Size="300"
               ErrorCorrection="High" />
```

```razor
<!-- Blazor: SVG by default; switch to PNG with ImageFormat="BarcodeImageFormat.Png" -->
<BarcodeView Value="5901234123457"
             Format="BarcodeFormat.Ean13"
             PixelWidth="400"
             PixelHeight="150" />

<QRCodeView Value="https://shinylib.net"
            Size="300"
            QRErrorCorrection="QRErrorCorrection.High" />
```

**BarcodeView properties (MAUI):**

| Property | Type | Default | Description |
|---|---|---|---|
| Value | string | "" | Content to encode. Empty clears the image |
| Format | BarcodeFormat | Code128 | Symbology (see list above) |
| PixelWidth | int | 400 | Output bitmap width in pixels |
| PixelHeight | int | 150 | Output bitmap height in pixels |
| MarginPixels | int | 10 | Quiet zone around the symbol (in pixels) |
| ForegroundColor | Color | Black | Bar / module color |
| BarcodeBackgroundColor | Color | White | Background fill |

**QRCodeView additional properties (MAUI):** inherits everything from `BarcodeView`; locks `Format` to `QRCode` and adds:

| Property | Type | Default | Description |
|---|---|---|---|
| Size | int | 300 | Square output edge length in px (sets both `PixelWidth` and `PixelHeight`) |
| ErrorCorrection | QRErrorCorrection | Medium | `Low` / `Medium` / `Quartile` / `High` — higher tolerates more damage at the cost of capacity |

**BarcodeView parameters (Blazor):**

| Parameter | Type | Default | Description |
|---|---|---|---|
| Value | string? | null | Content to encode |
| Format | BarcodeFormat | Code128 | Symbology |
| ImageFormat | BarcodeImageFormat | Svg | `Svg` (inline `<svg>`) or `Png` (`<img>` with `data:` URI) |
| PixelWidth / PixelHeight | int | 400 / 150 | Encoder pixel size (also default CSS size when `CssWidth`/`CssHeight` unset) |
| MarginPixels | int | 10 | Quiet zone in pixels |
| ForegroundColor / BackgroundColor | string | "#000000" / "#FFFFFF" | CSS hex colors |
| CssWidth / CssHeight | string? | null | CSS sizing overrides for the host element (e.g. `"100%"`, `"4cm"`) |
| AltText | string? | null | `alt` attribute when rendered as PNG `<img>` |
| QRErrorCorrection | QRErrorCorrection | Medium | Only honored when `Format=QRCode` |

**QRCodeView (Blazor):** inherits everything from `BarcodeView` and exposes `Size` (default `300`) which sets `PixelWidth`/`PixelHeight`. `Format` is forced to `QRCode`.

**Render directly from code (no view needed):**

```csharp
using Shiny.Controls.Barcodes;

var opts = new BarcodeRenderOptions
{
    PixelWidth = 600,
    PixelHeight = 200,
    Margin = 10,
    ForegroundColor = "#000000",
    BackgroundColor = "#FFFFFF",
    QRErrorCorrection = QRErrorCorrection.High // QR only
};

byte[] png  = BarcodeRenderer.RenderPng("Hello", BarcodeFormat.QRCode, opts);
string svg  = BarcodeRenderer.RenderSvg("Hello", BarcodeFormat.QRCode, opts);
string dataUri = BarcodeRenderer.RenderDataUri("Hello", BarcodeFormat.QRCode, BarcodeImageFormat.Png, opts);
```

**Notes:**
- The PNG encoder is pure managed (zlib stored blocks + CRC32 + Adler32) — no SkiaSharp / `System.Drawing` dependency, ships clean on iOS / Android / Mac Catalyst / Windows / Blazor WebAssembly.
- SVG output uses a single horizontal-run `<path>` (with `shape-rendering="crispEdges"`), so it scales infinitely without aliasing and stays tiny in DOM size — preferred for Blazor.
- `ErrorCorrection.High` adds ~30% redundancy to a QR code — use it for printed labels, stickers, or anything that might be partially obscured.
- 1D formats (EAN, UPC, Code 128, etc.) require a valid payload for the chosen symbology — invalid input clears the image silently rather than throwing.

### Keyframe Animation

Declarative keyframe animation for MAUI views — the CSS `@keyframes` model, with the one thing MAUI's own `Animation` class can't do: **seek**. Ships as `Shiny.Maui.Controls.Keyframe` (XAML + views) on top of `Shiny.Controls.Keyframe.Shared` (the host-neutral timing engine), with `Shiny.Maui.Controls.Keyframe.Export` as an optional headless renderer.

```xml
xmlns:kf="http://shiny.net/maui/keyframe"

<Border>
  <kf:Animate.Keyframes>
    <kf:Keyframes Duration="0:0:1.2" Iterations="Infinite" Direction="Alternate" Fill="Both">

      <kf:Track Property="Scale">
        <kf:Key Offset="0"   Value="1" />
        <kf:Key Offset="0.5" Value="1.15" Easing="CubicOut" />
        <kf:Key Offset="1"   Value="1" />
      </kf:Track>

      <kf:Track Property="BackgroundColor">
        <kf:Key Offset="0" Value="#2563EB" />
        <kf:Key Offset="1" Value="#EC4899" />
      </kf:Track>

    </kf:Keyframes>
  </kf:Animate.Keyframes>
</Border>
```

`Easing` takes named curves (`CubicOut`, `BounceOut`, `Emphasized`, …) **and** CSS function syntax, so a curve copied out of a design tool or browser devtools works verbatim: `cubic-bezier(0.34, 1.56, 0.64, 1)`, `steps(8)`, `spring(0.35, 14)`. Omit `Value` on a key and it resolves to the target's **live value when playback starts**, so a re-triggered animation continues from where it is instead of snapping.

The same model drives C# timelines and storyboards:

```csharp
var timeline = TimelineBuilder
    .Create(TimeSpan.FromSeconds(1))
    .PingPong()
    .RepeatForever()
    .Animate(view, (v, x) => v.Scale = x, k => k
        .From(1)
        .Key(0.5, 1.2, Easings.CubicOut)
        .To(1))
    .Build();

var player = view.Play(timeline);

player.Rate = -1;                 // reverse, mid-flight
player.SeekProgress(0.35);        // scrub from a slider or gesture
await player.PlayAsync();
```

…and a drawn scene graph rendered to a canvas, the Lottie-shaped lane:

```csharp
var scene = new KeyframeScene(400, 200);
var dot = scene.Add(new EllipseLayer { Size = new SizeF(28, 28), Fill = Colors.Blue });

scene.Animation = TimelineBuilder
    .Create(TimeSpan.FromSeconds(1.4))
    .AnimatePosition(dot, k => k.From(new PointF(0, 86)).To(new PointF(300, 86)))
    .AnimateFill(dot, k => k.From(Colors.Blue).To(Colors.HotPink))
    .Build();
```

```xml
<kf:KeyframeView Scene="{Binding Scene}"
                 Progress="{Binding Source={x:Reference Scrubber}, Path=Value, Mode=TwoWay}" />
```

**Notes:**
- **Evaluation is a pure function of time.** `Evaluate(t)` never looks at the previous frame, which is what makes scrubbing, mid-flight reversal, deterministic export, and non-flaky timing tests possible at all rather than merely approximable.
- **Colour blends in Oklab by default** — sRGB interpolation dips through grey at the midpoint. `ColorInterpolator.Srgb` and `.LinearRgb` are there if you want them.
- **Angles take the shortest arc.** `Rotation` 350° → 10° turns forward 20°, not back 340°. Use `Spin` when you genuinely want multiple turns.
- **Targets are held weakly**, so an infinite animation on a popped page goes inert and gets collected instead of pinning the visual tree.
- **The XAML property registry is explicit, not reflective** — `Property="Opacity"` resolves through hand-registered delegates in `AnimatableProperties`, because reflection and compiled `Expression` both work in the emulator and break under Native AOT. Register your own with `AnimatableProperties.Register(...)`.
- **Layout-affecting properties are the perf cliff.** `WidthRequest`, `HeightRequest`, `Margin` and `Padding` run a full measure/arrange every frame; they work, and `AnimatableProperty.InvalidatesLayout` flags them, but prefer transform and opacity where you have the choice.
- Everything ticks in managed code on the UI thread today. Transform and opacity tracks map 1:1 onto `CAKeyframeAnimation` / `ObjectAnimator` / `ScalarKeyFrameAnimation`, so **native compositor offload** is the design's biggest remaining perf win — but it isn't written yet.
- **It runs on every head.** Because nothing here touches a platform SDK — the clock is an `IDispatcherTimer`, the view is a `GraphicsView` — the package ships a single `net10.0` target that iOS, Android, Windows, MacCatalyst, macOS AppKit and Linux GTK4 heads all consume.

#### Offscreen export (optional package)

`Shiny.Maui.Controls.Keyframe.Export` samples a scene at exact frame times and yields frames lazily, so a long export never holds more than one frame in memory. It is a separate package because it is the only thing in Keyframe that needs a rasterizer — SkiaSharp — and nothing that animates a view ever touches it.

```bash
dotnet add package Shiny.Maui.Controls.Keyframe.Export
```

```csharp
var exporter = new FrameExporter(scene);
var options = new ExportOptions { Fps = 25, Scale = 2.0 };

GifEncoder.EncodeToFile("out.gif", exporter.Frames(options), options.Fps);
```

**Notes:**
- Frame times come from `index / fps` in ticks — never accumulated, and never through `TimeSpan.FromSeconds`, which rounds to whole milliseconds and would quantise every frame at 60fps.
- GIF stores delays in hundredths of a second, so only divisors of 100 are exact: **25 or 50fps** when timing matters. 30fps writes 3cs and actually plays at 33.3fps, and most browsers promote 0–1cs delays to 10cs, so anything above 50fps plays far slower than asked.
- `IFrameRenderer` is an interface — swap `SkiaFrameRenderer` for your own rasterizer if you'd rather not take the Skia dependency.
- MP4/video export is out of scope (it needs a real codec); pipe `FrameExporter.Frames()` to ffmpeg's stdin. APNG/WebP aren't implemented, and there's no Lottie parser yet — though `KeyframeScene` is the scene graph one would need.

### Motion Icons

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
- The two hosts run the *same* spec through different machinery: on MAUI it compiles to a `KeyframeScene` driven by the [Keyframe](#keyframe-animation) engine's `Player`, so motion icons and hand-written timelines share one animation engine, one clock per window and one set of easing curves. On Blazor it compiles to `@keyframes` once and the browser composites it, so no C# runs per frame and the animation keeps going while WebAssembly is busy.
- Because nothing touches a platform SDK, the MAUI side works on every head — including AppKit and GTK4.
- Playback never starts before the view is loaded, so an implicit `<Style TargetType="MotionIconView">` that sets `IsPlaying` cannot reach for a dispatcher from inside a constructor.
- Unset `Color` follows the theme pack's on-surface token via `SetDynamicResource`, so icons restyle with a live `SetTheme`.
- Every generic preset works on artwork the library has never seen; only `Draw` needs to know the parts, so it can stagger them.
- `prefers-reduced-motion` is honoured on the web: the icon renders and still responds, it just holds its resting pose.
- **Write path data with explicit `L` commands.** `Microsoft.Maui.Graphics` does not implement SVG's implicit-lineto rule (`M6 6 18 18` becomes two *movetos*, not a line) and cannot read run-together decimals (`l.06.06`). Browsers handle both, so artwork copied from a design tool can look perfect on Blazor and draw nothing on MAUI. Unit tests guard the built-in set against both.

### CarouselGallery

A Netflix-style horizontal carousel with snap-to-center behavior, configurable scale transforms for focused/unfocused items, peek area insets, and position tracking. Uses native platform recycler views on MAUI (Android `RecyclerView`, iOS `UICollectionView`, Windows `ItemsRepeater`) and CSS `scroll-snap` on Blazor.

```xml
<shiny:CarouselGallery ItemsSource="{Binding Items}"
                       ItemWidth="280"
                       ItemHeight="160"
                       ItemSpacing="16"
                       PeekAreaInsets="40"
                       FocusedItemScale="1.0"
                       UnfocusedItemScale="0.85"
                       CurrentPosition="{Binding Position}"
                       ItemSelectedCommand="{Binding SelectCommand}"
                       HeightRequest="180">
    <shiny:CarouselGallery.ItemTemplate>
        <DataTemplate>
            <Border BackgroundColor="{Binding Color}" StrokeThickness="0">
                <Label Text="{Binding Title}" TextColor="White" HorizontalTextAlignment="Center" VerticalTextAlignment="Center" />
            </Border>
        </DataTemplate>
    </shiny:CarouselGallery.ItemTemplate>
</shiny:CarouselGallery>
```

| Property | Type | Default | Description |
|---|---|---|---|
| `FocusedItemScale` | `double` | `1.0` | Scale of the centered item |
| `UnfocusedItemScale` | `double` | `0.8` | Scale of off-center items |
| `ItemWidth` | `double` | required | Width of each carousel item |
| `ItemHeight` | `double` | required | Height of each carousel item |
| `CurrentPosition` | `int` | `0` | Current centered item index (TwoWay) |
| `PeekAreaInsets` | `Thickness` | `0` | Visible area of adjacent items |
| `IsInfinite` | `bool` | `false` | Enable infinite loop scrolling |
| `SnapCount` | `int` | `1` | Number of items to snap into view at once. Set to `0` for free-scroll (Netflix-style) with no snapping |
| `PositionChangedCommand` | `ICommand` | `null` | Fires when position changes |

**Features:**
- Snap-to-center with smooth deceleration (configurable via `SnapCount`)
- Free-scroll mode (`SnapCount="0"`) for Netflix-style browsing without snapping
- Scale transforms for focused/unfocused items
- Peek area insets to show adjacent items
- Two-way position binding
- Infinite loop mode (MAUI)
- Dot indicators (Blazor)

### Carousel (Blazor)

`Carousel<TItem>` is a Blazor-only, **Embla-style** carousel built on a transform-based drag engine — click-and-drag on desktop, flick with momentum on touch — rather than native scroll-snap. It adds alignment, slides-per-view, scroll-linked effects, looping, autoplay/auto-scroll, and rich chrome on top of the simpler `CarouselGallery`.

```razor
<Carousel TItem="Photo" Items="@photos"
          SlidesPerView="3" Align="CarouselAlign.Start" SlidesToScroll="3"
          Loop="true" DragFree="false"
          Effect="CarouselEffect.Scale" FocusedItemScale="1" UnfocusedItemScale="0.8"
          AutoPlay="true" AutoPlayInterval="3000"
          ShowArrows="true" ShowCounter="true" ShowProgress="true"
          @bind-CurrentPosition="snap">
    <ItemTemplate Context="p">
        <img src="@p.Url" alt="@p.Title" />
    </ItemTemplate>
</Carousel>
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Effect` | `CarouselEffect` | `None` | Scroll-linked effect: `None` / `Scale` / `Opacity` / `Parallax` / `Fade` |
| `Align` | `CarouselAlign` | `Center` | Where the active slide settles: `Start` / `Center` / `End` |
| `SlidesPerView` | `double?` | `null` | Slides visible at once (slides fill the viewport when set) |
| `SlidesToScroll` | `int` | `1` | Slides advanced per arrow/dot step |
| `VariableWidths` | `bool` | `false` | Slides size to their own content |
| `Orientation` | `CarouselOrientation` | `Horizontal` | `Horizontal` / `Vertical` (vertical uses `ViewportHeight`) |
| `Loop` | `bool` | `true` | Seamless wrap-around |
| `Rtl` | `bool` | `false` | Mirrored layout + drag (horizontal) |
| `Draggable` / `DragFree` | `bool` | `true` / `false` | Pointer drag; free-scroll momentum with no snap |
| `AutoPlay` / `AutoPlayInterval` | `bool` / `int` | `false` / `4000` | Discrete timed advance (ms) |
| `AutoScroll` / `AutoScrollSpeed` | `bool` / `double` | `false` / `40` | Continuous marquee scroll (px/s) |
| `PauseOnHover` | `bool` | `true` | Pause auto-motion on hover/focus |
| `ShowArrows` / `ShowIndicators` | `bool` | `true` | Prev/next buttons; dots (one per snap) |
| `ShowCounter` / `ShowProgress` / `ShowThumbnails` | `bool` | `false` | "n / total" readout; scroll-position bar; thumbnail strip |
| `LazyLoad` / `LazyLoadBuffer` | `bool` / `int` | `false` / `1` | Defer item templates outside the buffer |
| `CurrentPosition` | `int` | `0` | Selected snap index (TwoWay) |
| `ItemTemplate` / `ThumbnailTemplate` / `PlaceholderTemplate` / `EmptyTemplate` | `RenderFragment` | — | Item, thumbnail, lazy placeholder, and empty-state content |

**Methods:** `NextAsync()`, `PreviousAsync()`, `GoToAsync(snapIndex)`, `GoToSlideAsync(itemIndex)`. **Events:** `CurrentPositionChanged`, `ItemSelected`.

**Features:**
- Transform-based pointer engine: desktop click-drag + touch flick with friction/momentum
- Alignment, slides-per-view, slides-to-scroll, variable widths, vertical axis, and RTL
- Seamless looping via per-slide repetition (works with variable widths)
- Scroll-linked effects (scale / opacity / parallax / fade) that animate live while dragging
- Autoplay with scroll-position progress bar, or continuous auto-scroll marquee
- Counter, dot indicators (per snap), and a thumbnail navigation strip
- Full keyboard support (arrows / Home / End) and lazy item loading

### Layout & AppLayout (Blazor)

Layout primitives and an application shell, Blazor only — MAUI already has `VerticalStackLayout`, `HorizontalStackLayout` and `Grid` in the box.

**`VStack` / `HStack`** — flexbox stacks with `Spacing` (px), `Align`, `Justify`, `Wrap`, `Reverse`, `Grow`, `Padding`, `Background` and `Scrollable`. Everything is an inline style, so there is no stylesheet to load.

```razor
<VStack Spacing="12" Align="StackAlign.Start" Padding="16">
    <h3>Title</h3>
    <HStack Spacing="8" Justify="StackJustify.SpaceBetween" Align="StackAlign.Center">
        <span>Label</span>
        <ShinyButton Text="Save" />
    </HStack>
</VStack>
```

**`Grid` / `Row` / `Column`** — a responsive 12-column grid. Each `Column` takes a span per breakpoint (`Xs` &lt; 576px, `Sm` ≥ 576, `Md` ≥ 768, `Lg` ≥ 992, `Xl` ≥ 1200, `Xxl` ≥ 1400) that cascades upwards, so `Md="6"` alone means full width on phones and half from 768px up. `OffsetXs…OffsetXxl` and `OrderXs…OrderXxl` are per-breakpoint too; a `Column` with no span shares the row equally with its siblings, and `Fit` shrinks to its content. `Columns`, `Gutter`/`GutterX`/`GutterY` and `MaxWidth` are configurable on the `Grid` and overridable per `Row`.

```razor
<Grid Gutter="16" MaxWidth="1200">
    <Row>
        <Column Md="8"><Article /></Column>
        <Column Md="4" OrderXs="1" OrderMd="2"><Sidebar /></Column>
    </Row>
</Grid>
```

**`AppLayout`** — an application shell of a header, footer, left and right panels and the content between them. Regions are placed by CSS grid areas, so they can appear in any order in the markup, and each owns its own scroll region.

```razor
<AppLayout Height="100dvh" HeaderSpan="LayoutSpan.Full" BorderWidth="1">
    <AppLayoutHeader Height="56" Padding="0 16">…</AppLayoutHeader>

    <AppLayoutPanel Side="PanelSide.Left"
                    @bind-State="leftState"
                    @bind-Size="leftWidth"
                    MinSize="180" MaxSize="420"
                    ToolbarSize="56"
                    CollapseBelow="900"
                    CollapsedState="PanelState.Toolbar"
                    PersistKey="nav">
        <HeaderContent>…</HeaderContent>
        <ToolbarContent>…</ToolbarContent>
        <ChildContent>…</ChildContent>
        <FooterContent>…</FooterContent>
    </AppLayoutPanel>

    <AppLayoutContent Padding="20">@Body</AppLayoutContent>
    <AppLayoutFooter Height="36">…</AppLayoutFooter>
</AppLayout>
```

- **Three panel states** — `PanelState.Hidden`, `Toolbar` (a narrow rail rendering `ToolbarContent`, configurable to whatever you want) and `Shown`. Two-way bindable via `@bind-State`, or driven from code with `SetStateAsync` / `ToggleAsync` on a `@ref`
- **Resizing** — drag the handle on the panel's inner edge; the width is clamped to `MinSize`/`MaxSize` and reported back through `@bind-Size`. Set `Resizable="false"` to pin it
- **Scroll regions** — the panel body, the toolbar rail and the content each scroll on their own, while `HeaderContent`/`FooterContent`, the shell header and the shell footer stay pinned
- **Borders** — `BorderWidth`/`BorderColor` on the `AppLayout` set the default for every region; each region overrides them or turns its divider off with `Border="false"`. Unset values resolve through CSS variables, so theme tokens still apply. A `Hidden` panel drops its divider entirely, so a collapsed panel leaves no sliver butting up against the next region's border
- **Responsive** — under `CollapseBelow` (measured on the shell, not the window) an expanded panel drops to `CollapsedState`, and re-expanding it there floats it over the content as a drawer with a scrim
- **Persistence** — set `PersistKey` and the panel's state and width are saved to localStorage and restored on load
- **`HeaderSpan`/`FooterSpan`** — `LayoutSpan.Full` runs the header/footer the full width, `LayoutSpan.Content` insets it between the panels so they run the full height

### StaggeredGrid

A Pinterest-style masonry/waterfall layout that arranges variable-height items in columns. Uses native staggered layout managers on MAUI (Android `StaggeredGridLayoutManager`, iOS custom `WaterfallLayout`, Windows `WaterfallVirtualizingLayout`) and CSS `column-count` on Blazor.

```xml
<shiny:StaggeredGrid ItemsSource="{Binding Items}"
                     ColumnCount="3"
                     ColumnSpacing="12"
                     RowSpacing="12"
                     ItemSelectedCommand="{Binding SelectCommand}">
    <shiny:StaggeredGrid.ItemTemplate>
        <DataTemplate>
            <Border BackgroundColor="{Binding Color}" HeightRequest="{Binding Height}" StrokeThickness="0">
                <Label Text="{Binding Title}" TextColor="White" Padding="12" />
            </Border>
        </DataTemplate>
    </shiny:StaggeredGrid.ItemTemplate>
</shiny:StaggeredGrid>
```

| Property | Type | Default | Description |
|---|---|---|---|
| `ColumnCount` | `int` | `2` | Number of columns (minimum 1) |
| `ColumnSpacing` | `double` | `0` | Horizontal gap between columns |
| `RowSpacing` | `double` | `0` | Vertical gap between items |

Inherits all `CollectionControlBase` properties: `ItemsSource`, `ItemTemplate`, `ItemTemplateSelector`, `HeaderTemplate`, `FooterTemplate`, `EmptyViewTemplate`, `ItemSelectedCommand`, `LoadMoreCommand`, `LoadMoreThreshold`, `ItemSpacing`.

### ParallaxCollectionView (MAUI) / ParallaxList (Blazor)

A scrollable list with a hero header that translates at a configurable fraction of the scroll offset — the App-Store / profile-page parallax effect. Pure cross-platform implementation: MAUI wraps a real `CollectionView` and drives the hero from `CollectionView.Scrolled` (no platform handlers); Blazor uses a small JS scroll listener that mutates `transform`/`opacity` directly via `requestAnimationFrame`, so the parallax runs at native scroll framerate without re-rendering Razor components.

```xml
<shiny:ParallaxCollectionView ItemsSource="{Binding Items}"
                              HeaderHeight="260"
                              MinHeaderHeight="96"
                              ParallaxFactor="0.5"
                              CollapseToSticky="True"
                              FadeHeaderOnScroll="False"
                              SelectionMode="Single"
                              ItemSelectedCommand="{Binding SelectCommand}">
    <shiny:ParallaxCollectionView.HeaderTemplate>
        <DataTemplate>
            <Grid>
                <Grid.Background>
                    <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                        <GradientStop Color="#7C3AED" Offset="0.0" />
                        <GradientStop Color="#2563EB" Offset="0.5" />
                        <GradientStop Color="#0EA5E9" Offset="1.0" />
                    </LinearGradientBrush>
                </Grid.Background>
                <Label Text="Destinations" FontSize="28" FontAttributes="Bold"
                       TextColor="White" VerticalOptions="Center" HorizontalOptions="Center" />
            </Grid>
        </DataTemplate>
    </shiny:ParallaxCollectionView.HeaderTemplate>
    <shiny:ParallaxCollectionView.ItemTemplate>
        <DataTemplate>
            <Border Margin="16,6" Padding="16">
                <Label Text="{Binding Title}" FontAttributes="Bold" />
            </Border>
        </DataTemplate>
    </shiny:ParallaxCollectionView.ItemTemplate>
</shiny:ParallaxCollectionView>
```

```razor
<div style="height:600px;">
    <ParallaxList TItem="DestinationItem"
                  Items="@items"
                  HeaderHeight="260"
                  MinHeaderHeight="96"
                  ParallaxFactor="0.5"
                  CollapseToSticky="true"
                  Scrolled="@(e => visible = e.HeaderVisibleHeight)">
        <HeroTemplate>
            <div style="height:100%;background:linear-gradient(135deg,#7C3AED,#2563EB,#0EA5E9);
                        color:white;display:flex;align-items:center;justify-content:center;
                        font-size:28px;font-weight:700;">Destinations</div>
        </HeroTemplate>
        <ItemTemplate Context="item">
            <div style="margin:6px 16px;padding:16px;background:white;border-radius:14px;">
                <strong>@item.Title</strong>
            </div>
        </ItemTemplate>
    </ParallaxList>
</div>
```

| Property | MAUI Type | Blazor Type | Default | Description |
|---|---|---|---|---|
| `ItemsSource` / `Items` | `IEnumerable` | `IReadOnlyList<TItem>` | — | Collection of items |
| `ItemTemplate` | `DataTemplate` | `RenderFragment<TItem>` | — | Template per row |
| `HeaderTemplate` / `HeroTemplate` | `DataTemplate` | `RenderFragment` | — | Parallax hero template |
| `EmptyView` / `EmptyTemplate` | `object` / `DataTemplate` | `RenderFragment` | — | Empty state |
| `HeaderHeight` | `double` | `double` | 240 | Hero height (px) |
| `MinHeaderHeight` | `double` | `double` | 0 | Minimum visible hero height when collapsed |
| `ParallaxFactor` | `double` | `double` | 0.5 | Fraction of scroll offset applied to hero translation (0 = pinned, 1 = scrolls with content) |
| `CollapseToSticky` | `bool` | `bool` | false | Clamp hero to `MinHeaderHeight` once scrolled that far |
| `FadeHeaderOnScroll` | `bool` | `bool` | false | Fade hero from 100% → 0% opacity as it scrolls past |
| `ItemsLayout` (MAUI) | `IItemsLayout` | — | Vertical | Passthrough to inner `CollectionView` — use `GridItemsLayout` for multi-column lists |
| `SelectionMode` / `SelectedItem` / `ItemSelectedCommand` (MAUI) | — | — | — | Passthrough to inner `CollectionView` |
| `ItemSelected` (Blazor) | — | `EventCallback<TItem>` | — | Fired on row click |
| `Height` (Blazor) | — | `string` | — | CSS height for the scroll container; omit to fill parent |

Both hosts fire a `Scrolled` event with `ParallaxScrollEventArgs(verticalOffset, headerTranslation, headerVisibleHeight)` so you can drive sticky titles, fading nav chrome, etc.

When no `HeaderTemplate`/`HeroTemplate` is set, the header reserves **no** space (so you never get a blank band above the list). MAUI also exposes `ScrollTo(...)` and a `ScrollToTop(bool animate = true)` method that returns the list to the very top including the header.

### VirtualizedGrid

A full-featured grouped grid with sticky section headers, virtualization, orientation-aware column counts, load-more, and cell padding. Uses native grid layouts on MAUI (Android `GridLayoutManager` with `StickyHeaderDecoration`, iOS `UICollectionViewCompositionalLayout` with pinned headers, Windows `ItemsRepeater` with `UniformGridLayout`) and CSS Grid with Blazor `Virtualize<T>` on Blazor (items are chunked into rows of `ColumnCount` cells and the rows are virtualized, so virtualization works correctly at any column count).

```xml
<shiny:VirtualizedGrid ItemsSource="{Binding Items}"
                       ColumnCount="3"
                       ItemSpacing="8"
                       CellPadding="4"
                       IsGroupingEnabled="True"
                       HasStickyHeaders="True"
                       ItemSelectedCommand="{Binding SelectCommand}">
    <shiny:VirtualizedGrid.GroupHeaderTemplate>
        <DataTemplate>
            <Label Text="{Binding .}" FontAttributes="Bold" Padding="8,4" />
        </DataTemplate>
    </shiny:VirtualizedGrid.GroupHeaderTemplate>
    <shiny:VirtualizedGrid.ItemTemplate>
        <DataTemplate>
            <Border BackgroundColor="{Binding Color}" StrokeThickness="0" Padding="12">
                <Label Text="{Binding Name}" TextColor="White" HorizontalTextAlignment="Center" />
            </Border>
        </DataTemplate>
    </shiny:VirtualizedGrid.ItemTemplate>
</shiny:VirtualizedGrid>
```

| Property | Type | Default | Description |
|---|---|---|---|
| `ColumnCount` | `int` | `1` | Number of grid columns |
| `PortraitColumnCount` | `int?` | `null` | Column count in portrait (uses `ColumnCount` if null) |
| `LandscapeColumnCount` | `int?` | `null` | Column count in landscape (uses `ColumnCount` if null) |
| `IsGroupingEnabled` | `bool` | `false` | Enable grouped layout with section headers |
| `GroupHeaderTemplate` | `DataTemplate` | `null` | Template for group headers |
| `HasStickyHeaders` | `bool` | `true` | Pin group headers while scrolling |
| `CellPadding` | `Thickness` | `0` | Padding inside each cell |
| `ShowLoadMoreButton` | `bool` | `false` | Show a load-more button at the end of the data |
| `LoadMoreButtonTemplate` | `DataTemplate` | `null` | Custom load-more button template; defaults to a centered "Load More" button |
| `IsLoadingMore` | `bool` | `false` | Loading state (OneWayToSource) |
| `ItemVisibleCommand` | `ICommand` | `null` | Fires when an item becomes visible |
| `ItemHiddenCommand` | `ICommand` | `null` | Fires when an item scrolls out of view |

Inherits all `CollectionControlBase` properties: `ItemsSource`, `ItemTemplate`, `ItemTemplateSelector`, `HeaderTemplate`, `FooterTemplate`, `EmptyViewTemplate`, `ItemSelectedCommand`, `LoadMoreCommand`, `LoadMoreThreshold`, `ItemSpacing`.

**Features:**
- Grouped data with sticky section headers that pin while scrolling
- Orientation-aware column count (portrait vs landscape)
- Built-in load-more button with loading state
- Item visibility tracking for analytics or lazy loading
- Full header, footer, and empty view templates

### Desktop (Tray Icon + Docking) &amp; the On-Screen Keyboard

`Shiny.Maui.Controls.Desktop` is a single desktop-only add-on that combines a cross-platform **system tray / status-bar icon** (Windows, macOS AppKit, MacCatalyst, Linux ayatana-appindicator) and Visual-Studio-style **window docking** (dockable tool windows, tabbed groups, splitters, auto-hide rails, tear-off floating windows). A touch / kiosk **on-screen keyboard** is planned for it but not built. On the Blazor side there is no equivalent add-on — docking *and* the on-screen keyboard both ship in the main `Shiny.Blazor.Controls` package.

```bash
dotnet add package Shiny.Maui.Controls.Desktop
```

Register in `MauiProgram.cs` — call one or both extensions depending on what you need:

```csharp
using Shiny;

builder
    .UseMauiApp<App>()
    .UseShinyControls()
    .UseTrayIcon()         // tray / status-bar icon
    .UseShinyDocking()     // docking host
    .AddDockPanel<SolutionExplorerPanel>("solution-explorer", displayName: "Explorer", icon: "📁")
    .AddDockPanel<OutputPanel>("output");
```

> Namespaces: `using Shiny.Maui.Controls.Desktop.TrayIcon;` for the tray API and `using Shiny.Maui.Controls.Desktop.Docking;` for docking. The extension methods themselves live in the `Shiny` namespace. There is no `UseOnScreenKeyboard` — see below.

#### Tray Icon

Resolve `ITrayIconFactory` from DI to create as many tray icons as you need. Build menus declaratively, set the icon from any `Stream`, and dispose to remove the icon cleanly. The same PNG asset works on every platform — Windows wraps it as an ICO internally.

```csharp
public class MyTrayHost
{
    readonly ITrayIcon icon;

    public MyTrayHost(ITrayIconFactory factory)
    {
        this.icon = factory.Create();
        this.icon.Tooltip = "My App";
        this.icon.IsTemplateImage = true; // macOS dark/light auto-tint
        this.icon.SetIcon(() => FileSystem.OpenAppPackageFileAsync("trayicon.png").Result);

        this.icon.SetMenu(TrayMenu.Build(b => b
            .Item(new TrayMenuItem("Show window", ShowMainWindow) { Accelerator = "Ctrl+Shift+W", Icon = OpenIconStream })
            .Item(new TrayMenuItem("New item", NewItem) { Accelerator = "Ctrl+N" })
            .Check("Notifications", true, on => SetNotifications(on))
            .Separator()
            .Submenu("Status", s => s
                .Item("Available", () => SetStatus(Status.Available))
                .Item("Busy", () => SetStatus(Status.Busy))
                .Item("Away", () => SetStatus(Status.Away)))
            .Separator()
            .Item(new TrayMenuItem("Quit", () => Application.Current!.Quit()) { Accelerator = "Ctrl+Q" })));

        this.icon.PrimaryClick += (_, e) => ShowMainWindow();
        this.icon.DoubleClick  += (_, e) => OpenSettings();

        // Badge, balloon/toast, animated icon — see the API table below
        this.icon.Badge = "3";
        this.icon.ShowNotification("Connected", "Background sync is running.");
    }
}
```

| Member | Description |
|---|---|
| `SetIcon(Func<Stream>)` | Set the icon from a stream factory — the host re-reads it for DPI/theme changes. PNG or ICO bytes both work |
| `Tooltip` | Hover tooltip (Windows / macOS) or accessible description (Linux) |
| `Title` | Optional text label shown beside or instead of the icon on macOS and Linux (ignored on Windows) |
| `Badge` | String composited onto the icon as a red pill on Windows; rendered beside the icon on macOS / Linux. Set to `null` to clear |
| `IsVisible` | Show/hide without disposing |
| `IsTemplateImage` | When `true`, macOS treats the icon as a template image and auto-tints for the light/dark menu bar |
| `SetMenu(TrayMenu)` | Assign the context menu — mutate items at any time and the menu rebuilds |
| `ShowMenu()` | Programmatically open the menu (useful from a left-click handler on Windows) |
| `ShowNotification(title, message)` | Best-effort balloon / toast via the native subsystem (Windows `NIF_INFO`, macOS / Catalyst `NSUserNotificationCenter`, Linux libnotify). For richer in-app toasts inside your MAUI UI use `Shiny.Maui.Controls.Toast` |
| `StartAnimation(frames, interval)` / `StopAnimation()` / `IsAnimating` | Cycle a list of `Func<Stream>` frames on a shared timer; reverts to the last static icon on stop |
| `PrimaryClick` / `SecondaryClick` / `DoubleClick` | Click events with screen coordinates (`TrayClickEventArgs`) |
| `Dispose()` | Removes the tray icon and frees native resources |

`TrayMenu.Build(b => …)` supports `Item`, `Check`, `Separator`, and `Submenu`. `TrayMenuItem` exposes `IsEnabled`, `IsVisible`, `Label`, optional `Icon` (`Func<Stream>` — rendered next to the label), and `Accelerator` (e.g. `"Ctrl+S"`, `"Cmd+Q"`, `"F1"`). The accelerator string is both the visual hint *and* the dispatch trigger — see the table below for per-platform behaviour. Use the shared `TrayAccelerator.Parse(string)` helper if you need the parsed `Modifiers` + `Key` yourself.

**Platform notes:**
- **Linux:** depends on `libayatana-appindicator3` and `libgtk-3` — install via your distro's package manager (`apt install libayatana-appindicator3-1 libgtk-3-0` on Debian/Ubuntu). `ShowNotification` additionally needs `libnotify` (usually pre-installed); if missing it silently no-ops
- **MacCatalyst:** bridges to AppKit via the Objective-C runtime — your app needs permission to `dlopen` AppKit at runtime (granted by default in normal Catalyst apps)
- **Windows:** uses `Shell_NotifyIcon` directly. Windows 11 hides new tray icons by default — users have to promote yours from the overflow flyout. Badge composition uses `System.Drawing.Common` (pulled in only for the Windows TFM)
- **macOS template images:** set `IsTemplateImage = true` and supply a flat black-on-transparent PNG for the menu bar to auto-tint with the user's appearance

**Accelerator dispatch matrix:**

| Platform | Mechanism | Scope |
|---|---|---|
| Windows | `RegisterHotKey` on the tray host window | Global system hotkey while your process is running |
| macOS (AppKit) | `NSMenuItem.KeyEquivalent` + modifier mask | App-wide while your app is foreground |
| MacCatalyst | Same as AppKit via `objc_msgSend` | App-wide while your app is foreground |
| Linux | `gtk_widget_add_accelerator` on a `GtkAccelGroup` | Best-effort — fires while the indicator menu is open or focused |

#### Docking

Visual-Studio-style docking host for MAUI desktop apps — schema, contracts, the in-window `DockHostView`, drag-drop, splitters, auto-hide rails, and tear-off floating windows.

```csharp
using Shiny;
using Sample.Features.Docking;  // SolutionExplorerPanel, OutputPanel

builder
    .UseMauiApp<App>()
    .UseShinyDocking()
    .AddDockPanel<SolutionExplorerPanel>("solution-explorer", displayName: "Explorer", icon: "📁")
    .AddDockPanel<OutputPanel>("output");
```

`AddDockPanel` takes optional `displayName` (tab title, defaults to the panel ID) and `icon` (emoji / unicode glyph) arguments. A panel view can also implement `IDockableContent` to control its own per-instance `Title`, `Icon`, `CanClose` / `CanFloat`, and receive `OnActivated` / `OnDeactivated` callbacks.

`DockHostView` attaches to any existing `ContentPage` — it does not subclass `ContentPage`, so your Shell / page architecture stays unchanged:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:docking="clr-namespace:Shiny.Maui.Controls.Desktop.Docking;assembly=Shiny.Maui.Controls.Desktop">
    <docking:DockHostView InitialLayout="{Binding StartupLayout}"
                          LayoutStore="{Binding LayoutStore}"
                          IsLocked="{Binding IsLayoutLocked}" />
</ContentPage>
```

| Building block | Purpose |
|---|---|
| `DockHostView` | Root dock surface (attaches inside any page); bindable `InitialLayout`, `LayoutStore`, `IsLocked` |
| `DockGroupView` | Tabbed group of panels |
| `DockTabStrip` | Tab strip with overflow + drag-to-reorder |
| `DockSplitter` | Draggable splitter between adjacent dock children |
| `IDockHost` | Per-window controller: `LoadAsync`, `Snapshot`, `ShowPanelAsync` / `HidePanelAsync` / `ActivatePanelAsync`, `ResetLayoutAsync`, `SetRailCollapsedAsync`, `IsLocked` |
| `IDockableContent` | Optional interface on panel views — per-instance title/icon, close/float gating, activation callbacks, pointer-down claim for embedded editors |
| `IDockableContentFactory` | `Task<View> CreateAsync(string instanceId, ...)` + `DisplayName` / `Icon` — registered via `AddDockPanel<T>` |
| `IDockLayoutStore` | Bring-your-own persistence contract — load/save the layout tree as JSON; saves are debounced via `SaveDebounceMs` |
| `IDockEvents` | `LayoutChanged`, `PanelActivated`, `DragStarted/Completed/Cancelled` |
| `IDockCommandScope` | Scopes Ctrl+W / Ctrl+Tab / Ctrl+Alt+PgUp/Dn to the dock surface |

Everything is interactive end-to-end: drag a tab onto another group's center to merge, onto an edge to split, or outside the host to tear off a floating window (move, resize, re-dock, close); drag splitters to resize; collapse individual panels (or whole rails via `SetRailCollapsedAsync`) to slim edge bars that restore on click. The full state — splits, ratios, collapsed panels, floating-window bounds — round-trips through `Snapshot()` / `LoadAsync()` and auto-saves through the attached `IDockLayoutStore`. `IsLocked = true` freezes the layout (tab switching still works) for kiosk / demo scenarios.

The layout schema (`DockRoot`, `DockWindowState`, `DockSplit`, `DockGroup`, `DockTab`) is a pure POCO tree with a source-generated `System.Text.Json` context — round-trip your dock layout to disk with `DockSerialization.Serialize` / `Deserialize`. Schema versioning (`SchemaVersion` + `MinReadableVersion`) and an `IDockLayoutMigrator` hook are wired in from day one so saved layouts survive future schema changes.

##### Blazor

Same shape, same contracts — different host. No extra package: docking is part of `Shiny.Blazor.Controls`.

```csharp
using Shiny.Blazor.Controls.Docking;

builder.Services
    .AddShinyDocking()
    .AddDockPanel<SolutionExplorerPanel>("solution-explorer", displayName: "Explorer", icon: "📁")
    .AddDockPanel<OutputPanel>("output");
```

```razor
@using Shiny.Blazor.Controls.Docking

<DockHost @ref="host"
          InitialLayout="@layout"
          LayoutStore="@layoutStore"
          IsLocked="@locked" />
```

The component itself implements `IDockHost` — grab it with `@ref` to call `ShowPanelAsync` / `ResetLayoutAsync` / `Snapshot` and subscribe to `Events`. CSS custom properties (e.g. `--shiny-dock-host-bg`) provide theming hooks without recompiling.

#### On-Screen Keyboard

> [!IMPORTANT]
> **Blazor only.** The keyboard ships in `Shiny.Blazor.Controls`. The MAUI half —
> `UseOnScreenKeyboard` / `IOnScreenKeyboard` / `OnScreenKeyboardView` in
> `Shiny.Maui.Controls.Desktop` — is still a design and will not compile.

Touch / kiosk soft keyboard. US-QWERTY with a symbols layer, bottom-docked, auto-shows when an `<input>` / `<textarea>` gains focus, and — critically — does **not** take the caret off it when keys are tapped.

```csharp
using Shiny.Blazor.Controls.OnScreenKeyboard;

builder.Services.AddShinyOnScreenKeyboard(opts =>
{
    opts.AutoShowOnFocus = true;
    opts.AutoHideOnBlur  = true;
    opts.HeightPx        = 280;
    opts.PushContent     = true;     // pad the body out from under the keys (false = overlay)
    opts.Theme           = OnScreenKeyboardTheme.Auto;   // follows the app's theme tokens
});
```

```razor
@using Shiny.Blazor.Controls.OnScreenKeyboard

@* Place once in MainLayout.razor — the host watches focus for the whole document *@
<OnScreenKeyboardHost />
```

Drive visibility from code via DI:

```csharp
@inject IOnScreenKeyboardService Keyboard

<button @onclick="() => Keyboard.Show()">Kiosk mode</button>
```

`IOnScreenKeyboardService` is `Show` / `Hide` / `Toggle` / `IsVisible` / `VisibilityChanged`. Both it and `OnScreenKeyboardOptions` are registered **scoped** — the options object is live, so change it at runtime and the host picks it up on the next render, and being per-scope means one user's settings are not everyone's under Blazor Server. `AddShinyControls()` covers this too; `ConfigureKeyboard` is the umbrella's equivalent of the `opts` delegate above.

`⇧` is momentary, `⇪` is sticky and only raises the letters (the number row keeps its digits), and holding a character, `⌫`, space or an arrow auto-repeats. Arrows are caret-aware: `▲` / `▼` walk to the same column on the adjacent line of a `<textarea>`. Enter dispatches real key events and submits the form on a single-line input; set `EnterInsertsNewLine` to type a newline in a `<textarea>` instead. Theming is entirely `--shiny-osk-*` custom properties.

Limitations: DOM inputs only — no injection into another window, process or cross-origin frame. No Shadow DOM (`focusin` does not pierce shadow roots). No IME / dead-key composition, English US-QWERTY only. No Ctrl / Alt chords — and no inert keys on the board pretending otherwise. Keys are `tabindex="-1"` by design, since taking focus is the one thing the control exists to avoid; the ARIA tree is there so the board is describable, not tab-navigable.
