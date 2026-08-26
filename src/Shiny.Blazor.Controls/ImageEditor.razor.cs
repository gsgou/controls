using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

public partial class ImageEditor : IAsyncDisposable
{
    IJSObjectReference? module;
    DotNetObjectReference<ImageEditor>? selfRef;
    ElementReference rootEl;
    ElementReference canvasEl;
    ElementReference textInputEl;
    bool initialized;
    bool canUndo;
    bool canRedo;
    string currentMode = "none";
    string activeColor = "#ffffff";
    double zoomLevel = 1;

    // Shape fill. `shapeFill` is the composed rgba() the canvas gets, and null means "no fill" —
    // the hex and the alpha are kept alongside it so turning fill back on restores what was set
    // rather than resetting to white.
    string? shapeFill;
    string shapeFillHex = "#ffffff";
    double shapeFillAlpha = 0.35;

    // Inline text input state
    bool isTextInputVisible;
    string textInputValue = "";
    double textInputLeft;
    double textInputTop;
    double textInputNormX;
    double textInputNormY;
    double textInputScale = 1;

    static readonly System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;

    [Parameter] public string? Source { get; set; }
    [Parameter] public byte[]? ImageData { get; set; }
    [Parameter] public bool AllowCrop { get; set; } = true;
    [Parameter] public bool AllowRotate { get; set; } = true;
    [Parameter] public bool AllowDraw { get; set; } = true;
    [Parameter] public bool AllowTextAnnotation { get; set; } = true;
    [Parameter] public bool AllowLine { get; set; } = true;
    [Parameter] public bool AllowArrow { get; set; } = true;
    [Parameter] public bool AllowRectangle { get; set; } = true;
    [Parameter] public bool AllowEllipse { get; set; } = true;
    [Parameter] public bool AllowCircle { get; set; } = true;
    /// <summary>
    /// Interior colour for the shape tools as a <c>#rrggbb</c> hex string. Null or empty — the
    /// default — leaves shapes unfilled, which is what you want for a highlight box over a photo;
    /// a solid colour turns the same tool into a redaction block.
    /// </summary>
    [Parameter] public string? ShapeFillColor { get; set; }
    /// <summary>
    /// Opacity of <see cref="ShapeFillColor"/>, 0-1. It is separate from the colour because
    /// <c>&lt;input type="color"&gt;</c> cannot express alpha (MAUI carries it in the Color itself).
    /// </summary>
    [Parameter] public double ShapeFillOpacity { get; set; } = 0.35;
    /// <summary>Shows the fill swatch, opacity slider and fill on/off toggle while a shape tool is active.</summary>
    [Parameter] public bool ShowShapeFillPicker { get; set; } = true;
    [Parameter] public bool AllowZoom { get; set; } = true;
    /// <summary>Lower zoom bound. 1.0 is fit-to-view.</summary>
    [Parameter] public double MinZoom { get; set; } = 1;
    /// <summary>Upper zoom bound. 8x by default, enough for per-pixel touch-ups.</summary>
    [Parameter] public double MaxZoom { get; set; } = 8;
    /// <summary>Shows the zoom out / percentage / zoom in / fit cluster in the default toolbar.</summary>
    [Parameter] public bool ShowZoomControls { get; set; } = true;
    /// <summary>Shows a caption under each tool icon. Turn off for a compact icon-only bar.</summary>
    [Parameter] public bool ShowToolLabels { get; set; } = true;
    /// <summary>Shows the pen-weight presets next to the colour swatch for the ink tools.</summary>
    [Parameter] public bool ShowStrokeWidthPicker { get; set; } = true;
    /// <summary>Pen weights offered by the stroke-width picker.</summary>
    [Parameter] public IEnumerable<double> StrokeWidthPresets { get; set; } = [2, 4, 8];
    /// <summary>Extra content rendered at the trailing edge of the toolbar (a save button, say).</summary>
    [Parameter] public RenderFragment? ToolbarActions { get; set; }
    [Parameter] public string CropApplyText { get; set; } = "Apply";
    [Parameter] public string CropCancelText { get; set; } = "Cancel";
    [Parameter] public EventCallback<double> ZoomLevelChanged { get; set; }
    [Parameter] public bool AllowFontSelection { get; set; }
    [Parameter] public bool AllowFontSizeSelection { get; set; }
    [Parameter] public string DrawStrokeColor { get; set; } = "#ffffff";
    [Parameter] public double DrawStrokeWidth { get; set; } = 3;
    [Parameter] public double TextFontSize { get; set; } = 16;
    [Parameter] public string TextColor { get; set; } = "#ffffff";
    [Parameter] public string TextFontFamily { get; set; } = "Arial";
    [Parameter] public IEnumerable<string>? AvailableFonts { get; set; }
    [Parameter] public IEnumerable<double>? AvailableFontSizes { get; set; }
    [Parameter] public EventCallback<string> TextFontFamilyChanged { get; set; }
    [Parameter] public EventCallback<double> TextFontSizeChanged { get; set; }
    [Parameter] public string ToolbarPosition { get; set; } = "bottom";
    [Parameter] public RenderFragment? ToolbarTemplate { get; set; }
    [Parameter] public EventCallback<bool> CanUndoChanged { get; set; }
    [Parameter] public EventCallback<bool> CanRedoChanged { get; set; }

