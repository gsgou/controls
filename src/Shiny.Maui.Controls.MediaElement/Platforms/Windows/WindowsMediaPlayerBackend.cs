using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Playback;
using Windows.System.Display;
using WinPlayer = Windows.Media.Playback.MediaPlayer;
using WinMediaSource = Windows.Media.Core.MediaSource;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The Windows backend, built on <see cref="Windows.Media.Playback.MediaPlayer"/> with a
/// <c>MediaPlayerElement</c> as the output surface (its own transport controls switched off — Shiny draws
/// those).
/// </summary>
class WindowsMediaPlayerBackend : IMediaPlayerBackend
{
    readonly WinPlayer player = new();
    readonly DisplayRequest displayRequest = new();

    MediaPlayerElement? output;
    MediaAspect aspect = MediaAspect.AspectFit;
    MediaMetadata? metadata;
    bool backgroundEnabled;
    bool keepScreenOnActive;
    bool disposed;

    public WindowsMediaPlayerBackend()
    {
        this.player.AutoPlay = false;
        this.player.MediaEnded += this.OnMediaEnded;
        this.player.MediaFailed += this.OnMediaFailed;
        this.player.MediaOpened += this.OnMediaOpened;
        this.player.PlaybackSession.PlaybackStateChanged += this.OnPlaybackStateChanged;

        // The SMTC integration is what gives Windows its "background" story: the player keeps running when
        // the window is minimised and the OS media flyout drives it.
        this.player.CommandManager.IsEnabled = true;
    }


    public MediaPlaybackCapabilities Capabilities =>
        MediaPlaybackCapabilities.BackgroundAudio
        | MediaPlaybackCapabilities.PlaybackRate
        | MediaPlaybackCapabilities.Volume
        | MediaPlaybackCapabilities.BufferProgress;
    // No PictureInPicture: WinUI has no per-element PiP. The nearest thing, a compact-overlay AppWindow,
    // shrinks the whole app window rather than detaching the video, so it is not offered as PiP.

    public MediaElementState State { get; private set; } = MediaElementState.None;

    public TimeSpan Position => this.player.PlaybackSession.Position;

    public TimeSpan Duration { get; private set; }

    public double BufferedProgress => Math.Clamp(this.player.PlaybackSession.DownloadProgress, 0d, 1d);

    public Size VideoSize { get; private set; }

    public bool IsPictureInPictureActive => false;

    public event EventHandler<MediaElementState>? StateChanged;
    public event EventHandler? MediaOpened;
    public event EventHandler? VideoSizeChanged;
    public event EventHandler? MediaEnded;
    public event EventHandler<MediaFailure>? Failed;
    // Neither is raised on Windows: there is no per-element PiP API, and the SMTC drives the MediaPlayer
    // directly, so the resulting state change already comes back through PlaybackStateChanged.
#pragma warning disable CS0067
    public event EventHandler<bool>? PictureInPictureChanged;
    public event EventHandler<MediaRemoteCommand>? RemoteCommandReceived;
#pragma warning restore CS0067


    public void SetOutput(object? nativeView)
    {
        this.output?.SetMediaPlayer(null);
        this.output = nativeView as MediaPlayerElement;

        if (this.output is null)
            return;

        this.output.AreTransportControlsEnabled = false;
        this.output.SetMediaPlayer(this.player);
        this.ApplyStretch();
    }


    public Task OpenAsync(MediaSource? source, CancellationToken ct = default)
    {
        this.Duration = TimeSpan.Zero;
        this.VideoSize = Size.Zero;

        if (source is null)
        {
            this.player.Source = null;
            this.SetState(MediaElementState.None);
            return Task.CompletedTask;
        }

        var uri = ResolveUri(source);
        if (uri is null)
        {
            this.RaiseFailed(new MediaFailure($"Could not resolve media source '{source}'"));
            return Task.CompletedTask;
        }

        this.SetState(MediaElementState.Opening);
        this.player.Source = WinMediaSource.CreateFromUri(uri);
        return Task.CompletedTask;
    }

    static Uri? ResolveUri(MediaSource source) => source switch
    {
        UriMediaSource { Uri: { } uri } => uri,
        FileMediaSource { Path: { Length: > 0 } path } => new Uri(path, UriKind.Absolute),
        // MAUI copies Resources/Raw into the package root, so the packaged-content scheme addresses them
        // directly — and works identically packaged or unpackaged.
        ResourceMediaSource { Path: { Length: > 0 } path } => new Uri($"ms-appx:///{path.TrimStart('/')}"),
        _ => null
    };


    public void Play() => this.player.Play();

    public void Pause() => this.player.Pause();

    public void Stop()
    {
        this.player.Pause();
        this.player.PlaybackSession.Position = TimeSpan.Zero;
        this.SetState(MediaElementState.Stopped);
    }

    public void Seek(TimeSpan position)
    {
        if (this.player.PlaybackSession.CanSeek)
            this.player.PlaybackSession.Position = position;
    }

    public void SetVolume(double volume) => this.player.Volume = Math.Clamp(volume, 0d, 1d);

    public void SetMuted(bool muted) => this.player.IsMuted = muted;

    public void SetRate(double rate) => this.player.PlaybackSession.PlaybackRate = rate;

