using Shiny.Controls.Office.Document;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Shiny.Maui.Controls.Office;

/// <summary>
/// Displays a <c>.docx</c> as a continuous, reflowing page.
/// </summary>
/// <remarks>
/// Read-only. Content reflows to the control's width rather than being paginated — see
/// <see cref="WordDocument"/> for why a viewer without a full pagination engine should not pretend to
/// have pages. Requires <c>UseSkiaSharp()</c> in <c>MauiProgram</c>.
/// </remarks>
public class DocumentView : ContentView, IDisposable
{
    readonly SKCanvasView canvas;
    readonly SkiaTextMeasurer measurer = new();
    readonly DocumentPainter painter;

    DocumentController? controller;
    double lastPanY;
    bool disposed;

    public DocumentView()
    {
        this.painter = new DocumentPainter(this.measurer);

        this.canvas = new SKCanvasView { EnableTouchEvents = true };
        this.canvas.PaintSurface += this.OnPaintSurface;
        this.canvas.Touch += this.OnTouch;

        this.Content = this.canvas;
    }

    public static readonly BindableProperty DocumentProperty = BindableProperty.Create(
        nameof(Document),
        typeof(WordDocument),
        typeof(DocumentView),
        propertyChanged: (b, _, _) => ((DocumentView)b).Rebuild());

    public static readonly BindableProperty ThemeProperty = BindableProperty.Create(
        nameof(Theme),
        typeof(DocumentTheme),
        typeof(DocumentView),
        DocumentTheme.Light,
        propertyChanged: (b, _, _) => ((DocumentView)b).Invalidate());

    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom),
        typeof(double),
        typeof(DocumentView),
        1.0,
        propertyChanged: (b, _, value) =>
        {
            if (((DocumentView)b).controller is { } controller)
                controller.Zoom = (double)value;
        });

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

    /// <summary>The live controller, so a toolbar or outline pane can drive the same state.</summary>
    public DocumentController? Controller => this.controller;

    void Rebuild()
    {
        if (this.controller is not null)
            this.controller.Changed -= this.OnControllerChanged;

        if (this.Document is null)
        {
            this.controller = null;
            this.Invalidate();
            return;
        }

        this.controller = new DocumentController(this.Document, this.measurer) { Zoom = this.Zoom };
        this.controller.Changed += this.OnControllerChanged;

        if (this.Width > 0 && this.Height > 0)
            this.controller.Resize(this.Width, this.Height);

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
            e.Surface.Canvas.Clear(new SKColor(theme.SurroundBackground.R, theme.SurroundBackground.G, theme.SurroundBackground.B));
            return;
        }

        var scale = this.Width > 0 ? (float)(e.Info.Width / this.Width) : 1f;

        this.painter.Paint(e.Surface.Canvas, new DocumentPaintRequest
        {
            Blocks = this.controller.Blocks,
            Viewport = this.controller.Viewport,
            Theme = theme,
            Scale = scale,
            PageX = this.controller.PageX + this.controller.PagePadding,
            PageWidth = this.controller.PageWidth
        });
    }

    void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (this.controller is null)
        {
            e.Handled = true;
            return;
        }

        var scale = this.Width > 0 ? (float)(this.canvas.CanvasSize.Width / this.Width) : 1f;
        var y = e.Location.Y / scale;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                this.lastPanY = y;
                break;

            case SKTouchAction.Moved when e.InContact:
                // Drag scrolls the document; content follows the finger, so the delta is inverted.
                this.controller.Scroll(this.lastPanY - y);
                this.lastPanY = y;
                break;

            case SKTouchAction.WheelChanged:
                this.controller.Scroll(-e.WheelDelta);
                break;
        }

        e.Handled = true;
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;

        if (this.controller is not null)
            this.controller.Changed -= this.OnControllerChanged;

        this.canvas.PaintSurface -= this.OnPaintSurface;
        this.canvas.Touch -= this.OnTouch;
        this.painter.Dispose();
        this.measurer.Dispose();

        GC.SuppressFinalize(this);
    }
}
