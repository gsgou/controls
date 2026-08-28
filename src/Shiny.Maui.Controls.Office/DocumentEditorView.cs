using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Icons;
using Shiny.Controls.Office.Packaging;
using Shiny.Controls.Office.Shapes;
using Shiny.Maui.Controls.ColorPicker;
using Shiny.Maui.Controls.FontPicker;
using Shiny.Controls.Office.Spreadsheet;
using TextAlignment = Shiny.Controls.Office.Text.TextAlignment;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// <see cref="DocumentEditor"/> with a formatting toolbar above it.
/// </summary>
/// <remarks>
/// <para>
/// The toolbar is built from MAUI primitives plus the core package's <c>FontPickerButton</c> and
/// <c>FontSizePickerButton</c>. MAUI has no <c>ShinyToolbar</c> — that control is Blazor-only — so the
/// bar itself is a scrolling row here, while the Blazor <c>DocumentEditorView</c> composes ShinyToolbar
/// for the same two slots. The API and behaviour match; only the internals differ.
/// </para>
/// <para>
/// Every plain button on it is an <see cref="OfficeToolbarButton"/> drawing from the shared
/// <see cref="OfficeIcons"/> set — one monochrome stroked weight, no colour of its own, the same
/// artwork the Blazor toolbar renders. The pickers are the exception, because a font, a size and a
/// colour have to show what they are currently set to.
/// </para>
/// </remarks>
public class DocumentEditorView : ContentView, IDisposable
{
    readonly DocumentEditor editor = new();
    readonly HorizontalStackLayout bar;
    readonly ScrollView barScroller;
    readonly Grid root;

    readonly OfficeToolbarButton bold;
    readonly OfficeToolbarButton italic;
    readonly OfficeToolbarButton underline;
    readonly OfficeToolbarButton strike;
    readonly OfficeToolbarButton undo;
    readonly OfficeToolbarButton redo;
    readonly OfficeToolbarButton alignLeft;
    readonly OfficeToolbarButton alignCenter;
    readonly OfficeToolbarButton alignRight;
    readonly OfficeToolbarButton alignJustify;
    readonly OfficeToolbarButton highlight;
    readonly OfficeToolbarButton insertShape;
    readonly OfficeToolbarButton insertTable;
    readonly OfficeToolbarButton insertPicture;
    readonly OfficeToolbarButton pageMargins;
    readonly ColorPickerButton textColor;
    readonly List<OfficeToolbarButton> buttons = [];

    View? fontPicker;
    View? sizePicker;
    bool suppressPickerEvents;
    bool disposed;

