# Shiny Controls

A rich, ready-to-use UI controls library for both **.NET MAUI** and **Blazor**. One package per host covers TableView, TreeView, Scheduler, FloatingPanel/OverlayHost, ShinyDurationPicker, FrostedGlassView, Toast, Fab/FabMenu, ShinyToolbar/ShinyTabBar (Blazor), PillView, BadgeView, SecurityPin, SignaturePad, ImageViewer, ImageEditor, ChatView, ColorPicker, FontPicker, Slider, ProgressBar, Overlay/LoadingOverlay, SkeletonView, AutoCompleteEntry, CountryPicker, AddressEntry, TextEntry, CarouselGallery, ParallaxCollectionView, StaggeredGrid, and VirtualizedGrid. Markdown, Mermaid Diagrams, and Barcodes (1D + 2D, QR codes) ship as separate add-on packages per host. **Desktop-only** features — system tray / status-bar icon, Visual-Studio-style docking, and a touch / kiosk on-screen keyboard — ship in a separate `Shiny.Maui.Controls.Desktop` add-on (Windows, macOS AppKit, MacCatalyst, and Linux), with a companion `Shiny.Blazor.Controls.Kiosk` for the web (docking + OSK).

[![MAUI NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.svg?label=Shiny.Maui.Controls)](https://www.nuget.org/packages/Shiny.Maui.Controls)
[![Blazor NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.svg?label=Shiny.Blazor.Controls)](https://www.nuget.org/packages/Shiny.Blazor.Controls)
[![MAUI Markdown NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Markdown.svg?label=Shiny.Maui.Controls.Markdown)](https://www.nuget.org/packages/Shiny.Maui.Controls.Markdown)
[![Blazor Markdown NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Markdown.svg?label=Shiny.Blazor.Controls.Markdown)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Markdown)
[![MAUI Mermaid NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.MermaidDiagrams.svg?label=Shiny.Maui.Controls.MermaidDiagrams)](https://www.nuget.org/packages/Shiny.Maui.Controls.MermaidDiagrams)
[![Blazor Mermaid NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.MermaidDiagrams.svg?label=Shiny.Blazor.Controls.MermaidDiagrams)](https://www.nuget.org/packages/Shiny.Blazor.Controls.MermaidDiagrams)
[![MAUI Barcodes NuGet](https://img.shields.io/nuget/v/Shiny.Maui.Controls.Barcodes.svg?label=Shiny.Maui.Controls.Barcodes)](https://www.nuget.org/packages/Shiny.Maui.Controls.Barcodes)
[![Blazor Barcodes NuGet](https://img.shields.io/nuget/v/Shiny.Blazor.Controls.Barcodes.svg?label=Shiny.Blazor.Controls.Barcodes)](https://www.nuget.org/packages/Shiny.Blazor.Controls.Barcodes)

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

No DI registration is required — drop the components into any `.razor` page.

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
| `<shiny:TextEntry>` | `<TextEntry>` |
| `<shiny:Overlay>` in `<shiny:ShinyContentPage.Panels>` | `<Overlay>` (wraps ChildContent; custom content in `<OverlayContent>` slot) |
| `<shiny:LoadingOverlay>` in `<shiny:ShinyContentPage.Panels>` | `<LoadingOverlay>` (wraps ChildContent) |
| `<shiny:ProgressBar>` | `<ProgressBar>` |

`ISchedulerEventProvider` is identical across both hosts.

## Controls

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
}
```

### FloatingPanel + OverlayHost

A floating panel overlay system for MAUI. Panels slide in from the bottom or top of the screen with configurable snap positions (detents), optional header peek when closed, backdrop dimming, and feedback. Multiple panels can coexist on the same page without blocking touches on content underneath.

**OverlayHost** is a transparent Grid layer that manages backdrop and touch passthrough for overlay clients (`FloatingPanel`, `Overlay`, `LoadingOverlay`). **ShinyContentPage** is a convenience ContentPage with a built-in OverlayHost.

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

### ShinyDurationPicker

A standalone duration picker control that opens a FloatingPanel for selection with hour/minute pickers and "hr"/"min" labels. Requires `ShinyContentPage` (or an `OverlayHost` in the visual tree).

```xml
<shiny:ShinyDurationPicker Duration="{Binding SelectedDuration, Mode=TwoWay}"
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

| Property | Type | Description |
|---|---|---|
| Source | ImageSource? | The image to display |
| IsOpen | bool | Show/hide the viewer (TwoWay) |
| Aspect | Aspect | Image aspect ratio mode (default: AspectFit) |
| MaxZoom | double | Maximum zoom scale (default: 5.0) |
| CloseButtonTemplate | DataTemplate? | Custom close button (tapping closes viewer) |
| HeaderTemplate | DataTemplate? | Custom header overlay |
| FooterTemplate | DataTemplate? | Custom footer overlay |
| UseFeedback | bool | Enable/disable feedback on double-tap zoom (default: true) |

**Features:**
- Pinch-to-zoom with origin tracking
- Pan when zoomed (clamped to image bounds)
- Double-tap to zoom in (2.5x) / reset
- Animated fade open/close with backdrop
- Close button overlay

### ImageEditor

An inline image editor with cropping, rotation, freehand drawing, line and arrow drawing, text annotations with font family and font size selection, and zoom. Includes a built-in undo/redo stack, reset-to-original, and export to PNG/JPEG/WEBP at configurable resolutions. Every feature can be toggled on/off, and the default toolbar can be replaced with a custom template.

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
| AllowZoom | bool | true | Enable/disable pinch-to-zoom |
| CanUndo | bool | false | Whether undo is available (OneWayToSource) |
| CanRedo | bool | false | Whether redo is available (OneWayToSource) |
| DrawStrokeColor | Color | White | Drawing stroke color — TwoWay |
| DrawStrokeWidth | double | 3 | Drawing stroke width |
| TextFontSize | double | 16 | Text annotation font size |
| TextFontFamily | string? | null | Font family for text annotations (TwoWay) |
| AnnotationTextColor | Color | White | Text annotation color |
| AvailableFonts | IList\<string\>? | null | Font families shown in font picker |
| AvailableFontSizes | IList\<double\>? | null | Font sizes shown in font size picker |
| SaveCommand | ICommand? | null | Invoked with `EditedImage` parameter on save |
| SaveText | string | "Save" | Save button label |
| CropApplyText | string | "Apply Crop" | Crop apply button label |
| CropCancelText | string | "Cancel" | Crop cancel button label |
| ToolbarTemplate | DataTemplate? | null | Custom toolbar (replaces default) |
| ToolbarPosition | ToolbarPosition | Bottom | Toolbar placement (Top or Bottom) |
| UseFeedback | bool | true | Feedback on actions |

**Features:**
- Move mode with pinch-to-zoom and pan (origin-aware, double-tap to toggle)
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

**Commands:** `UndoCommand`, `RedoCommand`, `RotateCommand`, `ResetCommand`, `CropCommand`, `DrawCommand`, `TextCommand`, `LineCommand`, `SaveCommand`

**Methods:** `Undo()`, `Redo()`, `Rotate(float)`, `Reset()`, `ApplyCrop()`, `GetEditedImage()`

### ChatView

A modern chat UI control with message bubbles, typing indicators, load-more pagination, acknowledgement reactions, bubble tools, custom message templates, and a configurable input bar. Supports single-person and multi-person conversations with per-participant colors and avatars.

![ChatView](assets/chat1.png)

```xml
<shiny:ChatView Messages="{Binding Messages}"
                Participants="{Binding Participants}"
                IsMultiPerson="True"
                TypingParticipants="{Binding TypingParticipants}"
                SendCommand="{Binding SendCommand}"
                AttachImageCommand="{Binding AttachImageCommand}"
                LoadMoreCommand="{Binding LoadMoreCommand}"
                MyBubbleColor="#DCF8C6"
                OtherBubbleColor="White"
                PlaceholderText="Type a message..." />
```

| Property | Type | Default | Description |
|---|---|---|---|
| Messages | IList\<ChatMessage\> | null | Bindable message collection (supports INotifyCollectionChanged) |
| Participants | IList\<ChatParticipant\> | null | Participant info for avatar/color lookup |
| IsMultiPerson | bool | false | Show avatars and names for other participants |
| ShowAvatarsInSingleChat | bool | false | Force avatars even in single-person mode |
| MyBubbleColor | Color | #DCF8C6 | Local user bubble color |
| MyTextColor | Color | Black | Local user text color |
| OtherBubbleColor | Color | White | Default other-user bubble color |
| OtherTextColor | Color | Black | Other-user text color |
| ChatBackgroundColor | Color? | null | Background color for the messages area |
| BubbleFontSize | double | 15 | Font size for bubble text |
| BubbleFontFamily | string? | null | Font family for bubble text |
| TimestampFontSize | double | 11 | Font size for timestamps |
| BubbleCornerRadius | double | 18 | Corner radius for bubbles (tail stays at 4) |
| PlaceholderText | string | "Type a message..." | Input placeholder |
| SendButtonText | string | "Send" | Send button label |
| SendButtonBackgroundColor | Color | #007AFF | Send button background color |
| SendButtonTextColor | Color | White | Send button text color |
| InputBarBackgroundColor | Color | #F5F5F5 | Input bar background color |
| InputBarBorderColor | Color | #E0E0E0 | Input bar top border color |
| IsInputBarVisible | bool | true | Show/hide the input bar |
| ShowTypingIndicator | bool | true | Enable typing notifications |
| TypingParticipants | IList\<ChatParticipant\> | null | Currently typing participants |
| ScrollToFirstUnread | bool | false | Scroll to first unread instead of end |
| FirstUnreadMessageId | string? | null | ID of the first unread message |
| ToolItems | IList\<ChatEntryTool\> | null | Input bar tools FAB menu (MAUI only) |
| BubbleToolItems | IList\<ChatBubbleTool\> | null | Bubble tools for received (other user) messages (MAUI only) |
| MyBubbleToolItems | IList\<ChatBubbleTool\> | null | Bubble tools for the local user's own messages (MAUI only) |
| MessageTemplate | DataTemplate? | null | Single template for all message content (MAUI only) |
| MessageTemplateSelector | DataTemplateSelector? | null | Per-type template selector (MAUI only) |
| UseFeedback | bool | true | Haptic feedback on interactions (MAUI only) |

**Commands:** `SendCommand` (text string), `AttachImageCommand`, `LoadMoreCommand`, `MessageTappedCommand` (ChatMessage)

**Methods (MAUI):** `ScrollToEnd(bool animate)`, `ScrollToMessage(string messageId, bool animate)`, `SubmitEntry()`, `EntryText` (get/set)

**Tool Base Classes (MAUI only):**

| Class | Purpose |
|---|---|
| `ChatEntryTool` | Base for input bar tools needing ChatView access (`ChatView` property auto-populated). Non-abstract — use directly with `Command` binding or subclass for self-contained tools. |
| `ChatBubbleTool` | Base for bubble tools acting on a message (`Message` property auto-populated). Non-abstract — use directly with `Command` binding or subclass. |
| `CopyBubbleTool` | Built-in: copies message text to clipboard |
| `TextToSpeechBubbleTool` | Built-in: reads message aloud (requires `Shiny.Maui.Controls.SpeechAddins`) |
| `SpeechToTextTool` | Built-in: voice input for chat entry (requires `Shiny.Maui.Controls.SpeechAddins`) |
| `PhotoGalleryEntryTool` | Built-in: opens device photo gallery via MediaPicker, fires `AttachImageCommand` with file path |
| `TakePhotoEntryTool` | Built-in: opens device camera via MediaPicker, fires `AttachImageCommand` with file path |
| `AcknowledgementBubbleTool` | Built-in: single-tap toggle for a specific reaction emoji (e.g. 👍, 👎). Set `Glyph` property. |
| `AcknowledgementSelectorBubbleTool` | Built-in: opens action sheet with 12 common emoji reactions to choose from |

```csharp
// Use ChatEntryTool directly with a Command binding
// <shiny:ChatEntryTool Text="Camera" Command="{Binding TakePhotoCommand}" />

// Or subclass for self-contained tools
public class QuickReplyTool : ChatEntryTool
{
    public QuickReplyTool() { Text = "Quick Reply"; Clicked += (s, e) => { ChatView?.EntryText = "Thanks!"; ChatView?.SubmitEntry(); }; }
}

// Use ChatBubbleTool directly — Command receives AcknowledgementChangedContext or ChatMessage via CommandParameter
// <shiny:ChatBubbleTool Text="Translate" Command="{Binding TranslateCommand}" />

// Or subclass for self-contained tools
public class TranslateTool : ChatBubbleTool
{
    public TranslateTool() { Text = "Translate"; Clicked += async (s, e) => { if (Message != null) { /* translate Message.Text */ } }; }
}

// Built-in acknowledgement tools
// <shiny:AcknowledgementBubbleTool Glyph="👍" Command="{Binding AckCommand}" />
// <shiny:AcknowledgementSelectorBubbleTool Command="{Binding AckCommand}" />
```

**Features:**
- Chat bubbles with left/right alignment and customizable colors per participant
- Visual grouping by sender and minute; timestamps on last message in each group
- Multi-person: avatar (initials or image) and name on first message in each group
- Typing indicators with animated dots, scroll-aware toast pill
- Acknowledgement reactions (emoji badges grouped by glyph with count)
- Bubble tools: per-message ⋮ menu with built-in and custom actions
- Input bar tools: FAB menu for camera, voice, custom actions
- Auto-link detection in text messages
- Image messages (text and image are mutually exclusive)
- DateSent pending state (null = pending/offline, renders at 50% opacity until server confirmation)
- Smart scrolling with unread message pill
- Load-more pagination (auto-trigger on MAUI, button on Blazor)
- Custom message templates for action buttons, cards, or rich content
- Entire input bar can be hidden for read-only use

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

A Material Design-inspired text entry control with animated floating placeholder, customizable border, left/right tool slots, hint text for validation errors, character count display, and input masking for formatted data entry.

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
| Placeholder | string | "" | Animated floating placeholder |
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

### Fab & FabMenu

A Material Design-style floating action button, plus an expanding multi-action menu that animates up from the main FAB.

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
| Text | string? | null | Optional label; when null the Fab is a perfect circle |
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
| UseFeedback | bool | true | Feedback on toggle |

Events: `ItemTapped` — fires the `FabMenuItem` that was tapped.

Methods: `Open()`, `Close()`, `Toggle()`.

**FabMenuItem** properties:

| Property | Type | Default | Description |
|---|---|---|---|
| Icon | ImageSource? | null | Circular icon |
| Text | string? | null | Side label next to the icon |
| Command | ICommand? | null | Invoked when tapped |
| CommandParameter | object? | null | Parameter for the Command |
| FabBackgroundColor | Color | #2196F3 | Icon button fill |
| BorderColor | Color? | null | Icon button outline |
| BorderThickness | double | 0 | Icon button outline thickness |
| TextColor | Color | Black | Side-label text color |
| LabelBackgroundColor | Color | White | Side-label background |
| FontSize | double | 13 | Side-label font size |
| Size | double | 44 | Icon button diameter |
| IconSize | double | 20 | Icon image size |
| UseFeedback | bool | true | Feedback on tap |

**Placement tip**: `FabMenu` should live in a `Grid` that fills the page (the same placement pattern as `ImageViewer`) so the backdrop can cover the page content. Alternatively, use `ShinyContentPage` with `OverlayHost` for easier overlay management.

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
| EntryCell | Text input field |
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

**Dynamic Sections** - Bind to a collection to generate sections from data:

```xml
<shiny:TableView ItemsSource="{Binding Items}" ItemTemplate="{StaticResource SectionTemplate}" />
```

### TreeView

Hierarchical tree control with lazy-loaded branches, configurable expand/collapse icons, single or multi-selection, per-item `CanExpand`/`CanSelect` predicates, retry on load failure, optional guide lines, and drag/drop reorder. Available on both MAUI and Blazor.

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
| SelectionMode | TreeSelectionMode | `None` / `Single` / `Multiple` |
| SelectedItem | object? | Two-way (Single mode) |
| SelectedItems | IList\<object\>? | Two-way (Multiple mode) |
| ExpandedIcon / CollapsedIcon / RetryIcon | ImageSource? | Fall back to ▼ / ▶ / ↻ glyphs |
| IndentSize | double | Pixels of indent per depth level (default 20) |
| ShowGuideLines | bool | Vertical connector lines between parent and children |
| EnableDragDrop | bool | Drag/drop with above/below/into drop positions and visual drop indicators; event-only, never mutates data |

**Events + Commands (MAUI):** `ItemSelected` / `ItemExpanded` / `ItemCollapsed` / `LoadFailed` / `ItemDropped` each have a matching `*Command` bindable property.

`ItemDropped` reports `Source`, `Target`, and `Position` (`Above` / `Below` reorder among siblings, `Into` drops into a folder) — your handler moves the data, then rebinds `ItemsSource` (MAUI) or calls `ReloadAsync()` (Blazor, which preserves expansion/selection state). Blazor drag/drop runs on native HTML5 drag events via a small JS module (required for Safari/Firefox `dataTransfer` support); MAUI uses platform drag gestures with a pan-gesture fallback on Mac Catalyst, AppKit, and GTK4 where those are broken or missing.

**Public methods:** `ExpandAll`, `ExpandAllAsync`, `CollapseAll`, `Expand(item)`, `Collapse(item)`, `Refresh(item)`, `ReloadAsync`, `FindNode(item)` — Blazor mirrors these as `ExpandAsync` / `CollapseAsync` / `ExpandAllAsync` / `CollapseAll` / `RefreshAsync` / `ReloadAsync` / `FindNode`.

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

### Desktop (Tray Icon + Docking + On-Screen Keyboard)

`Shiny.Maui.Controls.Desktop` is a single desktop-only add-on that combines three features: a cross-platform **system tray / status-bar icon** (Windows, macOS AppKit, MacCatalyst, Linux ayatana-appindicator), Visual-Studio-style **window docking** (dockable tool windows, tabbed groups, splitters, auto-hide rails, tear-off floating windows), and a touch / kiosk **on-screen keyboard** (US-QWERTY with shift / numbers / symbols layers, bottom-docked, auto-shows on input focus). Blazor gets the docking + on-screen keyboard via `Shiny.Blazor.Controls.Kiosk`.

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
    .AddDockPanel<OutputPanel>("output")
    .UseOnScreenKeyboard(opts =>  // touch / kiosk soft keyboard
    {
        opts.AutoShowOnFocus = true;
        opts.PushContent     = true;
    });
```

> Namespaces: `using Shiny.Maui.Controls.Desktop.TrayIcon;` for the tray API, `using Shiny.Maui.Controls.Desktop.Docking;` for docking, and `using Shiny.Maui.Controls.Desktop.OnScreenKeyboard;` for the on-screen keyboard. The extension methods themselves live in the `Shiny` namespace.

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

Same shape, same contracts — different host:

```bash
dotnet add package Shiny.Blazor.Controls.Kiosk
```

```csharp
using Shiny.Blazor.Controls.Kiosk.Docking;

builder.Services
    .AddShinyDocking()
    .AddDockPanel<SolutionExplorerPanel>("solution-explorer", displayName: "Explorer", icon: "📁")
    .AddDockPanel<OutputPanel>("output");
```

```razor
@using Shiny.Blazor.Controls.Kiosk.Docking

<DockHost @ref="host"
          InitialLayout="@layout"
          LayoutStore="@layoutStore"
          IsLocked="@locked" />
```

The component itself implements `IDockHost` — grab it with `@ref` to call `ShowPanelAsync` / `ResetLayoutAsync` / `Snapshot` and subscribe to `Events`. CSS custom properties (e.g. `--shiny-dock-host-bg`) provide theming hooks without recompiling.

#### On-Screen Keyboard

Touch / kiosk soft keyboard. US-QWERTY with shift / numbers / symbols layers, bottom-docked, auto-shows when an `Entry` / `Editor` (MAUI) or `<input>` / `<textarea>` (Blazor) gains focus, and — critically — does **not** steal focus when keys are tapped.

```csharp
// MAUI registration
using Shiny;
using Shiny.Maui.Controls.Desktop.OnScreenKeyboard;

builder
    .UseMauiApp<App>()
    .UseOnScreenKeyboard(opts =>
    {
        opts.AutoShowOnFocus = true;
        opts.AutoHideOnBlur  = true;
        opts.Height          = 280;
        opts.PushContent     = true;     // shrinks the page above by Height (false = overlay)
        opts.Theme           = OnScreenKeyboardTheme.Light;
    });
```

Drive visibility from code via DI:

```csharp
public class MyPageViewModel(IOnScreenKeyboard keyboard)
{
    public void StartKioskMode() => keyboard.Show();
}
```

##### Blazor

```bash
dotnet add package Shiny.Blazor.Controls.Kiosk
```

```csharp
using Shiny.Blazor.Controls.Kiosk.OnScreenKeyboard;

builder.Services.AddShinyOnScreenKeyboard(opts =>
{
    opts.AutoShowOnFocus = true;
    opts.HeightPx        = 280;
    opts.PushContent     = true;
});
```

```razor
@using Shiny.Blazor.Controls.Kiosk.OnScreenKeyboard

@* Place once in MainLayout.razor *@
<OnScreenKeyboardHost />
```

Limitations: MAUI / DOM inputs only — no system-wide injection. No Shadow DOM. No IME / dead-key composition. English US-QWERTY only. Full AutomationPeer (MAUI) / ARIA (Blazor) tree for switch-input accessibility from day one.
