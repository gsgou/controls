using Android.Graphics;
using Java.Nio;
using Shiny.Controls.Camera;
using AGraphicsRect = Android.Graphics.Rect;
using RectF = Microsoft.Maui.Graphics.RectF;

namespace Shiny.Maui.Controls.Camera.Ai;

// Android: there's no lightweight native rectangle detector for live frames (MLKit's document scanner is a
// full-screen flow), so presence detection uses the managed edge detector over the cached luminance plane —
// the same approach the OCR document path uses. Encoding pulls the YUV_420_888 frame, JPEGs it, then rotates,
// un-mirrors and crops to the requested region.
partial class DocumentImageExtractor
{
    public partial DocumentQuad? Detect(CameraFrame frame) => ManagedDocumentEdgeDetector.Detect(frame);

    public partial byte[]? Encode(CameraFrame frame, RectF cropUpright)
    {
        if (frame is not AndroidCameraFrame android)
            return null;

        var nv21 = Yuv420ToNv21(android.Proxy);
        int w = frame.Width, h = frame.Height;

        // YUV -> full-frame JPEG (still in sensor orientation)
        byte[] sensorJpeg;
        using (var yuv = new YuvImage(nv21, ImageFormatType.Nv21, w, h, null))
        using (var ms = new MemoryStream())
        {
            yuv.CompressToJpeg(new AGraphicsRect(0, 0, w, h), 90, ms);
            sensorJpeg = ms.ToArray();
        }

        using var sensor = BitmapFactory.DecodeByteArray(sensorJpeg, 0, sensorJpeg.Length);
        if (sensor == null)
            return null;

        // rotate upright + un-mirror the front camera in one transform
        Bitmap upright;
        if (frame.Rotation != 0 || frame.IsMirrored)
        {
            using var m = new Matrix();
            m.PostRotate(frame.Rotation);
            if (frame.IsMirrored)
                m.PostScale(-1f, 1f);
            upright = Bitmap.CreateBitmap(sensor, 0, 0, sensor.Width, sensor.Height, m, true)!;
        }
        else
        {
            upright = sensor;
        }

        try
        {
            int uw = upright.Width, uh = upright.Height;
            var x = Clamp((int)(cropUpright.X * uw), 0, uw - 1);
            var y = Clamp((int)(cropUpright.Y * uh), 0, uh - 1);
            var cw = Clamp((int)(cropUpright.Width * uw), 1, uw - x);
            var ch = Clamp((int)(cropUpright.Height * uh), 1, uh - y);

            using var cropped = Bitmap.CreateBitmap(upright, x, y, cw, ch)!;
            using var outMs = new MemoryStream();
            cropped.Compress(Bitmap.CompressFormat.Jpeg!, 88, outMs);
            return outMs.ToArray();
        }
        finally
        {
            if (!ReferenceEquals(upright, sensor))
                upright.Dispose();
        }
    }

    static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    // Convert a CameraX YUV_420_888 ImageProxy to an NV21 byte[] (Y plane followed by interleaved V,U),
    // honoring each plane's row/pixel stride.
    static byte[] Yuv420ToNv21(AndroidX.Camera.Core.IImageProxy image)
    {
        int w = image.Width, h = image.Height;
        var planes = image.GetPlanes()!;
        var nv21 = new byte[w * h * 3 / 2];

        // Y plane
        CopyPlane(planes[0].Buffer!, planes[0].RowStride, planes[0].PixelStride, w, h, nv21, 0, 1);

        // VU interleaved (NV21 = ...VUVUVU). Chroma planes are half resolution.
        var uBuffer = planes[1].Buffer!;
        var vBuffer = planes[2].Buffer!;
        int uRowStride = planes[1].RowStride, uPixStride = planes[1].PixelStride;
        int vRowStride = planes[2].RowStride, vPixStride = planes[2].PixelStride;
        int cw = w / 2, ch = h / 2;
        var pos = w * h;

        var uRow = new byte[uRowStride];
        var vRow = new byte[vRowStride];
        for (var row = 0; row < ch; row++)
        {
            vBuffer.Position(row * vRowStride);
            vBuffer.Get(vRow, 0, Math.Min(vRowStride, vBuffer.Remaining()));
            uBuffer.Position(row * uRowStride);
            uBuffer.Get(uRow, 0, Math.Min(uRowStride, uBuffer.Remaining()));
            for (var col = 0; col < cw; col++)
            {
                nv21[pos++] = vRow[col * vPixStride];
                nv21[pos++] = uRow[col * uPixStride];
            }
        }
        return nv21;
    }

    static void CopyPlane(ByteBuffer buffer, int rowStride, int pixelStride, int w, int h, byte[] dest, int destOffset, int destPixelStride)
    {
        var pos = destOffset;
        if (pixelStride == 1 && destPixelStride == 1 && rowStride == w)
        {
            buffer.Position(0);
            buffer.Get(dest, destOffset, w * h);
            return;
        }

        var rowBytes = new byte[rowStride];
        for (var row = 0; row < h; row++)
        {
            buffer.Position(row * rowStride);
            buffer.Get(rowBytes, 0, Math.Min(rowStride, buffer.Remaining()));
            for (var col = 0; col < w; col++)
            {
                dest[pos] = rowBytes[col * pixelStride];
                pos += destPixelStride;
            }
        }
    }
}
