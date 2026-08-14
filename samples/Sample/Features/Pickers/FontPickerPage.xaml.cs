using System.ComponentModel;
using System.Runtime.CompilerServices;
using Shiny;

namespace Sample.Features.Pickers;

public partial class FontPickerPage : ContentPage
{
    public FontPickerPage()
    {
        InitializeComponent();
    }
}

[ShellMap<FontPickerPage>(registerRoute: false)]
public class FontPickerViewModel : INotifyPropertyChanged
{
    string? selectedFont = FontCatalog.Families[0];
    double selectedFontSize = 18;

    /// <summary>
    /// The families the picker offers. These are native family names, so the list differs per
    /// platform — MAUI resolves <c>FontFamily</c> against registered aliases first and the
    /// platform's own font table second.
    /// </summary>
    public IList<string> AvailableFonts { get; } = FontCatalog.Families;

    public IList<double> AvailableFontSizes { get; } =
        [10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 42, 48, 56, 64];

    public string PreviewText => "The quick brown fox jumps over the lazy dog";

    public string? SelectedFont
    {
        get => selectedFont;
        set => SetProperty(ref selectedFont, value);
    }

    public double SelectedFontSize
    {
        get => selectedFontSize;
        set => SetProperty(ref selectedFontSize, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

static class FontCatalog
{
    public static readonly IList<string> Families =
    [
        // Registered with ConfigureFonts in MauiProgram, so these resolve everywhere.
        "OpenSansRegular",
        "OpenSansSemibold",

#if IOS || MACCATALYST
        "Helvetica Neue",
        "Georgia",
        "Times New Roman",
        "Courier New",
        "Verdana",
        "Trebuchet MS",
        "Palatino",
        "Baskerville",
        "Futura",
        "Optima",
        "American Typewriter",
        "Menlo",
        "Chalkduster",
        "Papyrus",
        "Snell Roundhand"
#elif ANDROID
        "sans-serif",
        "sans-serif-light",
        "sans-serif-medium",
        "sans-serif-thin",
        "sans-serif-condensed",
        "serif",
        "serif-monospace",
        "monospace",
        "casual",
        "cursive"
#else
        "Segoe UI",
        "Arial",
        "Calibri",
        "Cambria",
        "Consolas",
        "Courier New",
        "Georgia",
        "Impact",
        "Times New Roman",
        "Trebuchet MS",
        "Verdana",
        "Comic Sans MS"
#endif
    ];
}
