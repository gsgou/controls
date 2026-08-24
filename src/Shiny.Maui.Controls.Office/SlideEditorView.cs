using Shiny.Controls.Office.Presentation;
using Shiny.Maui.Controls.FontPicker;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// <see cref="SlideEditor"/> with an editing toolbar above it.
/// </summary>
/// <remarks>
/// Built from MAUI primitives plus the core package's <c>FontPickerButton</c> and
/// <c>FontSizePickerButton</c>. MAUI has no <c>ShinyToolbar</c> — that control is Blazor-only — so the
/// bar is a scrolling row here, while the Blazor <c>SlideEditorView</c> composes ShinyToolbar for the
/// same slots. The API and behaviour match; only the internals differ.
/// </remarks>
public class SlideEditorView : ContentView, IDisposable
{
    readonly SlideEditor editor = new();
    readonly HorizontalStackLayout bar;
    readonly ScrollView barScroller;
    readonly Grid root;
    readonly Label status;
    readonly Label counter;

    readonly Button previous;
    readonly Button next;
    readonly Button bold;
    readonly Button italic;
    readonly Button underline;
    readonly Button strike;
    readonly Button alignLeft;
    readonly Button alignCenter;
    readonly Button alignRight;
    readonly Button outdent;
    readonly Button indent;
    readonly Button addTextBox;
    readonly Button deleteShape;
    readonly Button undo;
    readonly Button redo;

    View? fontPicker;
    View? sizePicker;
    bool suppressPickerEvents;
    bool disposed;

    public SlideEditorView()
    {
        this.previous = this.MakeToggle("‹", FontAttributes.None, () => this.editor.Previous());
        this.next = this.MakeToggle("›", FontAttributes.None, () => this.editor.Next());

        this.bold = this.MakeToggle("B", FontAttributes.Bold, () => this.editor.Controller?.ToggleBold());
        this.italic = this.MakeToggle("I", FontAttributes.Italic, () => this.editor.Controller?.ToggleItalic());
        this.underline = this.MakeToggle("U", FontAttributes.None, () => this.editor.Controller?.ToggleUnderline());
        this.strike = this.MakeToggle("S", FontAttributes.None, () => this.editor.Controller?.ToggleStrikethrough());

        this.alignLeft = this.MakeToggle("⯇", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Left));
        this.alignCenter = this.MakeToggle("≡", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Center));
        this.alignRight = this.MakeToggle("⯈", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Right));

        this.outdent = this.MakeToggle("⇤", FontAttributes.None, () => this.editor.Controller?.ShiftLevel(-1));
        this.indent = this.MakeToggle("⇥", FontAttributes.None, () => this.editor.Controller?.ShiftLevel(1));

        this.addTextBox = this.MakeToggle("＋T", FontAttributes.None, this.AddTextBox);
        this.deleteShape = this.MakeToggle("🗑", FontAttributes.None, () => this.editor.Controller?.DeleteSelectedShape());

        this.undo = this.MakeToggle("↶", FontAttributes.None, () => this.editor.Controller?.Undo());
        this.redo = this.MakeToggle("↷", FontAttributes.None, () => this.editor.Controller?.Redo());

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
        this.bar.Add(this.alignLeft);
        this.bar.Add(this.alignCenter);
        this.bar.Add(this.alignRight);
        this.bar.Add(this.outdent);
        this.bar.Add(this.indent);
        this.bar.Add(Separator());
        this.bar.Add(this.addTextBox);
        this.bar.Add(this.deleteShape);
        this.bar.Add(Separator());
        this.bar.Add(this.undo);
        this.bar.Add(this.redo);

        this.RefreshBar();
    }

    /// <summary>The core package's font picker, which renders each family in its own typeface.</summary>
    FontPickerButton CreateFontPicker()
    {
        var picker = new FontPickerButton
        {
            AvailableFonts = (this.FontFamilies ?? DefaultFontFamilies).ToList(),
            Placeholder = "Font",
            WidthRequest = 150
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
            WidthRequest = 84
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

    static readonly IList<string> DefaultFontFamilies =
        ["Calibri", "Cambria", "Arial", "Times New Roman", "Georgia", "Verdana", "Courier New"];

    // Slide type runs large: 18pt is a small body size on a deck, where a document's is 11.
    static readonly IList<double> DefaultFontSizes =
        [8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 44, 54, 66, 88];

    static BoxView Separator() => new()
    {
        WidthRequest = 1,
        HeightRequest = 22,
        Color = Colors.Gray,
        Opacity = 0.35,
        VerticalOptions = LayoutOptions.Center,
        Margin = new Thickness(3, 0)
    };

    Button MakeToggle(string text, FontAttributes attributes, Action action)
    {
        var button = new Button
        {
            Text = text,
            FontAttributes = attributes,
            WidthRequest = 38,
            HeightRequest = 34,
            Padding = 0,
            CornerRadius = 5,
            BackgroundColor = Colors.Transparent
        };

        // A TapGestureRecognizer never fires on a Button, so Clicked is the only option here.
        button.Clicked += (_, _) =>
        {
            action();
            this.AfterCommand();
        };

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

        SetActive(this.alignLeft, format.Alignment == TextAlignment.Left);
        SetActive(this.alignCenter, format.Alignment == TextAlignment.Center);
        SetActive(this.alignRight, format.Alignment == TextAlignment.Right);

        foreach (var button in new[] { this.bold, this.italic, this.underline, this.strike,
                                       this.alignLeft, this.alignCenter, this.alignRight,
                                       this.outdent, this.indent })
        {
            button.IsEnabled = hasText;
        }

        this.addTextBox.IsEnabled = enabled;
        this.deleteShape.IsEnabled = hasSelection;

        this.previous.IsEnabled = controller?.CanGoPrevious ?? false;
        this.next.IsEnabled = controller?.CanGoNext ?? false;

        this.undo.IsEnabled = enabled && (controller?.CanUndo ?? false);
        this.redo.IsEnabled = enabled && (controller?.CanRedo ?? false);

        this.counter.Text = controller is null ? "—" : $"{controller.Index + 1}/{controller.Count}";

        this.status.Text = controller switch
        {
            { SelectedShape: >= 0, IsEditingText: true } => "Editing text — Esc to leave",
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

        this.suppressPickerEvents = false;
    }

    static void SetActive(Button button, bool active)
        => button.BackgroundColor = active ? Colors.SteelBlue.WithAlpha(0.25f) : Colors.Transparent;

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
