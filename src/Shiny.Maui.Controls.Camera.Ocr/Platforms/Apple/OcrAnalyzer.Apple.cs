using Foundation;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Vision;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class OcrAnalyzer
{
    private partial Task<List<Detection>> RecognizeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AppleCameraFrame apple)
            return Task.FromResult(new List<Detection>());

        var cg = apple.ToCGImage();
        if (cg == null)
            return Task.FromResult(new List<Detection>());

        var tcs = new TaskCompletionSource<List<Detection>>();
        var request = new VNRecognizeTextRequest((req, err) =>
        {
            var detections = new List<Detection>();
            if (err == null && req.GetResults<VNRecognizedTextObservation>() is { } results)
            {
                foreach (var obs in results)
                {
                    var candidate = obs.TopCandidates(1)?.FirstOrDefault();
                    var bb = obs.BoundingBox;
                    var raw = new RectF((float)bb.X, (float)(1 - bb.Y - bb.Height), (float)bb.Width, (float)bb.Height);
                    var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
                    detections.Add(new Detection(DetectionType.Text, box, null, candidate?.String, candidate?.Confidence ?? 1f));
                }
            }
            tcs.TrySetResult(detections);
            cg.Dispose();
        })
        {
            RecognitionLevel = VNRequestTextRecognitionLevel.Accurate
        };

        try
        {
            using var handler = new VNImageRequestHandler(cg, new NSDictionary());
            handler.Perform([request], out _);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }
}
