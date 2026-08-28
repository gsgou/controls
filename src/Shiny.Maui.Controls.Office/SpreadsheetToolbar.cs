using Shiny.Controls.Office.Icons;
using Shiny.Maui.Controls.ColorPicker;
using Shiny.Maui.Controls.FontPicker;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// The formatting bar above a <see cref="SpreadsheetView"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two halves. The left is what any editor has — font, size, bold, colour, alignment — and the right
/// is what only a spreadsheet has: number formats, decimal places, AutoSum and column fitting. The
/// second half is the reason this is a control rather than a copy of the document toolbar.
/// </para>
/// <para>
/// Everything it does goes through <see cref="SpreadsheetController"/>, so a button here is the same
/// undoable command a keyboard shortcut would raise — not a second path into the workbook. It also
/// means the bar can be used on its own: set <see cref="Controller"/> from
/// <see cref="SpreadsheetView.Controller"/> and put it wherever the app's chrome belongs.
/// </para>
/// <para>
/// The icons come from the shared Office set, so this bar, the document bar and the slide bar draw
/// the same mark for the same command on both hosts.
/// </para>
/// </remarks>
public class SpreadsheetToolbar : ContentView
{
    readonly HorizontalStackLayout bar;
    readonly ScrollView scroller;

    readonly OfficeToolbarButton bold;
    readonly OfficeToolbarButton italic;
    readonly OfficeToolbarButton underline;
    readonly OfficeToolbarButton strike;
    readonly OfficeToolbarButton alignLeft;
    readonly OfficeToolbarButton alignCenter;
    readonly OfficeToolbarButton alignRight;
    readonly OfficeToolbarButton alignTop;
    readonly OfficeToolbarButton alignMiddle;
    readonly OfficeToolbarButton alignBottom;
    readonly OfficeToolbarButton wrap;
    readonly OfficeToolbarButton currency;
    readonly OfficeToolbarButton percent;
    readonly OfficeToolbarButton decimalDecrease;
    readonly OfficeToolbarButton decimalIncrease;
    readonly OfficeToolbarButton numberFormats;
    readonly OfficeToolbarButton sum;
    readonly OfficeToolbarButton autoFunctions;
    readonly OfficeToolbarButton fill;
    readonly OfficeToolbarButton autoFit;
    readonly OfficeToolbarButton clearFormat;
    readonly OfficeToolbarButton undo;
    readonly OfficeToolbarButton redo;

    readonly ColorPickerButton textColor;

    FontPickerButton? fontPicker;
    FontSizePickerButton? sizePicker;
    SpreadsheetController? controller;
    bool suppressPickerEvents;

    public SpreadsheetToolbar()
    {
        this.bold = this.Make(OfficeIcon.Bold, "Bold", c => c.ToggleBold());
        this.italic = this.Make(OfficeIcon.Italic, "Italic", c => c.ToggleItalic());
        this.underline = this.Make(OfficeIcon.Underline, "Underline", c => c.ToggleUnderline());
        this.strike = this.Make(OfficeIcon.Strikethrough, "Strikethrough", c => c.ToggleStrikethrough());

        this.alignLeft = this.Make(OfficeIcon.AlignLeft, "Align left", c => c.SetAlignment(CellHorizontalAlignment.Left));
        this.alignCenter = this.Make(OfficeIcon.AlignCenter, "Centre", c => c.SetAlignment(CellHorizontalAlignment.Center));
        this.alignRight = this.Make(OfficeIcon.AlignRight, "Align right", c => c.SetAlignment(CellHorizontalAlignment.Right));

        this.alignTop = this.Make(OfficeIcon.AlignTop, "Align top", c => c.SetVerticalAlignment(CellVerticalAlignment.Top));
        this.alignMiddle = this.Make(OfficeIcon.AlignMiddle, "Align middle", c => c.SetVerticalAlignment(CellVerticalAlignment.Center));
        this.alignBottom = this.Make(OfficeIcon.AlignBottom, "Align bottom", c => c.SetVerticalAlignment(CellVerticalAlignment.Bottom));

        this.wrap = this.Make(OfficeIcon.WrapText, "Wrap text", c => c.ToggleWrapText());

        this.currency = this.Make(OfficeIcon.Currency, "Currency", c => c.SetNumberFormat(NumberFormatPreset.Currency));
        this.percent = this.Make(OfficeIcon.Percent, "Percent", c => c.SetNumberFormat(NumberFormatPreset.Percent));
        this.decimalDecrease = this.Make(OfficeIcon.DecimalDecrease, "Fewer decimal places", c => c.AdjustDecimals(-1));
        this.decimalIncrease = this.Make(OfficeIcon.DecimalIncrease, "More decimal places", c => c.AdjustDecimals(1));

        this.sum = this.Make(OfficeIcon.Sum, "AutoSum", c => c.ApplyAutoFunction(AutoFunction.Sum));

        this.fill = this.Make(OfficeIcon.FillColor, "Fill colour", null);
        this.fill.Clicked += async (_, _) => await this.PickFillAsync();

        this.numberFormats = this.Make(OfficeIcon.Chevron, "More number formats", null);
        this.numberFormats.Clicked += async (_, _) => await this.PickNumberFormatAsync();

        this.autoFunctions = this.Make(OfficeIcon.Chevron, "More auto functions", null);
        this.autoFunctions.Clicked += async (_, _) => await this.PickAutoFunctionAsync();

        this.autoFit = this.Make(OfficeIcon.ColumnWidth, "Fit column to contents", c => c.AutoFitColumns());
        this.clearFormat = this.Make(OfficeIcon.ClearFormat, "Clear formatting", c => c.ClearFormatting());

        this.undo = this.Make(OfficeIcon.Undo, "Undo", c => c.Undo());
        this.redo = this.Make(OfficeIcon.Redo, "Redo", c => c.Redo());

        this.textColor = this.CreateColorPicker();

        this.bar = new HorizontalStackLayout { Spacing = 4, Padding = new Thickness(8, 6) };
        this.scroller = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = this.bar
        };

