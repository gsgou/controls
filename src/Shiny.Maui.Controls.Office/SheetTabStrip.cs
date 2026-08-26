namespace Shiny.Maui.Controls.Office;

/// <summary>
/// The row of sheet tabs under a workbook grid.
/// </summary>
/// <remarks>
/// <para>
/// Below the grid rather than above it, where every spreadsheet program has put it for thirty years.
/// Hidden sheets are left out of the row — they are hidden in Excel too — and reachable instead
/// through the overflow button at the end, which is the only place anything can be unhidden from.
/// </para>
/// <para>
/// Every action goes through <see cref="SpreadsheetController"/>, so every action lands on the same
/// undo stack as a cell edit. Nothing here touches a workbook directly.
/// </para>
/// </remarks>
public class SheetTabStrip : ContentView
{
    readonly HorizontalStackLayout tabs;
    readonly Button add;
    readonly Button overflow;
    readonly Border frame;

    SpreadsheetController? controller;

    public SheetTabStrip()
    {
        this.tabs = new HorizontalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Fill };

        this.add = new Button { Text = "+", WidthRequest = 34, Padding = 0 };
        this.add.Clicked += this.OnAdd;

        this.overflow = new Button { Text = "⋯", WidthRequest = 34, Padding = 0 };
        this.overflow.Clicked += this.OnOverflow;

