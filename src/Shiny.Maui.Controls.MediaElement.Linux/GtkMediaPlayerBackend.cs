using Shiny.Controls.Media;
using Shiny.Maui.Controls.Media;

namespace Shiny.Maui.Controls.Media.Gtk;

/// <summary>
/// The GTK4 backend, built on <c>GtkMediaFile</c> (a <c>GtkMediaStream</c>, which is also a
/// <c>GdkPaintable</c>) rendered into a <c>GtkPicture</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>GtkVideo</c> would have been the obvious widget, but it draws its own hover controls that would
/// fight Shiny's transport bar. <c>GtkPicture</c> + the media stream as a paintable gives the same
/// decoding with a bare surface.
/// </para>
/// <para>
/// Decoding is GStreamer's, via <c>gtk4-media-gstreamer</c>. A distro without it installed still builds
/// and lays out — the stream simply reports an error on open, which surfaces through
/// <see cref="Failed"/> like any other bad source.
/// </para>
/// </remarks>
class GtkMediaPlayerBackend : IMediaPlayerBackend
{
    // GTK measures media time in microseconds; every public surface here is a TimeSpan.
    const long MicrosecondsPerSecond = 1_000_000;

    global::Gtk.MediaFile? stream;
    global::Gtk.Picture? output;
    MediaAspect aspect = MediaAspect.AspectFit;
    double volume = 1d;
    bool muted;
    bool looping;
    bool endedRaised;
    bool disposed;

    public MediaPlaybackCapabilities Capabilities => MediaPlaybackCapabilities.Volume;
    // Deliberately no BackgroundAudio: playback does continue while the window is hidden (a desktop
    // process is never suspended), but there is no MPRIS integration yet, so there are no OS transport
    // controls to advertise. No PlaybackRate either — GtkMediaStream has no rate control. And no
    // BufferProgress: it reports no download progress.

    public MediaElementState State { get; private set; } = MediaElementState.None;

    public TimeSpan Position => this.stream is null
        ? TimeSpan.Zero
        : TimeSpan.FromSeconds(this.stream.GetTimestamp() / (double)MicrosecondsPerSecond);

    public TimeSpan Duration { get; private set; }

    public double BufferedProgress => 0d;

    public Size VideoSize { get; private set; }

    public bool IsPictureInPictureActive => false;

    public event EventHandler<MediaElementState>? StateChanged;
    public event EventHandler? MediaOpened;
    public event EventHandler? MediaEnded;
    public event EventHandler<MediaFailure>? Failed;

    // Neither is ever raised on GTK: there is no PiP, and no OS transport UI to send commands from.
#pragma warning disable CS0067
    public event EventHandler<bool>? PictureInPictureChanged;
    public event EventHandler<MediaRemoteCommand>? RemoteCommandReceived;
#pragma warning restore CS0067


    public void SetOutput(object? nativeView)
    {
        if (this.output is not null)
            this.output.SetPaintable(null);

        this.output = nativeView as global::Gtk.Picture;

        if (this.output is null)
            return;

        this.ApplyContentFit();

        if (this.stream is not null)
            this.output.SetPaintable(this.stream);
    }


    public Task OpenAsync(MediaSource? source, CancellationToken ct = default)
    {
        this.TeardownStream();
        this.Duration = TimeSpan.Zero;
        this.VideoSize = Size.Zero;
        this.endedRaised = false;

        if (source is null)
        {
            this.SetState(MediaElementState.None);
            return Task.CompletedTask;
        }

        this.SetState(MediaElementState.Opening);

        try
        {
            this.stream = Create(source);
        }
        catch (Exception ex)
        {
            this.RaiseFailed(new MediaFailure($"Could not open media source '{source}'", ex));
            return Task.CompletedTask;
        }

        if (this.stream is null)
        {
            this.RaiseFailed(new MediaFailure($"Could not resolve media source '{source}'"));
            return Task.CompletedTask;
        }

        this.stream.SetVolume(this.muted ? 0d : this.volume);
        this.stream.SetMuted(this.muted);
        this.stream.SetLoop(this.looping);

        // One handler for every property: GtkMediaStream signals "prepared", "ended", "playing", "error"
        // and "duration" all through GObject::notify, and re-reading the lot is cheaper than matching on
        // the pspec name for each.
        this.stream.OnNotify += this.OnStreamNotify;

        this.output?.SetPaintable(this.stream);
        this.SyncFromStream();
        return Task.CompletedTask;
    }

