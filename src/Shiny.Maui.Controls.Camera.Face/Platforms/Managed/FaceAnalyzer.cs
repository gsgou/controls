using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>No-op fallback for the platform-agnostic (net10.0) target; face detection requires a native backend.</summary>
public class FaceAnalyzer : IFrameAnalyzer
{
    public string Id => "shiny.camera.face";

    public ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
        => new(DetectionResult.Empty(this.Id));
}
