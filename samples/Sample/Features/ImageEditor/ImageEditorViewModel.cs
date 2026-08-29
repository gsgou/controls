using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Maui.Controls.ImageEditor;

namespace Sample.Features.ImageEditor;

[ShellMap<ImageEditorPage>]
public partial class ImageEditorViewModel(IDialogs dialogs) : ObservableObject
{
    [ObservableProperty]
    ImageSource? imageSource = "dotnet_bot.png";

    public Action<string>? OnImageSaved { get; set; }

    [ObservableProperty]
    bool canUndo;

    [ObservableProperty]
    bool canRedo;

    // Move, not the enum's default of None. The binding is two-way, so an unset field pushes None into
    // the editor at bind time and overrides the control's own Move default - leaving the toolbar with
    // no tool selected and nothing highlighted, which reads as the selection being broken.
    [ObservableProperty]
    ImageEditorToolMode currentToolMode = ImageEditorToolMode.Move;

    [ObservableProperty]
    Color drawColor = Colors.White;

    [ObservableProperty]
    double drawStrokeWidth = 4;

    /// <summary>Null so the shape tools start as outlines — the toolbar's fill toggle turns it on.</summary>
    [ObservableProperty]
    Color? shapeFillColor;

    [ObservableProperty]
    double zoomLevel = 1;

    public IList<string> AvailableFonts { get; } = new List<string>
    {
        "OpenSansRegular",
        "OpenSansSemibold"
    };

    public IList<double> AvailableFontSizes { get; } = new List<double>
    {
        12, 14, 16, 18, 20, 24, 32, 40, 56
    };

    [ObservableProperty]
    string? textFontFamily = "OpenSansRegular";

    [ObservableProperty]
    double textFontSize = 16;

    [RelayCommand]
    async Task PickImage()
    {
        var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
        {
            Title = "Select an image to edit"
        });
        if (result == null)
            return;

        ImageSource = ImageSource.FromFile(result.FullPath);
    }

    [RelayCommand]
    async Task Save(EditedImage editedImage)
    {
        await using var stream = await editedImage.ToStreamAsync(ImageExportFormat.Png);

        var fileName = $"edited_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await using (var fileStream = File.Create(filePath))
            await stream.CopyToAsync(fileStream);

        OnImageSaved?.Invoke(filePath);
        await dialogs.Alert("Saved", $"Image saved to:\n{filePath}");
    }
}
