using Android.Runtime;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Face;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>Face detection via Android MLKit.</summary>
public class FaceAnalyzer : IFrameAnalyzer
{
    readonly IFaceDetector detector = FaceDetection.GetClient(
        new FaceDetectorOptions.Builder()
            .SetPerformanceMode(FaceDetectorOptions.PerformanceModeFast)
            .Build());

    public string Id => "shiny.camera.face";

    public async ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AndroidCameraFrame android)
            return DetectionResult.Empty(this.Id);

        var mediaImage = android.Proxy.Image;
        if (mediaImage == null)
            return DetectionResult.Empty(this.Id);

        var rotation = android.Proxy.ImageInfo.RotationDegrees;
        var input = InputImage.FromMediaImage(mediaImage, rotation);

        // MLKit returns boxes in the upright (rotation-applied) image space
        var uprightW = rotation is 90 or 270 ? frame.Height : frame.Width;
        var uprightH = rotation is 90 or 270 ? frame.Width : frame.Height;

        var result = await GmsTaskAwaiter.AwaitAsync(this.detector.Process(input)).ConfigureAwait(false);

        var detections = new List<Detection>();
        if (result is JavaList faces)
        {
            foreach (var item in faces)
            {
                if (item is not Xamarin.Google.MLKit.Vision.Face.Face face)
                    continue;

                var r = face.BoundingBox;
                var raw = new RectF((float)r.Left / uprightW, (float)r.Top / uprightH,
                    (float)r.Width() / uprightW, (float)r.Height() / uprightH);
                var box = CoordinateTransform.ApplyOrientation(raw, 0, frame.IsMirrored);
                detections.Add(new Detection(DetectionType.Face, box, "Face"));
            }
        }
        return new DetectionResult(this.Id, detections);
    }
}
