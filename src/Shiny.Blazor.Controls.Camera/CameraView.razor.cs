using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shiny.Controls.Camera;

namespace Shiny.Blazor.Controls.Camera;

public partial class CameraView : IAsyncDisposable
{
    [Inject] IJSRuntime JS { get; set; } = default!;

    IJSObjectReference? module;
    DotNetObjectReference<CameraView>? selfRef;
    ElementReference videoEl;
    ElementReference overlayEl;
    bool started;

    /// <summary>Which camera to use. <see cref="CameraFacing.Front"/> maps to the browser "user" facing mode.</summary>
    [Parameter] public CameraFacing Facing { get; set; } = CameraFacing.Back;

    /// <summary>Run the in-browser barcode detector (native <c>BarcodeDetector</c> where available).</summary>
    [Parameter] public bool EnableBarcode { get; set; } = true;

    /// <summary>Draw detection bounding boxes on the overlay canvas.</summary>
    [Parameter] public bool ShowOverlay { get; set; } = true;

    /// <summary>Start the preview automatically on first render.</summary>
    [Parameter] public bool AutoStart { get; set; } = true;

    /// <summary>Live color filter applied to the preview via CSS.</summary>
    [Parameter] public CameraFilter Filter { get; set; } = CameraFilter.None;

    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? Style { get; set; }

    /// <summary>Raised whenever the in-browser analyzers produce a new styled overlay-box set.</summary>
    [Parameter] public EventCallback<IReadOnlyList<OverlayBox>> OverlaysChanged { get; set; }

    /// <summary>Raised with the decoded barcode (format + value) when one is detected in-browser.</summary>
    [Parameter] public EventCallback<CameraBarcode> BarcodeDetected { get; set; }

    /// <summary>Raised when the camera cannot start (permission denied, no device, insecure context).</summary>
    [Parameter] public EventCallback<string> OnError { get; set; }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        this.module = await this.JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Shiny.Blazor.Controls.Camera/camera.js");
        this.selfRef = DotNetObjectReference.Create(this);

        if (this.AutoStart)
            await this.StartAsync();

        await this.ApplyFilterAsync();
    }


    CameraFilter appliedFilter = CameraFilter.None;

    protected override async Task OnParametersSetAsync()
    {
        if (this.module != null && this.Filter != this.appliedFilter)
            await this.ApplyFilterAsync();
    }


    async Task ApplyFilterAsync()
    {
        if (this.module == null)
            return;
        this.appliedFilter = this.Filter;
        await this.module.InvokeVoidAsync("setFilter", this.videoEl, BlazorCameraFilters.ToCss(this.Filter));
    }


    /// <summary>Request the camera and begin previewing + analyzing.</summary>
    public async Task StartAsync()
    {
        if (this.module == null || this.started)
            return;
        try
        {
            await this.module.InvokeVoidAsync(
                "start", this.videoEl, this.overlayEl, this.selfRef,
                this.Facing == CameraFacing.Front ? "user" : "environment",
                this.EnableBarcode, this.ShowOverlay);
            this.started = true;
        }
        catch (Exception ex)
        {
            await this.OnError.InvokeAsync(ex.Message);
        }
    }


    /// <summary>Stop the preview and release the camera.</summary>
    public async Task StopAsync()
    {
        if (this.module == null || !this.started)
            return;
        await this.module.InvokeVoidAsync("stop", this.videoEl);
        this.started = false;
    }


    /// <summary>Capture a still frame as JPEG bytes.</summary>
    public async Task<byte[]> CapturePhotoAsync()
    {
        if (this.module == null)
            return [];
        return await this.module.InvokeAsync<byte[]>("capture", this.videoEl);
    }


    /// <summary>Start recording (via MediaRecorder). Pass <c>false</c> to record without audio.</summary>
    public async Task StartRecordingAsync(bool includeAudio = true)
    {
        if (this.module != null)
            await this.module.InvokeVoidAsync("startRecording", this.videoEl, includeAudio);
    }


    /// <summary>Stop recording and return the encoded video bytes (typically WebM).</summary>
    public async Task<byte[]> StopRecordingAsync()
    {
        if (this.module == null)
            return [];
        return await this.module.InvokeAsync<byte[]>("stopRecording", this.videoEl);
    }


    /// <summary>Invoked from JS with the latest styled overlay boxes. Public + named DTO for trim-safe interop.</summary>
    [JSInvokable]
    public async Task OnOverlays(CameraOverlayBox[] boxes)
    {
        var mapped = boxes.Select(b => b.ToOverlayBox()).ToArray();
        await this.OverlaysChanged.InvokeAsync(mapped);
    }


    /// <summary>Invoked from JS when a barcode is decoded. Public + named DTO for trim-safe interop.</summary>
    [JSInvokable]
    public Task OnBarcode(CameraBarcode barcode) => this.BarcodeDetected.InvokeAsync(barcode);


    /// <summary>Invoked from JS when an error occurs after startup.</summary>
    [JSInvokable]
    public Task OnJsError(string message) => this.OnError.InvokeAsync(message);


    public async ValueTask DisposeAsync()
    {
        try
        {
            if (this.module != null)
            {
                await this.StopAsync();
                await this.module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { /* circuit gone */ }
        this.selfRef?.Dispose();
    }
}
