using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;
using Shiny.Maui.Controls.ImageEditor;
using Shiny.Maui.Controls.Media;

namespace Sample.Features.Media;

[ShellMap<MediaPickerPage>]
public partial class MediaPickerViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<MediaPickerItem> photos = new();

    [ObservableProperty]
    bool allowGallery = true;

    [ObservableProperty]
    bool allowCamera = true;

    [ObservableProperty]
    bool allowPhotoEdit = true;

    [ObservableProperty]
    bool showAsCarousel = true;

    [ObservableProperty]
    int maxPhotos = 5;

    [ObservableProperty]
    double compressionQuality = 85;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputFormat))]
    bool usePng;

    public ImageExportFormat OutputFormat => this.UsePng ? ImageExportFormat.Png : ImageExportFormat.Jpeg;

    public int CompressionQualityPercent => (int)this.CompressionQuality;

    partial void OnCompressionQualityChanged(double value)
        => OnPropertyChanged(nameof(CompressionQualityPercent));
}
