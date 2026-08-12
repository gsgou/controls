using AppKit;
using AVFoundation;
using CoreAnimation;

namespace Shiny.Maui.Controls.Media;

/// <summary>The macOS AppKit video surface: an <see cref="NSView"/> backed directly by an <see cref="AVPlayerLayer"/>.</summary>
/// <remarks>
/// AppKit has no <c>layerClass</c>, so the equivalent is <see cref="MakeBackingLayer"/> plus
/// <c>WantsLayer</c>. Same reasoning as the UIKit view: making the player layer the <i>backing</i> layer
/// means AppKit resizes it, rather than us chasing the frame on every layout.
/// </remarks>
public class MediaPlayerNSView : NSView, IApplePlayerOutput
{
    public MediaPlayerNSView()
    {
        this.WantsLayer = true;
        this.LayerContentsRedrawPolicy = NSViewLayerContentsRedrawPolicy.OnSetNeedsDisplay;
    }

    public override CALayer MakeBackingLayer() => new AVPlayerLayer();

    /// <inheritdoc />
    public AVPlayerLayer PlayerLayer => (AVPlayerLayer)this.Layer!;
}
