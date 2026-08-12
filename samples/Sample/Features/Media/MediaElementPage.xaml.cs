using Shiny.Controls.Media;
using Shiny.Maui.Controls;

namespace Sample.Features.Media;

public partial class MediaElementPage : ShinyContentPage
{
    public MediaElementPage()
    {
        this.InitializeComponent();

        this.AspectPicker.SelectedIndex = 0;
        this.RatePicker.SelectedIndex = 1;

        // Now-playing information for the lock screen / notification / SMTC while the audio player is
        // backgrounded. Assigning a whole new instance is what the control watches for.
        this.AudioPlayer.Metadata = new MediaMetadata
        {
            Title = "SoundHelix Song 1",
            Artist = "T. Schürger",
            Album = "Shiny Controls Sample"
        };
    }

    void OnAspectChanged(object? sender, EventArgs e)
        => this.Player.Aspect = this.AspectPicker.SelectedIndex switch
        {
            1 => MediaAspect.AspectFill,
            2 => MediaAspect.Fill,
            _ => MediaAspect.AspectFit
        };

    void OnRateChanged(object? sender, EventArgs e)
        => this.Player.PlaybackRate = this.RatePicker.SelectedIndex switch
        {
            0 => 0.5,
            2 => 1.5,
            3 => 2.0,
            _ => 1.0
        };
}
