using System.Collections.ObjectModel;
using Shiny.Controls.MotionIcons;

namespace Sample.Features.MotionIcons;

public partial class MotionIconsPage : ContentPage
{
    readonly IReadOnlyList<string> allIconNames;
    bool busy;

    public MotionIconsPage()
    {
        InitializeComponent();
        SampleSourceCode.Attach(this);

        this.allIconNames = MotionIconLibrary.Names.OrderBy(x => x, StringComparer.Ordinal).ToList();

        IconNames = [.. this.allIconNames];
        PresetNames = Enum.GetNames<MotionPreset>().ToList();

        BindingContext = this;
    }

    // The set is well past a hundred icons now, so the gallery is filtered rather than scrolled.
    public ObservableCollection<string> IconNames { get; }

    public IReadOnlyList<string> PresetNames { get; }

    public string GalleryCaption => this.IconNames.Count == this.allIconNames.Count
        ? $"Hover or tap any of them. {this.allIconNames.Count} icons, each with its own motion."
        : $"{this.IconNames.Count} of {this.allIconNames.Count} icons.";

    void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        var filter = e.NewTextValue?.Trim();

        this.IconNames.Clear();

        foreach (var name in this.allIconNames)
        {
            if (String.IsNullOrEmpty(filter) || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                this.IconNames.Add(name);
        }

        this.OnPropertyChanged(nameof(this.GalleryCaption));
    }

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
