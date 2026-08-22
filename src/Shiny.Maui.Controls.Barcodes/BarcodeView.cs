namespace Shiny.Maui.Controls.Barcodes;

/// <summary>
/// Renders any supported 1D or 2D barcode (Code128, EAN13, PDF417, Data Matrix, etc.) as a PNG.
/// For QR codes specifically, prefer <see cref="QRCodeView"/> which exposes error-correction.
/// Need an SVG string instead? Call <see cref="BarcodeRenderer.RenderSvg(string, BarcodeFormat, BarcodeRenderOptions?)"/> directly.
/// </summary>
public class BarcodeView : ContentView
{
    readonly Image image;

    public BarcodeView()
    {
        image = new Image
        {
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        Content = image;
    }

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(BarcodeView), string.Empty,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty FormatProperty = BindableProperty.Create(
        nameof(Format), typeof(BarcodeFormat), typeof(BarcodeView), BarcodeFormat.Code128,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public BarcodeFormat Format
    {
        get => (BarcodeFormat)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public static readonly BindableProperty PixelWidthProperty = BindableProperty.Create(
        nameof(PixelWidth), typeof(int), typeof(BarcodeView), 400,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public int PixelWidth
    {
        get => (int)GetValue(PixelWidthProperty);
        set => SetValue(PixelWidthProperty, value);
    }

    public static readonly BindableProperty PixelHeightProperty = BindableProperty.Create(
        nameof(PixelHeight), typeof(int), typeof(BarcodeView), 150,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public int PixelHeight
    {
        get => (int)GetValue(PixelHeightProperty);
        set => SetValue(PixelHeightProperty, value);
    }

    public static readonly BindableProperty MarginPixelsProperty = BindableProperty.Create(
        nameof(MarginPixels), typeof(int), typeof(BarcodeView), 10,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public int MarginPixels
    {
        get => (int)GetValue(MarginPixelsProperty);
        set => SetValue(MarginPixelsProperty, value);
    }

    public static readonly BindableProperty ForegroundColorProperty = BindableProperty.Create(
        nameof(ForegroundColor), typeof(Color), typeof(BarcodeView), Colors.Black,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public Color ForegroundColor
    {
        get => (Color)GetValue(ForegroundColorProperty);
        set => SetValue(ForegroundColorProperty, value);
    }

    public static readonly BindableProperty BarcodeBackgroundColorProperty = BindableProperty.Create(
        nameof(BarcodeBackgroundColor), typeof(Color), typeof(BarcodeView), Colors.White,
        propertyChanged: (b, _, _) => ((BarcodeView)b).Rebuild());

    public Color BarcodeBackgroundColor
    {
        get => (Color)GetValue(BarcodeBackgroundColorProperty);
        set => SetValue(BarcodeBackgroundColorProperty, value);
    }

    /// <summary>
    /// Re-encodes and re-applies the image. Call this from a subclass whose own property changed
    /// the render options - never by writing to <see cref="Value"/>, which stamps a manual value
    /// that permanently outranks any binding the caller put on it.
    /// </summary>
    protected void Rebuild()
    {
        if (string.IsNullOrEmpty(Value))
        {
            image.Source = null;
            return;
        }

        try
        {
            var options = new BarcodeRenderOptions
            {
                PixelWidth = PixelWidth,
                PixelHeight = PixelHeight,
                Margin = MarginPixels,
                ForegroundColor = ForegroundColor.ToArgbHex(),
                BackgroundColor = BarcodeBackgroundColor.ToArgbHex()
            };
            ApplyExtraOptions(options);

            var bytes = BarcodeRenderer.RenderPng(Value, Format, options);
            image.Source = ImageSource.FromStream(() => new System.IO.MemoryStream(bytes));
        }
        catch
        {
            image.Source = null;
        }
    }

    /// <summary>Override hook for subclasses (e.g., <see cref="QRCodeView"/>) to apply additional render options.</summary>
    protected virtual void ApplyExtraOptions(BarcodeRenderOptions options) { }
}
