using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Security.Cryptography;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>Face detection via Windows.Media.FaceAnalysis over the frame's Gray8 luminance plane.</summary>
public class FaceAnalyzer : IFrameAnalyzer
{
    FaceDetector? detector;

    public string Id => "shiny.camera.face";

    public async ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not WindowsCameraFrame)
            return DetectionResult.Empty(this.Id);

        this.detector ??= await FaceDetector.CreateAsync();

        var lum = frame.GetLuminance().ToArray();
        var buffer = CryptographicBuffer.CreateFromByteArray(lum);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Gray8, frame.Width, frame.Height);

        var faces = await this.detector.DetectFacesAsync(bitmap);

        var detections = new List<Detection>();
        foreach (var face in faces)
        {
            var b = face.FaceBox;
            var raw = new RectF((float)b.X / frame.Width, (float)b.Y / frame.Height,
                (float)b.Width / frame.Width, (float)b.Height / frame.Height);
            var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
            detections.Add(new Detection(DetectionType.Face, box, "Face"));
        }
        return new DetectionResult(this.Id, detections);
    }
}
