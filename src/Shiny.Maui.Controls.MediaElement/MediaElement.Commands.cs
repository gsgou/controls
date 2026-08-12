using System.Globalization;
using System.Windows.Input;

namespace Shiny.Maui.Controls.Media;

public partial class MediaElement
{
    /// <summary>Starts or resumes playback. Bind a button to this from a view model.</summary>
    public ICommand PlayCommand { get; private set; } = null!;

    /// <summary>Suspends playback at the current position.</summary>
    public ICommand PauseCommand { get; private set; } = null!;

    /// <summary>Halts playback and rewinds to the start.</summary>
    public ICommand StopCommand { get; private set; } = null!;

    /// <summary>Flips between playing and paused.</summary>
    public ICommand TogglePlayPauseCommand { get; private set; } = null!;

    /// <summary>
    /// Moves the playhead. The parameter may be a <see cref="TimeSpan"/>, a number of seconds
    /// (<see cref="double"/>/<see cref="int"/>), or a string either of those parses from — so
    /// <c>CommandParameter="30"</c> and <c>CommandParameter="00:00:30"</c> both work in XAML.
    /// </summary>
    public ICommand SeekCommand { get; private set; } = null!;

    /// <summary>
    /// Toggles <see cref="IsMuted"/>, or sets it outright when passed a <see cref="bool"/> parameter.
    /// </summary>
    public ICommand MuteCommand { get; private set; } = null!;

    /// <summary>Toggles <see cref="IsFullScreen"/>.</summary>
    public ICommand ToggleFullScreenCommand { get; private set; } = null!;

    /// <summary>
    /// Detaches the video into a Picture-in-Picture window. Its <c>CanExecute</c> is false where the
    /// platform can't do PiP, so a bound button disables itself.
    /// </summary>
    public ICommand PictureInPictureCommand { get; private set; } = null!;


    void InitializeCommands()
    {
        this.PlayCommand = new Command(this.Play);
        this.PauseCommand = new Command(this.Pause);
        this.StopCommand = new Command(this.Stop);
        this.TogglePlayPauseCommand = new Command(this.TogglePlayPause);
        this.ToggleFullScreenCommand = new Command(this.ToggleFullScreen);

        this.SeekCommand = new Command(parameter =>
        {
            if (TryParsePosition(parameter, out var position))
                _ = this.SeekAsync(position);
        });

        this.MuteCommand = new Command(parameter =>
        {
            if (parameter is bool muted)
                this.IsMuted = muted;
            else if (parameter is string text && Boolean.TryParse(text, out var parsed))
                this.IsMuted = parsed;
            else
                this.ToggleMute();
        });

        this.PictureInPictureCommand = new Command(
            execute: async () => await this.TryEnterPictureInPictureAsync().ConfigureAwait(true),
            canExecute: () => this.Capabilities.HasFlag(MediaPlaybackCapabilities.PictureInPicture));
    }


    static bool TryParsePosition(object? parameter, out TimeSpan position)
    {
        switch (parameter)
        {
            case TimeSpan span:
                position = span;
                return true;

            case double seconds:
                position = TimeSpan.FromSeconds(seconds);
                return true;

            case int seconds:
                position = TimeSpan.FromSeconds(seconds);
                return true;

            // A bare "30" from XAML means thirty seconds. TimeSpan.TryParse would happily read it as thirty
            // *days*, so the colon is what decides which parser gets the string — not parser precedence.
            case string text when text.Contains(':'):
                return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out position);

            case string text when Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSeconds):
                position = TimeSpan.FromSeconds(parsedSeconds);
                return true;

            default:
                position = TimeSpan.Zero;
                return false;
        }
    }
}