    public DocumentEditorView()
    {
        this.bold = this.MakeButton(OfficeIcon.Bold, "Bold (Ctrl+B)", () => this.editor.Controller?.ToggleBold());
        this.italic = this.MakeButton(OfficeIcon.Italic, "Italic (Ctrl+I)", () => this.editor.Controller?.ToggleItalic());
        this.underline = this.MakeButton(OfficeIcon.Underline, "Underline (Ctrl+U)", () => this.editor.Controller?.ToggleUnderline());
        this.strike = this.MakeButton(OfficeIcon.Strikethrough, "Strikethrough", () => this.editor.Controller?.ToggleStrikethrough());

        this.alignLeft = this.MakeButton(OfficeIcon.AlignLeft, "Align left", () => this.editor.Controller?.SetAlignment(TextAlignment.Left));
        this.alignCenter = this.MakeButton(OfficeIcon.AlignCenter, "Centre", () => this.editor.Controller?.SetAlignment(TextAlignment.Center));
        this.alignRight = this.MakeButton(OfficeIcon.AlignRight, "Align right", () => this.editor.Controller?.SetAlignment(TextAlignment.Right));
        this.alignJustify = this.MakeButton(OfficeIcon.AlignJustify, "Justify", () => this.editor.Controller?.SetAlignment(TextAlignment.Justify));

        this.highlight = this.MakeAsyncButton(OfficeIcon.Highlight, "Highlight", this.PickHighlightAsync);
        this.insertShape = this.MakeAsyncButton(OfficeIcon.Shape, "Shapes", this.InsertShapeAsync);
        this.insertTable = this.MakeAsyncButton(OfficeIcon.Table, "Table", this.InsertTableAsync);
        this.insertPicture = this.MakeAsyncButton(OfficeIcon.Picture, "Picture", this.InsertPictureAsync);

        this.pageMargins = this.MakeAsyncButton(OfficeIcon.PageMargins, "Page margins", this.PickPageMarginsAsync);

        this.undo = this.MakeButton(OfficeIcon.Undo, "Undo (Ctrl+Z)", () => this.editor.Controller?.Undo());
        this.redo = this.MakeButton(OfficeIcon.Redo, "Redo (Ctrl+Shift+Z)", () => this.editor.Controller?.Redo());

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
                new RowDefinition(GridLength.Star)
            }
        };

        this.root.Add(this.barScroller);
        this.root.Add(this.editor);
        Grid.SetRow(this.editor, 1);

        this.editor.DocumentChanged += this.OnDocumentChanged;
        this.Content = this.root;

        this.BuildBar();
        this.AttachDrop();
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
        typeof(DocumentEditorView),
        OfficeToolbarButton.TooltipsByDefault,
        propertyChanged: (b, _, value) =>
        {
            foreach (var button in ((DocumentEditorView)b).buttons)
                button.SetTooltipEnabled((bool)value);
        });

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

    /// <summary>Hover tooltips on the icon-only toolbar buttons. Desktop only by default.</summary>
    public bool ShowToolbarTooltips
    {
        get => (bool)this.GetValue(ShowToolbarTooltipsProperty);
        set => this.SetValue(ShowToolbarTooltipsProperty, value);
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

    /// <summary>Raised when a dropped or chosen file could not be inserted, so a host can say so.</summary>
    public event EventHandler<OfficeDropRejected>? DropRejected;

    public static readonly BindableProperty ShapeWidthProperty = BindableProperty.Create(
        nameof(ShapeWidth), typeof(double), typeof(DocumentEditorView), 160d);

    public static readonly BindableProperty ShapeHeightProperty = BindableProperty.Create(
        nameof(ShapeHeight), typeof(double), typeof(DocumentEditorView), 120d);

    public static readonly BindableProperty PictureWidthProperty = BindableProperty.Create(
        nameof(PictureWidth), typeof(double), typeof(DocumentEditorView), 240d);

    /// <summary>The size of shape the toolbar inserts, in pixels.</summary>
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

    /// <summary>How wide an inserted picture is, in pixels. Its height follows the image's own ratio.</summary>
    public double PictureWidth
    {
        get => (double)this.GetValue(PictureWidthProperty);
        set => this.SetValue(PictureWidthProperty, value);
    }

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
        this.bar.Add(this.textColor);
        this.bar.Add(this.highlight);
        this.bar.Add(Separator());
        this.bar.Add(this.insertShape);
        this.bar.Add(this.insertTable);
        this.bar.Add(this.insertPicture);
        this.bar.Add(Separator());
        this.bar.Add(this.alignLeft);
        this.bar.Add(this.alignCenter);
        this.bar.Add(this.alignRight);
        this.bar.Add(this.alignJustify);
        this.bar.Add(Separator());
        this.bar.Add(this.pageMargins);
        this.bar.Add(Separator());
        this.bar.Add(this.undo);
        this.bar.Add(this.redo);
    }

    /// <summary>
    /// The core package's colour picker, in its button form.
    /// </summary>
    /// <remarks>
    /// Not a row of preset swatches: a document's text can be any colour, and a fixed palette is a
    /// promise the format does not make. The button shows the colour at the caret and opens the full
    /// spectrum — the same control the Blazor toolbar puts in this slot.
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

    static readonly IList<string> DefaultFontFamilies =
        ["Calibri", "Cambria", "Arial", "Times New Roman", "Georgia", "Verdana", "Courier New"];

    static readonly IList<double> DefaultFontSizes =
        [8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 72];

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

    // ---- insert ----

    async Task PickHighlightAsync()
    {
        var (chosen, color) = await OfficeMenus.PickHighlightAsync(OfficeMenus.PageOf(this));
        if (!chosen)
            return;

        this.editor.Controller?.SetHighlight(color);
        this.AfterCommand();
    }

    /// <summary>
    /// Applies a margin preset to the whole document.
    /// </summary>
    /// <remarks>
    /// Enabled in both layouts, though only <see cref="DocumentPageLayout.Print"/> can show the
    /// result: the margins are the document's own and are written and saved either way, exactly as a
    /// page break is. Disabling it in reflow would hide a setting the file still has.
    /// </remarks>
    async Task PickPageMarginsAsync()
    {
        if (await OfficeMenus.PickPageMarginsAsync(OfficeMenus.PageOf(this)) is not { } margins)
            return;

        this.editor.Controller?.SetPageMargins(margins);
        this.AfterCommand();
    }

    async Task InsertShapeAsync()
    {
        if (await OfficeMenus.PickShapeAsync(OfficeMenus.PageOf(this)) is not { } geometry)
            return;

        this.editor.Controller?.InsertShape(geometry, this.ShapeWidth, this.ShapeHeight);
        this.AfterCommand();
    }

    async Task InsertTableAsync()
    {
        if (await OfficeMenus.PickTableAsync(OfficeMenus.PageOf(this)) is not { } size)
            return;

        this.editor.Controller?.InsertTable(size.Rows, size.Columns);
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

        if (image is null)
            return;

        this.InsertImage(image);
    }

    void InsertImage(OfficePickedImage image)
    {
        this.editor.Controller?.InsertImage(
            image.Data,
            image.ContentType,
            this.PictureWidth,
            name: Path.GetFileNameWithoutExtension(image.FileName));

        this.AfterCommand();
    }

    // ---- file drop ----

    /// <summary>
    /// Attaches the drop gesture to the editor surface.
    /// </summary>
    /// <remarks>
    /// On the editor rather than on this view, so a drop onto the toolbar is not a drop into the
    /// document — the toolbar is chrome, and dropping a picture on the Bold button should do nothing.
    /// </remarks>
    void AttachDrop()
    {
        var drop = new DropGestureRecognizer { AllowDrop = true };
        drop.Drop += this.OnDropAsync;
        this.editor.GestureRecognizers.Add(drop);
    }

    async void OnDropAsync(object? sender, DropEventArgs e)
    {
        if (this.IsReadOnly || this.Document is null)
            return;

        try
        {
            // The deferral that keeps the payload alive across the await is taken inside, on the one
            // platform that needs one — it lives on the platform args, not on these.
            foreach (var image in await OfficeFileDrop.ReadImagesAsync(e))
                this.InsertImage(image);
        }
        catch (Exception ex)
        {
            this.DropRejected?.Invoke(this, new OfficeDropRejected(string.Empty, ex.Message));
        }
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
        SetActive(this.highlight, format.Highlight is not null);

        SetActive(this.alignLeft, format.Alignment == TextAlignment.Left);
        SetActive(this.alignCenter, format.Alignment == TextAlignment.Center);
        SetActive(this.alignRight, format.Alignment == TextAlignment.Right);
        SetActive(this.alignJustify, format.Alignment == TextAlignment.Justify);

        foreach (var button in this.buttons)
            button.SetEnabled(enabled);

        this.undo.SetEnabled(enabled && (this.editor.Controller?.CanUndo ?? false));
        this.redo.SetEnabled(enabled && (this.editor.Controller?.CanRedo ?? false));

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

        this.textColor.SelectedColor = FromArgb(format.Color);
        this.textColor.IsEnabled = enabled;

        this.suppressPickerEvents = false;
    }

    static void SetActive(OfficeToolbarButton button, bool active) => button.IsActive = active;

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