    static global::Gtk.MediaFile? Create(MediaSource source) => source switch
    {
        UriMediaSource { Uri: { } uri } =>
            global::Gtk.MediaFile.NewForFile(Gio.FileHelper.NewForUri(uri.AbsoluteUri)),

        FileMediaSource { Path: { Length: > 0 } path } =>
            global::Gtk.MediaFile.NewForFilename(path),

        // The GTK head is a plain console-style app rather than a bundle, so packaged assets sit beside
        // the executable rather than in any platform resource container.
        ResourceMediaSource { Path: { Length: > 0 } path } =>
            global::Gtk.MediaFile.NewForFilename(Path.Combine(AppContext.BaseDirectory, path)),

        _ => null
    };


    public void Play() => this.stream?.SetPlaying(true);

    public void Pause() => this.stream?.SetPlaying(false);

    public void Stop()
    {
        if (this.stream is null)
            return;

        this.stream.SetPlaying(false);
        this.stream.Seek(0);
        this.SetState(MediaElementState.Stopped);
    }

    public void Seek(TimeSpan position)
    {
        if (this.stream?.IsSeekable() == true)
            this.stream.Seek((long)(position.TotalSeconds * MicrosecondsPerSecond));
    }

    public void SetVolume(double volume)
    {
        this.volume = Math.Clamp(volume, 0d, 1d);

        if (!this.muted)
            this.stream?.SetVolume(this.volume);
    }

    public void SetMuted(bool muted)
    {
        this.muted = muted;
        this.stream?.SetMuted(muted);
    }

    // GtkMediaStream has no rate control, and Capabilities says so — the transport bar never offers it.
    public void SetRate(double rate)
    {
    }

    public void SetLooping(bool looping)
    {
        this.looping = looping;
        this.stream?.SetLoop(looping);
    }

    public void SetAspect(MediaAspect aspect)
    {
        this.aspect = aspect;
        this.ApplyContentFit();
    }

    void ApplyContentFit()
    {
        if (this.output is null)
            return;

        this.output.SetContentFit(this.aspect switch
        {
            MediaAspect.AspectFill => global::Gtk.ContentFit.Cover,
            MediaAspect.Fill => global::Gtk.ContentFit.Fill,
            _ => global::Gtk.ContentFit.Contain
        });
    }

    // No portable way to inhibit the screensaver without a session-bus dependency; a desktop that dims
    // mid-video is a far smaller problem than a hard dependency on a D-Bus service that may not be there.
    public void SetKeepScreenOn(bool keepOn)
    {
    }

    public void SetBackgroundPlayback(bool enabled, MediaMetadata? metadata)
    {
    }

    public Task<bool> TryEnterPictureInPictureAsync() => Task.FromResult(false);

    public Task ExitPictureInPictureAsync() => Task.CompletedTask;


    void OnStreamNotify(GObject.Object sender, GObject.Object.NotifySignalArgs args) => this.SyncFromStream();

    void SyncFromStream()
    {
        if (this.stream is null)
            return;

        if (this.stream.GetError() is { } error)
        {
            this.RaiseFailed(new MediaFailure(error.Message ?? "Playback failed"));
            return;
        }

        var prepared = this.stream.IsPrepared();

        if (prepared && this.Duration == TimeSpan.Zero)
        {
            var duration = this.stream.GetDuration();
            if (duration > 0)
                this.Duration = TimeSpan.FromSeconds(duration / (double)MicrosecondsPerSecond);

            this.VideoSize = new Size(this.stream.GetIntrinsicWidth(), this.stream.GetIntrinsicHeight());
            this.MediaOpened?.Invoke(this, EventArgs.Empty);
        }

        if (this.stream.GetEnded())
        {
            // "ended" stays latched until the next seek, so without this guard every subsequent notify
            // would raise MediaEnded again.
            if (!this.endedRaised)
            {
                this.endedRaised = true;
                this.SetState(MediaElementState.Paused);
                this.MediaEnded?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        this.endedRaised = false;

        if (!prepared)
        {
            this.SetState(MediaElementState.Opening);
            return;
        }

        this.SetState(this.stream.GetPlaying() ? MediaElementState.Playing : MediaElementState.Paused);
    }

    void SetState(MediaElementState state)
    {
        if (this.State == state)
            return;

        this.State = state;
        this.StateChanged?.Invoke(this, state);
    }

    void RaiseFailed(MediaFailure failure)
    {
        if (this.State == MediaElementState.Failed)
            return;

        this.State = MediaElementState.Failed;
        this.StateChanged?.Invoke(this, MediaElementState.Failed);
        this.Failed?.Invoke(this, failure);
    }


    void TeardownStream()
    {
        if (this.stream is null)
            return;

        this.stream.OnNotify -= this.OnStreamNotify;
        this.stream.SetPlaying(false);
        this.output?.SetPaintable(null);
        this.stream.Clear();
        this.stream = null;
    }

    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.TeardownStream();
        this.output = null;
    }
}
