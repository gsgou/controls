using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;

namespace Sample.Features.Barcodes;

public partial class BarcodeGalleryPage : ContentPage
{
    public BarcodeGalleryPage()
    {
        InitializeComponent();
    }
}

[ShellMap<BarcodeGalleryPage>(registerRoute: false)]
public partial class BarcodeGalleryViewModel : ObservableObject
{
}
