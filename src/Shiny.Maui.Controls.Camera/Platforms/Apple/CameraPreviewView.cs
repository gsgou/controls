using AVFoundation;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// A <see cref="UIView"/> whose backing layer is an <see cref="AVCaptureVideoPreviewLayer"/>, so the
/// preview resizes with the view automatically. Used on iOS and MacCatalyst.
/// </summary>
public sealed class CameraPreviewView : UIView
{
    [Export("layerClass")]
    public static Class LayerClass() => new(typeof(AVCaptureVideoPreviewLayer));

    public CameraPreviewView()
    {
        this.BackgroundColor = UIColor.Black;
        this.PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
    }

    public AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer)this.Layer!;
}
