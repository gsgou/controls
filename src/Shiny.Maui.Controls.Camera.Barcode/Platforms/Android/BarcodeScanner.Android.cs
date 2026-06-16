using Android.Runtime;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;
using MlBarcode = Xamarin.Google.MLKit.Vision.Barcode.Common.Barcode;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>Barcode scanning via Android MLKit.</summary>
public partial class BarcodeScanner
{
    IBarcodeScanner? client;

    IBarcodeScanner GetClient()
    {
        if (this.client != null)
            return this.client;

        var formats = ToMlFormats(this.Formats);
        var builder = new BarcodeScannerOptions.Builder();
        if (formats == null)
            builder.SetBarcodeFormats(MlBarcode.FormatAllFormats);
        else
            builder.SetBarcodeFormats(formats[0], formats.Skip(1).ToArray());

        this.client = BarcodeScanning.GetClient(builder.Build());
        return this.client;
    }

    public async partial Task<List<DetectedBarcode>> ScanAsync(CameraFrame frame, CancellationToken ct)
    {
        if (frame is not AndroidCameraFrame android)
            return [];

        var mediaImage = android.Proxy.Image;
        if (mediaImage == null)
            return [];

        var rotation = android.Proxy.ImageInfo.RotationDegrees;
        var input = InputImage.FromMediaImage(mediaImage, rotation);

        // MLKit returns boxes in the upright (rotation-applied) image space
        var uprightW = rotation is 90 or 270 ? frame.Height : frame.Width;
        var uprightH = rotation is 90 or 270 ? frame.Width : frame.Height;

        var result = await GmsTaskAwaiter.AwaitAsync(this.GetClient().Process(input)).ConfigureAwait(false);

        var list = new List<DetectedBarcode>();
        if (result is JavaList items)
        {
            foreach (var item in items)
            {
                if (item is not MlBarcode bc)
                    continue;

                var value = bc.RawValue ?? bc.DisplayValue;
                if (String.IsNullOrEmpty(value))
                    continue;

                var r = bc.BoundingBox;
                if (r == null)
                    continue;

                var raw = new RectF((float)r.Left / uprightW, (float)r.Top / uprightH,
                    (float)r.Width() / uprightW, (float)r.Height() / uprightH);
                var box = CoordinateTransform.ApplyOrientation(raw, 0, frame.IsMirrored);
                list.Add(new DetectedBarcode(value, Map(bc.Format), box));
            }
        }
        return list;
    }

    static BarcodeFormat Map(int format) => format switch
    {
        MlBarcode.FormatQrCode => BarcodeFormat.QrCode,
        MlBarcode.FormatAztec => BarcodeFormat.Aztec,
        MlBarcode.FormatDataMatrix => BarcodeFormat.DataMatrix,
        MlBarcode.FormatPdf417 => BarcodeFormat.Pdf417,
        MlBarcode.FormatCode128 => BarcodeFormat.Code128,
        MlBarcode.FormatCode39 => BarcodeFormat.Code39,
        MlBarcode.FormatCode93 => BarcodeFormat.Code93,
        MlBarcode.FormatCodabar => BarcodeFormat.Codabar,
        MlBarcode.FormatEan8 => BarcodeFormat.Ean8,
        MlBarcode.FormatEan13 => BarcodeFormat.Ean13,
        MlBarcode.FormatUpcA => BarcodeFormat.UpcA,
        MlBarcode.FormatUpcE => BarcodeFormat.UpcE,
        MlBarcode.FormatItf => BarcodeFormat.Itf,
        _ => BarcodeFormat.Unknown
    };

    static int[]? ToMlFormats(IList<BarcodeFormat>? formats)
    {
        if (formats == null || formats.Count == 0)
            return null;

        var set = new List<int>();
        foreach (var f in formats)
        {
            var ml = f switch
            {
                BarcodeFormat.QrCode => MlBarcode.FormatQrCode,
                BarcodeFormat.Aztec => MlBarcode.FormatAztec,
                BarcodeFormat.DataMatrix => MlBarcode.FormatDataMatrix,
                BarcodeFormat.Pdf417 => MlBarcode.FormatPdf417,
                BarcodeFormat.Code128 => MlBarcode.FormatCode128,
                BarcodeFormat.Code39 => MlBarcode.FormatCode39,
                BarcodeFormat.Code93 => MlBarcode.FormatCode93,
                BarcodeFormat.Codabar => MlBarcode.FormatCodabar,
                BarcodeFormat.Ean8 => MlBarcode.FormatEan8,
                BarcodeFormat.Ean13 => MlBarcode.FormatEan13,
                BarcodeFormat.UpcA => MlBarcode.FormatUpcA,
                BarcodeFormat.UpcE => MlBarcode.FormatUpcE,
                BarcodeFormat.Itf => MlBarcode.FormatItf,
                _ => 0
            };
            if (ml != 0)
                set.Add(ml);
        }
        return set.Count == 0 ? null : set.Distinct().ToArray();
    }
}
