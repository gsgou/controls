using Shiny.Controls.Office.Icons;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Presentation;
using Shiny.Controls.Office.Shapes;
using Shiny.Maui.Controls.ColorPicker;
using Shiny.Maui.Controls.FontPicker;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// <see cref="SlideEditor"/> with an editing toolbar above it.
/// </summary>
/// <remarks>
/// <para>
/// Built from MAUI primitives plus the core package's <c>FontPickerButton</c> and
/// <c>FontSizePickerButton</c>. MAUI has no <c>ShinyToolbar</c> — that control is Blazor-only — so the
/// bar is a scrolling row here, while the Blazor <c>SlideEditorView</c> composes ShinyToolbar for the
/// same slots. The API and behaviour match; only the internals differ.
/// </para>
/// <para>
/// Every plain button on it is an <see cref="OfficeToolbarButton"/> drawing from the shared
/// <see cref="OfficeIcons"/> set — the same artwork, at the same weight, as the document toolbar and
/// as both Blazor toolbars. The pickers are the exception: a font, a size and a colour have to show
/// what they are currently set to.
/// </para>
/// </remarks>
public class SlideEditorView : ContentView, IDisposable
{
    readonly SlideEditor editor = new();
    readonly HorizontalStackLayout bar;
    readonly ScrollView barScroller;
    readonly Grid root;
    readonly Label status;
    readonly Label counter;

    readonly OfficeToolbarButton previous;
    readonly OfficeToolbarButton next;
    readonly OfficeToolbarButton bold;
    readonly OfficeToolbarButton italic;
    readonly OfficeToolbarButton underline;
    readonly OfficeToolbarButton strike;
    readonly OfficeToolbarButton alignLeft;
    readonly OfficeToolbarButton alignCenter;
    readonly OfficeToolbarButton alignRight;
    readonly OfficeToolbarButton outdent;
    readonly OfficeToolbarButton indent;
    readonly OfficeToolbarButton addTextBox;
    readonly OfficeToolbarButton highlight;
    readonly OfficeToolbarButton insertShape;
    readonly OfficeToolbarButton insertTable;
    readonly OfficeToolbarButton insertPicture;
    readonly OfficeToolbarButton deleteShape;
    readonly OfficeToolbarButton undo;
    readonly OfficeToolbarButton redo;
    readonly ColorPickerButton textColor;
    readonly List<OfficeToolbarButton> buttons = [];

    View? fontPicker;
    View? sizePicker;
    bool suppressPickerEvents;
    bool disposed;

