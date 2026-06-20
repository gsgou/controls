using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Barcode;

/// <summary>
/// Decodes 1D/2D barcodes and QR codes from each frame using the native scanner — Apple Vision on
/// iOS/macOS and Android MLKit (a no-op on Windows and bare net10.0). Raises <see cref="BarcodeDetected"/>
/// with the decoded format + value for each code in view, and draws a box (captioned with the value) around
/// each; clears them when no code is in view.
/// </summary>
public class BarcodeAnalyzer : FrameAnalyzer
{
    readonly BarcodeScanner scanner = new();

    /// <inheritdoc/>
    public override string Id => "shiny.camera.barcode";

    /// <summary>
    /// Restrict to specific symbologies (null = all supported). Filters the native scanner. Settable in XAML
    /// as a comma-separated list — e.g. <c>Formats="QrCode,Ean13,Code128"</c>.
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(BarcodeFormatCollectionTypeConverter))]
    public IList<BarcodeFormat>? Formats
    {
        get => this.scanner.Formats;
        set => this.scanner.Formats = value;
    }

    /// <summary>Box outline + caption color. Default a green accent.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#22C55E");

    /// <summary>Command invoked (with the <see cref="BarcodeDetectedEventArgs"/>) when a barcode is decoded.</summary>
    public static readonly BindableProperty BarcodeDetectedCommandProperty = BindableProperty.Create(
        nameof(BarcodeDetectedCommand), typeof(ICommand), typeof(BarcodeAnalyzer));

    /// <inheritdoc cref="BarcodeDetectedCommandProperty"/>
    public ICommand? BarcodeDetectedCommand
    {
        get => (ICommand?)this.GetValue(BarcodeDetectedCommandProperty);
        set => this.SetValue(BarcodeDetectedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw for a decode; return <c>null</c> for no overlay. When
    /// unset the analyzer draws a single <see cref="BoxColor"/> box captioned with the value, per barcode.
    /// </summary>
    public Func<BarcodeDetectedEventArgs, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>
    /// Continuation invoked (on the UI thread) with each decoded barcode while the analyzer is armed; return
    /// <c>true</c> to keep scanning (stay armed), <c>false</c> to stop until the next <see cref="CameraView.Scan"/>.
    /// When unset, delivery is single-shot (one barcode per arm). Bindable so it can target a VM method in XAML.
    /// </summary>
    public static readonly BindableProperty OnDetectedProperty = BindableProperty.Create(
        nameof(OnDetected), typeof(Func<BarcodeDetectedEventArgs, Task<bool>>), typeof(BarcodeAnalyzer));

    /// <inheritdoc cref="OnDetectedProperty"/>
    public Func<BarcodeDetectedEventArgs, Task<bool>>? OnDetected
    {
        get => (Func<BarcodeDetectedEventArgs, Task<bool>>?)this.GetValue(OnDetectedProperty);
        set => this.SetValue(OnDetectedProperty, value);
    }

    /// <summary>Raised on the UI thread for each barcode decoded in a frame, while the analyzer is armed.</summary>
    public event EventHandler<BarcodeDetectedEventArgs>? BarcodeDetected;

    readonly HashSet<string> delivered = new();

    /// <inheritdoc/>
    public override async ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var codes = await this.scanner.ScanAsync(frame, ct).ConfigureAwait(false);
        if (codes.Count == 0)
        {
            this.delivered.Clear(); // codes left the frame -> allow re-delivery when one returns
            return null; // nothing in view -> clear this analyzer's boxes
        }

        List<OverlayBox>? boxes = null;
        foreach (var code in codes)
        {
            var args = new BarcodeDetectedEventArgs(code.Format, code.Value, code.BoundingBox);
            // don't re-deliver the same value while it lingers in view (avoids 30x/sec when staying armed);
            // boxes are still drawn every frame below regardless of delivery
            if (this.delivered.Add(code.Value))
                this.Deliver(args, () => this.BarcodeDetected?.Invoke(this, args), this.BarcodeDetectedCommand, this.OnDetected);

            var drawn = this.ResolveOverlay(args, this.OverlayProvider,
                () => new[] { new OverlayBox(code.BoundingBox, this.BoxColor, code.Value, this.BoxColor) });
            if (drawn is { Count: > 0 })
                (boxes ??= []).AddRange(drawn);
        }
        return boxes;
    }
}