    public void SetLooping(bool looping) => this.player.IsLoopingEnabled = looping;

    public void SetAspect(MediaAspect aspect)
    {
        this.aspect = aspect;
        this.ApplyStretch();
    }

    void ApplyStretch()
    {
        if (this.output is null)
            return;

        // Fully qualified: Microsoft.Maui.Controls also has a Stretch, and both namespaces are in scope
        // here, so the bare name is ambiguous.
        this.output.Stretch = this.aspect switch
        {
            MediaAspect.AspectFill => Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
            MediaAspect.Fill => Microsoft.UI.Xaml.Media.Stretch.Fill,
            _ => Microsoft.UI.Xaml.Media.Stretch.Uniform
        };
    }

    public void SetKeepScreenOn(bool keepOn)
    {
        // RequestActive/RequestRelease are reference-counted; calling either twice in a row leaks a count
        // and the display never sleeps again.
        if (keepOn == this.keepScreenOnActive)
            return;

        if (keepOn)
            this.displayRequest.RequestActive();
        else
            this.displayRequest.RequestRelease();

        this.keepScreenOnActive = keepOn;
    }

    public void SetBackgroundPlayback(bool enabled, MediaMetadata? metadata)
    {
        this.backgroundEnabled = enabled;
        this.metadata = metadata;
        this.PublishTransportControls();
    }

    void PublishTransportControls()
    {
        try
        {
            var controls = this.player.SystemMediaTransportControls;
            controls.IsEnabled = true;
            controls.IsPlayEnabled = true;
            controls.IsPauseEnabled = true;
            controls.IsStopEnabled = true;

            var updater = controls.DisplayUpdater;
            if (this.backgroundEnabled && this.metadata is not null)
            {
                updater.Type = Windows.Media.MediaPlaybackType.Video;
                updater.VideoProperties.Title = this.metadata.Title ?? String.Empty;
                updater.VideoProperties.Subtitle = this.metadata.Artist ?? String.Empty;

                if (Uri.TryCreate(this.metadata.ArtworkUri, UriKind.Absolute, out var artwork))
                    updater.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(artwork);
            }
            else
            {
                updater.ClearAll();
            }

            updater.Update();
        }
        catch (Exception)
        {
            // SMTC is unavailable in some host configurations (notably unpackaged test hosts). Losing the
            // OS flyout must not take playback down with it.
        }
    }

    public Task<bool> TryEnterPictureInPictureAsync() => Task.FromResult(false);

    public Task ExitPictureInPictureAsync() => Task.CompletedTask;


    // ── player events (all arrive off the UI thread) ─────────────────────────────────────────────

    void OnMediaOpened(WinPlayer sender, object args)
        => OnMain(() =>
        {
            var session = this.player.PlaybackSession;
            this.Duration = session.NaturalDuration;

            // Windows has the natural size by the time it says the media is open, so this always fires
            // ahead of MediaOpened rather than after it as Android's does. The control handles either.
            var size = new Size(session.NaturalVideoWidth, session.NaturalVideoHeight);
            if (size != this.VideoSize)
            {
                this.VideoSize = size;
                this.VideoSizeChanged?.Invoke(this, EventArgs.Empty);
            }

            this.MediaOpened?.Invoke(this, EventArgs.Empty);
            this.PublishTransportControls();
        });

    void OnMediaEnded(WinPlayer sender, object args)
        => OnMain(() => this.MediaEnded?.Invoke(this, EventArgs.Empty));

    void OnMediaFailed(WinPlayer sender, MediaPlayerFailedEventArgs args)
        => OnMain(() => this.RaiseFailed(new MediaFailure(args.ErrorMessage ?? args.Error.ToString())));

    void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        => OnMain(() =>
        {
            var state = sender.PlaybackState switch
            {
                MediaPlaybackState.Opening => MediaElementState.Opening,
                MediaPlaybackState.Buffering => MediaElementState.Buffering,
                MediaPlaybackState.Playing => MediaElementState.Playing,
                MediaPlaybackState.Paused => MediaElementState.Paused,
                _ => MediaElementState.None
            };

            this.SetState(state);
        });

    void SetState(MediaElementState state)
    {
        if (this.State == state)
            return;

        this.State = state;
        this.StateChanged?.Invoke(this, state);
    }

    void RaiseFailed(MediaFailure failure)
    {
        this.State = MediaElementState.Failed;
        this.StateChanged?.Invoke(this, MediaElementState.Failed);
        this.Failed?.Invoke(this, failure);
    }

    static void OnMain(Action action)
    {
        if (MainThread.IsMainThread)
            action();
        else
            MainThread.BeginInvokeOnMainThread(action);
    }


    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;

        this.SetKeepScreenOn(false);

        this.player.MediaEnded -= this.OnMediaEnded;
        this.player.MediaFailed -= this.OnMediaFailed;
        this.player.MediaOpened -= this.OnMediaOpened;
        this.player.PlaybackSession.PlaybackStateChanged -= this.OnPlaybackStateChanged;

        this.output?.SetMediaPlayer(null);
        this.output = null;

        this.player.Pause();
        this.player.Source = null;
        this.player.Dispose();
    }
}