    public SlideEditorView()
    {
        this.previous = this.MakeButton(OfficeIcon.Previous, "Previous slide", () => this.editor.Previous());
        this.next = this.MakeButton(OfficeIcon.Next, "Next slide", () => this.editor.Next());

        this.bold = this.MakeButton(OfficeIcon.Bold, "Bold (Ctrl+B)", () => this.editor.Controller?.ToggleBold());
        this.italic = this.MakeButton(OfficeIcon.Italic, "Italic (Ctrl+I)", () => this.editor.Controller?.ToggleItalic());
        this.underline = this.MakeButton(OfficeIcon.Underline, "Underline (Ctrl+U)", () => this.editor.Controller?.ToggleUnderline());
        this.strike = this.MakeButton(OfficeIcon.Strikethrough, "Strikethrough", () => this.editor.Controller?.ToggleStrikethrough());

        this.alignLeft = this.MakeButton(OfficeIcon.AlignLeft, "Align left", () => this.editor.Controller?.SetAlignment(TextAlignment.Left));
        this.alignCenter = this.MakeButton(OfficeIcon.AlignCenter, "Centre", () => this.editor.Controller?.SetAlignment(TextAlignment.Center));
        this.alignRight = this.MakeButton(OfficeIcon.AlignRight, "Align right", () => this.editor.Controller?.SetAlignment(TextAlignment.Right));

        this.outdent = this.MakeButton(OfficeIcon.Outdent, "Outdent (Shift+Tab)", () => this.editor.Controller?.ShiftLevel(-1));
        this.indent = this.MakeButton(OfficeIcon.Indent, "Indent (Tab)", () => this.editor.Controller?.ShiftLevel(1));

        this.addTextBox = this.MakeButton(OfficeIcon.TextBox, "Add a text box", this.AddTextBox);
        this.deleteShape = this.MakeButton(OfficeIcon.Delete, "Delete the selected shape", () => this.editor.Controller?.DeleteSelectedShape());

        this.highlight = this.MakeAsyncButton(OfficeIcon.Highlight, "Highlight", this.PickHighlightAsync);
        this.insertShape = this.MakeAsyncButton(OfficeIcon.Shape, "Shapes", this.InsertShapeAsync);
        this.insertTable = this.MakeAsyncButton(OfficeIcon.Table, "Table", this.InsertTableAsync);
        this.insertPicture = this.MakeAsyncButton(OfficeIcon.Picture, "Picture", this.InsertPictureAsync);

        this.undo = this.MakeButton(OfficeIcon.Undo, "Undo (Ctrl+Z)", () => this.editor.Controller?.Undo());
        this.redo = this.MakeButton(OfficeIcon.Redo, "Redo (Ctrl+Shift+Z)", () => this.editor.Controller?.Redo());

        this.counter = new Label
        {
            FontSize = 13,
            WidthRequest = 54,
            HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center
        };

        this.status = new Label
        {
            FontSize = 12,
            Opacity = 0.7,
            Padding = new Thickness(10, 4),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        this.textColor = this.CreateColorPicker();

        this.bar = new HorizontalStackLayout { Spacing = 4, Padding = new Thickness(8, 6) };
        this.barScroller = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = this.bar
        };

        this.root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        this.root.Add(this.barScroller);
        this.root.Add(this.editor);
        this.root.Add(this.status);
        Grid.SetRow(this.editor, 1);
        Grid.SetRow(this.status, 2);

        this.editor.DeckChanged += this.OnDeckChanged;
        this.AttachDrop();
        this.editor.SlideChanged += this.OnSlideChanged;
        this.Content = this.root;

        this.BuildBar();
    }

    public static readonly BindableProperty DeckProperty = BindableProperty.Create(
        nameof(Deck),
        typeof(SlideDeck),
        typeof(SlideEditorView),
        propertyChanged: (b, _, value) =>
        {
            var view = (SlideEditorView)b;
            view.editor.Deck = (SlideDeck?)value;
            view.AttachController();
        });

    public static readonly BindableProperty ThemeProperty = BindableProperty.Create(
        nameof(Theme),
        typeof(SlideTheme),
        typeof(SlideEditorView),
        SlideTheme.Light,
        propertyChanged: (b, _, value) => ((SlideEditorView)b).editor.Theme = (SlideTheme)value);

