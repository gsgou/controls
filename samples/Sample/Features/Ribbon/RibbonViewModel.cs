using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.Ribbons;

namespace Sample.Features.Ribbon;

/// <summary>
/// Drives the sample ribbon. Everything the bar does is a command or a two-way bound property — there
/// is no code-behind reaching into the control, which is the point of the demo.
/// </summary>
[ShellMap<RibbonPage>(registerRoute: false)]
public partial class RibbonViewModel : ObservableObject
{
    [ObservableProperty] string log = "Nothing yet — press something on the bar.";

    [ObservableProperty] RibbonDisplayMode displayMode = RibbonDisplayMode.Expanded;
    [ObservableProperty] bool allowGroupCollapse = true;

    /// <summary>Drives the contextual tab's visibility. That is all a contextual tab is.</summary>
    [ObservableProperty] bool pictureSelected;

    [ObservableProperty] bool bold;
    [ObservableProperty] bool italic;
    [ObservableProperty] bool underline;
    [ObservableProperty] double fontSize = 11;

    [ObservableProperty] bool alignLeft = true;
    [ObservableProperty] bool alignCenter;
    [ObservableProperty] bool alignRight;

    [ObservableProperty] bool showRuler = true;
    [ObservableProperty] bool showGridlines;


    [RelayCommand]
    void Run(string? what) => this.Say(what ?? "(unnamed command)");

    [RelayCommand]
    void OpenFileMenu() => this.Say("File");

    [RelayCommand]
    void OpenFontDialog() => this.Say("Font dialog (the group's corner arrow)");

    /// <summary>Selecting a picture is what brings the contextual tab up; the ribbon then moves to it.</summary>
    [RelayCommand]
    void InsertPicture()
    {
        this.PictureSelected = true;
        this.Say("Insert picture — the Picture Tools tab appeared");
    }

    [RelayCommand]
    void SetMode(string mode)
    {
        this.DisplayMode = Enum.Parse<RibbonDisplayMode>(mode);
        this.Say($"Display mode: {mode}");
    }


    partial void OnBoldChanged(bool value) => this.Say($"Bold {(value ? "on" : "off")}");

    partial void OnItalicChanged(bool value) => this.Say($"Italic {(value ? "on" : "off")}");

    partial void OnUnderlineChanged(bool value) => this.Say($"Underline {(value ? "on" : "off")}");

    // The three alignments are one choice, so turning one on turns the others off. The ribbon has no
    // opinion about that — a toggle group is the app's business, not the bar's.
    partial void OnAlignLeftChanged(bool value) => this.Exclusive(value, nameof(this.AlignLeft));

    partial void OnAlignCenterChanged(bool value) => this.Exclusive(value, nameof(this.AlignCenter));

    partial void OnAlignRightChanged(bool value) => this.Exclusive(value, nameof(this.AlignRight));

    bool suppress;

    void Exclusive(bool value, string which)
    {
        if (!value || this.suppress)
            return;

        this.suppress = true;
        try
        {
            if (which != nameof(this.AlignLeft)) this.AlignLeft = false;
            if (which != nameof(this.AlignCenter)) this.AlignCenter = false;
            if (which != nameof(this.AlignRight)) this.AlignRight = false;
        }
        finally
        {
            this.suppress = false;
        }

        this.Say($"Align {which.Replace("Align", string.Empty).ToLowerInvariant()}");
    }


    void Say(string what) => this.Log = $"{DateTime.Now:HH:mm:ss}  {what}";
}
