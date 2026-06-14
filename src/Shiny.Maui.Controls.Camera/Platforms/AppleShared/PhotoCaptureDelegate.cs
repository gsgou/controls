using AVFoundation;
using CoreGraphics;
using CoreImage;
using Foundation;
using ImageIO;

namespace Shiny.Maui.Controls.Camera;

// Bridges AVCapturePhotoOutput's delegate callback to a Task<CameraPhoto>. When a Filter is set, the captured
// still is run through the same CameraFilter as the live preview before encoding, so photos match what the
// user sees. Stays UIKit/AppKit-free (CoreImage + ImageIO) so the one delegate serves iOS and macOS alike.
sealed class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
{
    readonly TaskCompletionSource<CameraPhoto> tcs = new();

    public Task<CameraPhoto> Task => this.tcs.Task;

    /// <summary>Set by the handler to the preview's current filter; null = unfiltered (raw capture).</summary>
    public CIFilter? Filter;

    public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
    {
        if (error != null)
        {
            this.tcs.TrySetException(new InvalidOperationException(error.LocalizedDescription));
            return;
        }

        try
        {
            var result = this.Filter != null
                ? Filtered(photo, this.Filter)
                : Raw(photo);

            if (result == null)
                this.tcs.TrySetException(new InvalidOperationException("No photo data was produced"));
            else
                this.tcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            this.tcs.TrySetException(ex);
        }
    }

    static CameraPhoto? Raw(AVCapturePhoto photo)
    {
        using var data = photo.FileDataRepresentation;
        if (data == null)
            return null;

        var dims = photo.ResolvedSettings.PhotoDimensions;
        return new CameraPhoto(data.ToArray(), dims.Width, dims.Height);
    }

    static CameraPhoto? Filtered(AVCapturePhoto photo, CIFilter filter)
    {
        using var cg = photo.CGImageRepresentation;
        if (cg == null)
            return Raw(photo); // no CGImage to filter — fall back to the unfiltered JPEG

        // CGImageRepresentation is in stored (sensor) orientation; apply the EXIF orientation so the filtered
        // result is upright. This orientation handling is the part most worth verifying on-device.
        using var source = new CIImage(cg);
        var oriented = source.CreateByApplyingOrientation(ReadOrientation(photo));

        filter.SetValueForKey(oriented, new NSString("inputImage"));
        using var outputImage = filter.OutputImage;
        if (outputImage == null)
            return Raw(photo);

        using var context = new CIContext();
        using var outCg = context.CreateCGImage(outputImage, outputImage.Extent);
        if (outCg == null)
            return Raw(photo);

        var bytes = EncodeJpeg(outCg);
        return bytes == null ? Raw(photo) : new CameraPhoto(bytes, (int)outCg.Width, (int)outCg.Height);
    }

    static CGImagePropertyOrientation ReadOrientation(AVCapturePhoto photo)
    {
        // CGImageRepresentation drops orientation; read the EXIF orientation (1..8) from the JPEG instead
        using var data = photo.FileDataRepresentation;
        if (data == null)
            return CGImagePropertyOrientation.Up;

        using var source = CGImageSource.FromData(data);
        var props = source?.CopyProperties((NSDictionary?)null, 0);
        if (props?[(NSString)"Orientation"] is NSNumber n)
            return (CGImagePropertyOrientation)n.Int32Value;
        return CGImagePropertyOrientation.Up;
    }

    static byte[]? EncodeJpeg(CGImage image)
    {
        using var data = new NSMutableData();
        using var dest = CGImageDestination.Create(data, "public.jpeg", 1);
        if (dest == null)
            return null;

        dest.AddImage(image);
        return dest.Close() ? data.ToArray() : null;
    }
}
