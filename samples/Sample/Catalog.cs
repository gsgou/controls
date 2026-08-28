namespace Sample;

/// <summary>One demo page: the Shell route, what it is called, its glyph and a one-line description.</summary>
public record CatalogItem(string Route, string Label, string Icon, string Blurb);

/// <summary>A group of demo pages, rendered as one block on the home page.</summary>
/// <remarks>
/// <paramref name="Accent"/> stays a hex string to match the Blazor catalogue, and is parsed once into
/// <see cref="AccentColor"/> — a binding won't run a type converter, so XAML needs the real thing.
/// </remarks>
public record CatalogSection(string Title, string Accent, CatalogItem[] Items)
{
    public Color AccentColor { get; } = Color.FromArgb(Accent);
}

/// <summary>
/// What the gallery contains, mirroring <c>Sample.Blazor.Catalog</c> so both samples present the same
/// catalogue in the same shape. The flyout in <c>AppShell.xaml</c> stays the navigation source of truth —
/// this drives the home page's browse grid, so the routes here must match the flyout's.
/// </summary>
public static class Catalog
{
    public static readonly CatalogSection[] Sections =
    [
        new("Layout & Collections", "#60A5FA",
        [
            new("expander", "Expander", "▾", "Animated disclosure panels and accordion lists"),
            new("carouselgallery", "Carousel Gallery", "◀", "Swipeable image gallery with indicators"),
            new("staggeredgrid", "Staggered Grid", "▦", "Pinterest-style masonry of variable-height items"),
            new("virtualizedgrid", "Virtualized Grid", "▣", "Windowed grid that stays smooth over huge lists"),
            new("parallaxcollectionview", "Parallax Collection", "▰", "Collection with a hero that scrolls at half speed"),
            new("treeview", "Tree View", "⎇", "Lazy-loaded hierarchy with drag-reorder"),
            new("datagrid", "Data Grid", "▩", "Sortable, templated columns over tabular data"),
            new("docking", "Docking", "▨", "Visual-Studio-style tear-off tool windows"),
            new("ribbon", "Ribbon", "☷", "Office-style tabbed command bar for desktop windows")
        ]),

        new("Table View", "#818CF8",
        [
            new("basic", "Basic", "☰", "Settings-style sectioned lists"),
            new("dynamic", "Dynamic", "☷", "Sections built and mutated at runtime"),
            new("dragsort", "Drag & Sort", "⇅", "Reorder rows by dragging"),
            new("picker", "Picker", "◎", "The picker cell types"),
            new("styling", "Styling", "◧", "Theming rows, sections and separators")
        ]),

        new("Panels & Overlays", "#22D3EE",
        [
            new("flyout", "Flyout", "◧", "Side panel that collapses to a rail and pushes or floats"),
            new("flyoutdrawer", "Flyout Drawer", "◨", "A drawer installed over every page from one declaration"),
            new("tabbedpage", "Tabbed Page", "▤", "Tabs with motion icons, badges, transitions and a centre button"),
            new("sheet", "Floating Panel", "▣", "Bottom sheet with detents"),
            new("minimizedsheetstandalone", "Header Peek", "▤", "Collapsed sheet that peeks its header"),
            new("minimizedsheet", "Bottom Tabs", "▁", "A peeking panel over bottom tabs"),
            new("topsheet", "Top Panel", "▔", "A panel that drops from the top"),
            new("dualpanel", "Dual Panels", "◫", "Top and bottom panels together"),
            new("overlay", "Overlay", "▦", "Loading and blocking overlays over any content"),
            new("frostedglass", "Frosted Glass", "◇", "Native blur / glass effect behind content")
        ]),

        new("Input", "#34D399",
        [
            new("textentry", "Text Entry", "✏", "Floating-label entry with validation states"),
            new("autocomplete", "AutoComplete", "≣", "Type-ahead suggestions from any source"),
            new("colorpicker", "Color Picker", "◉", "Wheel, sliders and swatches"),
            new("countryaddress", "Country & Address", "⚑", "Country picker and structured address entry"),
            new("durationpicker", "Duration Picker", "◷", "Hours and minutes on a floating panel"),
            new("fontpicker", "Font Picker", "𝔸", "Family and size pickers, inline or popup"),
            new("slider", "Slider", "━", "Single-value slider with ticks and labels"),
            new("rangeslider", "Range Slider", "≡", "Two-thumb range selection"),
            new("securitypin", "Security Pin", "✱", "PIN entry with masking and shake-on-error"),
            new("passwordstrength", "Password Strength", "▓", "Password field with a live strength meter and rule checklist"),
            new("signaturepad", "Signature Pad", "✍", "Draw, clear and export a signature"),
            new("onscreenkeyboard", "On-Screen Keyboard", "⌨", "Desktop virtual keyboard for kiosks")
        ]),

        new("Actions & Navigation", "#2DD4BF",
        [
            new("buttons", "ShinyButton", "⬭", "States, icons, loading and long-press"),
            new("fab", "Fab & FabMenu", "➕", "Floating action button and expanding menu"),
            new("navigationpage", "Navigation Page", "▢", "A NavigationPage with items on both sides of the title"),
            new("stateview", "State View", "⇄", "Named branches switched by one string"),
            new("wizard", "Wizard", "➤", "Multi-step flow with a pointed progress bar")
        ]),

        new("Status & Feedback", "#F472B6",
        [
            new("pills", "Pills", "●", "Status badges in a range of tones"),
            new("badge", "Badge", "◍", "Corner badge that wraps any content"),
            new("toast", "Toast", "▬", "Queued toasts with progress and spinners"),
            new("dialogs", "Dialogs", "❕", "Owned alert, confirm, prompt and action sheet"),
            new("feedback", "Feedback", "◈", "Haptics and system sounds"),
            new("progressbar", "Progress Bar", "▤", "Determinate and indeterminate progress"),
            new("skeleton", "Skeleton", "☰", "Shimmering placeholders while content loads")
        ]),

        new("Animation", "#FB923C",
        [
            new("keyframe", "Keyframe", "◐", "Seekable CSS-style keyframe animation in XAML"),
            new("motionicons", "Motion Icons", "✦", "42 animated icons on timer, hover, tap or command")
        ]),

        new("Media", "#C084FC",
        [
            new("camera", "Camera", "◉", "Preview, capture and pluggable frame analysis"),
            new("mediaelement", "Media Element", "▶", "Audio and video with a themed transport bar"),
            new("documentsession", "Scanned Documents", "▧", "AI document scanning and extraction"),
            new("shinyimage", "Shiny Image", "▥", "Placeholder, download progress and error artwork"),
            new("imageviewer", "Image Viewer", "▣", "Pinch, pan and double-tap zoom"),
            new("imagegallery", "Image Gallery", "▦", "Paged gallery of zoomable images"),
            new("imageeditor", "Image Editor", "✎", "Crop, rotate, draw, text, undo and export"),
            new("mediapicker", "Media Picker", "◫", "Pick or capture photos and video")
        ]),

        new("Scheduler", "#F87171",
        [
            new("calendar", "Calendar", "□", "Month grid with custom event providers"),
            new("agenda", "Agenda", "≡", "Timeline of a single day"),
            new("agendacalendarpicker", "Calendar Picker", "◰", "Compact date picker over an agenda"),
            new("calendarlist", "Event List", "☷", "Grouped, scrollable list of upcoming events")
        ]),

        new("Communication", "#38BDF8",
        [
            new("chat", "Chat", "✉", "Bubbles, typing indicators, load-more and input bar"),
            new("chattemplates", "Chat Templates", "❏", "Per-message rendering with custom templates")
        ]),

        new("Content", "#A3E635",
        [
            new("markdownview", "Markdown Viewer", "↓", "Markdig-powered renderer"),
            new("markdowneditor", "Markdown Editor", "✍", "Toolbar editor with live preview")
        ]),

        new("Diagrams", "#FBBF24",
        [
            new("flowchart", "Flowchart", "⬓", "Mermaid flowcharts rendered natively"),
            new("directions", "Directions", "⇉", "Every flow direction"),
            new("themes", "Themes", "◑", "Diagram theming"),
            new("subgraphs", "Subgraphs", "⊞", "Nested and grouped nodes"),
            new("editor", "Editor", "✍", "Live Mermaid editor with preview")
        ]),

        new("Barcodes", "#A78BFA",
        [
            new("qrcode", "QR Code", "▦", "QR rendering with sizing and error correction"),
            new("barcodegallery", "Barcode Gallery", "☰", "All 13 supported symbologies")
        ]),

        new("Desktop", "#94A3B8",
        [
            new("trayicon", "System Tray", "◭", "Tray icon with menu and balloon tips"),
            new("filedrop", "File Drop", "⇩", "Window-level file drop, over top of any web view")
        ])
    ];

    public static int TotalControls => Sections.Sum(s => s.Items.Length);
}
