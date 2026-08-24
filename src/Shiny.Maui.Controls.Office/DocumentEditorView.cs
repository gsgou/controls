using Shiny.Controls.Office.Document;
using Shiny.Maui.Controls.FontPicker;
using Shiny.Controls.Office.Spreadsheet;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// <see cref="DocumentEditor"/> with a formatting toolbar above it.
/// </summary>
/// <remarks>
/// The toolbar is built from MAUI primitives plus the core package's <c>FontPickerButton</c> and
/// <c>FontSizePickerButton</c>. MAUI has no <c>ShinyToolbar</c> — that control is Blazor-only — so the
/// bar itself is a scrolling row here, while the Blazor <c>DocumentEditorView</c> composes ShinyToolbar
/// for the same two slots. The API and behaviour match; only the internals differ.
/// </remarks>
public class DocumentEditorView : ContentView, IDisposable
{
    readonly DocumentEditor editor = new();
    readonly HorizontalStackLayout bar;
    readonly ScrollView barScroller;
    readonly Grid root;

    readonly Button bold;
    readonly Button italic;
    readonly Button underline;
    readonly Button strike;
    readonly Button undo;
    readonly Button redo;
    readonly Button alignLeft;
    readonly Button alignCenter;
    readonly Button alignRight;
    readonly Button alignJustify;

    View? fontPicker;
    View? sizePicker;
    bool suppressPickerEvents;
    bool disposed;

