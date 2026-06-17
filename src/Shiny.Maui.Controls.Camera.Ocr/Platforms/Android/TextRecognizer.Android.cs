using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class TextRecognizer
{
    readonly Xamarin.Google.MLKit.Vision.Text.ITextRecognizer recognizer =
        TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);

    // Document path: detect the document quad in managed code (no OpenCV), deskew it natively with
    // Matrix.SetPolyToPoly (a true 4-point perspective warp), then OCR the flat crop. Falls back to shared
    // whole-frame OCR when no plausible document is found.
    private async partial Task<RecognizedDocument> RecognizeDocumentCoreAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AndroidCameraFrame)
            return new RecognizedDocument([], null);

        int w = frame.Width, h = frame.Height;
        var lum = frame.GetLuminance().ToArray();

        if (!ManagedDocumentDetector.TryDetect(lum, w, h, out var tl, out var tr, out var br, out var bl))
            return new RecognizedDocument(await this.RecognizeAsync(frame, ct).ConfigureAwait(false), null);

        // Re-assign the corners to TL/TR/BR/BL by their *upright* position so the deskewed crop comes out
        // upright (the sensor buffer may be rotated 90/270). src points stay in sensor pixels for the warp.
        var sensor = new[] { tl, tr, br, bl };
        var up = new PointF[4];
        for (var i = 0; i < 4; i++)
            up[i] = CoordinateTransform.ApplyOrientation(sensor[i], frame.Rotation, frame.IsMirrored);

        int iTL = 0, iTR = 0, iBR = 0, iBL = 0;
        float minSum = float.MaxValue, maxSum = float.MinValue, minDiff = float.MaxValue, maxDiff = float.MinValue;
        for (var i = 0; i < 4; i++)
        {
            float s = up[i].X + up[i].Y, d = up[i].X - up[i].Y;
            if (s < minSum) { minSum = s; iTL = i; }
            if (s > maxSum) { maxSum = s; iBR = i; }
            if (d > maxDiff) { maxDiff = d; iTR = i; }
            if (d < minDiff) { minDiff = d; iBL = i; }
        }

        int uw = frame.Rotation is 90 or 270 ? h : w;
        int uh = frame.Rotation is 90 or 270 ? w : h;
        var dstW = Math.Clamp((int)(Math.Max(Len(up[iTL], up[iTR]), Len(up[iBL], up[iBR])) * uw), 64, uw);
        var dstH = Math.Clamp((int)(Math.Max(Len(up[iTL], up[iBL]), Len(up[iTR], up[iBR])) * uh), 64, uh);

        var src = new[]
        {
            sensor[iTL].X * w, sensor[iTL].Y * h, sensor[iTR].X * w, sensor[iTR].Y * h,
            sensor[iBR].X * w, sensor[iBR].Y * h, sensor[iBL].X * w, sensor[iBL].Y * h
        };
        var dst = new float[] { 0, 0, dstW, 0, dstW, dstH, 0, dstH };

        using var srcBitmap = LuminanceToBitmap(lum, w, h);
        using var matrix = new Android.Graphics.Matrix();
        matrix.SetPolyToPoly(src, 0, dst, 0, 4);

        using var warped = Android.Graphics.Bitmap.CreateBitmap(dstW, dstH, Android.Graphics.Bitmap.Config.Argb8888!);
        using (var canvas = new Android.Graphics.Canvas(warped))
        using (var paint = new Android.Graphics.Paint { FilterBitmap = true })
            canvas.DrawBitmap(srcBitmap, matrix, paint);

        var input = InputImage.FromBitmap(warped, 0);
        var result = await GmsTaskAwaiter.AwaitAsync(this.recognizer.Process(input)).ConfigureAwait(false);

        var blocks = new List<RecognizedText>();
        if (result is Text text)
        {
            foreach (var block in text.TextBlocks)
            {
                foreach (var line in block.Lines)
                {
                    var r = line.BoundingBox;
                    if (r == null)
                        continue;
                    // boxes are already in upright document space
                    var box = new RectF((float)r.Left / dstW, (float)r.Top / dstH, (float)r.Width() / dstW, (float)r.Height() / dstH);
                    blocks.Add(new RecognizedText(line.Text ?? string.Empty, box));
                }
            }
        }

        var quad = new DocumentQuad(up[iTL], up[iTR], up[iBR], up[iBL]);
        return new RecognizedDocument(blocks, quad);
    }

    static float Len(PointF a, PointF b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    static Android.Graphics.Bitmap LuminanceToBitmap(byte[] lum, int w, int h)
    {
        var pixels = new int[w * h];
        for (var i = 0; i < pixels.Length; i++)
        {
            int v = lum[i];
            pixels[i] = unchecked((int)0xFF000000) | (v << 16) | (v << 8) | v;
        }
        return Android.Graphics.Bitmap.CreateBitmap(pixels, w, h, Android.Graphics.Bitmap.Config.Argb8888!);
    }

    private async partial Task<List<RecognizedText>> RecognizeCoreAsync(CameraFrame frame, CancellationToken ct)
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

        var blocks = new List<RecognizedText>();
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
                    blocks.Add(new RecognizedText(line.Text ?? string.Empty, box));
                }
            }
        }
        return blocks;
    }
}
