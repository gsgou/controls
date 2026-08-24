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

        this.Content = this.root;
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
        propertyChanged: (b, _, _) => ((SpreadsheetView)b).Invalidate());

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

    /// <summary>The live controller, so a toolbar or formula bar can drive the same state.</summary>
    public SpreadsheetController? Controller => this.controller;

    /// <summary>Raised after a cell is committed.</summary>
    public event EventHandler<CellRef>? CellChanged;

    void Rebuild()
    {
        this.DetachController();

        var workbook = this.Workbook;
        if (workbook is null)
        {
            this.controller = null;
            this.Invalidate();
            return;
        }

        var sheet = this.SheetName is null
            ? workbook.Sheets.FirstOrDefault()
            : workbook.Sheets.FirstOrDefault(x => x.Name == this.SheetName);

        if (sheet is null)
        {
            this.controller = null;
            this.Invalidate();
            return;
        }

        this.controller = new SpreadsheetController(workbook, sheet);
        this.controller.Changed += this.OnControllerChanged;
        this.controller.EditingChanged += this.OnEditingChanged;
        this.controller.Resize(this.Width > 0 ? this.Width : 800, this.Height > 0 ? this.Height : 600);
        this.Invalidate();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0 && height > 0)
            this.controller?.Resize(width, height);
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
        var scale = this.Width > 0 ? (float)(e.Info.Width / this.Width) : 1f;

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
        var scale = this.Width > 0 ? (float)(this.canvas.CanvasSize.Width / this.Width) : 1f;
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
        this.editor.Focus();
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

    void DetachController()
    {
        if (this.controller is null)
            return;

        this.controller.Changed -= this.OnControllerChanged;
        this.controller.EditingChanged -= this.OnEditingChanged;
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.DetachController();
        this.canvas.PaintSurface -= this.OnPaintSurface;
        this.canvas.Touch -= this.OnTouch;
        this.editor.TextChanged -= this.OnEditorTextChanged;
        this.editor.Completed -= this.OnEditorCompleted;
        this.editor.Unfocused -= this.OnEditorUnfocused;
        this.painter.Dispose();

        GC.SuppressFinalize(this);
    }
}
