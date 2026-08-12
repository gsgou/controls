using Microsoft.Maui.Handlers;
using UIKit;

namespace Shiny.Maui.Controls.Media;

public partial class MediaSurfaceHandler : ViewHandler<MediaSurface, MediaPlayerUIView>
{
    protected override MediaPlayerUIView CreatePlatformView()
        => new() { BackgroundColor = UIColor.Black };

    protected override void ConnectHandler(MediaPlayerUIView platformView)
    {
        base.ConnectHandler(platformView);
        this.VirtualView.AttachOutput(platformView);
    }

    protected override void DisconnectHandler(MediaPlayerUIView platformView)
    {
        this.MaybeVirtualView?.DetachOutput();
        base.DisconnectHandler(platformView);
    }
}
