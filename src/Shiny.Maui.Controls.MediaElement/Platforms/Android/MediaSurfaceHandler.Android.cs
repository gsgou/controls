using AndroidX.Media3.UI;
using Microsoft.Maui.Handlers;

namespace Shiny.Maui.Controls.Media;

public partial class MediaSurfaceHandler : ViewHandler<MediaSurface, PlayerView>
{
    protected override PlayerView CreatePlatformView()
    {
        var view = new PlayerView(this.Context)
        {
            // Shiny draws the transport bar; Media3's own controller would sit on top of it.
            UseController = false
        };

        // Hold the last frame rather than blanking to black when the player is detached — which is
        // exactly what happens on the way in and out of fullscreen.
        view.SetKeepContentOnPlayerReset(true);
        return view;
    }

    protected override void ConnectHandler(PlayerView platformView)
    {
        base.ConnectHandler(platformView);
        this.VirtualView.AttachOutput(platformView);
    }

    protected override void DisconnectHandler(PlayerView platformView)
    {
        this.MaybeVirtualView?.DetachOutput();
        platformView.Player = null;
        base.DisconnectHandler(platformView);
    }
}
