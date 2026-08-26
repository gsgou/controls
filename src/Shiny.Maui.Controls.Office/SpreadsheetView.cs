using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Displays and edits an <c>.xlsx</c> worksheet.
/// </summary>
/// <remarks>
/// <para>
/// The grid is painted by the same <see cref="SpreadsheetPainter"/> the Blazor host uses, and driven by
/// the same <see cref="SpreadsheetController"/>. This class owns only two things MAUI has to provide:
/// a Skia surface, and a real <see cref="Entry"/> to host the in-cell editor so the platform's soft
/// keyboard and IME work without a custom text stack.
/// </para>
/// <para>
/// Requires <c>UseSkiaSharp()</c> in <c>MauiProgram</c>.
/// </para>
/// </remarks>
public class SpreadsheetView : ContentView, IDisposable
{
    readonly SKCanvasView canvas;
    readonly Entry editor;
    readonly AbsoluteLayout root;
    readonly SheetTabStrip sheetTabs;
    readonly FormulaBar formulaBar;
    readonly Grid layout;
    readonly SpreadsheetPainter painter = new();

    SpreadsheetController? controller;
    bool suppressEditorEvents;
    bool disposed;

    public SpreadsheetView()
    {
        this.canvas = new SKCanvasView { EnableTouchEvents = true };
        this.canvas.PaintSurface += this.OnPaintSurface;
        this.canvas.Touch += this.OnTouch;

        this.editor = new Entry
        {
            IsVisible = false,
            Margin = 0,
            ReturnType = ReturnType.Done
        };

        this.editor.TextChanged += this.OnEditorTextChanged;
        this.editor.Completed += this.OnEditorCompleted;
        this.editor.Unfocused += this.OnEditorUnfocused;

        this.root = new AbsoluteLayout();
        this.root.Add(this.canvas);
        AbsoluteLayout.SetLayoutFlags(this.canvas, AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(this.canvas, new Rect(0, 0, 1, 1));
        this.root.Add(this.editor);

        this.sheetTabs = new SheetTabStrip();
        this.sheetTabs.Changed += this.OnSheetTabsChanged;

        this.formulaBar = new FormulaBar();
        this.formulaBar.Changed += this.OnSheetTabsChanged;

        this.layout = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            ]
        };

        this.layout.Add(this.formulaBar);
        this.layout.Add(this.root, 0, 1);
        this.layout.Add(this.sheetTabs, 0, 2);

        // The canvas no longer fills this view - the strip takes a slice off the bottom - so the grid
        // has to be sized from the canvas rather than from the control, or every pointer coordinate
        // and every visible row is out by the height of the tabs.
        this.canvas.SizeChanged += this.OnCanvasSizeChanged;

