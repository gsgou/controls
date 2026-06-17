using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace Shiny.Maui.Controls.Camera.Ocr;

public partial class TextRecognizer
{
    OcrEngine? engine;

    // Document path: detect the document quad and deskew it with OpenCvSharp (official Windows runtime), then
    // OCR the flat crop with Windows.Media.Ocr. Falls back to shared whole-frame OCR when no document is found.
    private async partial Task<RecognizedDocument> RecognizeDocumentCoreAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not WindowsCameraFrame)
            return new RecognizedDocument([], null);

        this.engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        if (this.engine == null)
            return new RecognizedDocument([], null);

        int w = frame.Width, h = frame.Height;
        var lum = frame.GetLuminance().ToArray();

        if (!TryRectify(lum, w, h, out var warpedBytes, out var dw, out var dh, out var corners))
            return new RecognizedDocument(await this.RecognizeAsync(frame, ct).ConfigureAwait(false), null);

        var buffer = CryptographicBuffer.CreateFromByteArray(warpedBytes);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Gray8, dw, dh);
        var result = await this.engine.RecognizeAsync(bitmap);

        var blocks = new List<RecognizedText>();
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

            // boxes are already in upright document space
            var box = new RectF((float)(minX / dw), (float)(minY / dh), (float)((maxX - minX) / dw), (float)((maxY - minY) / dh));
            blocks.Add(new RecognizedText(line.Text ?? string.Empty, box));
        }

        // Windows frames are un-rotated (Rotation == 0); apply mirror only.
        var quad = new DocumentQuad(
            CoordinateTransform.ApplyOrientation(corners[0], frame.Rotation, frame.IsMirrored),
            CoordinateTransform.ApplyOrientation(corners[1], frame.Rotation, frame.IsMirrored),
            CoordinateTransform.ApplyOrientation(corners[2], frame.Rotation, frame.IsMirrored),
            CoordinateTransform.ApplyOrientation(corners[3], frame.Rotation, frame.IsMirrored));

        return new RecognizedDocument(blocks, quad);
    }

    // Detect the largest convex 4-gon (document) via Canny + contours, then perspective-warp it flat. Returns
    // the deskewed Gray8 bytes (dw x dh) and the source corners normalized in sensor space (TL,TR,BR,BL).
    static bool TryRectify(byte[] lum, int w, int h, out byte[] warped, out int dw, out int dh, out PointF[] corners)
    {
        warped = [];
        dw = dh = 0;
        corners = [];

        using var gray = new OpenCvSharp.Mat(h, w, OpenCvSharp.MatType.CV_8UC1);
        System.Runtime.InteropServices.Marshal.Copy(lum, 0, gray.Data, lum.Length);

        using var blurred = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);
        using var edges = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.Canny(blurred, edges, 50, 150);

        OpenCvSharp.Cv2.FindContours(edges, out OpenCvSharp.Point[][] contours, out _,
            OpenCvSharp.RetrievalModes.External, OpenCvSharp.ContourApproximationModes.ApproxSimple);

        OpenCvSharp.Point[]? best = null;
        double bestArea = 0;
        foreach (var c in contours)
        {
            var peri = OpenCvSharp.Cv2.ArcLength(c, true);
            var approx = OpenCvSharp.Cv2.ApproxPolyDP(c, 0.02 * peri, true);
            if (approx.Length != 4 || !OpenCvSharp.Cv2.IsContourConvex(approx))
                continue;
            var area = Math.Abs(OpenCvSharp.Cv2.ContourArea(approx));
            if (area > bestArea)
            {
                bestArea = area;
                best = approx;
            }
        }

        if (best == null || bestArea < 0.15 * w * h)
            return false;

        var ordered = OrderCorners(best);
        dw = Math.Clamp((int)Math.Max(Dist(ordered[0], ordered[1]), Dist(ordered[3], ordered[2])), 64, w);
        dh = Math.Clamp((int)Math.Max(Dist(ordered[0], ordered[3]), Dist(ordered[1], ordered[2])), 64, h);

        var src = new[]
        {
            new OpenCvSharp.Point2f(ordered[0].X, ordered[0].Y),
            new OpenCvSharp.Point2f(ordered[1].X, ordered[1].Y),
            new OpenCvSharp.Point2f(ordered[2].X, ordered[2].Y),
            new OpenCvSharp.Point2f(ordered[3].X, ordered[3].Y)
        };
        var dst = new[]
        {
            new OpenCvSharp.Point2f(0, 0), new OpenCvSharp.Point2f(dw, 0),
            new OpenCvSharp.Point2f(dw, dh), new OpenCvSharp.Point2f(0, dh)
        };

        using var transform = OpenCvSharp.Cv2.GetPerspectiveTransform(src, dst);
        using var outMat = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.WarpPerspective(gray, outMat, transform, new OpenCvSharp.Size(dw, dh));

        warped = new byte[dw * dh];
        System.Runtime.InteropServices.Marshal.Copy(outMat.Data, warped, 0, warped.Length);

        corners =
        [
            new PointF(ordered[0].X / (float)w, ordered[0].Y / (float)h),
            new PointF(ordered[1].X / (float)w, ordered[1].Y / (float)h),
            new PointF(ordered[2].X / (float)w, ordered[2].Y / (float)h),
            new PointF(ordered[3].X / (float)w, ordered[3].Y / (float)h)
        ];
        return true;
    }

    // Order four points as TL (min x+y), TR (max x-y), BR (max x+y), BL (min x-y).
    static OpenCvSharp.Point[] OrderCorners(OpenCvSharp.Point[] pts)
    {
        OpenCvSharp.Point tl = pts[0], tr = pts[0], br = pts[0], bl = pts[0];
        int minS = int.MaxValue, maxS = int.MinValue, minD = int.MaxValue, maxD = int.MinValue;
        foreach (var p in pts)
        {
            int s = p.X + p.Y, d = p.X - p.Y;
            if (s < minS) { minS = s; tl = p; }
            if (s > maxS) { maxS = s; br = p; }
            if (d > maxD) { maxD = d; tr = p; }
            if (d < minD) { minD = d; bl = p; }
        }
        return [tl, tr, br, bl];
    }

    static double Dist(OpenCvSharp.Point a, OpenCvSharp.Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private async partial Task<List<RecognizedText>> RecognizeCoreAsync(CameraFrame frame, CancellationToken ct)
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

        var blocks = new List<RecognizedText>();
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
            blocks.Add(new RecognizedText(line.Text ?? string.Empty, box));
        }
        return blocks;
    }
}