        this.Content = this.scroller;
        this.BuildBar();
    }

    public static readonly BindableProperty ThemeProperty = BindableProperty.Create(
        nameof(Theme),
        typeof(SpreadsheetTheme),
        typeof(SpreadsheetToolbar),
        SpreadsheetTheme.Light,
        propertyChanged: (b, _, _) => ((SpreadsheetToolbar)b).Refresh());

    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(SpreadsheetToolbar),
        false,
        propertyChanged: (b, _, _) => ((SpreadsheetToolbar)b).Refresh());

    public static readonly BindableProperty FontFamiliesProperty = BindableProperty.Create(
        nameof(FontFamilies),
        typeof(IList<string>),
        typeof(SpreadsheetToolbar),
        propertyChanged: (b, _, _) => ((SpreadsheetToolbar)b).BuildBar());

    public static readonly BindableProperty FontSizesProperty = BindableProperty.Create(
        nameof(FontSizes),
        typeof(IList<double>),
        typeof(SpreadsheetToolbar),
        propertyChanged: (b, _, _) => ((SpreadsheetToolbar)b).BuildBar());

    public static readonly BindableProperty ShowTooltipsProperty = BindableProperty.Create(
        nameof(ShowTooltips),
        typeof(bool),
        typeof(SpreadsheetToolbar),
        OfficeToolbarButton.TooltipsByDefault,
        propertyChanged: (b, _, _) => ((SpreadsheetToolbar)b).Refresh());

    public SpreadsheetTheme Theme
    {
        get => (SpreadsheetTheme)this.GetValue(ThemeProperty);
        set => this.SetValue(ThemeProperty, value);
    }

    /// <summary>Shows the current formatting but refuses to change it.</summary>
    public bool IsReadOnly
    {
        get => (bool)this.GetValue(IsReadOnlyProperty);
        set => this.SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Font families offered by the picker. Defaults to the set Excel ships with.</summary>
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

    /// <summary>
    /// Whether the buttons show a hover tooltip. On everywhere but phones by default.
    /// </summary>
    /// <remarks>
    /// The buttons are icon-only, so the tooltip is the only place the command is named. It is off on
    /// iOS and Android because a hover tooltip has nothing to open it there, not because the label
    /// matters less — which is why the accessible description is set regardless.
    /// </remarks>
    public bool ShowTooltips
    {
        get => (bool)this.GetValue(ShowTooltipsProperty);
        set => this.SetValue(ShowTooltipsProperty, value);
    }

    /// <summary>The grid this bar drives. Set by <see cref="SpreadsheetView"/>.</summary>
    public SpreadsheetController? Controller
    {
        get => this.controller;
        set
        {
            if (ReferenceEquals(this.controller, value))
                return;

            if (this.controller is not null)
                this.controller.Changed -= this.OnControllerChanged;

            this.controller = value;

            if (this.controller is not null)
                this.controller.Changed += this.OnControllerChanged;

            this.Refresh();
        }
    }

    /// <summary>Raised after a command runs, so a host can repaint and track the dirty state.</summary>
    public event EventHandler? Changed;

    /// <summary>Extra views appended after the built-in controls.</summary>
    /// <remarks>
    /// A list rather than a template because the bar is rebuilt whenever the font lists change, and
    /// the extras have to survive that. Assign it before the toolbar is first laid out.
    /// </remarks>
    public IList<View> ToolbarItems { get; } = new List<View>();

    /// <summary>Detaches from the controller. Called by the hosting view when it is disposed.</summary>
    public void Detach() => this.Controller = null;

    void BuildBar()
    {
        this.bar.Clear();

        this.fontPicker = this.CreateFontPicker();
        this.sizePicker = this.CreateSizePicker();

        this.bar.Add(this.fontPicker);
        this.bar.Add(this.sizePicker);
        this.bar.Add(Separator());

        this.bar.Add(this.bold);
        this.bar.Add(this.italic);
        this.bar.Add(this.underline);
        this.bar.Add(this.strike);
        this.bar.Add(Separator());

        this.bar.Add(this.textColor);
        this.bar.Add(this.fill);
        this.bar.Add(Separator());

        this.bar.Add(this.alignLeft);
        this.bar.Add(this.alignCenter);
        this.bar.Add(this.alignRight);

        // A separator between the two axes, not decoration: both sets are rules on a grid, and side
        // by side without a break the six of them read as one run of six horizontal alignments.
        this.bar.Add(Separator());

        this.bar.Add(this.alignTop);
        this.bar.Add(this.alignMiddle);
        this.bar.Add(this.alignBottom);
        this.bar.Add(this.wrap);
        this.bar.Add(Separator());

        this.bar.Add(this.currency);
        this.bar.Add(this.percent);
        this.bar.Add(this.decimalDecrease);
        this.bar.Add(this.decimalIncrease);
        this.bar.Add(this.numberFormats);
        this.bar.Add(Separator());

        this.bar.Add(this.sum);
        this.bar.Add(this.autoFunctions);
        this.bar.Add(Separator());

        this.bar.Add(this.autoFit);
        this.bar.Add(this.clearFormat);
        this.bar.Add(Separator());

        this.bar.Add(this.undo);
        this.bar.Add(this.redo);

        foreach (var item in this.ToolbarItems)
            this.bar.Add(item);

        this.Refresh();
    }

    OfficeToolbarButton Make(OfficeIcon icon, string hint, Action<SpreadsheetController>? action)
    {
        var button = new OfficeToolbarButton(icon, hint);

        if (action is not null)
        {
            button.Clicked += (_, _) =>
            {
                if (this.controller is { } current && !this.IsReadOnly)
                {
                    action(current);
                    this.AfterCommand();
                }
            };
        }

        return button;
    }

    ColorPickerButton CreateColorPicker()
    {
        var picker = new ColorPickerButton
        {
            Text = string.Empty,
            ShowOpacity = false,
            WidthRequest = 44,
            HeightRequest = OfficeToolbarButton.ItemHeight,
            VerticalOptions = LayoutOptions.Center
        };

        picker.ColorChanged += (_, color) =>
        {
            if (this.suppressPickerEvents || this.controller is not { } current || this.IsReadOnly)
                return;

            current.SetTextColor(ToArgb(color));
            this.AfterCommand();
        };

        return picker;
    }

    FontPickerButton CreateFontPicker()
    {
        var picker = new FontPickerButton
        {
            AvailableFonts = (this.FontFamilies ?? DefaultFontFamilies).ToList(),
            Placeholder = "Font",
            WidthRequest = 150,
            HeightRequest = OfficeToolbarButton.ItemHeight,
            VerticalOptions = LayoutOptions.Center
        };

        picker.FontChanged += (_, family) =>
        {
            if (this.suppressPickerEvents || this.controller is not { } current || this.IsReadOnly)
                return;

            current.SetFontFamily(family);
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
            HeightRequest = OfficeToolbarButton.ItemHeight,
            VerticalOptions = LayoutOptions.Center
        };

        picker.FontSizeChanged += (_, size) =>
        {
            if (this.suppressPickerEvents || this.controller is not { } current || this.IsReadOnly)
                return;

            current.SetFontSize(size);
            this.AfterCommand();
        };

        return picker;
    }

    async Task PickFillAsync()
    {
        if (this.controller is not { } current || this.IsReadOnly)
            return;

        // The same gallery the document toolbar's highlight button opens, and for the same reason: a
        // cell fill wants a few readable colours plus a way to remove it, not a colour spectrum.
        var (chosen, color) = await OfficeMenus.PickHighlightAsync(OfficeMenus.PageOf(this));
        if (!chosen)
            return;

        current.SetFillColor(color);
        this.AfterCommand();
    }

    async Task PickNumberFormatAsync()
    {
        if (this.controller is not { } current || this.IsReadOnly)
            return;

        if (await SpreadsheetMenus.PickNumberFormatAsync(OfficeMenus.PageOf(this)) is not { } preset)
            return;

        current.SetNumberFormat(preset);
        this.AfterCommand();
    }

    async Task PickAutoFunctionAsync()
    {
        if (this.controller is not { } current || this.IsReadOnly)
            return;

        if (await SpreadsheetMenus.PickAutoFunctionAsync(OfficeMenus.PageOf(this)) is not { } function)
            return;

        current.ApplyAutoFunction(function);
        this.AfterCommand();
    }

    void OnControllerChanged(object? sender, EventArgs e) => this.Refresh();

    void AfterCommand()
    {
        this.Refresh();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Reflects the active cell's formatting back into the bar.</summary>
    void Refresh()
    {
        var format = this.controller?.ActiveFormat ?? ResolvedFormat.Default;
        var enabled = this.controller is not null && !this.IsReadOnly;

        this.IsVisible = this.controller is not null;

        var theme = this.Theme;
        this.BackgroundColor = Color.FromRgba(theme.Background.R, theme.Background.G, theme.Background.B, theme.Background.A);

        this.bold.IsActive = format.Bold;
        this.italic.IsActive = format.Italic;
        this.underline.IsActive = format.Underline;
        this.strike.IsActive = format.Strike;
        this.wrap.IsActive = format.WrapText;

        this.alignLeft.IsActive = format.HorizontalAlignment == CellHorizontalAlignment.Left;
        this.alignCenter.IsActive = format.HorizontalAlignment == CellHorizontalAlignment.Center;
        this.alignRight.IsActive = format.HorizontalAlignment == CellHorizontalAlignment.Right;

        this.alignTop.IsActive = format.VerticalAlignment == CellVerticalAlignment.Top;
        this.alignMiddle.IsActive = format.VerticalAlignment == CellVerticalAlignment.Center;
        this.alignBottom.IsActive = format.VerticalAlignment == CellVerticalAlignment.Bottom;

        var preset = NumberFormats.PresetOf(format.NumberFormatCode);
        this.currency.IsActive = preset == NumberFormatPreset.Currency;
        this.percent.IsActive = preset == NumberFormatPreset.Percent;
        this.fill.IsActive = !format.Background.IsTransparent;

        foreach (var button in this.bar.Children.OfType<OfficeToolbarButton>())
        {
            button.SetEnabled(enabled);
            button.SetTooltipEnabled(this.ShowTooltips);
        }

        this.undo.SetEnabled(enabled && (this.controller?.CanUndo ?? false));
        this.redo.SetEnabled(enabled && (this.controller?.CanRedo ?? false));

        // Writing a picker's selection raises its change event, which would immediately re-apply the
        // format that was only being displayed.
        this.suppressPickerEvents = true;

        if (this.fontPicker is not null)
            this.fontPicker.SelectedFont = format.FontName;

        if (this.sizePicker is not null)
        {
            // Snap to the nearest offered size: a cell can hold any value, the picker only some.
            var sizes = this.FontSizes ?? DefaultFontSizes;
            this.sizePicker.SelectedFontSize = sizes.OrderBy(x => Math.Abs(x - format.FontSize)).FirstOrDefault();
        }

        this.textColor.SelectedColor = Color.FromRgba(format.Foreground.R, format.Foreground.G, format.Foreground.B, format.Foreground.A);
        this.textColor.IsEnabled = enabled;

        if (this.fontPicker is not null)
            this.fontPicker.IsEnabled = enabled;

        if (this.sizePicker is not null)
            this.sizePicker.IsEnabled = enabled;

        this.suppressPickerEvents = false;
    }

    /// <summary>MAUI colours are floats in 0..1; the spreadsheet kernel stores bytes.</summary>
    static ArgbColor ToArgb(Color color) => new(
        (byte)Math.Round(color.Alpha * 255),
        (byte)Math.Round(color.Red * 255),
        (byte)Math.Round(color.Green * 255),
        (byte)Math.Round(color.Blue * 255));

    static BoxView Separator() => new()
    {
        WidthRequest = 1,
        HeightRequest = 22,
        Color = Colors.Gray,
        Opacity = 0.35,
        VerticalOptions = LayoutOptions.Center,
        Margin = new Thickness(3, 0)
    };

    /// <summary>Excel's own default plus the faces most workbooks actually use.</summary>
    static readonly IList<string> DefaultFontFamilies =
        ["Calibri", "Cambria", "Arial", "Times New Roman", "Georgia", "Verdana", "Courier New"];

    static readonly IList<double> DefaultFontSizes =
        [8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36, 48, 72];
}
