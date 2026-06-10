using Microsoft.AspNetCore.Components;
using Shiny.Controls.Barcodes;

namespace Shiny.Blazor.Controls.Barcodes;


/// <summary>
/// A plain class (not .razor) so it inherits BarcodeView's render tree —
/// a .razor file would override BuildRenderTree with its own empty markup
/// </summary>
public class QRCodeView : BarcodeView
{
    [Parameter] public int Size { get; set; } = 300;

    protected override void OnParametersSet()
    {
        Format = BarcodeFormat.QRCode;
        PixelWidth = Size;
        PixelHeight = Size;
        base.OnParametersSet();
    }
}
