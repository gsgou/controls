using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class OcrAnalyzer
{
    OcrEngine? engine;

    private async partial Task<List<Detection>> RecognizeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not WindowsCameraFrame)
            return [];

        this.engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        if (this.engine == null)
            return [];

        var lum = frame.GetLuminance().ToArray();
        var buffer = CryptographicBuffer.CreateFromByteArray(lum);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Gray8, frame.Width, frame.Height);

        var result = await this.engine.RecognizeAsync(bitmap);

        var detections = new List<Detection>();
        foreach (var line in result.Lines)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = 0, maxY = 0;
            foreach (var word in line.Words)
            {
                var r = word.BoundingRect;
                minX = Math.Min(minX, r.X);
                minY = Math.Min(minY, r.Y);
                maxX = Math.Max(maxX, r.X + r.Width);
                maxY = Math.Max(maxY, r.Y + r.Height);
            }
            if (maxX <= minX)
                continue;

            var raw = new RectF((float)(minX / frame.Width), (float)(minY / frame.Height),
                (float)((maxX - minX) / frame.Width), (float)((maxY - minY) / frame.Height));
            var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
            detections.Add(new Detection(DetectionType.Text, box, null, line.Text));
        }
        return detections;
    }
}
