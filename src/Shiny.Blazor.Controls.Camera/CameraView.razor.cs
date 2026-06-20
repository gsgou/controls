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
    TaskCompletionSource<CameraBarcode>? pendingScan;

    /// <summary>Which camera to use. <see cref="CameraFacing.Front"/> maps to the browser "user" facing mode.</summary>
    [Parameter] public CameraFacing Facing { get; set; } = CameraFacing.Back;

    /// <summary>Exact device id (from <see cref="GetAvailableCamerasAsync"/>) to use; overrides <see cref="Facing"/> when set.</summary>
    [Parameter] public string? CameraId { get; set; }

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

    /// <summary>
    /// Raised with the decoded barcode (format + value) when one is detected in-browser — but only while a
    /// <see cref="RequestBarcodeAsync"/> is outstanding (gated, so it's quiet by default rather than a per-frame
    /// firehose). For most flows prefer awaiting <see cref="RequestBarcodeAsync"/> directly.
    /// </summary>
    [Parameter] public EventCallback<CameraBarcode> BarcodeDetected { get; set; }

    /// <summary>Raised when the camera cannot start (permission denied, no device, insecure context).</summary>
    [Parameter] public EventCallback<string> OnError { get; set; }

    /// <summary>Raised after the preview has started (permission granted) — a good time to call <see cref="GetAvailableCamerasAsync"/>, whose device labels only populate once permission is granted.</summary>
    [Parameter] public EventCallback OnStarted { get; set; }


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
    bool appliedBarcode;
    bool appliedOverlay;
    CameraFacing appliedFacing;
    string? appliedCameraId;

    protected override async Task OnParametersSetAsync()
    {
        if (this.module == null)
            return;

        if (this.Filter != this.appliedFilter)
            await this.ApplyFilterAsync();   // CSS filter — applied live without a restart

        // EnableBarcode / ShowOverlay / Facing / CameraId are read by the JS `start` call, so changing them
        // re-acquires the stream. Re-apply by restarting while running (matches MAUI's live toggling).
        if (this.started &&
            (this.EnableBarcode != this.appliedBarcode ||
             this.ShowOverlay != this.appliedOverlay ||
             this.Facing != this.appliedFacing ||
             this.CameraId != this.appliedCameraId))
        {
            await this.StopAsync();
            await this.StartAsync();
        }
    }

    /// <summary>
    /// List the video input devices the browser exposes. Device <c>Name</c>s are only populated once camera
    /// permission has been granted, so call this after the preview has started.
    /// </summary>
    public async Task<IReadOnlyList<CameraDevice>> GetAvailableCamerasAsync()
    {
        if (this.module == null)
            return [];
        return await this.module.InvokeAsync<CameraDevice[]>("listCameras");
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
                this.EnableBarcode, this.ShowOverlay, this.CameraId);
            this.started = true;
            this.appliedBarcode = this.EnableBarcode;
            this.appliedOverlay = this.ShowOverlay;
            this.appliedFacing = this.Facing;
            this.appliedCameraId = this.CameraId;
            await this.OnStarted.InvokeAsync();
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


    /// <summary>
    /// Arm the detector and complete on the <i>next</i> decoded barcode, then go quiet — the gated equivalent of
    /// the MAUI <c>OnDetected</c> flow. Bounding boxes keep drawing throughout. Call it again to "keep scanning"
    /// (e.g. in a loop to collect several codes). Pass a <paramref name="ct"/> to cancel an outstanding request.
    /// </summary>
    public async Task<CameraBarcode> RequestBarcodeAsync(CancellationToken ct = default)
    {
        if (this.module == null)
            throw new InvalidOperationException("CameraView is not started");

        // only one outstanding request at a time — supersede any prior wait
        this.pendingScan?.TrySetCanceled();
        var tcs = new TaskCompletionSource<CameraBarcode>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.pendingScan = tcs;

        await using var reg = ct.Register(() =>
        {
            if (tcs.TrySetCanceled(ct))
                _ = this.module.InvokeVoidAsync("disarm", this.videoEl).AsTask();
        });

        await this.module.InvokeVoidAsync("arm", this.videoEl);
        return await tcs.Task;
    }


    /// <summary>Capture a still frame as JPEG bytes.</summary>
    public async Task<byte[]> CapturePhotoAsync()
    {
        if (this.module == null)
            return [];
        // bake the current filter into the still so it matches the preview (parity with MAUI)
        return await this.module.InvokeAsync<byte[]>("capture", this.videoEl, BlazorCameraFilters.ToCss(this.Filter));
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


    /// <summary>
    /// Invoked from JS when a barcode is decoded (only while armed). Completes any outstanding
    /// <see cref="RequestBarcodeAsync"/> and raises <see cref="BarcodeDetected"/>. Public + named DTO for
    /// trim-safe interop.
    /// </summary>
    [JSInvokable]
    public async Task OnBarcode(CameraBarcode barcode)
    {
        var pending = this.pendingScan;
        this.pendingScan = null;
        pending?.TrySetResult(barcode);
        await this.BarcodeDetected.InvokeAsync(barcode);
    }


    /// <summary>Invoked from JS when an error occurs after startup.</summary>
    [JSInvokable]
    public Task OnJsError(string message) => this.OnError.InvokeAsync(message);


    public async ValueTask DisposeAsync()
    {
        this.pendingScan?.TrySetCanceled();
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