    string? previousSource;
    byte[]? previousImageData;
    string? previousShapeFillColor;
    double previousShapeFillOpacity;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            activeColor = DrawStrokeColor;

            shapeFillAlpha = Math.Clamp(ShapeFillOpacity, 0, 1);
            previousShapeFillColor = ShapeFillColor;
            previousShapeFillOpacity = ShapeFillOpacity;
            if (!string.IsNullOrWhiteSpace(ShapeFillColor))
            {
                shapeFillHex = ShapeFillColor;
                shapeFill = ComposeFill();
            }

            module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Shiny.Blazor.Controls/image-editor.js");

            selfRef = DotNetObjectReference.Create(this);

            // a named DTO, not an anonymous type: trimmed/AOT publish strips anonymous-type
            // constructor parameter names, which the JS interop serializer requires
            await module.InvokeVoidAsync("init", rootEl, canvasEl, selfRef, new ImageEditorJsOptions
            {
                DrawColor = activeColor,
                DrawWidth = DrawStrokeWidth,
                TextColor = activeColor,
                TextSize = TextFontSize,
                TextFont = TextFontFamily,
                AllowZoom = AllowZoom,
                MinZoom = MinZoom,
                MaxZoom = MaxZoom,
                ShapeFill = shapeFill
            });

            initialized = true;
            await LoadImageAsync();
        }
        else if (initialized)
        {
            await SyncParametersAsync();

            if (isTextInputVisible)
            {
                try
                {
                    await textInputEl.FocusAsync();
                }
                catch { }
            }
        }
    }

    async Task SyncParametersAsync()
    {
        if (module == null)
            return;

        // Check if source changed
        if (Source != previousSource || ImageData != previousImageData)
            await LoadImageAsync();

        await module.InvokeVoidAsync("updateDrawSettings", rootEl, activeColor, DrawStrokeWidth);
        await module.InvokeVoidAsync("updateTextSettings", rootEl, activeColor, TextFontSize, TextFontFamily);
        await module.InvokeVoidAsync("updateAllowZoom", rootEl, AllowZoom);
        await module.InvokeVoidAsync("updateZoomLimits", rootEl, MinZoom, MaxZoom);

        // Re-derive the fill only when the host actually changed it, so a re-render does not
        // stomp on what the toolbar's own swatch and slider set
        if (ShapeFillColor != previousShapeFillColor || Math.Abs(ShapeFillOpacity - previousShapeFillOpacity) > 0.0001)
        {
            previousShapeFillColor = ShapeFillColor;
            previousShapeFillOpacity = ShapeFillOpacity;
            shapeFillAlpha = Math.Clamp(ShapeFillOpacity, 0, 1);

            if (string.IsNullOrWhiteSpace(ShapeFillColor))
            {
                shapeFill = null;
            }
            else
            {
                shapeFillHex = ShapeFillColor;
                shapeFill = ComposeFill();
            }
        }

        await module.InvokeVoidAsync("updateShapeSettings", rootEl, shapeFill);
    }

    async Task LoadImageAsync()
    {
        if (module == null) return;

        previousSource = Source;
        previousImageData = ImageData;

        if (ImageData is { Length: > 0 })
            await module.InvokeVoidAsync("loadImageData", rootEl, ImageData);
        else if (!string.IsNullOrEmpty(Source))
            await module.InvokeVoidAsync("loadImage", rootEl, Source);
    }

    // Public methods callable via @ref
    public async ValueTask UndoAsync()
    {
        if (module != null)
            await module.InvokeVoidAsync("undo", rootEl);
    }

    public async ValueTask RedoAsync()
    {
        if (module != null)
            await module.InvokeVoidAsync("redo", rootEl);
    }

    public async ValueTask RotateAsync(float degrees)
    {
        if (module != null)
            await module.InvokeVoidAsync("rotate", rootEl, degrees);
    }

    public async ValueTask ResetAsync()
    {
        if (module != null)
        {
            await module.InvokeVoidAsync("reset", rootEl);
            currentMode = "none";
            DismissTextInput();
            StateHasChanged();
        }
    }

    public async ValueTask SetModeAsync(string mode)
    {
        if (module != null)
        {
            DismissTextInput();
            await module.InvokeVoidAsync("setMode", rootEl, mode);
            currentMode = mode;
            StateHasChanged();
        }
    }

    /// <summary>Current zoom factor, where 1.0 is fit-to-view.</summary>
    public double ZoomLevel => zoomLevel;

    public async ValueTask ZoomInAsync()
    {
        if (module != null)
            await module.InvokeVoidAsync("zoomIn", rootEl);
    }

    public async ValueTask ZoomOutAsync()
    {
        if (module != null)
            await module.InvokeVoidAsync("zoomOut", rootEl);
    }

    public async ValueTask ZoomToFitAsync()
    {
        if (module != null)
            await module.InvokeVoidAsync("zoomToFit", rootEl);
    }

    /// <summary>Sets an explicit zoom factor, anchored on the centre of the view.</summary>
    public async ValueTask SetZoomAsync(double scale)
    {
        if (module != null)
            await module.InvokeVoidAsync("setZoom", rootEl, scale);
    }

    public async ValueTask ApplyCropAsync()
    {
        if (module != null)
        {
            await module.InvokeVoidAsync("applyCrop", rootEl);
            currentMode = "none";
            StateHasChanged();
        }
    }

    public async Task<byte[]> ExportAsync(string format = "png", double quality = 0.92, int? width = null, int? height = null)
    {
        if (module == null)
            return [];

        return await module.InvokeAsync<byte[]>("exportImage", rootEl, format, quality,
            width ?? 0, height ?? 0);
    }

    // Toolbar actions
    async Task ToggleCrop()
    {
        var newMode = currentMode == "crop" ? "none" : "crop";
        await SetModeAsync(newMode);
    }

    async Task ToggleDraw()
    {
        var newMode = currentMode == "draw" ? "none" : "draw";
        await SetModeAsync(newMode);
    }

    async Task ToggleText()
    {
        var newMode = currentMode == "text" ? "none" : "text";
        await SetModeAsync(newMode);
    }

    async Task ToggleLine()
    {
        var newMode = currentMode == "line" ? "none" : "line";
        await SetModeAsync(newMode);
    }

    async Task ToggleArrow()
    {
        var newMode = currentMode == "arrow" ? "none" : "arrow";
        await SetModeAsync(newMode);
    }

    async Task ToggleRectangle()
    {
        var newMode = currentMode == "rect" ? "none" : "rect";
        await SetModeAsync(newMode);
    }

    async Task ToggleEllipse()
    {
        var newMode = currentMode == "ellipse" ? "none" : "ellipse";
        await SetModeAsync(newMode);
    }

    async Task ToggleCircle()
    {
        var newMode = currentMode == "circle" ? "none" : "circle";
        await SetModeAsync(newMode);
    }

    async Task OnFillColorChanged(ChangeEventArgs e)
    {
        shapeFillHex = e.Value?.ToString() ?? "#ffffff";
        ShapeFillColor = shapeFillHex;
        shapeFill = ComposeFill();
        await PushShapeFillAsync();
    }

    async Task OnFillOpacityChanged(ChangeEventArgs e)
    {
        if (!double.TryParse(e.Value?.ToString(), System.Globalization.NumberStyles.Any, Culture, out var percent))
            return;

        shapeFillAlpha = Math.Clamp(percent / 100d, 0, 1);
        ShapeFillOpacity = shapeFillAlpha;

        // Dragging the slider while fill is off is a request for fill, not a no-op
        ShapeFillColor = shapeFillHex;
        shapeFill = ComposeFill();
        await PushShapeFillAsync();
    }

    async Task ToggleShapeFill()
    {
        shapeFill = shapeFill == null ? ComposeFill() : null;
        ShapeFillColor = shapeFill == null ? null : shapeFillHex;
        await PushShapeFillAsync();
    }

    async Task PushShapeFillAsync()
    {
        if (module != null)
            await module.InvokeVoidAsync("updateShapeSettings", rootEl, shapeFill);
    }

    /// <summary>Folds the opacity into the hex swatch, since a colour input can't carry alpha.</summary>
    string ComposeFill()
    {
        var hex = shapeFillHex.TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, Culture, out var rgb))
            return shapeFillHex;

        var r = (rgb >> 16) & 0xFF;
        var g = (rgb >> 8) & 0xFF;
        var b = rgb & 0xFF;
        return $"rgba({r},{g},{b},{shapeFillAlpha.ToString("0.###", Culture)})";
    }

    string FillOpacityPercent => Math.Round(shapeFillAlpha * 100).ToString("0", Culture);

    async Task OnColorChanged(ChangeEventArgs e)
    {
        var color = e.Value?.ToString() ?? "#ffffff";
        activeColor = color;
        DrawStrokeColor = color;
        TextColor = color;

        if (module != null)
        {
            await module.InvokeVoidAsync("updateDrawSettings", rootEl, color, DrawStrokeWidth);
            await module.InvokeVoidAsync("updateTextSettings", rootEl, color, TextFontSize, TextFontFamily);
        }
    }

    async Task OnFontFamilySelected(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        TextFontFamily = value;
        await TextFontFamilyChanged.InvokeAsync(value);
        if (module != null)
            await module.InvokeVoidAsync("updateTextSettings", rootEl, activeColor, TextFontSize, TextFontFamily);
    }

    async Task OnFontSizeSelected(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var size))
        {
            TextFontSize = size;
            await TextFontSizeChanged.InvokeAsync(size);
            if (module != null)
                await module.InvokeVoidAsync("updateTextSettings", rootEl, activeColor, TextFontSize, TextFontFamily);
        }
    }

    Task CancelCrop() => SetModeAsync("none").AsTask();

    // Named handlers rather than inline lambdas: the razor attribute delimiter is a double quote,
    // so a mode string can't be written inline, and returning the Task keeps Blazor awaiting it
    Task SelectMoveTool() => SetModeAsync("none").AsTask();

    Task RotateClockwise() => RotateAsync(90).AsTask();

    Task ApplyCrop() => ApplyCropAsync().AsTask();

    Task Undo() => UndoAsync().AsTask();

    Task Redo() => RedoAsync().AsTask();

    Task Reset() => ResetAsync().AsTask();

    Task ZoomIn() => ZoomInAsync().AsTask();

    Task ZoomOut() => ZoomOutAsync().AsTask();

    Task ZoomToFit() => ZoomToFitAsync().AsTask();

    async Task SetStrokeWidthAsync(double width)
    {
        DrawStrokeWidth = width;
        if (module != null)
            await module.InvokeVoidAsync("updateDrawSettings", rootEl, activeColor, width);
    }

    bool IsSelectedWidth(double width) => Math.Abs(DrawStrokeWidth - width) < 0.01;

    bool IsInkMode => currentMode is "draw" or "line" or "arrow";

    bool IsShapeMode => currentMode is "rect" or "ellipse" or "circle";

    string ToolbarOrderClass => ToolbarPosition == "top" ? "shiny-imgeditor-toolbar--top" : string.Empty;

    string ZoomText => $"{Math.Round(zoomLevel * 100)}%";

    string Active(string mode) => currentMode == mode ? "active" : string.Empty;

    // Inline text input
    async Task OnTextInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await CommitTextInput();
        else if (e.Key == "Escape")
            DismissTextInput();
    }

    async Task CommitTextInput()
    {
        if (!isTextInputVisible) return;

        var text = textInputValue?.Trim();
        isTextInputVisible = false;
        textInputValue = "";

        if (!string.IsNullOrEmpty(text) && module != null)
        {
            await module.InvokeVoidAsync("addTextAnnotation", rootEl, text, textInputNormX, textInputNormY);
        }

        StateHasChanged();
    }

    void DismissTextInput()
    {
        isTextInputVisible = false;
        textInputValue = "";
    }

    // JS callbacks
    [JSInvokable]
    public async Task OnCanUndoChanged(bool value)
    {
        canUndo = value;
        await CanUndoChanged.InvokeAsync(value);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnCanRedoChanged(bool value)
    {
        canRedo = value;
        await CanRedoChanged.InvokeAsync(value);
        StateHasChanged();
    }

    [JSInvokable]
    public Task OnRequestTextInput(double canvasX, double canvasY, double normX, double normY, double scale)
    {
        textInputLeft = canvasX;
        textInputTop = canvasY;
        textInputNormX = normX;
        textInputNormY = normY;
        textInputScale = scale;
        textInputValue = "";
        isTextInputVisible = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnZoomChanged(double value)
    {
        zoomLevel = value;
        await ZoomLevelChanged.InvokeAsync(value);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (module != null)
        {
            try
            {
                await module.InvokeVoidAsync("dispose", rootEl);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
        selfRef?.Dispose();
    }

    sealed class ImageEditorJsOptions
    {
        public string? DrawColor { get; set; }
        public double DrawWidth { get; set; }
        public string? TextColor { get; set; }
        public double TextSize { get; set; }
        public string? TextFont { get; set; }
        public bool AllowZoom { get; set; }
        public double MinZoom { get; set; }
        public double MaxZoom { get; set; }
        public string? ShapeFill { get; set; }
    }
}
