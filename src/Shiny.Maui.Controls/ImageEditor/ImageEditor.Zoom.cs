using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.ImageEditor;

/// <summary>
/// Zoom/pan of the editing surface.
///
/// The transform lives on the drawable rather than on the native GraphicsView so that it applies
/// to *every* tool: touch points are converted back through the transform before any tool math
/// runs, which means you can zoom to 400% and draw, crop or place text with pixel accuracy.
/// </summary>
public partial class ImageEditor
{
    /// <summary>Raised whenever <see cref="ZoomLevel"/> changes, including from gestures.</summary>
    public event EventHandler<double>? ZoomChanged;

    bool suppressZoomPropertyCallback;

    public static readonly BindableProperty ZoomLevelProperty = BindableProperty.Create(
        nameof(ZoomLevel), typeof(double), typeof(ImageEditor), 1.0, BindingMode.TwoWay,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
        {
            var editor = (ImageEditor)b;
            if (editor.suppressZoomPropertyCallback)
                return;

            editor.SetZoom((float)(double)n, editor.drawable.GetViewport().Center);
        }));

    /// <summary>Current zoom factor where 1.0 is fit-to-view. Two-way bindable.</summary>
    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public static readonly BindableProperty MinZoomProperty = BindableProperty.Create(
        nameof(MinZoom), typeof(double), typeof(ImageEditor), 1.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                var editor = (ImageEditor)b;
                editor.SetZoom(editor.zoomScale, editor.drawable.GetViewport().Center);
            }));

    /// <summary>Lower zoom bound. Defaults to 1.0 (fit-to-view).</summary>
    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public static readonly BindableProperty MaxZoomProperty = BindableProperty.Create(
        nameof(MaxZoom), typeof(double), typeof(ImageEditor), 8.0,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                var editor = (ImageEditor)b;
                editor.SetZoom(editor.zoomScale, editor.drawable.GetViewport().Center);
            }));

    /// <summary>Upper zoom bound. Defaults to 8x, which is enough for per-pixel touch-ups.</summary>
    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public static readonly BindableProperty ShowZoomControlsProperty = BindableProperty.Create(
        nameof(ShowZoomControls), typeof(bool), typeof(ImageEditor), true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(ImageEditor), () =>
            {
                ((ImageEditor)b).BuildDefaultToolbar();
            }));

    /// <summary>
    /// Shows the zoom in / out / fit cluster in the default toolbar. Keep this on for desktop,
    /// where there is no pinch gesture.
    /// </summary>
    public bool ShowZoomControls
    {
        get => (bool)GetValue(ShowZoomControlsProperty);
        set => SetValue(ShowZoomControlsProperty, value);
    }

    /// <summary>Zooms in one step about the centre of the view.</summary>
    public void ZoomIn() => SetZoom(zoomScale * 1.5f, drawable.GetViewport().Center);

    /// <summary>Zooms out one step about the centre of the view.</summary>
    public void ZoomOut() => SetZoom(zoomScale / 1.5f, drawable.GetViewport().Center);

    /// <summary>Returns to fit-to-view and re-centres the image.</summary>
    public void ZoomToFit() => SetZoom(1f, drawable.GetViewport().Center);

    float zoomScale = 1f;
    float zoomOffsetX;
    float zoomOffsetY;

    /// <summary>
    /// Applies a zoom factor while keeping the image content under <paramref name="anchor"/>
    /// (a point in view coordinates) pinned in place.
    /// </summary>
    void SetZoom(float scale, PointF anchor)
    {
        if (!AllowZoom)
            scale = 1f;

        var min = (float)Math.Max(0.1, MinZoom);
        var max = (float)Math.Max(min, MaxZoom);
        scale = Math.Clamp(scale, min, max);

        // World point currently under the anchor must stay under the anchor afterwards
        var world = drawable.ScreenToWorld(anchor);
        ApplyTransform(scale, anchor, world);
    }

    /// <summary>
    /// Positions the view so <paramref name="world"/> lands under <paramref name="screen"/> at
    /// the given scale. This is the one place the transform is written.
    /// </summary>
    void ApplyTransform(float scale, PointF screen, PointF world)
    {
        var viewport = drawable.GetViewport();
        var cx = viewport.Center.X;
        var cy = viewport.Center.Y;

        zoomScale = scale;
        zoomOffsetX = screen.X - cx - (world.X - cx) * scale;
        zoomOffsetY = screen.Y - cy - (world.Y - cy) * scale;

        ClampOffsets();
        PushTransformToDrawable();
    }

    /// <summary>
    /// Keeps the image anchored to the viewport: centred while it is smaller than the view,
    /// and edge-locked (no empty gutters) once zooming has made it larger.
    /// </summary>
    void ClampOffsets()
    {
        var viewport = drawable.GetViewport();
        var imageRect = drawable.GetImageRect();

        if (viewport is not { Width: > 0, Height: > 0 } || imageRect is not { Width: > 0, Height: > 0 })
        {
            zoomOffsetX = 0;
            zoomOffsetY = 0;
            return;
        }

        zoomOffsetX = ClampAxis(
            zoomOffsetX, imageRect.Center.X, imageRect.Width,
            viewport.Center.X, viewport.Left, viewport.Right, viewport.Width);

        zoomOffsetY = ClampAxis(
            zoomOffsetY, imageRect.Center.Y, imageRect.Height,
            viewport.Center.Y, viewport.Top, viewport.Bottom, viewport.Height);
    }

    float ClampAxis(float offset, float imageCenter, float imageSize, float viewCenter, float viewMin, float viewMax, float viewSize)
    {
        var scaledSize = imageSize * zoomScale;
        var scaledCenter = (imageCenter - viewCenter) * zoomScale + viewCenter;
        var half = scaledSize / 2f;

        return scaledSize >= viewSize
            // Image fills the view — don't let either edge drift inside it
            ? Math.Clamp(offset, viewMax - scaledCenter - half, viewMin - scaledCenter + half)
            // Image is smaller than the view — keep it fully visible
            : Math.Clamp(offset, viewMin - scaledCenter + half, viewMax - scaledCenter - half);
    }

    void PushTransformToDrawable()
    {
        drawable.ViewScale = zoomScale;
        drawable.ViewOffsetX = zoomOffsetX;
        drawable.ViewOffsetY = zoomOffsetY;

        RepositionActiveTextEntry();
        Invalidate();

        if (Math.Abs(ZoomLevel - zoomScale) > 0.0001)
        {
            suppressZoomPropertyCallback = true;
            ZoomLevel = zoomScale;
            suppressZoomPropertyCallback = false;
            ZoomChanged?.Invoke(this, zoomScale);
        }

        UpdateZoomReadout();
    }

    void ResetViewTransform()
    {
        zoomScale = 1f;
        zoomOffsetX = 0;
        zoomOffsetY = 0;
        PushTransformToDrawable();
    }
}
