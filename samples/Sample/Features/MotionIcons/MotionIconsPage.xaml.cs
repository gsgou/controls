using Shiny.Controls.MotionIcons;

namespace Sample.Features.MotionIcons;

public partial class MotionIconsPage : ContentPage
{
    bool busy;

    public MotionIconsPage()
    {
        InitializeComponent();
        SampleSourceCode.Attach(this);

        IconNames = MotionIconLibrary.Names.OrderBy(x => x, StringComparer.Ordinal).ToList();
        PresetNames = Enum.GetNames<MotionPreset>().ToList();

        BindingContext = this;
    }

    public IReadOnlyList<string> IconNames { get; }

    public IReadOnlyList<string> PresetNames { get; }

    void OnToggleBusy(object? sender, EventArgs e)
    {
        busy = !busy;

        // The spinner loops for as long as the flag is set, then settles back to a full ring.
        this.Spinner.IsPlaying = busy;
    }

    void OnPlayOnce(object? sender, EventArgs e) => this.Downloader.Play();

    void OnPresetChanged(object? sender, EventArgs e)
    {
        if (this.PresetPicker.SelectedIndex < 0)
            return;

        var preset = Enum.Parse<MotionPreset>(this.PresetNames[this.PresetPicker.SelectedIndex]);

        this.PresetSample.Motion = preset;
        this.PresetCustom.Motion = preset;
    }
}
