#if ANDROID || IOS || MACCATALYST || WINDOWS || MACOS
using Microsoft.Maui.Handlers;
using Shiny.Maui.Controls.Camera.Internal;

namespace Shiny.Maui.Controls.Camera;

public partial class CameraViewHandler
{
    internal readonly CameraPipeline Pipeline = new();

    // Wire overlay delivery (marshaled to the UI thread) and bind the active analyzer to the control's
    // single Analyzer property. Call from each platform's ConnectHandler.
    private protected void InitPipeline()
    {
        this.Pipeline.OnOverlays = (boxes, window, w, h) =>
            this.VirtualView?.Dispatcher.Dispatch(() => this.VirtualView?.OnOverlaysChanged(boxes, window, w, h));

        // the active analyzer changed (assignment or an IsEnabled toggle); platforms that bind use cases
        // up-front (Android) re-evaluate, others no-op
        this.Pipeline.OnActiveChanged = () => this.OnAnalyzersSynced();

        // the analyzer raises its typed events on the UI thread via this dispatcher
        this.Pipeline.SetDispatcher(a => this.VirtualView?.Dispatcher.Dispatch(a));

        this.SyncAnalyzer();
    }

    private protected void TeardownPipeline()
    {
        this.Pipeline.SetAnalyzer(null);
    }

    void SyncAnalyzer() => this.Pipeline.SetAnalyzer(this.VirtualView?.Analyzer);

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
            [nameof(CameraView.Overlays)] = MapOverlay,
            [nameof(CameraView.ScanWindow)] = MapOverlay,
            [nameof(CameraView.Analyzer)] = MapAnalyzer,
            [nameof(CameraView.Filter)] = MapFilter,
        };

    static void MapAnalyzer(CameraViewHandler handler, CameraView view) => handler.SyncAnalyzer();

    public static CommandMapper<CameraView, CameraViewHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper);

    public CameraViewHandler() : base(Mapper, CommandMapper)
    {
    }

    // Implemented only where binding the analyzer use case is decided up-front (Android); elsewhere elided.
    partial void OnAnalyzersSynced();

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
