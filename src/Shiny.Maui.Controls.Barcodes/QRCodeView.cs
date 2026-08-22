namespace Shiny.Maui.Controls.Barcodes;

/// <summary>
/// Square QR code view. Same render pipeline as <see cref="BarcodeView"/> but locked to
/// <see cref="BarcodeFormat.QRCode"/> with an <see cref="ErrorCorrection"/> property and
/// a single <c>Size</c> property since QR codes are square.
/// </summary>
public class QRCodeView : BarcodeView
{
    public QRCodeView()
    {
        Format = BarcodeFormat.QRCode;
        PixelWidth = 300;
        PixelHeight = 300;
    }

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(int), typeof(QRCodeView), 300,
        propertyChanged: (b, _, n) =>
        {
            var v = (QRCodeView)b;
            v.PixelWidth = (int)n;
            v.PixelHeight = (int)n;
        });

    public int Size
    {
        get => (int)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly BindableProperty ErrorCorrectionProperty = BindableProperty.Create(
        nameof(ErrorCorrection), typeof(QRErrorCorrection), typeof(QRCodeView), QRErrorCorrection.Medium,
        propertyChanged: (b, _, _) => ((QRCodeView)b).Rebuild());

    public QRErrorCorrection ErrorCorrection
    {
        get => (QRErrorCorrection)GetValue(ErrorCorrectionProperty);
        set => SetValue(ErrorCorrectionProperty, value);
    }

    protected override void ApplyExtraOptions(BarcodeRenderOptions options)
    {
        options.QRErrorCorrection = ErrorCorrection;
    }
}