    public DocumentEditorView()
    {
        this.bold = MakeToggle("B", FontAttributes.Bold, () => this.editor.Controller?.ToggleBold());
        this.italic = MakeToggle("I", FontAttributes.Italic, () => this.editor.Controller?.ToggleItalic());
        this.underline = MakeToggle("U", FontAttributes.None, () => this.editor.Controller?.ToggleUnderline());
        this.strike = MakeToggle("S", FontAttributes.None, () => this.editor.Controller?.ToggleStrikethrough());

        this.alignLeft = MakeToggle("⯇", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Left));
        this.alignCenter = MakeToggle("≡", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Center));
        this.alignRight = MakeToggle("⯈", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Right));
        this.alignJustify = MakeToggle("▤", FontAttributes.None, () => this.editor.Controller?.SetAlignment(TextAlignment.Justify));

        this.undo = MakeToggle("↶", FontAttributes.None, () => this.editor.Controller?.Undo());
        this.redo = MakeToggle("↷", FontAttributes.None, () => this.editor.Controller?.Redo());

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
                new RowDefinition(GridLength.Star)
            }
        };

        this.root.Add(this.barScroller);
        this.root.Add(this.editor);
        Grid.SetRow(this.editor, 1);

        this.editor.DocumentChanged += this.OnDocumentChanged;
        this.Content = this.root;

        this.BuildBar();
    }

    public static readonly BindableProperty DocumentProperty = BindableProperty.Create(
        nameof(Document),
        typeof(WordDocument),
        typeof(DocumentEditorView),
        propertyChanged: (b, _, value) =>
        {
            var view = (DocumentEditorView)b;
            view.editor.Document = (WordDocument?)value;
            view.AttachController();
        });

    public static readonly BindableProperty ThemeProperty = BindableProperty.Create(
        nameof(Theme),
        typeof(DocumentTheme),
        typeof(DocumentEditorView),
        DocumentTheme.Light,
        propertyChanged: (b, _, value) => ((DocumentEditorView)b).editor.Theme = (DocumentTheme)value);

    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom),
        typeof(double),
        typeof(DocumentEditorView),
        1.0,
        propertyChanged: (b, _, value) => ((DocumentEditorView)b).editor.Zoom = (double)value);

    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(DocumentEditorView),
        false,
        propertyChanged: (b, _, value) =>
        {
            var view = (DocumentEditorView)b;
            view.editor.IsReadOnly = (bool)value;
            view.RefreshBar();
        });

    /// <summary>Passed straight through to the inner <see cref="DocumentEditor"/>.</summary>
    public static readonly BindableProperty SpellCheckerProperty = BindableProperty.Create(
        nameof(SpellChecker),
        typeof(ISpellChecker),
        typeof(DocumentEditorView),
        propertyChanged: (b, _, value) => ((DocumentEditorView)b).editor.SpellChecker = (ISpellChecker?)value);

    public static readonly BindableProperty IsSpellCheckEnabledProperty = BindableProperty.Create(
        nameof(IsSpellCheckEnabled),
        typeof(bool),
        typeof(DocumentEditorView),
        true,
        propertyChanged: (b, _, value) => ((DocumentEditorView)b).editor.IsSpellCheckEnabled = (bool)value);

    public static readonly BindableProperty ShowToolbarProperty = BindableProperty.Create(
        nameof(ShowToolbar),
        typeof(bool),
        typeof(DocumentEditorView),
        true,
        propertyChanged: (b, _, value) => ((DocumentEditorView)b).barScroller.IsVisible = (bool)value);

    public static readonly BindableProperty FontFamiliesProperty = BindableProperty.Create(
        nameof(FontFamilies),
        typeof(IList<string>),
        typeof(DocumentEditorView),
        propertyChanged: (b, _, _) => ((DocumentEditorView)b).BuildBar());

    public static readonly BindableProperty FontSizesProperty = BindableProperty.Create(
        nameof(FontSizes),
        typeof(IList<double>),
        typeof(DocumentEditorView),
        propertyChanged: (b, _, _) => ((DocumentEditorView)b).BuildBar());

    public WordDocument? Document
    {
        get => (WordDocument?)this.GetValue(DocumentProperty);
        set => this.SetValue(DocumentProperty, value);
    }

    public DocumentTheme Theme
    {
        get => (DocumentTheme)this.GetValue(ThemeProperty);
        set => this.SetValue(ThemeProperty, value);
    }

    public double Zoom
    {
        get => (double)this.GetValue(ZoomProperty);
        set => this.SetValue(ZoomProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)this.GetValue(IsReadOnlyProperty);
        set => this.SetValue(IsReadOnlyProperty, value);
    }

    public ISpellChecker? SpellChecker
    {
        get => (ISpellChecker?)this.GetValue(SpellCheckerProperty);
        set => this.SetValue(SpellCheckerProperty, value);
    }

    public bool IsSpellCheckEnabled
    {
        get => (bool)this.GetValue(IsSpellCheckEnabledProperty);
        set => this.SetValue(IsSpellCheckEnabledProperty, value);
    }

    public bool ShowToolbar
    {
        get => (bool)this.GetValue(ShowToolbarProperty);
        set => this.SetValue(ShowToolbarProperty, value);
    }

    /// <summary>Font families offered by the picker. Defaults to a small cross-platform set.</summary>
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

    /// <summary>The underlying editor, for focus and direct key routing.</summary>
    public DocumentEditor Editor => this.editor;

    public DocumentEditorController? Controller => this.editor.Controller;

    public event EventHandler? DocumentChanged;

    /// <summary>Routes a physical key press to the editor. See <see cref="DocumentEditor.HandleKey"/>.</summary>
    public bool HandleKey(EditorKey key, bool shift = false, bool control = false)
    {
        var handled = this.editor.HandleKey(key, shift, control);
        this.RefreshBar();
        return handled;
    }

    void BuildBar()
    {
        this.bar.Clear();

        this.fontPicker = this.CreateFontPicker();
        this.sizePicker = this.CreateSizePicker();

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
        this.bar.Add(this.alignJustify);
        this.bar.Add(Separator());
        this.bar.Add(this.undo);
        this.bar.Add(this.redo);
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

    static readonly IList<string> DefaultFontFamilies =
        ["Calibri", "Cambria", "Arial", "Times New Roman", "Georgia", "Verdana", "Courier New"];

    static readonly IList<double> DefaultFontSizes =
        [8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72];

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
        this.DocumentChanged?.Invoke(this, EventArgs.Empty);
    }

    void AttachController()
    {
        if (this.editor.Controller is { } controller)
            controller.Changed += (_, _) => this.RefreshBar();

        this.RefreshBar();
    }

    void OnDocumentChanged(object? sender, EventArgs e)
    {
        this.RefreshBar();
        this.DocumentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Reflects the formatting under the caret back into the toolbar.</summary>
    void RefreshBar()
    {
        var format = this.editor.Controller?.CaretFormat ?? CaretFormat.Default;
        var enabled = !this.IsReadOnly && this.Document is not null;

        SetActive(this.bold, format.Bold);
        SetActive(this.italic, format.Italic);
        SetActive(this.underline, format.Underline);
        SetActive(this.strike, format.Strike);

        SetActive(this.alignLeft, format.Alignment == TextAlignment.Left);
        SetActive(this.alignCenter, format.Alignment == TextAlignment.Center);
        SetActive(this.alignRight, format.Alignment == TextAlignment.Right);
        SetActive(this.alignJustify, format.Alignment == TextAlignment.Justify);

        foreach (var child in this.bar.Children.OfType<Button>())
            child.IsEnabled = enabled;

        this.undo.IsEnabled = enabled && (this.editor.Controller?.CanUndo ?? false);
        this.redo.IsEnabled = enabled && (this.editor.Controller?.CanRedo ?? false);

        // Writing the pickers' selection raises their change events, which would immediately re-apply
        // the format that was only being displayed - so the handlers are muted while they are updated.
        this.suppressPickerEvents = true;

        if (this.fontPicker is FontPickerButton font)
            font.SelectedFont = format.FontFamily;

        if (this.sizePicker is FontSizePickerButton size)
        {
            // Snap to the nearest offered size: a document can hold any value, the picker only some.
            var sizes = this.FontSizes ?? DefaultFontSizes;
            size.SelectedFontSize = sizes.OrderBy(x => Math.Abs(x - format.FontSize)).FirstOrDefault();
        }

        this.suppressPickerEvents = false;
    }

    static void SetActive(Button button, bool active)
        => button.BackgroundColor = active ? Color.FromArgb("#25639EB5") : Colors.Transparent;

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.editor.DocumentChanged -= this.OnDocumentChanged;
        this.editor.Dispose();

        GC.SuppressFinalize(this);
    }
}
