#if ANDROID || IOS || MACCATALYST || WINDOWS || MACOS
using Microsoft.Maui.Handlers;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// Hosts the native video view for a <see cref="MediaSurface"/>. There are no property mappers: the
/// surface carries no state of its own — everything is driven through <see cref="IMediaPlayerBackend"/>,
/// which outlives the handler.
/// </summary>
public partial class MediaSurfaceHandler
{
    /// <summary>
    /// The bound <see cref="MediaSurface"/>, or <c>null</c> once the handler has been disconnected.
    /// </summary>
    /// <remarks>
    /// Use this, not <c>VirtualView?.</c>, from <c>DisconnectHandler</c> and anything that can outlive the
    /// handler. <c>ViewHandler&lt;T,T&gt;.VirtualView</c> <i>throws</i> when disconnected rather than
    /// returning null, so the null-conditional never gets its chance — the getter has already thrown.
    /// </remarks>
    internal MediaSurface? MaybeVirtualView => ((IElementHandler)this).VirtualView as MediaSurface;

    public static IPropertyMapper<MediaSurface, MediaSurfaceHandler> Mapper =
        new PropertyMapper<MediaSurface, MediaSurfaceHandler>(ViewHandler.ViewMapper);

    public static CommandMapper<MediaSurface, MediaSurfaceHandler> CommandMapper =
        new(ViewHandler.ViewCommandMapper);

    public MediaSurfaceHandler() : base(Mapper, CommandMapper)
    {
    }
}
#endif
