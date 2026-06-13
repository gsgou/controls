using Android.Runtime;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Face;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>Face detection via Android MLKit.</summary>
public partial class FaceAnalyzer
{
    readonly IFaceDetector detector = FaceDetection.GetClient(
        new FaceDetectorOptions.Builder()
            .SetPerformanceMode(FaceDetectorOptions.PerformanceModeFast)
            .Build());

    /// <inheritdoc/>
    public override async ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AndroidCameraFrame android)
            return null;

        var mediaImage = android.Proxy.Image;
        if (mediaImage == null)
            return null;

        var rotation = android.Proxy.ImageInfo.RotationDegrees;
        var input = InputImage.FromMediaImage(mediaImage, rotation);

        // MLKit returns boxes in the upright (rotation-applied) image space
        var uprightW = rotation is 90 or 270 ? frame.Height : frame.Width;
        var uprightH = rotation is 90 or 270 ? frame.Width : frame.Height;

        var result = await GmsTaskAwaiter.AwaitAsync(this.detector.Process(input)).ConfigureAwait(false);

        var faces = new List<DetectedFace>();
        if (result is JavaList items)
        {
            foreach (var item in items)
            {
                if (item is not Xamarin.Google.MLKit.Vision.Face.Face face)
                    continue;

                var r = face.BoundingBox;
                var raw = new RectF((float)r.Left / uprightW, (float)r.Top / uprightH,
                    (float)r.Width() / uprightW, (float)r.Height() / uprightH);
                var box = CoordinateTransform.ApplyOrientation(raw, 0, frame.IsMirrored);
                faces.Add(new DetectedFace(box));
            }
        }
        return this.Report(faces);
    }
}
