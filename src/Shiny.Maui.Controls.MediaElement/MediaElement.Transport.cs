using Shiny.Maui.Controls.Media.Internal;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Media;

public partial class MediaElement
{
    static readonly Color DefaultScrim = Color.FromRgba(0, 0, 0, 140);
    static readonly Color DefaultControlColor = Colors.White;

    Grid transportRow = null!;
    Border transportBar = null!;
    Grid tapCatcher = null!;
    Grid busyOverlay = null!;
    Border errorOverlay = null!;
    Label errorLabel = null!;

    MediaGlyphButton playPauseButton = null!;
    MediaGlyphButton muteButton = null!;
    MediaGlyphButton fullScreenButton = null!;
    MediaGlyphButton pipButton = null!;
    MediaSeekBar seekBar = null!;
    // Shiny's Slider, not Microsoft.Maui.Controls.Slider — the enclosing namespace wins the lookup, but
    // the two are one character apart in a file that also uses plenty of MAUI types, so spell it out.
    Shiny.Maui.Controls.Slider volumeSlider = null!;
    Label positionLabel = null!;
    Label durationLabel = null!;


    void BuildTransportBar()
    {
        // Sits between the video and the bar so a tap anywhere on the frame toggles the controls. The
        // native video view would otherwise swallow the touch, and an explicitly transparent (not null)
        // background is what makes Android hit-test it at all.
        this.tapCatcher = new Grid { BackgroundColor = Colors.Transparent };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => this.ToggleTransportBar();
        this.tapCatcher.GestureRecognizers.Add(tap);

        this.playPauseButton = new MediaGlyphButton(MediaGlyph.Play, "MediaPlayPauseButton", "Play");
        this.playPauseButton.Tapped += (_, _) => this.TogglePlayPause();

        this.muteButton = new MediaGlyphButton(MediaGlyph.VolumeOn, "MediaMuteButton", "Mute");
        this.muteButton.Tapped += (_, _) => this.ToggleMute();

        this.fullScreenButton = new MediaGlyphButton(MediaGlyph.FullScreenEnter, "MediaFullScreenButton", "Enter full screen");
        this.fullScreenButton.Tapped += (_, _) => this.ToggleFullScreen();

        this.pipButton = new MediaGlyphButton(MediaGlyph.PictureInPicture, "MediaPictureInPictureButton", "Picture in picture");
        this.pipButton.Tapped += async (_, _) => await this.TryEnterPictureInPictureAsync().ConfigureAwait(true);

        this.seekBar = new MediaSeekBar { HorizontalOptions = LayoutOptions.Fill };
        this.seekBar.DragStarted += (_, _) => this.isScrubbing = true;
        this.seekBar.Seeking += (_, position) =>
        {
            // Live-update the labels under the finger without touching the player yet — scrubbing a remote
            // stream on every pan tick would thrash the buffer.
            this.positionLabel.Text = MediaTimeFormatter.Format(position, this.UseHourFormat);
            this.KeepTransportBarAwake();
        };
        this.seekBar.DragCompleted += async (_, position) =>
        {
            this.isScrubbing = false;
            await this.SeekAsync(position).ConfigureAwait(true);
        };

        this.volumeSlider = new Shiny.Maui.Controls.Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = 1,
            WidthRequest = 80,
            TrackHeight = 4,
            ThumbSize = 12,
            // Slider shows a value tooltip by default, which on a transport bar means a permanent little
            // "1" floating over the video. The volume level is self-evident from the thumb position.
            ShowTooltip = false,
            VerticalOptions = LayoutOptions.Center
        };
        this.volumeSlider.ValueChangedEvent += (_, value) =>
        {
            this.Volume = value;
            if (this.IsMuted && value > 0)
                this.IsMuted = false;

            this.KeepTransportBarAwake();
        };

        this.positionLabel = CreateTimeLabel();
        this.durationLabel = CreateTimeLabel();