        var row = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            ],
            ColumnSpacing = 2,
            Padding = new Thickness(6, 3)
        };

        // The tabs scroll and the two buttons do not: with a dozen sheets the buttons must stay
        // reachable rather than being pushed off the end with everything else.
        row.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = this.tabs
        });

        row.Add(this.add, 1);
        row.Add(this.overflow, 2);

        this.frame = new Border
        {
            StrokeThickness = 0,
            Padding = 0,
            Content = row
        };

        this.Content = this.frame;
    }

    public static readonly BindableProperty ThemeProperty = BindableProperty.Create(
        nameof(Theme),
        typeof(SpreadsheetTheme),
        typeof(SheetTabStrip),
        SpreadsheetTheme.Light,
        propertyChanged: (b, _, _) => ((SheetTabStrip)b).Rebuild());

    public static readonly BindableProperty AllowEditingProperty = BindableProperty.Create(
        nameof(AllowEditing),
        typeof(bool),
        typeof(SheetTabStrip),
        true,
        propertyChanged: (b, _, _) => ((SheetTabStrip)b).Rebuild());

    public SpreadsheetTheme Theme
    {
        get => (SpreadsheetTheme)this.GetValue(ThemeProperty);
        set => this.SetValue(ThemeProperty, value);
    }

    /// <summary>
    /// Whether tabs can be added, renamed, reordered, hidden and deleted, as opposed to only switched
    /// between.
    /// </summary>
    public bool AllowEditing
    {
        get => (bool)this.GetValue(AllowEditingProperty);
        set => this.SetValue(AllowEditingProperty, value);
    }

    /// <summary>The grid this strip belongs to. Set by <see cref="SpreadsheetView"/>.</summary>
    public SpreadsheetController? Controller
    {
        get => this.controller;
        set
        {
            if (ReferenceEquals(this.controller, value))
                return;

            if (this.controller is not null)
                this.controller.ActiveSheetChanged -= this.OnActiveSheetChanged;

            this.controller = value;

            if (this.controller is not null)
                this.controller.ActiveSheetChanged += this.OnActiveSheetChanged;

            this.Rebuild();
        }
    }

    /// <summary>Raised after anything that changes which sheet is showing or what sheets exist.</summary>
    public event EventHandler? Changed;

    void OnActiveSheetChanged(object? sender, Worksheet sheet) => this.Rebuild();

    /// <summary>Redraws the row from the workbook. Cheap enough to do wholesale; sheets are few.</summary>
    public void Rebuild()
    {
        var theme = this.Theme;
        this.frame.BackgroundColor = ToColor(theme.HeaderBackground);
        this.add.IsVisible = this.AllowEditing;

        this.tabs.Clear();

        if (this.controller is not { } current)
        {
            this.overflow.IsVisible = false;
            this.IsVisible = false;
            return;
        }

        this.IsVisible = true;
        this.overflow.IsVisible = this.AllowEditing || current.Workbook.Sheets.Any(x => !x.IsVisible);

        foreach (var sheet in current.VisibleSheets)
            this.tabs.Add(this.BuildTab(sheet, ReferenceEquals(sheet, current.Sheet), theme));
    }

    View BuildTab(Worksheet sheet, bool on, SpreadsheetTheme theme)
    {
        var label = new Label
        {
            Text = sheet.Name,
            FontSize = 13,
            FontAttributes = on ? FontAttributes.Bold : FontAttributes.None,
            TextColor = ToColor(on ? theme.CellText : theme.HeaderText),
            VerticalOptions = LayoutOptions.Center
        };

        var tab = new Border
        {
            Padding = new Thickness(12, 5),
            StrokeThickness = on ? 1 : 0,
            Stroke = ToColor(theme.HeaderBorder),

            // The current tab reads as continuous with the grid above it, which is what the matching
            // background is doing - it is not decoration.
            BackgroundColor = on ? ToColor(theme.Background) : Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(4, 4, 0, 0) },
            Content = label
        };

        var tap = new TapGestureRecognizer();

        // Tapping the tab you are already on opens its actions. There is no right-click on a phone,
        // and a long press is not discoverable enough to be the only way in.
        tap.Tapped += (_, _) =>
        {
            if (on)
                _ = this.ShowSheetMenuAsync(sheet);
            else
                this.Switch(sheet);
        };

        tab.GestureRecognizers.Add(tap);
        return tab;
    }

    void Switch(Worksheet sheet)
    {
        this.controller?.SwitchSheet(sheet);
        this.Rebuild();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    async void OnAdd(object? sender, EventArgs e)
    {
        if (this.controller is not { } current || !this.AllowEditing)
            return;

        var added = current.AddSheet();
        this.Rebuild();
        this.Changed?.Invoke(this, EventArgs.Empty);

        // Straight into a rename: a new tab is almost always about to be named something.
        await this.RenameAsync(added);
    }

    async void OnOverflow(object? sender, EventArgs e)
    {
        if (this.controller is not { } current)
            return;

        var hidden = current.Workbook.Sheets.Where(x => !x.IsVisible).ToList();
        if (this.Page is not { } page)
            return;

        var options = new List<string>();
        if (this.AllowEditing)
            options.Add($"Sheet actions…");

        options.AddRange(hidden.Select(x => $"Show “{x.Name}”"));

        if (options.Count == 0)
            return;

        var choice = await page.DisplayActionSheetAsync("Sheets", "Cancel", null, [.. options]);
        if (choice is null or "Cancel")
            return;

        if (this.AllowEditing && choice == options[0])
        {
            await this.ShowSheetMenuAsync(current.Sheet);
            return;
        }

        var target = hidden.FirstOrDefault(x => choice == $"Show “{x.Name}”");
        if (target is not null)
            this.Act(() => current.SetSheetVisible(target, true));
    }

    async Task ShowSheetMenuAsync(Worksheet sheet)
    {
        if (this.controller is not { } current || !this.AllowEditing || this.Page is not { } page)
            return;

        var position = current.IndexOf(sheet);
        var removable = current.CanRemoveFromView(sheet);

        var options = new List<string> { "Rename", "Duplicate" };

        if (position > 0)
            options.Add("Move left");

        if (position >= 0 && position < current.Workbook.Sheets.Count - 1)
            options.Add("Move right");

        if (removable)
            options.Add("Hide");

        var choice = await page.DisplayActionSheetAsync(
            sheet.Name,
            "Cancel",
            removable ? "Delete" : null,
            [.. options]);

        switch (choice)
        {
            case "Rename":
                await this.RenameAsync(sheet);
                break;

            case "Duplicate":
                this.Act(() => current.DuplicateSheet(sheet));
                break;

            case "Move left":
                this.Act(() => current.MoveSheet(sheet, position - 1));
                break;

            case "Move right":
                this.Act(() => current.MoveSheet(sheet, position + 1));
                break;

            case "Hide":
                this.Act(() => current.SetSheetVisible(sheet, false));
                break;

            case "Delete":
                if (await page.DisplayAlertAsync("Delete sheet", $"Delete “{sheet.Name}” and everything on it?", "Delete", "Cancel"))
                    this.Act(() => current.DeleteSheet(sheet));

                break;
        }
    }

    async Task RenameAsync(Worksheet sheet)
    {
        if (this.controller is not { } current || this.Page is not { } page)
            return;

        var typed = await page.DisplayPromptAsync(
            "Rename sheet",
            "Name",
            "Rename",
            "Cancel",
            initialValue: sheet.Name,
            maxLength: SheetNames.MaxLength);

        if (typed is null)
            return;

        // Checked before it is applied so the reason can be shown; the command would only throw.
        if (!SheetNames.IsAvailable(typed.Trim(), current.Workbook.Sheets.Select(x => x.Name), sheet.Name, out var error))
        {
            await page.DisplayAlertAsync("Rename sheet", error, "OK");
            await this.RenameAsync(sheet);
            return;
        }

        this.Act(() => current.RenameSheet(sheet, typed.Trim()));
    }

    /// <summary>Runs a sheet edit, surfacing the workbook's refusal rather than throwing out of a handler.</summary>
    void Act(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _ = this.Page?.DisplayAlertAsync("Sheets", ex.Message, "OK");
            return;
        }

        this.Rebuild();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The page hosting this strip, which is what MAUI's dialogs are asked of.</summary>
    Page? Page => this.Window?.Page;

    static Color ToColor(ArgbColor color) => Color.FromRgba(color.R, color.G, color.B, color.A);
}
