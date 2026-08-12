namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The bare native video output of a <see cref="MediaElement"/> — an <c>AVPlayerLayer</c>-backed view on
/// Apple, a Media3 <c>PlayerView</c> on Android, a <c>MediaPlayerElement</c> on Windows, a
/// <c>Gtk.Picture</c> on Linux. It draws frames and nothing else: no buttons, no scrubber, no gestures.
/// </summary>
/// <remarks>
/// You rarely place one directly — <see cref="MediaElement"/> owns one and layers the transport bar over
/// it. It is public because fullscreen works by handing the <i>same</i> <see cref="IMediaPlayerBackend"/>
/// to a second surface on a modal page, and because a custom transport UI needs something to draw over.
/// Assign <see cref="Backend"/> before the handler connects.
/// </remarks>
public class MediaSurface : View
{
    IMediaPlayerBackend? backend;

    /// <summary>
    /// The player whose frames this surface displays. Setting it re-binds the live output, so moving a
    /// backend between surfaces (inline ⇄ fullscreen) keeps playing without re-buffering.
    /// </summary>
    public IMediaPlayerBackend? Backend
    {
        get => this.backend;
        set
        {
            if (ReferenceEquals(this.backend, value))
                return;

            // drop the old player's output before handing the view to the new one, or the first backend
            // keeps rendering into a surface it no longer owns
            if (this.backend is not null && this.IsOutputBound)
                this.backend.SetOutput(null);

            this.backend = value;

            if (value is not null && this.IsOutputBound)
                value.SetOutput(this.PlatformOutput);
        }
    }

    // Set by the handler on connect/disconnect. Kept here (rather than reading Handler.PlatformView) so the
    // control can re-bind a swapped-in backend without the handler being involved.
    internal object? PlatformOutput { get; private set; }

    bool IsOutputBound => this.PlatformOutput is not null;

    internal void AttachOutput(object nativeView)
    {
        this.PlatformOutput = nativeView;
        this.backend?.SetOutput(nativeView);
    }

    internal void DetachOutput()
    {
        if (this.PlatformOutput is not null)
            this.backend?.SetOutput(null);

        this.PlatformOutput = null;
    }

    /// <summary>
    /// Re-point the backend at this surface's view. Needed because a backend has exactly one output: while
    /// the fullscreen page owns it, this (still-connected, still-visible-behind-the-modal) surface is dark,
    /// and nothing re-binds it when that page pops.
    /// </summary>
    internal void RebindOutput()
    {
        if (this.PlatformOutput is not null)
            this.backend?.SetOutput(this.PlatformOutput);
    }
}
