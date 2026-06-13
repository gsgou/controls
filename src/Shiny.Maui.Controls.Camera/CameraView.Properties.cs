namespace Shiny.Maui.Controls.Camera;

public partial class CameraView
{
    /// <summary>Which physical camera to use. Default <see cref="CameraFacing.Back"/>.</summary>
    public static readonly BindableProperty FacingProperty = BindableProperty.Create(
        nameof(Facing), typeof(CameraFacing), typeof(CameraView), CameraFacing.Back);

    /// <summary>
    /// Exact device id to use (from <see cref="GetAvailableCamerasAsync"/>). When null, the camera is
    /// chosen by <see cref="Facing"/>. Setting this overrides <see cref="Facing"/>.
    /// </summary>
    public static readonly BindableProperty CameraIdProperty = BindableProperty.Create(
        nameof(CameraId), typeof(string), typeof(CameraView), null);

    /// <summary>Whether the session should be running. Default <c>true</c>.</summary>
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(CameraView), true);

    /// <summary>Turn the torch (continuous flashlight) on/off where supported. Default <c>false</c>.</summary>
    public static readonly BindableProperty IsTorchOnProperty = BindableProperty.Create(
        nameof(IsTorchOn), typeof(bool), typeof(CameraView), false);

    /// <summary>Flash behaviour for still capture. Default <see cref="CameraFlashMode.Off"/>.</summary>
    public static readonly BindableProperty FlashModeProperty = BindableProperty.Create(
        nameof(FlashMode), typeof(CameraFlashMode), typeof(CameraView), CameraFlashMode.Off);

    /// <summary>Current zoom factor (1.0 = no zoom). Clamped to <see cref="MinZoom"/>..<see cref="MaxZoom"/>.</summary>
    public static readonly BindableProperty ZoomProperty = BindableProperty.Create(
        nameof(Zoom), typeof(double), typeof(CameraView), 1d, coerceValue: CoerceZoom);

    /// <summary>Smallest supported zoom factor (reported by the handler).</summary>
    public static readonly BindableProperty MinZoomProperty = BindableProperty.Create(
        nameof(MinZoom), typeof(double), typeof(CameraView), 1d);

    /// <summary>Largest supported zoom factor (reported by the handler).</summary>
    public static readonly BindableProperty MaxZoomProperty = BindableProperty.Create(
        nameof(MaxZoom), typeof(double), typeof(CameraView), 1d);

    /// <summary>How the preview fills the view. Default <see cref="PreviewScaleMode.AspectFill"/>.</summary>
    public static readonly BindableProperty ScaleModeProperty = BindableProperty.Create(
        nameof(ScaleMode), typeof(PreviewScaleMode), typeof(CameraView), PreviewScaleMode.AspectFill);

    /// <summary>Whether the built-in bounding-box overlay is drawn. Default <c>true</c>.</summary>
    public static readonly BindableProperty ShowDetectionOverlayProperty = BindableProperty.Create(
        nameof(ShowDetectionOverlay), typeof(bool), typeof(CameraView), true);

    /// <summary>Live color filter applied to the preview. Default <see cref="CameraFilter.None"/>.</summary>
    public static readonly BindableProperty FilterProperty = BindableProperty.Create(
        nameof(Filter), typeof(CameraFilter), typeof(CameraView), CameraFilter.None);

    /// <summary>Whether a video recording is currently in progress (updated by the control).</summary>
    public static readonly BindableProperty IsRecordingProperty = BindableProperty.Create(
        nameof(IsRecording), typeof(bool), typeof(CameraView), false, BindingMode.OneWayToSource);

    /// <summary>The most recent aggregated overlay boxes (read-only; updated by the pipeline).</summary>
    public static readonly BindableProperty OverlaysProperty = BindableProperty.Create(
        nameof(Overlays), typeof(IReadOnlyList<OverlayBox>), typeof(CameraView), Array.Empty<OverlayBox>());


    /// <inheritdoc cref="FacingProperty"/>
    public CameraFacing Facing
    {
        get => (CameraFacing)this.GetValue(FacingProperty);
        set => this.SetValue(FacingProperty, value);
    }

    /// <inheritdoc cref="CameraIdProperty"/>
    public string? CameraId
    {
        get => (string?)this.GetValue(CameraIdProperty);
        set => this.SetValue(CameraIdProperty, value);
    }

    /// <inheritdoc cref="IsActiveProperty"/>
    public bool IsActive
    {
        get => (bool)this.GetValue(IsActiveProperty);
        set => this.SetValue(IsActiveProperty, value);
    }

    /// <inheritdoc cref="IsTorchOnProperty"/>
    public bool IsTorchOn
    {
        get => (bool)this.GetValue(IsTorchOnProperty);
        set => this.SetValue(IsTorchOnProperty, value);
    }

    /// <inheritdoc cref="FlashModeProperty"/>
    public CameraFlashMode FlashMode
    {
        get => (CameraFlashMode)this.GetValue(FlashModeProperty);
        set => this.SetValue(FlashModeProperty, value);
    }

    /// <inheritdoc cref="ZoomProperty"/>
    public double Zoom
    {
        get => (double)this.GetValue(ZoomProperty);
        set => this.SetValue(ZoomProperty, value);
    }

    /// <inheritdoc cref="MinZoomProperty"/>
    public double MinZoom
    {
        get => (double)this.GetValue(MinZoomProperty);
        set => this.SetValue(MinZoomProperty, value);
    }

    /// <inheritdoc cref="MaxZoomProperty"/>
    public double MaxZoom
    {
        get => (double)this.GetValue(MaxZoomProperty);
        set => this.SetValue(MaxZoomProperty, value);
    }

    /// <inheritdoc cref="ScaleModeProperty"/>
    public PreviewScaleMode ScaleMode
    {
        get => (PreviewScaleMode)this.GetValue(ScaleModeProperty);
        set => this.SetValue(ScaleModeProperty, value);
    }

    /// <inheritdoc cref="ShowDetectionOverlayProperty"/>
    public bool ShowDetectionOverlay
    {
        get => (bool)this.GetValue(ShowDetectionOverlayProperty);
        set => this.SetValue(ShowDetectionOverlayProperty, value);
    }

    /// <inheritdoc cref="FilterProperty"/>
    public CameraFilter Filter
    {
        get => (CameraFilter)this.GetValue(FilterProperty);
        set => this.SetValue(FilterProperty, value);
    }

    /// <inheritdoc cref="IsRecordingProperty"/>
    public bool IsRecording
    {
        get => (bool)this.GetValue(IsRecordingProperty);
        private set => this.SetValue(IsRecordingProperty, value);
    }

    /// <inheritdoc cref="OverlaysProperty"/>
    public IReadOnlyList<OverlayBox> Overlays
    {
        get => (IReadOnlyList<OverlayBox>)this.GetValue(OverlaysProperty);
        private set => this.SetValue(OverlaysProperty, value);
    }


    static object CoerceZoom(BindableObject bindable, object value)
    {
        var view = (CameraView)bindable;
        var zoom = (double)value;
        var min = view.MinZoom <= 0 ? 1d : view.MinZoom;
        var max = view.MaxZoom < min ? min : view.MaxZoom;
        return Math.Clamp(zoom, min, max);
    }
}
