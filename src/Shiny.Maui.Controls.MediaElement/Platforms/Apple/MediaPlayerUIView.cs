using AVFoundation;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Shiny.Maui.Controls.Media;

/// <summary>The iOS / Mac Catalyst video surface: a <see cref="UIView"/> whose backing layer <i>is</i> the player layer.</summary>
/// <remarks>
/// Overriding <c>layerClass</c> rather than adding a sublayer means the layer resizes with the view for
/// free. A hosted sublayer would need its frame re-set on every layout pass, and any pass missed while
/// rotating leaves the video the wrong size.
/// </remarks>
public class MediaPlayerUIView : UIView, IApplePlayerOutput
{
    [Export("layerClass")]
    public static Class GetLayerClass() => new(typeof(AVPlayerLayer));

    /// <inheritdoc />
    public AVPlayerLayer PlayerLayer => (AVPlayerLayer)this.Layer;
}
