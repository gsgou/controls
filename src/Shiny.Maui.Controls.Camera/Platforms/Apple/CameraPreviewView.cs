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

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        if (this.fallbackLayer != null)
            this.fallbackLayer.Frame = this.Bounds;
    }
}
