using Foundation;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Vision;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>Face detection via Apple Vision (iOS, MacCatalyst, macOS).</summary>
public class FaceAnalyzer : IFrameAnalyzer
{
    public string Id => "shiny.camera.face";

    public ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AppleCameraFrame apple)
            return new ValueTask<DetectionResult?>(DetectionResult.Empty(this.Id));

        using var cg = apple.ToCGImage();
        if (cg == null)
            return new ValueTask<DetectionResult?>(DetectionResult.Empty(this.Id));

        var tcs = new TaskCompletionSource<DetectionResult?>();
        var request = new VNDetectFaceRectanglesRequest((req, err) =>
        {
            var detections = new List<Detection>();
            if (err == null && req.GetResults<VNFaceObservation>() is { } faces)
            {
                foreach (var face in faces)
                {
                    var bb = face.BoundingBox; // normalized, origin bottom-left
                    var raw = new RectF((float)bb.X, (float)(1 - bb.Y - bb.Height), (float)bb.Width, (float)bb.Height);
                    var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
                    detections.Add(new Detection(DetectionType.Face, box, "Face", null, face.Confidence));
                }
            }
            tcs.TrySetResult(new DetectionResult(this.Id, detections));
        });

        try
        {
            using var handler = new VNImageRequestHandler(cg, new NSDictionary());
            handler.Perform([request], out _);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return new ValueTask<DetectionResult?>(tcs.Task);
    }
}
