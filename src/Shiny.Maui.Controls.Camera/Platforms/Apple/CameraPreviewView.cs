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

    AVCaptureVideoPreviewLayer? fallbackLayer;

    public CameraPreviewView()
    {
        this.BackgroundColor = UIColor.Black;
        this.PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
    }

    /// <summary>
    /// The preview layer. Normally the view's backing layer (via <c>layerClass</c>); if that override is not
    /// honored (e.g. the static export is trimmed on an AOT build) we fall back to a managed sublayer rather
    /// than crashing on an invalid cast.
    /// </summary>
    public AVCaptureVideoPreviewLayer PreviewLayer
    {
        get
        {
            if (this.Layer is AVCaptureVideoPreviewLayer backing)
                return backing;

            if (this.fallbackLayer == null)
            {
                this.fallbackLayer = new AVCaptureVideoPreviewLayer { Frame = this.Bounds };
                this.Layer!.AddSublayer(this.fallbackLayer);
            }
            return this.fallbackLayer;
        }
    }

    /// <summary>
    /// Raised after every layout pass. The handler uses it to re-apply capture orientation.
    /// </summary>
    /// <remarks>
    /// A rotation always produces a layout pass, and by the time one runs the window scene's interface
    /// orientation has settled — which <c>UIDeviceOrientationDidChangeNotification</c> has not, since it
    /// fires ahead of the interface catching up (and reports <c>FaceUp</c>/<c>FaceDown</c> for a device
    /// lying flat, which is not an orientation the camera can be pointed in). Layout is therefore the
    /// signal, not the notification. It fires for plenty of reasons that are not rotation, so whatever
    /// subscribes must be idempotent and cheap.
    /// </remarks>
    public Action? LayoutChanged { get; set; }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        if (this.fallbackLayer != null)
            this.fallbackLayer.Frame = this.Bounds;
        this.LayoutChanged?.Invoke();
    }
}
