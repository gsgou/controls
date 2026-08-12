using Microsoft.Maui.Handlers;

namespace Shiny.Maui.Controls.Media;

// macOS AppKit head (dotnet/maui-labs). AVFoundation itself is solid here; the MAUI host is preview
// quality, so layout edge cases may need on-device tuning.
public partial class MediaSurfaceHandler : ViewHandler<MediaSurface, MediaPlayerNSView>
{
    protected override MediaPlayerNSView CreatePlatformView() => new();

    protected override void ConnectHandler(MediaPlayerNSView platformView)
    {
        base.ConnectHandler(platformView);
        this.VirtualView.AttachOutput(platformView);
    }

    protected override void DisconnectHandler(MediaPlayerNSView platformView)
    {
        this.MaybeVirtualView?.DetachOutput();
        base.DisconnectHandler(platformView);
    }
}
