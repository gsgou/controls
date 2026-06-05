using System.Collections.Generic;
using System.Text;
using ZXingFormat = ZXing.BarcodeFormat;

namespace Shiny.Controls.Barcodes;

/// <summary>
/// Stateless renderer that produces PNG bytes or SVG markup for any supported barcode format.
/// Backed by ZXing.Net; pure managed, AOT-safe.
/// </summary>
public static class BarcodeRenderer
{
    public static byte[] RenderPng(string content, BarcodeFormat format, BarcodeRenderOptions? options = null)
    {
        options ??= new BarcodeRenderOptions();
        var matrix = Encode(content, format, options);
        var (fr, fg, fb) = ParseColor(options.ForegroundColor);
        var (br, bg, bb) = ParseColor(options.BackgroundColor);
        return PngEncoder.Encode(matrix, fr, fg, fb, br, bg, bb);
    }

    public static string RenderSvg(string content, BarcodeFormat format, BarcodeRenderOptions? options = null)
    {
        options ??= new BarcodeRenderOptions();
        var matrix = Encode(content, format, options);
        return BuildSvg(matrix, options.ForegroundColor, options.BackgroundColor);
    }

    /// <summary>Render and return a data URI suitable for an HTML img src or CSS background.</summary>
    public static string RenderDataUri(string content, BarcodeFormat format, BarcodeImageFormat imageFormat, BarcodeRenderOptions? options = null)
    {
        if (imageFormat == BarcodeImageFormat.Svg)
        {
            var svg = RenderSvg(content, format, options);
            return "data:image/svg+xml;base64," + System.Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        }
        var png = RenderPng(content, format, options);
        return "data:image/png;base64," + System.Convert.ToBase64String(png);
    }

    static ZXing.Common.BitMatrix Encode(string content, BarcodeFormat format, BarcodeRenderOptions options)
    {
        var writer = new ZXing.BarcodeWriterGeneric
        {
            Format = MapFormat(format),
            Options = BuildEncodingOptions(format, options)
        };
        return writer.Encode(content);
    }

    static ZXing.Common.EncodingOptions BuildEncodingOptions(BarcodeFormat format, BarcodeRenderOptions options)
    {
        if (format == BarcodeFormat.QRCode)
        {
            var qr = new ZXing.QrCode.QrCodeEncodingOptions
            {
                Width = options.PixelWidth,
                Height = options.PixelHeight,
                Margin = options.Margin,
                ErrorCorrection = options.QRErrorCorrection switch
                {
                    QRErrorCorrection.Low => ZXing.QrCode.Internal.ErrorCorrectionLevel.L,
                    QRErrorCorrection.Quartile => ZXing.QrCode.Internal.ErrorCorrectionLevel.Q,
                    QRErrorCorrection.High => ZXing.QrCode.Internal.ErrorCorrectionLevel.H,
                    _ => ZXing.QrCode.Internal.ErrorCorrectionLevel.M
                },
                CharacterSet = "UTF-8"
            };
            return qr;
        }
        return new ZXing.Common.EncodingOptions
        {
            Width = options.PixelWidth,
            Height = options.PixelHeight,
            Margin = options.Margin
        };
    }

    static ZXingFormat MapFormat(BarcodeFormat format) => format switch
    {
        BarcodeFormat.QRCode => ZXingFormat.QR_CODE,
        BarcodeFormat.Aztec => ZXingFormat.AZTEC,
        BarcodeFormat.DataMatrix => ZXingFormat.DATA_MATRIX,
        BarcodeFormat.Pdf417 => ZXingFormat.PDF_417,
        BarcodeFormat.Code128 => ZXingFormat.CODE_128,
        BarcodeFormat.Code39 => ZXingFormat.CODE_39,
        BarcodeFormat.Code93 => ZXingFormat.CODE_93,
        BarcodeFormat.Codabar => ZXingFormat.CODABAR,
        BarcodeFormat.Ean8 => ZXingFormat.EAN_8,
        BarcodeFormat.Ean13 => ZXingFormat.EAN_13,
        BarcodeFormat.UpcA => ZXingFormat.UPC_A,
        BarcodeFormat.UpcE => ZXingFormat.UPC_E,
        BarcodeFormat.Itf => ZXingFormat.ITF,
        _ => ZXingFormat.QR_CODE
    };

    internal static (byte R, byte G, byte B) ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return (0, 0, 0);

        var s = hex.Trim();
        if (s[0] == '#') s = s.Substring(1);

        if (s.Length == 3)
        {
            byte r = (byte)(System.Convert.ToInt32(new string(s[0], 2), 16));
            byte g = (byte)(System.Convert.ToInt32(new string(s[1], 2), 16));
            byte b = (byte)(System.Convert.ToInt32(new string(s[2], 2), 16));
            return (r, g, b);
        }
        if (s.Length == 6)
        {
            return (
                (byte)System.Convert.ToInt32(s.Substring(0, 2), 16),
                (byte)System.Convert.ToInt32(s.Substring(2, 2), 16),
                (byte)System.Convert.ToInt32(s.Substring(4, 2), 16)
            );
        }
        if (s.Length == 8)
        {
            // assume #AARRGGBB — strip alpha
            return (
                (byte)System.Convert.ToInt32(s.Substring(2, 2), 16),
                (byte)System.Convert.ToInt32(s.Substring(4, 2), 16),
                (byte)System.Convert.ToInt32(s.Substring(6, 2), 16)
            );
        }
        return (0, 0, 0);
    }

    static string BuildSvg(ZXing.Common.BitMatrix matrix, string fgColor, string bgColor)
    {
        int w = matrix.Width, h = matrix.Height;
        var sb = new StringBuilder(2048);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
          .Append(w).Append(' ').Append(h)
          .Append("\" shape-rendering=\"crispEdges\">");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(NormalizeHex(bgColor)).Append("\"/>");
        sb.Append("<path fill=\"").Append(NormalizeHex(fgColor)).Append("\" d=\"");
        for (int y = 0; y < h; y++)
        {
            int x = 0;
            while (x < w)
            {
                if (!matrix[x, y]) { x++; continue; }
                int runStart = x;
                while (x < w && matrix[x, y]) x++;
                int len = x - runStart;
                sb.Append('M').Append(runStart).Append(' ').Append(y)
                  .Append('h').Append(len)
                  .Append("v1h-").Append(len).Append('z');
            }
        }
        sb.Append("\"/></svg>");
        return sb.ToString();
    }

    static string NormalizeHex(string hex)
    {
        var (r, g, b) = ParseColor(hex);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