    public static readonly BindableProperty SlideIndexProperty = BindableProperty.Create(
        nameof(SlideIndex),
        typeof(int),
        typeof(SlideEditorView),
        0,
        BindingMode.TwoWay,
        propertyChanged: (b, _, value) => ((SlideEditorView)b).editor.SlideIndex = (int)value);

    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(SlideEditorView),
        false,
        propertyChanged: (b, _, value) =>
        {
            var view = (SlideEditorView)b;
            view.editor.IsReadOnly = (bool)value;
            view.RefreshBar();
        });

    public static readonly BindableProperty ShowToolbarProperty = BindableProperty.Create(
        nameof(ShowToolbar),
        typeof(bool),
        typeof(SlideEditorView),
        true,
        propertyChanged: (b, _, value) => ((SlideEditorView)b).barScroller.IsVisible = (bool)value);

    /// <summary>A one-line hint under the canvas saying what the current gesture will do.</summary>
    public static readonly BindableProperty ShowStatusProperty = BindableProperty.Create(
        nameof(ShowStatus),
        typeof(bool),
        typeof(SlideEditorView),
        true,
        propertyChanged: (b, _, value) => ((SlideEditorView)b).status.IsVisible = (bool)value);

    /// <summary>
    /// Whether the icon-only toolbar buttons carry a hover tooltip naming what they do.
    /// </summary>
    /// <remarks>
    /// On for desktop, off for phones and tablets. Every button on this bar is icon only, and an icon
    /// with no label is a guess until something names it — but the tooltip that names it opens on
    /// hover, and there is no hover on a touch screen. A long-press tooltip is not the answer either:
    /// it would compete with the tap the button exists for. Touch hosts get the semantic description
    /// instead, which is what a screen reader reads on any platform.
    /// </remarks>
    public static readonly BindableProperty ShowToolbarTooltipsProperty = BindableProperty.Create(
        nameof(ShowToolbarTooltips),
        typeof(bool),
        typeof(SlideEditorView),
        OfficeToolbarButton.TooltipsByDefault,
        propertyChanged: (b, _, value) =>
        {
            foreach (var button in ((SlideEditorView)b).buttons)
                button.SetTooltipEnabled((bool)value);
        });

    public static readonly BindableProperty FontFamiliesProperty = BindableProperty.Create(
        nameof(FontFamilies),
        typeof(IList<string>),
        typeof(SlideEditorView),
        propertyChanged: (b, _, _) => ((SlideEditorView)b).BuildBar());

    public static readonly BindableProperty FontSizesProperty = BindableProperty.Create(
        nameof(FontSizes),
        typeof(IList<double>),
        typeof(SlideEditorView),
        propertyChanged: (b, _, _) => ((SlideEditorView)b).BuildBar());

    /// <summary>The deck to edit. Must have been opened with <c>editable: true</c>.</summary>
    public SlideDeck? Deck
    {
        get => (SlideDeck?)this.GetValue(DeckProperty);
        set => this.SetValue(DeckProperty, value);
    }

    public SlideTheme Theme
    {
        get => (SlideTheme)this.GetValue(ThemeProperty);
        set => this.SetValue(ThemeProperty, value);
    }

    public int SlideIndex
    {
        get => (int)this.GetValue(SlideIndexProperty);
        set => this.SetValue(SlideIndexProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)this.GetValue(IsReadOnlyProperty);
        set => this.SetValue(IsReadOnlyProperty, value);
    }

    public bool ShowToolbar
    {
        get => (bool)this.GetValue(ShowToolbarProperty);
        set => this.SetValue(ShowToolbarProperty, value);
    }

    public bool ShowStatus
    {
        get => (bool)this.GetValue(ShowStatusProperty);
        set => this.SetValue(ShowStatusProperty, value);
    }

    /// <summary>Hover tooltips on the icon-only toolbar buttons. Desktop only by default.</summary>
    public bool ShowToolbarTooltips
    {
        get => (bool)this.GetValue(ShowToolbarTooltipsProperty);
        set => this.SetValue(ShowToolbarTooltipsProperty, value);
    }

    public IList<string>? FontFamilies
    {
        get => (IList<string>?)this.GetValue(FontFamiliesProperty);
        set => this.SetValue(FontFamiliesProperty, value);
    }

    public IList<double>? FontSizes
    {
        get => (IList<double>?)this.GetValue(FontSizesProperty);
        set => this.SetValue(FontSizesProperty, value);
    }

    /// <summary>Raised after every edit.</summary>
    public event EventHandler? DeckChanged;

    /// <summary>Raised when a dropped or chosen file could not be inserted, so a host can say so.</summary>
    public event EventHandler<OfficeDropRejected>? DropRejected;

    public static readonly BindableProperty ShapeWidthProperty = BindableProperty.Create(
        nameof(ShapeWidth), typeof(double), typeof(SlideEditorView), 240d);

    public static readonly BindableProperty ShapeHeightProperty = BindableProperty.Create(
        nameof(ShapeHeight), typeof(double), typeof(SlideEditorView), 180d);

    public static readonly BindableProperty PictureWidthProperty = BindableProperty.Create(
        nameof(PictureWidth), typeof(double), typeof(SlideEditorView), 400d);

    /// <summary>The size of shape the toolbar inserts, in slide pixels.</summary>
    public double ShapeWidth
    {
        get => (double)this.GetValue(ShapeWidthProperty);
        set => this.SetValue(ShapeWidthProperty, value);
    }

    public double ShapeHeight
    {
        get => (double)this.GetValue(ShapeHeightProperty);
        set => this.SetValue(ShapeHeightProperty, value);
    }

    /// <summary>How wide an inserted picture is, in slide pixels.</summary>
    public double PictureWidth
    {
        get => (double)this.GetValue(PictureWidthProperty);
        set => this.SetValue(PictureWidthProperty, value);
    }

    public SlideEditorController? Controller => this.editor.Controller;

    /// <summary>Routes a physical key press to the editor. See <see cref="SlideEditor.HandleKey"/>.</summary>
    public bool HandleKey(EditorKey key, bool shift = false, bool control = false)
    {
        var handled = this.editor.HandleKey(key, shift, control);
        this.RefreshBar();
        return handled;
    }

    public void FocusEditor() => this.editor.FocusEditor();

    // ---- toolbar ----

    void BuildBar()
    {
        this.bar.Clear();

        this.fontPicker = this.CreateFontPicker();
        this.sizePicker = this.CreateSizePicker();

        this.bar.Add(this.previous);
        this.bar.Add(this.counter);
        this.bar.Add(this.next);
        this.bar.Add(Separator());

        if (this.fontPicker is not null)
            this.bar.Add(this.fontPicker);

        if (this.sizePicker is not null)
            this.bar.Add(this.sizePicker);

        this.bar.Add(Separator());
        this.bar.Add(this.bold);
        this.bar.Add(this.italic);
        this.bar.Add(this.underline);
        this.bar.Add(this.strike);
        this.bar.Add(Separator());
        this.bar.Add(this.textColor);
        this.bar.Add(this.highlight);
        this.bar.Add(Separator());
        this.bar.Add(this.alignLeft);
        this.bar.Add(this.alignCenter);
        this.bar.Add(this.alignRight);
        this.bar.Add(this.outdent);
        this.bar.Add(this.indent);
        this.bar.Add(Separator());
        this.bar.Add(this.addTextBox);
        this.bar.Add(this.insertShape);
        this.bar.Add(this.insertTable);
        this.bar.Add(this.insertPicture);
        this.bar.Add(this.deleteShape);
        this.bar.Add(Separator());
        this.bar.Add(this.undo);
        this.bar.Add(this.redo);

        this.RefreshBar();
    }

    /// <summary>
    /// The core package's colour picker, in its button form.
    /// </summary>
    /// <remarks>
    /// Not a row of preset swatches: a deck's text can be any colour, and a fixed palette is a promise
    /// the format does not make. The button shows the colour at the caret and opens the full spectrum —
    /// the same control the Blazor toolbar puts in this slot.
    /// </remarks>
    ColorPickerButton CreateColorPicker()
    {
        var picker = new ColorPickerButton
        {
            Text = string.Empty,
            ShowOpacity = false,
            WidthRequest = 44,
            HeightRequest = ToolbarItemHeight,
            VerticalOptions = LayoutOptions.Center
        };

        picker.ColorChanged += (_, color) =>
        {
            if (this.suppressPickerEvents)
                return;

            this.editor.Controller?.SetTextColor(ToArgb(color));
            this.AfterCommand();
        };

        return picker;
    }

    /// <summary>MAUI colours are floats in 0..1; the document kernel stores bytes.</summary>
    static ArgbColor ToArgb(Color color) => new(
        (byte)Math.Round(color.Alpha * 255),
        (byte)Math.Round(color.Red * 255),
        (byte)Math.Round(color.Green * 255),
        (byte)Math.Round(color.Blue * 255));

    static Color FromArgb(ArgbColor color) => Color.FromRgba(color.R, color.G, color.B, color.A);

    /// <summary>The core package's font picker, which renders each family in its own typeface.</summary>
    FontPickerButton CreateFontPicker()
    {
        var picker = new FontPickerButton
        {
            AvailableFonts = (this.FontFamilies ?? DefaultFontFamilies).ToList(),
            Placeholder = "Font",
            WidthRequest = 150,
            HeightRequest = ToolbarItemHeight,
            VerticalOptions = LayoutOptions.Center
        };

        picker.FontChanged += (_, family) =>
        {
            if (this.suppressPickerEvents || string.IsNullOrEmpty(family))
                return;

            this.editor.Controller?.SetFontFamily(family);
            this.AfterCommand();
        };

        return picker;
    }

    FontSizePickerButton CreateSizePicker()
    {
        var picker = new FontSizePickerButton
        {
            AvailableFontSizes = (this.FontSizes ?? DefaultFontSizes).ToList(),
            WidthRequest = 84,
            HeightRequest = ToolbarItemHeight,
            VerticalOptions = LayoutOptions.Center
        };

        picker.FontSizeChanged += (_, size) =>
        {
            if (this.suppressPickerEvents)
                return;

            this.editor.Controller?.SetFontSize(size);
            this.AfterCommand();
        };

        return picker;
    }

    void AddTextBox()
    {
        if (this.editor.Controller is not { } controller)
            return;

        controller.AddTextBox(
            Math.Max(0, controller.Deck.SlideWidth / 2 - 160),
            Math.Max(0, controller.Deck.SlideHeight / 2 - 32));
    }

    // ---- insert ----

    /// <summary>
    /// Where a new object goes: the middle of the slide.
    /// </summary>
    /// <remarks>
    /// Not the origin, which is under the title placeholder — an object inserted there is both hidden
    /// and awkward to grab.
    /// </remarks>
    static (double X, double Y) Centred(SlideEditorController controller, double width, double height)
        => (Math.Max(0, (controller.Deck.SlideWidth - width) / 2),
            Math.Max(0, (controller.Deck.SlideHeight - height) / 2));

    async Task PickHighlightAsync()
    {
        var (chosen, color) = await OfficeMenus.PickHighlightAsync(OfficeMenus.PageOf(this));
        if (!chosen)
            return;

        this.editor.Controller?.SetHighlight(color);
        this.AfterCommand();
    }

    async Task InsertShapeAsync()
    {
        if (this.editor.Controller is not { } controller)
            return;

        if (await OfficeMenus.PickShapeAsync(OfficeMenus.PageOf(this)) is not { } geometry)
            return;

        var (x, y) = Centred(controller, this.ShapeWidth, this.ShapeHeight);
        controller.AddShape(geometry, x, y, this.ShapeWidth, this.ShapeHeight);
        this.AfterCommand();
    }

    async Task InsertTableAsync()
    {
        if (this.editor.Controller is not { } controller)
            return;

        if (await OfficeMenus.PickTableAsync(OfficeMenus.PageOf(this)) is not { } size)
            return;

        // Sized to the slide rather than to the grid: a 2x2 and a 6x4 both want to be a table on a
        // slide, not a postage stamp and something that overflows the edge.
        var width = controller.Deck.SlideWidth * 0.7;
        var height = Math.Min(controller.Deck.SlideHeight * 0.6, size.Rows * 44);
        var (x, y) = Centred(controller, width, height);

        controller.AddTable(size.Rows, size.Columns, x, y, width, height);
        this.AfterCommand();
    }

    async Task InsertPictureAsync()
    {
        var (image, rejected) = await OfficeMenus.PickImageAsync();

        if (rejected is not null)
        {
            this.DropRejected?.Invoke(this, rejected);
            return;
        }

        if (image is not null)
            this.InsertImage(image, null);
    }

    /// <summary>
    /// Places a picture, at a point when one was given and in the middle otherwise.
    /// </summary>
    /// <remarks>
    /// A drop knows where it landed and should use it; the toolbar button has no such point, and
    /// centring is the honest answer rather than a guess at where the user was looking.
    /// </remarks>
    void InsertImage(OfficePickedImage image, (double X, double Y)? at)
    {
        if (this.editor.Controller is not { } controller)
            return;

        var width = Math.Min(controller.Deck.SlideWidth / 2, this.PictureWidth);
        var height = width * 0.75;

        var (x, y) = at is { } point
            ? (point.X - (width / 2), point.Y - (height / 2))
            : Centred(controller, width, height);

        controller.AddPicture(
            image.Data,
            image.ContentType,
            x,
            y,
            width,
            height,
            Path.GetFileNameWithoutExtension(image.FileName));

        this.AfterCommand();
    }

    // ---- file drop ----

    void AttachDrop()
    {
        var drop = new DropGestureRecognizer { AllowDrop = true };
        drop.Drop += this.OnDropAsync;
        this.editor.GestureRecognizers.Add(drop);
    }

    async void OnDropAsync(object? sender, DropEventArgs e)
    {
        if (this.IsReadOnly || this.Deck is null || this.editor.Controller is not { } controller)
            return;

        // Where the drop landed, in slide coordinates. Read before the await, while the gesture's
        // position still means something.
        var point = e.GetPosition(this.editor) is { } position
            ? controller.ToSlide(position.X, position.Y)
            : null;

        try
        {
            foreach (var image in await OfficeFileDrop.ReadImagesAsync(e))
                this.InsertImage(image, point);
        }
        catch (Exception ex)
        {
            this.DropRejected?.Invoke(this, new OfficeDropRejected(string.Empty, ex.Message));
        }
    }

    static readonly IList<string> DefaultFontFamilies =
        ["Calibri", "Cambria", "Arial", "Times New Roman", "Georgia", "Verdana", "Courier New"];

    // Slide type runs large: 18pt is a small body size on a deck, where a document's is 11.
    static readonly IList<double> DefaultFontSizes =
        [8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 44, 54, 66, 88];

    /// <summary>One height for every control in the bar. See <see cref="OfficeToolbarButton.ItemHeight"/>.</summary>
    const double ToolbarItemHeight = OfficeToolbarButton.ItemHeight;

    static BoxView Separator() => new()
    {
        WidthRequest = 1,
        HeightRequest = 22,
        Color = Colors.Gray,
        Opacity = 0.35,
        VerticalOptions = LayoutOptions.Center,
        Margin = new Thickness(3, 0)
    };

    OfficeToolbarButton MakeButton(OfficeIcon icon, string hint, Action action)
    {
        var button = this.NewButton(icon, hint);

        button.Clicked += (_, _) =>
        {
            action();
            this.AfterCommand();
        };

        return button;
    }

    /// <summary>
    /// A button whose work opens a menu or a file picker first.
    /// </summary>
    /// <remarks>
    /// It does not call <c>AfterCommand</c> itself: each of these has to wait for the user to choose
    /// something, and refreshing the bar and stealing focus back before then would happen while the
    /// menu is still up.
    /// </remarks>
    OfficeToolbarButton MakeAsyncButton(OfficeIcon icon, string hint, Func<Task> action)
    {
        var button = this.NewButton(icon, hint);
        button.Clicked += async (_, _) => await action();

        return button;
    }

    OfficeToolbarButton NewButton(OfficeIcon icon, string hint)
    {
        var button = new OfficeToolbarButton(icon, hint);
        button.SetTooltipEnabled(this.ShowToolbarTooltips);
        this.buttons.Add(button);

        return button;
    }

    void AfterCommand()
    {
        // Focus returns to the editor after every toolbar action; leaving it on the button means the
        // next keystroke goes nowhere, which reads as the editor having stopped working.
        this.editor.FocusEditor();
        this.RefreshBar();
        this.DeckChanged?.Invoke(this, EventArgs.Empty);
    }

    void AttachController()
    {
        if (this.editor.Controller is { } controller)
            controller.Changed += this.OnControllerChanged;

        this.RefreshBar();
    }

    void OnControllerChanged(object? sender, EventArgs e) => this.RefreshBar();

    void OnDeckChanged(object? sender, EventArgs e)
    {
        this.RefreshBar();
        this.DeckChanged?.Invoke(this, EventArgs.Empty);
    }

    void OnSlideChanged(object? sender, int index)
    {
        this.SlideIndex = index;
        this.RefreshBar();
    }

    /// <summary>Reflects the state under the caret back into the toolbar.</summary>
    void RefreshBar()
    {
        var controller = this.editor.Controller;
        var format = controller?.CaretFormat ?? SlideCaretFormat.Default;

        var enabled = !this.IsReadOnly && this.Deck is not null;
        var hasSelection = enabled && controller?.SelectedShape >= 0;

        // Text formatting only means something while a caret is inside a shape's text. A live Bold
        // button with nothing to embolden is worse than a disabled one: it says the click did
        // something.
        var hasText = enabled && controller?.IsEditingText == true;

        SetActive(this.bold, format.Bold);
        SetActive(this.italic, format.Italic);
        SetActive(this.underline, format.Underline);
        SetActive(this.strike, format.Strike);
        SetActive(this.highlight, format.Highlight is not null);

        SetActive(this.alignLeft, format.Alignment == TextAlignment.Left);
        SetActive(this.alignCenter, format.Alignment == TextAlignment.Center);
        SetActive(this.alignRight, format.Alignment == TextAlignment.Right);

        foreach (var button in new[] { this.bold, this.italic, this.underline, this.strike, this.highlight,
                                       this.alignLeft, this.alignCenter, this.alignRight,
                                       this.outdent, this.indent })
        {
            button.SetEnabled(hasText);
        }

        this.addTextBox.SetEnabled(enabled);
        this.insertShape.SetEnabled(enabled);
        this.insertTable.SetEnabled(enabled);
        this.insertPicture.SetEnabled(enabled);
        this.deleteShape.SetEnabled(hasSelection);

        this.previous.SetEnabled(controller?.CanGoPrevious ?? false);
        this.next.SetEnabled(controller?.CanGoNext ?? false);

        this.undo.SetEnabled(enabled && (controller?.CanUndo ?? false));
        this.redo.SetEnabled(enabled && (controller?.CanRedo ?? false));

        this.counter.Text = controller is null ? "—" : $"{controller.Index + 1}/{controller.Count}";

        this.status.Text = controller switch
        {
            { SelectedShape: >= 0, IsEditingText: true } => "Editing text — double-tap a word to select it, Esc to leave",
            { SelectedShape: >= 0 } => "Shape selected — double-tap to edit its text",
            _ => "Tap a shape to select it; double-tap to edit its text."
        };

        // Writing the pickers' selection raises their change events, which would immediately re-apply
        // the format that was only being displayed - so the handlers are muted while they update.
        this.suppressPickerEvents = true;

        if (this.fontPicker is FontPickerButton font)
            font.SelectedFont = format.FontFamily;

        if (this.sizePicker is FontSizePickerButton size)
        {
            // Snap to the nearest offered size: a deck can hold any value, the picker only some.
            var sizes = this.FontSizes ?? DefaultFontSizes;
            size.SelectedFontSize = sizes.OrderBy(x => Math.Abs(x - format.FontSize)).FirstOrDefault();
        }

        this.textColor.SelectedColor = FromArgb(format.Color);
        this.textColor.IsEnabled = hasText;

        this.suppressPickerEvents = false;
    }

    static void SetActive(OfficeToolbarButton button, bool active) => button.IsActive = active;

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed || !disposing)
            return;

        this.disposed = true;

        this.editor.DeckChanged -= this.OnDeckChanged;
        this.editor.SlideChanged -= this.OnSlideChanged;

        if (this.editor.Controller is { } controller)
            controller.Changed -= this.OnControllerChanged;

        this.editor.Dispose();
    }
}
