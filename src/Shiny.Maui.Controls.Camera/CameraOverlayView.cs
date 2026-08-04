namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A drop-in overlay that draws the analyzer-supplied <see cref="OverlayBox"/>es for a
/// <see cref="CameraView"/>. Place it in the same layout cell directly over the camera and point it at the
/// camera; it subscribes to <see cref="CameraView.OverlaysChanged"/> and redraws automatically:
/// <code>
/// &lt;Grid&gt;
///     &lt;cam:CameraView x:Name="Camera" /&gt;
///     &lt;cam:CameraOverlayView Camera="{x:Reference Camera}" /&gt;
/// &lt;/Grid&gt;
/// </code>
/// Each box carries its own colors/text; the <c>Default*</c> properties here fill in anything a box leaves
/// unset.
/// </summary>
public class CameraOverlayView : GraphicsView
{
    readonly CameraOverlayDrawable drawable = new();

    public CameraOverlayView()
    {
        this.Drawable = this.drawable;
        this.InputTransparent = true;
    }

    /// <summary>The camera whose overlay boxes this view draws.</summary>
    public static readonly BindableProperty CameraProperty = BindableProperty.Create(
        nameof(Camera), typeof(CameraView), typeof(CameraOverlayView), null, propertyChanged: OnCameraChanged);

    /// <summary>Default box stroke color for boxes that don't specify one.</summary>
    public static readonly BindableProperty DefaultBoxColorProperty = BindableProperty.Create(
        nameof(DefaultBoxColor), typeof(Color), typeof(CameraOverlayView), Color.FromArgb("#22D3EE"),
        propertyChanged: (b, _, v) => ((CameraOverlayView)b).drawable.BoxColor = (Color)v);

    /// <summary>Default caption color for boxes that don't specify one.</summary>
    public static readonly BindableProperty DefaultTextColorProperty = BindableProperty.Create(
        nameof(DefaultTextColor), typeof(Color), typeof(CameraOverlayView), Colors.White,
        propertyChanged: (b, _, v) => ((CameraOverlayView)b).drawable.LabelColor = (Color)v);

    /// <summary>Color of the scrim dimming the area outside the analyzer's scan window. Null disables the scrim.</summary>
    public static readonly BindableProperty ScanWindowScrimColorProperty = BindableProperty.Create(
        nameof(ScanWindowScrimColor), typeof(Color), typeof(CameraOverlayView), Color.FromRgba(0, 0, 0, 110),
        propertyChanged: (b, _, v) => ((CameraOverlayView)b).drawable.ScanWindowScrimColor = (Color?)v);

    /// <summary>Color of the viewfinder reticle framing the analyzer's scan window. Null disables the reticle outline.</summary>
    public static readonly BindableProperty ScanWindowColorProperty = BindableProperty.Create(
        nameof(ScanWindowColor), typeof(Color), typeof(CameraOverlayView), Colors.White,
        propertyChanged: (b, _, v) => ((CameraOverlayView)b).drawable.ScanWindowColor = (Color?)v);

    /// <inheritdoc cref="CameraProperty"/>
    public CameraView? Camera
    {
        get => (CameraView?)this.GetValue(CameraProperty);
        set => this.SetValue(CameraProperty, value);
    }

    /// <inheritdoc cref="DefaultBoxColorProperty"/>
    public Color DefaultBoxColor
    {
        get => (Color)this.GetValue(DefaultBoxColorProperty);
        set => this.SetValue(DefaultBoxColorProperty, value);
    }

    /// <inheritdoc cref="DefaultTextColorProperty"/>
    public Color DefaultTextColor
    {
        get => (Color)this.GetValue(DefaultTextColorProperty);
        set => this.SetValue(DefaultTextColorProperty, value);
    }

    /// <inheritdoc cref="ScanWindowScrimColorProperty"/>
    public Color? ScanWindowScrimColor
    {
        get => (Color?)this.GetValue(ScanWindowScrimColorProperty);
        set => this.SetValue(ScanWindowScrimColorProperty, value);
    }

    /// <inheritdoc cref="ScanWindowColorProperty"/>
    public Color? ScanWindowColor
    {
        get => (Color?)this.GetValue(ScanWindowColorProperty);
        set => this.SetValue(ScanWindowColorProperty, value);
    }

    static void OnCameraChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var self = (CameraOverlayView)bindable;
        if (oldValue is CameraView oldCam)
            oldCam.OverlaysChanged -= self.OnOverlaysChanged;
        if (newValue is CameraView newCam)
            newCam.OverlaysChanged += self.OnOverlaysChanged;
    }

    void OnOverlaysChanged(object? sender, CameraOverlaysChangedEventArgs e)
    {
        this.drawable.Boxes = e.Overlays;
        this.drawable.ScanWindow = e.ScanWindow;
        this.drawable.ImageAspect = e.ImageHeight == 0 ? 1f : (float)e.ImageWidth / e.ImageHeight;
        if (this.Camera is { } cam)
        {
            this.drawable.ScaleMode = cam.ScaleMode;
            this.drawable.Facing = cam.Facing;

            // Draw effects paint over the preview here rather than being composited into every frame on the
            // capture thread — the same effect instances are used for stills and recordings, where compositing
            // is unavoidable, but the live path stays a cheap canvas draw.
            this.drawable.DrawEffects = cam.EffectChain.DrawEffects;
            this.drawable.AnalyzerResult = cam.LastAnalyzerResult;
        }
        this.Invalidate();
    }
}
