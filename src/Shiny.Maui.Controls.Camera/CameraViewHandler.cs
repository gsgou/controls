#if ANDROID || IOS || MACCATALYST || WINDOWS || MACOS
using System.Collections.Specialized;
using Microsoft.Maui.Handlers;
using Shiny.Maui.Controls.Camera.Internal;

namespace Shiny.Maui.Controls.Camera;

public partial class CameraViewHandler
{
    internal readonly CameraPipeline Pipeline = new();

    // Wire detection delivery (marshaled to the UI thread) and keep the runner set in sync with the
    // control's Analyzers collection. Call from each platform's ConnectHandler.
    private protected void InitPipeline()
    {
        this.Pipeline.OnDetections = (dets, w, h) =>
            this.VirtualView?.Dispatcher.Dispatch(() => this.VirtualView?.OnDetectionsChanged(dets, w, h));

        this.SyncAnalyzers();
        if (this.VirtualView.Analyzers is INotifyCollectionChanged incc)
            incc.CollectionChanged += this.OnAnalyzersChanged;
    }

    private protected void TeardownPipeline()
    {
        if (this.VirtualView?.Analyzers is INotifyCollectionChanged incc)
            incc.CollectionChanged -= this.OnAnalyzersChanged;
    }

    void OnAnalyzersChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.SyncAnalyzers();

    void SyncAnalyzers() => this.Pipeline.SetAnalyzers(this.VirtualView.Analyzers);

    public static IPropertyMapper<CameraView, CameraViewHandler> Mapper =
        new PropertyMapper<CameraView, CameraViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(CameraView.Facing)] = MapFacing,
            [nameof(CameraView.CameraId)] = MapFacing,
            [nameof(CameraView.IsActive)] = MapIsActive,
            [nameof(CameraView.IsTorchOn)] = MapTorch,
            [nameof(CameraView.FlashMode)] = MapFlashMode,
            [nameof(CameraView.Zoom)] = MapZoom,
            [nameof(CameraView.ScaleMode)] = MapScaleMode,
            [nameof(CameraView.ShowDetectionOverlay)] = MapOverlay,
            [nameof(CameraView.Detections)] = MapOverlay,
            [nameof(CameraView.Filter)] = MapFilter,
        };

    public static CommandMapper<CameraView, CameraViewHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper);

    public CameraViewHandler() : base(Mapper, CommandMapper)
    {
    }

    static partial void MapFacing(CameraViewHandler handler, CameraView view);
    static partial void MapIsActive(CameraViewHandler handler, CameraView view);
    static partial void MapTorch(CameraViewHandler handler, CameraView view);
    static partial void MapFlashMode(CameraViewHandler handler, CameraView view);
    static partial void MapZoom(CameraViewHandler handler, CameraView view);
    static partial void MapScaleMode(CameraViewHandler handler, CameraView view);
    static partial void MapOverlay(CameraViewHandler handler, CameraView view);
    static partial void MapFilter(CameraViewHandler handler, CameraView view);
}
#endif