        this.Content = this.layout;
    }

    public static readonly BindableProperty WorkbookProperty = BindableProperty.Create(
        nameof(Workbook),
        typeof(Workbook),
        typeof(SpreadsheetView),
        propertyChanged: (b, _, _) => ((SpreadsheetView)b).Rebuild());

    public static readonly BindableProperty SheetNameProperty = BindableProperty.Create(
        nameof(SheetName),
        typeof(string),
        typeof(SpreadsheetView),
        propertyChanged: (b, _, _) => ((SpreadsheetView)b).Rebuild());

    public static readonly BindableProperty ThemeProperty = BindableProperty.Create(
        nameof(Theme),
        typeof(SpreadsheetTheme),
        typeof(SpreadsheetView),
        SpreadsheetTheme.Light,
        propertyChanged: (b, _, value) =>
        {
            var view = (SpreadsheetView)b;
            view.sheetTabs.Theme = (SpreadsheetTheme)value;
            view.formulaBar.Theme = (SpreadsheetTheme)value;
            view.Invalidate();
        });

    public static readonly BindableProperty ShowFormulaBarProperty = BindableProperty.Create(
        nameof(ShowFormulaBar),
        typeof(bool),
        typeof(SpreadsheetView),
        true,
        propertyChanged: (b, _, _) => ((SpreadsheetView)b).UpdateChrome());

    public static readonly BindableProperty ShowSheetTabsProperty = BindableProperty.Create(
        nameof(ShowSheetTabs),
        typeof(bool),
        typeof(SpreadsheetView),
        true,
        propertyChanged: (b, _, _) => ((SpreadsheetView)b).UpdateChrome());

    public static readonly BindableProperty AllowSheetEditingProperty = BindableProperty.Create(
        nameof(AllowSheetEditing),
        typeof(bool),
        typeof(SpreadsheetView),
        true,
        propertyChanged: (b, _, _) => ((SpreadsheetView)b).UpdateChrome());

    public Workbook? Workbook
    {
        get => (Workbook?)this.GetValue(WorkbookProperty);
        set => this.SetValue(WorkbookProperty, value);
    }

    public string? SheetName
    {
        get => (string?)this.GetValue(SheetNameProperty);
        set => this.SetValue(SheetNameProperty, value);
    }

    public SpreadsheetTheme Theme
    {
        get => (SpreadsheetTheme)this.GetValue(ThemeProperty);
        set => this.SetValue(ThemeProperty, value);
    }

    /// <summary>
    /// Whether to show the strip of sheet tabs under the grid. On by default: a workbook with no way
    /// to reach its other sheets is a worse default than a single tab that does nothing.
    /// </summary>
    public bool ShowSheetTabs
    {
        get => (bool)this.GetValue(ShowSheetTabsProperty);
        set => this.SetValue(ShowSheetTabsProperty, value);
    }

    /// <summary>
    /// Whether the tab strip can add, rename, reorder, hide and delete sheets, as opposed to only
    /// switching between them.
    /// </summary>
    public bool AllowSheetEditing
    {
        get => (bool)this.GetValue(AllowSheetEditingProperty);
        set => this.SetValue(AllowSheetEditingProperty, value);
    }

    /// <summary>The live controller, so a toolbar or formula bar can drive the same state.</summary>
    public SpreadsheetController? Controller => this.controller;

    /// <summary>Raised after a cell is committed.</summary>
    public event EventHandler<CellRef>? CellChanged;

    /// <summary>Raised when the sheet on screen changes, by a tab tap or by a sheet edit.</summary>
    public event EventHandler<Worksheet>? ActiveSheetChanged;

    /// <summary>
    /// Whether to show the name box and formula field above the grid.
    /// </summary>
    /// <remarks>
    /// On by default. The grid paints the <em>result</em> of a formula, so without this a cell reading
    /// 84 gives no way to discover that it holds <c>=B1*2</c> — and no way to edit it as a formula
    /// rather than retyping it from scratch.
    /// </remarks>
    public bool ShowFormulaBar
    {
        get => (bool)this.GetValue(ShowFormulaBarProperty);
        set => this.SetValue(ShowFormulaBarProperty, value);
    }

    /// <summary>The tab strip, exposed so a host can hide or restyle it beyond the two properties above.</summary>
    public SheetTabStrip SheetTabs => this.sheetTabs;

    /// <summary>The formula bar, exposed so a host can make it read-only or restyle it.</summary>
    public FormulaBar FormulaBar => this.formulaBar;

    void Rebuild()
    {
        var workbook = this.Workbook;
        if (workbook is null)
        {
            this.DetachController();
            this.controller = null;
            this.UpdateChrome();
            this.Invalidate();
            return;
        }

        var sheet = this.SheetName is null
            ? workbook.Sheets.FirstOrDefault()
            : workbook.Sheets.FirstOrDefault(x => x.Name == this.SheetName);

        if (sheet is null)
        {
            this.DetachController();
            this.controller = null;
            this.UpdateChrome();
            this.Invalidate();
            return;
        }

        // Switch rather than rebuild when it is the same workbook. A new controller would throw away
        // every sheet's remembered selection, scroll position and column widths - and since the tab
        // strip writes the sheet name back through this property, that would happen on every tap.
        if (this.controller is { } existing && ReferenceEquals(existing.Workbook, workbook))
        {
            if (!ReferenceEquals(existing.Sheet, sheet))
                existing.SwitchSheet(sheet);

            this.UpdateChrome();
            this.Invalidate();
            return;
        }

        this.DetachController();
        this.controller = new SpreadsheetController(workbook, sheet);
        this.controller.Changed += this.OnControllerChanged;
        this.controller.EditingChanged += this.OnEditingChanged;
        this.controller.ActiveSheetChanged += this.OnActiveSheetChanged;
        this.controller.Resize(
            this.canvas.Width > 0 ? this.canvas.Width : 800,
            this.canvas.Height > 0 ? this.canvas.Height : 600);

        this.UpdateChrome();
        this.Invalidate();
    }

    void OnCanvasSizeChanged(object? sender, EventArgs e)
    {
        if (this.canvas.Width > 0 && this.canvas.Height > 0)
            this.controller?.Resize(this.canvas.Width, this.canvas.Height);
    }

    void UpdateChrome()
    {
        this.sheetTabs.AllowEditing = this.AllowSheetEditing;
        this.sheetTabs.Controller = this.ShowSheetTabs ? this.controller : null;
        this.sheetTabs.Rebuild();

        this.formulaBar.Controller = this.ShowFormulaBar ? this.controller : null;
        this.formulaBar.Refresh();
    }

    void OnSheetTabsChanged(object? sender, EventArgs e) => this.Invalidate();

    void OnActiveSheetChanged(object? sender, Worksheet sheet)
    {
        // Written back so that a host binding SheetName and a tab tap do not fight: without this the
        // next Rebuild would switch straight back to the sheet the user just left.
        this.SetValue(SheetNameProperty, sheet.Name);

        this.ActiveSheetChanged?.Invoke(this, sheet);
        this.sheetTabs.Rebuild();
        this.formulaBar.Refresh();
        this.Invalidate();
    }

    void OnControllerChanged(object? sender, EventArgs e) => this.Invalidate();

    void Invalidate() => this.canvas.InvalidateSurface();

    void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var theme = this.Theme;
        if (this.controller is null)
        {
            e.Surface.Canvas.Clear(new SKColor(theme.Background.R, theme.Background.G, theme.Background.B));
            return;
        }

        // The surface is in device pixels while layout is in device-independent units.
        var scale = this.canvas.Width > 0 ? (float)(e.Info.Width / this.canvas.Width) : 1f;

        this.painter.Paint(e.Surface.Canvas, new SpreadsheetPaintRequest
        {
            Workbook = this.controller.Workbook,
            Sheet = this.controller.Sheet,
            Viewport = this.controller.Viewport,
            Selection = this.controller.Selection,
            Theme = theme,
            Scale = scale,
            EditingCell = this.controller.EditingCell
        });
    }

    void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (this.controller is null)
        {
            e.Handled = true;
            return;
        }

        // Touch locations arrive in device pixels; the controller works in the same units as layout.
        var scale = this.canvas.Width > 0 ? (float)(this.canvas.CanvasSize.Width / this.canvas.Width) : 1f;
        var x = e.Location.X / scale;
        var y = e.Location.Y / scale;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                this.controller.PointerDown(x, y);
                break;

            case SKTouchAction.Moved:
                if (e.InContact)
                    this.controller.PointerMove(x, y);

                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                this.controller.PointerUp();
                break;

            case SKTouchAction.WheelChanged:
                // A wheel notch is reported in platform units; treat it as a vertical scroll.
                this.controller.Scroll(0, -e.WheelDelta);
                break;
        }

        e.Handled = true;
    }

    void OnEditingChanged(object? sender, CellRef? cell)
    {
        if (this.controller is null)
            return;

        if (cell is not { } target)
        {
            this.editor.IsVisible = false;
            return;
        }

        var rect = this.controller.Viewport.CellRect(target);

        this.suppressEditorEvents = true;
        this.editor.Text = this.controller.EditingText;
        this.suppressEditorEvents = false;

        AbsoluteLayout.SetLayoutFlags(this.editor, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(this.editor, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        this.editor.IsVisible = true;
        this.editor.FocusForEditing();
    }

    void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!this.suppressEditorEvents)
            this.controller?.UpdateEditingText(e.NewTextValue ?? string.Empty);
    }

    void OnEditorCompleted(object? sender, EventArgs e) => this.Commit(EditCommitDirection.Down);

    void OnEditorUnfocused(object? sender, FocusEventArgs e)
    {
        // Tapping elsewhere commits, matching Excel rather than discarding what was typed.
        if (this.editor.IsVisible)
            this.Commit(EditCommitDirection.None);
    }

    void Commit(EditCommitDirection direction)
    {
        var cell = this.controller?.EditingCell;
        this.controller?.CommitEdit(direction);

        if (cell is { } committed)
            this.CellChanged?.Invoke(this, committed);
    }

    /// <summary>
    /// Opens the editor on the active cell. Exposed because MAUI has no portable key-down event, so a
    /// desktop host wires physical keys through its own platform hook and calls in here.
    /// </summary>
    public void BeginEdit(string? initialText = null) => this.controller?.BeginEdit(initialText);

    public void Move(MoveDirection direction, bool extend = false, bool toEdge = false)
        => this.controller?.Move(direction, extend, toEdge);

    public void ClearSelection() => this.controller?.ClearSelection();

    public void Undo() => this.controller?.Undo();

    public void Redo() => this.controller?.Redo();

    /// <summary>Moves to the next or previous visible sheet, stopping at either end.</summary>
    public void StepSheet(int offset)
    {
        if (this.controller is not { } current)
            return;

        var sheets = current.VisibleSheets;
        var at = -1;
        for (var i = 0; i < sheets.Count && at < 0; i++)
        {
            if (ReferenceEquals(sheets[i], current.Sheet))
                at = i;
        }

        if (at >= 0 && sheets.ElementAtOrDefault(at + offset) is { } target)
            current.SwitchSheet(target);
    }

    void DetachController()
    {
        if (this.controller is null)
            return;

        this.controller.Changed -= this.OnControllerChanged;
        this.controller.EditingChanged -= this.OnEditingChanged;
        this.controller.ActiveSheetChanged -= this.OnActiveSheetChanged;
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.DetachController();
        this.sheetTabs.Changed -= this.OnSheetTabsChanged;
        this.sheetTabs.Controller = null;
        this.formulaBar.Changed -= this.OnSheetTabsChanged;
        this.formulaBar.Detach();
        this.canvas.SizeChanged -= this.OnCanvasSizeChanged;
        this.canvas.PaintSurface -= this.OnPaintSurface;
        this.canvas.Touch -= this.OnTouch;
        this.editor.TextChanged -= this.OnEditorTextChanged;
        this.editor.Completed -= this.OnEditorCompleted;
        this.editor.Unfocused -= this.OnEditorUnfocused;
        this.painter.Dispose();

        GC.SuppressFinalize(this);
    }
}
