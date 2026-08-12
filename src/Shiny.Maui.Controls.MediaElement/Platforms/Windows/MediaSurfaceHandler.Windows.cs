using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace Shiny.Maui.Controls.Media;

public partial class MediaSurfaceHandler : ViewHandler<MediaSurface, MediaPlayerElement>
{
    protected override MediaPlayerElement CreatePlatformView()
        => new()
        {
            // Shiny draws the transport bar; the built-in one would sit on top of ours.
            AreTransportControlsEnabled = false,
            AutoPlay = false
        };

    protected override void ConnectHandler(MediaPlayerElement platformView)
    {
        base.ConnectHandler(platformView);
        this.VirtualView.AttachOutput(platformView);
    }

    protected override void DisconnectHandler(MediaPlayerElement platformView)
    {
        this.MaybeVirtualView?.DetachOutput();
        platformView.SetMediaPlayer(null);
        base.DisconnectHandler(platformView);
    }
}