        this.transportRow = new Grid
        {
            ColumnSpacing = 6,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),  // play/pause
                new ColumnDefinition(GridLength.Auto),  // elapsed
                new ColumnDefinition(GridLength.Star),  // scrubber
                new ColumnDefinition(GridLength.Auto),  // total
                new ColumnDefinition(GridLength.Auto),  // mute
                new ColumnDefinition(GridLength.Auto),  // volume
                new ColumnDefinition(GridLength.Auto),  // picture-in-picture
                new ColumnDefinition(GridLength.Auto)   // fullscreen
            }
        };

        this.transportRow.Add(this.playPauseButton, 0);
        this.transportRow.Add(this.positionLabel, 1);
        this.transportRow.Add(this.seekBar, 2);
        this.transportRow.Add(this.durationLabel, 3);
        this.transportRow.Add(this.muteButton, 4);
        this.transportRow.Add(this.volumeSlider, 5);
        this.transportRow.Add(this.pipButton, 6);
        this.transportRow.Add(this.fullScreenButton, 7);

        this.transportBar = new Border
        {
            Padding = new Thickness(10, 4),
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            BackgroundColor = DefaultScrim,
            VerticalOptions = LayoutOptions.End,
            Content = this.transportRow
        };

        this.busyOverlay = new Grid
        {
            IsVisible = false,
            InputTransparent = true,
            Children =
            {
                new ActivityIndicator
                {
                    IsRunning = true,
                    Color = DefaultControlColor,
                    WidthRequest = 42,
                    HeightRequest = 42,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };

        this.errorLabel = new Label
        {
            TextColor = DefaultControlColor,
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        this.errorOverlay = new Border
        {
            IsVisible = false,
            Padding = new Thickness(16, 12),
            Margin = new Thickness(20),
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            BackgroundColor = Color.FromRgba(0, 0, 0, 190),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = this.errorLabel
        };

        this.rootGrid.Add(this.tapCatcher);
        this.rootGrid.Add(this.busyOverlay);
        this.rootGrid.Add(this.errorOverlay);
        this.rootGrid.Add(this.transportBar);
    }

    static Label CreateTimeLabel() => new()
    {
        Text = "0:00",
        FontSize = 12,
        TextColor = DefaultControlColor,
        VerticalOptions = LayoutOptions.Center,
        HorizontalTextAlignment = TextAlignment.Center
    };


    // ── visibility ───────────────────────────────────────────────────────────────────────────────

    void ApplyTransportVisibility()
    {
        if (this.transportBar is null)
            return;

        this.transportBar.IsVisible = this.ShowTransportBar && this.isTransportBarShown;
        this.playPauseButton.IsVisible = this.ShowPlayPauseButton;
        this.seekBar.IsVisible = this.ShowSeekBar;
        this.positionLabel.IsVisible = this.ShowTimeLabels;
        this.durationLabel.IsVisible = this.ShowTimeLabels;
        this.fullScreenButton.IsVisible = this.ShowFullScreenButton;

        // Volume is two pieces: the mute toggle works everywhere, but the slider is pointless on a backend
        // that refuses programmatic volume (mobile browsers, notably), so it's dropped rather than dead.
        var volume = this.ShowVolumeControl;
        this.muteButton.IsVisible = volume;
        this.volumeSlider.IsVisible = volume && this.Capabilities.HasFlag(MediaPlaybackCapabilities.Volume);

        this.pipButton.IsVisible = this.ShowPictureInPictureButton
            && this.Capabilities.HasFlag(MediaPlaybackCapabilities.PictureInPicture);

        this.UpdateFullScreenGlyph();
    }

    bool isTransportBarShown = true;

    void ToggleTransportBar()
    {
        if (!this.ShowTransportBar)
            return;

        if (this.isTransportBarShown)
            this.HideTransportBar();
        else
            this.ShowTransportBarNow();
    }

    void ShowTransportBarNow()
    {
        this.isTransportBarShown = true;
        this.ApplyTransportVisibility();
        this.RestartAutoHide();
    }

    void HideTransportBar()
    {
        this.isTransportBarShown = false;
        this.ApplyTransportVisibility();
    }

    // Push the auto-hide deadline back without re-showing a deliberately hidden bar.
    void KeepTransportBarAwake() => this.RestartAutoHide();

    void RestartAutoHide()
    {
        if (this.autoHideTimer is null)
        {
            var timer = this.Dispatcher?.CreateTimer();
            if (timer is null)
                return;

            timer.IsRepeating = false;
            timer.Tick += (_, _) =>
            {
                // Only fade out over playing video — a paused frame or an audio-only track keeps its
                // controls, which is what every player does and what the a11y tree needs.
                if (this.AutoHideTransportBar && this.CurrentState == MediaElementState.Playing && !this.isScrubbing)
                    this.HideTransportBar();
            };
            this.autoHideTimer = timer;
        }

        this.autoHideTimer.Stop();

        if (!this.AutoHideTransportBar || this.CurrentState != MediaElementState.Playing)
            return;

        this.autoHideTimer.Interval = this.TransportBarAutoHideDelay;
        this.autoHideTimer.Start();
    }


    // ── styling ──────────────────────────────────────────────────────────────────────────────────

    void ApplyTransportStyling()
    {
        if (this.transportBar is null)
            return;

        var control = this.ControlColor ?? DefaultControlColor;

        this.playPauseButton.GlyphColor = control;
        this.muteButton.GlyphColor = control;
        this.fullScreenButton.GlyphColor = control;
        this.pipButton.GlyphColor = control;
        this.positionLabel.TextColor = control;
        this.durationLabel.TextColor = control;
        this.errorLabel.TextColor = control;

        this.transportBar.BackgroundColor = this.TransportBarBackgroundColor ?? DefaultScrim;

        if (this.SeekBarColor is { } seek)
        {
            this.seekBar.RemoveDynamicResource(MediaSeekBar.ProgressColorProperty);
            this.seekBar.ProgressColor = seek;
        }
        else
        {
            // No explicit colour: follow the active Shiny theme pack so the scrubber matches the app.
            this.seekBar.SetDynamicResource(MediaSeekBar.ProgressColorProperty, ShinyThemeKeys.Color.Primary);
        }

        this.volumeSlider.HotColor = control;
        this.volumeSlider.ColdColor = Color.FromRgba(255, 255, 255, 70);
        this.volumeSlider.ThumbColor = control;
    }


    // ── state-driven visuals ─────────────────────────────────────────────────────────────────────

    bool UseHourFormat => this.Duration >= TimeSpan.FromHours(1);

    void UpdatePlayPauseGlyph()
    {
        if (this.playPauseButton is null)
            return;

        var playing = this.CurrentState == MediaElementState.Playing;
        this.playPauseButton.Glyph = playing ? MediaGlyph.Pause : MediaGlyph.Play;
        this.playPauseButton.SetDescription(playing ? "Pause" : "Play");
    }

    void UpdateMuteGlyph()
    {
        if (this.muteButton is null)
            return;

        this.muteButton.Glyph = this.IsMuted ? MediaGlyph.VolumeOff : MediaGlyph.VolumeOn;
        this.muteButton.SetDescription(this.IsMuted ? "Unmute" : "Mute");
    }

    void UpdateFullScreenGlyph()
    {
        if (this.fullScreenButton is null)
            return;

        var full = this.IsFullScreen;
        this.fullScreenButton.Glyph = full ? MediaGlyph.FullScreenExit : MediaGlyph.FullScreenEnter;
        this.fullScreenButton.SetDescription(full ? "Exit full screen" : "Enter full screen");
    }

    void UpdateSeekBar()
    {
        if (this.seekBar is null || this.isScrubbing)
            return;

        this.seekBar.Duration = this.Duration;
        this.seekBar.Position = this.Position;
        this.seekBar.BufferedProgress = this.BufferedProgress;
    }

    void UpdateTimeLabels()
    {
        if (this.positionLabel is null)
            return;

        var hours = this.UseHourFormat;
        this.positionLabel.Text = MediaTimeFormatter.Format(this.Position, hours);
        this.durationLabel.Text = MediaTimeFormatter.Format(this.Duration, hours);
    }

    void UpdateBusyOverlay()
    {
        if (this.busyOverlay is null)
            return;

        this.busyOverlay.IsVisible = this.CurrentState
            is MediaElementState.Opening
            or MediaElementState.Buffering;
    }

    void ShowError(string message)
    {
        if (this.errorOverlay is null)
            return;

        this.errorLabel.Text = message;
        this.errorOverlay.IsVisible = true;
        this.busyOverlay.IsVisible = false;
    }

    void HideError()
    {
        if (this.errorOverlay is not null)
            this.errorOverlay.IsVisible = false;
    }
}
