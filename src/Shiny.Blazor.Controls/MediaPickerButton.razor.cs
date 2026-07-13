using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls;

public partial class MediaPickerButton : IAsyncDisposable
{
    IJSObjectReference? module;
    DotNetObjectReference<MediaPickerButton>? selfRef;
    ElementReference rootEl;
    ElementReference galleryInputEl;
    ElementReference cameraInputEl;
    ImageEditor? editor;

    readonly List<MediaPickerItem> items = new();

    bool viewerOpen;
    string? viewerSource;
    bool showChooser;
    bool permissionDenied;
    bool editing;
    int currentIndex;

    [Parameter] public bool AllowGallery { get; set; } = true;
    [Parameter] public bool AllowCamera { get; set; } = true;
    [Parameter] public bool AllowPhotoEdit { get; set; }
    [Parameter] public string PermissionDeniedText { get; set; } = "Permission denied. Please enable access in Settings.";
    [Parameter] public RenderFragment? NoImagesTemplate { get; set; }
    [Parameter] public bool ShowAsCarouselInView { get; set; } = true;
    [Parameter] public int MaxPhotos { get; set; } = 1;

    /// <summary>Encoder quality as a percentage (1..100). Default 92.</summary>
    [Parameter] public int CompressionQuality { get; set; } = 92;

    /// <summary>If &gt; 0, the longest edge of each saved photo is capped to this many pixels.</summary>
    [Parameter] public int MaxImageDimension { get; set; }

    /// <summary>Output image format: <c>"jpeg"</c> or <c>"png"</c>.</summary>
    [Parameter] public string OutputFormat { get; set; } = "jpeg";

    [Parameter] public string AddButtonText { get; set; } = "➕ Add Photo";
    [Parameter] public string GalleryActionText { get; set; } = "Choose from Gallery";
    [Parameter] public string CameraActionText { get; set; } = "Take Photo";

    [Parameter] public IReadOnlyList<MediaPickerItem> Photos { get; set; } = [];
    [Parameter] public EventCallback<IReadOnlyList<MediaPickerItem>> PhotosChanged { get; set; }
    [Parameter] public EventCallback<MediaPickerItem> PhotoAdded { get; set; }
    [Parameter] public EventCallback<MediaPickerItem> PhotoRemoved { get; set; }
    [Parameter] public EventCallback<string> PermissionDenied { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        module = await JS.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/Shiny.Blazor.Controls/media-picker.js");
        selfRef = DotNetObjectReference.Create(this);

        await module.InvokeVoidAsync("init", rootEl, galleryInputEl, cameraInputEl, selfRef, BuildOptions());
    }

    MediaPickerJsOptions BuildOptions() => new()
    {
        Format = OutputFormat == "png" ? "png" : "jpeg",
        Quality = Math.Clamp(CompressionQuality, 1, 100) / 100.0,
        MaxDimension = MaxImageDimension
    };

    async Task OnAddClickAsync()
    {
        permissionDenied = false;
        if (items.Count >= MaxPhotos)
            return;

        if (module != null)
            await module.InvokeVoidAsync("updateOptions", rootEl, BuildOptions());

        if (AllowGallery && AllowCamera)
        {
            showChooser = true;
        }
        else if (AllowCamera)
        {
            await CaptureFromCameraAsync();
        }
        else if (AllowGallery)
        {
            await PickFromGalleryAsync();
        }
    }

    async Task PickFromGalleryAsync()
    {
        showChooser = false;
        if (module != null)
            await module.InvokeVoidAsync("openGallery", rootEl);
    }

    async Task CaptureFromCameraAsync()
    {
        showChooser = false;
        if (module != null)
            await module.InvokeVoidAsync("openCamera", rootEl);
    }

    [JSInvokable]
    public async Task OnFilePicked(MediaPickerJsResult result)
    {
        if (items.Count >= MaxPhotos)
            return;

        var item = ToItem(result);
        items.Add(item);
        await NotifyChangedAsync();
        await PhotoAdded.InvokeAsync(item);
        StateHasChanged();
    }

    static MediaPickerItem ToItem(MediaPickerJsResult result)
    {
        var data = Convert.FromBase64String(result.DataBase64);
        return new MediaPickerItem
        {
            Data = data,
            DataUri = $"data:{result.ContentType};base64,{result.DataBase64}",
            Width = result.Width,
            Height = result.Height,
            ContentType = result.ContentType
        };
    }

    void OpenViewer(int index)
    {
        if (items.Count == 0)
            return;
        currentIndex = Math.Clamp(index, 0, items.Count - 1);
        viewerSource = items[currentIndex].DataUri;
        viewerOpen = true;
    }

    void Page(int delta)
    {
        if (items.Count == 0)
            return;
        currentIndex = (currentIndex + delta + items.Count) % items.Count;
        viewerSource = items[currentIndex].DataUri;
    }

    async Task RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count)
            return;
        var removed = items[index];
        items.RemoveAt(index);
        await NotifyChangedAsync();
        await PhotoRemoved.InvokeAsync(removed);
        StateHasChanged();
    }

    void StartEdit()
    {
        viewerOpen = false;
        editing = true;
    }

    async Task SaveEditAsync()
    {
        if (editor == null || currentIndex < 0 || currentIndex >= items.Count)
        {
            editing = false;
            return;
        }

        var quality = Math.Clamp(CompressionQuality, 1, 100) / 100.0;
        var format = OutputFormat == "png" ? "png" : "jpeg";
        var bytes = await editor.ExportAsync(format, quality);
        editing = false;

        if (bytes.Length == 0)
            return;

        var base64 = Convert.ToBase64String(bytes);
        var contentType = format == "png" ? "image/png" : "image/jpeg";
        var size = module != null
            ? await module.InvokeAsync<MediaPickerJsSize>("measure", rootEl, base64, contentType)
            : new MediaPickerJsSize();

        items[currentIndex] = new MediaPickerItem
        {
            Data = bytes,
            DataUri = $"data:{contentType};base64,{base64}",
            Width = size.Width,
            Height = size.Height,
            ContentType = contentType
        };
        await NotifyChangedAsync();
        StateHasChanged();
    }

    async Task NotifyChangedAsync()
    {
        Photos = items.ToArray();
        await PhotosChanged.InvokeAsync(Photos);
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

    // Named DTOs for trim/AOT-safe JS interop (anonymous types lose ctor param names on publish).
    public sealed class MediaPickerJsResult
    {
        public string DataBase64 { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public string ContentType { get; set; } = "";
    }

    sealed class MediaPickerJsOptions
    {
        public string Format { get; set; } = "jpeg";
        public double Quality { get; set; } = 0.92;
        public int MaxDimension { get; set; }
    }

    sealed class MediaPickerJsSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
