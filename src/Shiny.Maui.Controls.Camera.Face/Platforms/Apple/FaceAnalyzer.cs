using Foundation;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Vision;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>Face detection via Apple Vision (iOS, MacCatalyst, macOS).</summary>
public partial class FaceAnalyzer
{
    /// <inheritdoc/>
    public override ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AppleCameraFrame apple)
            return default;

        using var cg = apple.ToCGImage();
        if (cg == null)
            return default;

        var tcs = new TaskCompletionSource<IReadOnlyList<OverlayBox>?>();
        var request = new VNDetectFaceRectanglesRequest((req, err) =>
        {
            var faces = new List<DetectedFace>();
            if (err == null && req.GetResults<VNFaceObservation>() is { } observations)
            {
                foreach (var face in observations)
                {
                    var bb = face.BoundingBox; // normalized, origin bottom-left
                    var raw = new RectF((float)bb.X, (float)(1 - bb.Y - bb.Height), (float)bb.Width, (float)bb.Height);
                    var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
                    faces.Add(new DetectedFace(box, face.Confidence));
                }
            }
            tcs.TrySetResult(this.Report(faces));
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

        return new ValueTask<IReadOnlyList<OverlayBox>?>(tcs.Task);
    }
}
