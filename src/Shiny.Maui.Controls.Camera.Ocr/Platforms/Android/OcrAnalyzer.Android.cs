using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class OcrAnalyzer
{
    readonly ITextRecognizer recognizer = TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);

    private async partial Task<List<Detection>> RecognizeAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AndroidCameraFrame android)
            return [];

        var mediaImage = android.Proxy.Image;
        if (mediaImage == null)
            return [];

        var rotation = android.Proxy.ImageInfo.RotationDegrees;
        var input = InputImage.FromMediaImage(mediaImage, rotation);

        var uprightW = rotation is 90 or 270 ? frame.Height : frame.Width;
        var uprightH = rotation is 90 or 270 ? frame.Width : frame.Height;

        var result = await GmsTaskAwaiter.AwaitAsync(this.recognizer.Process(input)).ConfigureAwait(false);

        var detections = new List<Detection>();
        if (result is Text text)
        {
            foreach (var block in text.TextBlocks)
            {
                foreach (var line in block.Lines)
                {
                    var r = line.BoundingBox;
                    if (r == null)
                        continue;
                    var raw = new RectF((float)r.Left / uprightW, (float)r.Top / uprightH,
                        (float)r.Width() / uprightW, (float)r.Height() / uprightH);
                    var box = CoordinateTransform.ApplyOrientation(raw, 0, frame.IsMirrored);
                    detections.Add(new Detection(DetectionType.Text, box, null, line.Text));
                }
            }
        }
        return detections;
    }
}
