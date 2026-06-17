using CoreGraphics;
using CoreImage;
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

        using var cg = apple.ToCGImage();
        if (cg == null)
            return Task.FromResult(new List<RecognizedText>());

        try
        {
            // whole-frame OCR: boxes are in sensor space, so apply the frame's orientation
            return Task.FromResult(RecognizeText(cg, frame));
        }
        catch (Exception ex)
        {
            return Task.FromException<List<RecognizedText>>(ex);
        }
    }

    private partial Task<RecognizedDocument> RecognizeDocumentCoreAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AppleCameraFrame apple)
            return Task.FromResult(new RecognizedDocument([], null));

        using var cg = apple.ToCGImage();
        if (cg == null)
            return Task.FromResult(new RecognizedDocument([], null));

        try
        {
            // 1) Detect the document quad. Vision document segmentation is iOS 15+/macOS 12+; if it's
            //    unavailable or finds nothing we just OCR the whole frame.
            VNRectangleObservation? doc = null;
            try
            {
                var seg = new VNDetectDocumentSegmentationRequest((req, err) =>
                {
                    if (err == null)
                        doc = req.GetResults<VNRectangleObservation>()?.FirstOrDefault();
                });
                using var segHandler = new VNImageRequestHandler(cg, new NSDictionary());
                segHandler.Perform([seg], out _);
            }
            catch
            {
                doc = null;
            }

            if (doc == null)
                // no document found: whole-frame OCR (boxes in sensor space → orient)
                return Task.FromResult(new RecognizedDocument(RecognizeText(cg, frame), null));

            // 2) Deskew the document into a flat image, then OCR that (boxes already in upright doc space).
            using var rectified = PerspectiveCorrect(cg, doc);
            var quad = ToFrameQuad(doc, frame);
            if (rectified == null)
                return Task.FromResult(new RecognizedDocument(RecognizeText(cg, frame), quad));

            return Task.FromResult(new RecognizedDocument(RecognizeText(rectified, null), quad));
        }
        catch (Exception ex)
        {
            return Task.FromException<RecognizedDocument>(ex);
        }
    }

    // Run Vision text recognition over an image. When frameForOrientation is set the image is the raw sensor
    // buffer, so results are mapped into upright space; when null the image is already upright (a deskewed crop).
    static List<RecognizedText> RecognizeText(CGImage image, CameraFrame? frameForOrientation)
    {
        var blocks = new List<RecognizedText>();
        var request = new VNRecognizeTextRequest((req, err) =>
        {
            if (err == null && req.GetResults<VNRecognizedTextObservation>() is { } results)
            {
                foreach (var obs in results)
                {
                    var candidate = obs.TopCandidates(1)?.FirstOrDefault();
                    var bb = obs.BoundingBox; // normalized, origin bottom-left
                    var raw = new RectF((float)bb.X, (float)(1 - bb.Y - bb.Height), (float)bb.Width, (float)bb.Height);
                    var box = frameForOrientation is { } f
                        ? CoordinateTransform.ApplyOrientation(raw, f.Rotation, f.IsMirrored)
                        : raw;
                    blocks.Add(new RecognizedText(candidate?.String ?? string.Empty, box, candidate?.Confidence ?? 1f));
                }
            }
        })
        {
            RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
            // Off: Vision's dictionary "correction" mangles structured fields (license/MRZ/card numbers,
            // totals, dates) by snapping codes to words (0->O, 1->I). Our parsers fuzzy-match raw text.
            UsesLanguageCorrection = false
        };

        using var handler = new VNImageRequestHandler(image, new NSDictionary());
        handler.Perform([request], out _);
        return blocks;
    }

    // Perspective-correct (deskew) the document region into a flat CGImage using Core Image. Vision corner
    // points are normalized with a bottom-left origin, matching CIImage's coordinate space.
    static CGImage? PerspectiveCorrect(CGImage source, VNRectangleObservation doc)
    {
        using var ci = new CIImage(source);
        var ext = ci.Extent;
        CIVector V(CGPoint p) => new((nfloat)(ext.X + p.X * ext.Width), (nfloat)(ext.Y + p.Y * ext.Height));

        using var filter = CIFilter.FromName("CIPerspectiveCorrection");
        if (filter == null)
            return null;
        filter.SetValueForKey(ci, (NSString)"inputImage");
        filter.SetValueForKey(V(doc.TopLeft), (NSString)"inputTopLeft");
        filter.SetValueForKey(V(doc.TopRight), (NSString)"inputTopRight");
        filter.SetValueForKey(V(doc.BottomRight), (NSString)"inputBottomRight");
        filter.SetValueForKey(V(doc.BottomLeft), (NSString)"inputBottomLeft");

        var output = filter.OutputImage;
        if (output == null)
            return null;

        using var context = new CIContext();
        return context.CreateCGImage(output, output.Extent);
    }

    // Vision corners are normalized with a bottom-left origin; flip Y to top-left and apply the frame's
    // orientation so the quad lands in the same upright space the overlay draws in.
    static DocumentQuad ToFrameQuad(VNRectangleObservation doc, CameraFrame frame)
    {
        PointF P(CGPoint p) => CoordinateTransform.ApplyOrientation(
            new PointF((float)p.X, (float)(1 - p.Y)), frame.Rotation, frame.IsMirrored);

        return new DocumentQuad(P(doc.TopLeft), P(doc.TopRight), P(doc.BottomRight), P(doc.BottomLeft));
    }
}
