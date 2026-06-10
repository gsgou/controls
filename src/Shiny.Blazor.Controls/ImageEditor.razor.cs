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

    // Inline text input state
    bool isTextInputVisible;
    string textInputValue = "";
    double textInputLeft;
    double textInputTop;
    double textInputNormX;
    double textInputNormY;

    [Parameter] public string? Source { get; set; }
    [Parameter] public byte[]? ImageData { get; set; }
    [Parameter] public bool AllowCrop { get; set; } = true;
    [Parameter] public bool AllowRotate { get; set; } = true;
    [Parameter] public bool AllowDraw { get; set; } = true;
    [Parameter] public bool AllowTextAnnotation { get; set; } = true;
    [Parameter] public bool AllowLine { get; set; } = true;
    [Parameter] public bool AllowArrow { get; set; } = true;
    [Parameter] public bool AllowZoom { get; set; } = true;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            activeColor = DrawStrokeColor;

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
                AllowZoom = AllowZoom
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
    public Task OnRequestTextInput(double canvasX, double canvasY, double normX, double normY)
    {
        textInputLeft = canvasX;
        textInputTop = canvasY;
        textInputNormX = normX;
        textInputNormY = normY;
        textInputValue = "";
        isTextInputVisible = true;
        StateHasChanged();
        return Task.CompletedTask;
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
    }
}
