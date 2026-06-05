using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;
using Shiny.Controls.Barcodes;

namespace Sample.Features.Barcodes;

public partial class QRCodePage : ContentPage
{
    public QRCodePage()
    {
        InitializeComponent();
    }
}

[ShellMap<QRCodePage>(registerRoute: false)]
public partial class QRCodeViewModel : ObservableObject
{
    [ObservableProperty]
    string value = "https://shinylib.net/controls/";

    [ObservableProperty]
    int size = 300;

    [ObservableProperty]
    QRErrorCorrection errorCorrection = QRErrorCorrection.Medium;

    public QRErrorCorrection[] ErrorCorrectionLevels { get; } = Enum.GetValues<QRErrorCorrection>();
}
