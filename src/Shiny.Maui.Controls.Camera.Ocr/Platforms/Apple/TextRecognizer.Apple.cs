using Foundation;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Vision;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class TextRecognizer
{
    private partial Task<List<RecognizedText>> RecognizeCoreAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AppleCameraFrame apple)
            return Task.FromResult(new List<RecognizedText>());

        var cg = apple.ToCGImage();
        if (cg == null)
            return Task.FromResult(new List<RecognizedText>());

        var tcs = new TaskCompletionSource<List<RecognizedText>>();
        var request = new VNRecognizeTextRequest((req, err) =>
        {
            var blocks = new List<RecognizedText>();
            if (err == null && req.GetResults<VNRecognizedTextObservation>() is { } results)
            {
                foreach (var obs in results)
                {
                    var candidate = obs.TopCandidates(1)?.FirstOrDefault();
                    var bb = obs.BoundingBox;
                    var raw = new RectF((float)bb.X, (float)(1 - bb.Y - bb.Height), (float)bb.Width, (float)bb.Height);
                    var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
                    blocks.Add(new RecognizedText(candidate?.String ?? string.Empty, box, candidate?.Confidence ?? 1f));
                }
            }
            tcs.TrySetResult(blocks);
            cg.Dispose();
        })
        {
            RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
            // Off: Vision's dictionary "correction" mangles structured fields (license/MRZ/card numbers,
            // totals, dates) by snapping codes to words (0->O, 1->I). Our parsers fuzzy-match raw text.
            UsesLanguageCorrection = false
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
