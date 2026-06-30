using Microsoft.Maui.Graphics;

namespace Shiny.Blazor.Controls.Camera;

/// <summary>
/// Base for the single in-browser frame analyzer assigned to <see cref="CameraView.Analyzer"/>. Mirrors the
/// MAUI <c>FrameAnalyzer</c> shape (one analyzer at a time, a <see cref="ScanWindow"/>, a box toggle) within
/// what the browser can do — today only <see cref="BarcodeAnalyzer"/> is wired to a JS engine
/// (<c>BarcodeDetector</c>); the others are placeholders for future engines and report a "not supported"
/// error if assigned.
/// </summary>
public abstract class CameraAnalyzer
{
    /// <summary>Stable kind discriminator passed to the JS module (e.g. <c>"barcode"</c>).</summary>
    public abstract string Kind { get; }

    /// <summary>
    /// Restricts both detection and the overlay to a normalized rectangle (0..1) in upright video space; null
    /// scans the whole frame. Only detections centered inside it are reported and drawn, and the overlay dims
    /// outside it and frames a viewfinder reticle.
    /// </summary>
    public RectF? ScanWindow { get; set; }

    /// <summary>Whether this analyzer's bounding boxes are drawn. Default <c>true</c>.</summary>
    public bool ShowBoundingBox { get; set; } = true;

    // flat [x, y, w, h] (or null) — primitives only, so it's trim-safe across JS interop
    internal float[]? ScanWindowArray()
        => this.ScanWindow is { } w ? [w.X, w.Y, w.Width, w.Height] : null;
}

/// <summary>
/// Decodes 1D/2D barcodes and QR codes in-browser via the native <c>BarcodeDetector</c> (Chromium/Android;
/// a no-op with an error on Firefox/Safari). Raises <see cref="CameraView.BarcodesDetected"/> with every code
/// in view. Set <see cref="CameraAnalyzer.ScanWindow"/> to restrict scanning to a region.
/// </summary>
public class BarcodeAnalyzer : CameraAnalyzer
{
    /// <inheritdoc/>
    public override string Kind => "barcode";
}

/// <summary>Placeholder face analyzer — no in-browser engine is wired yet; assigning it reports "not supported".</summary>
public class FaceAnalyzer : CameraAnalyzer
{
    /// <inheritdoc/>
    public override string Kind => "face";
}

/// <summary>
/// Detects when a document is <i>present</i> in the frame (a cheap in-browser luminance/edge heuristic — no
/// OCR), so the (slow, paid) AI extraction only runs on a real document. Pair it with
/// <see cref="CameraView.RequestDocumentImageAsync"/> to get the cropped JPEG of the next steadily-present
/// document, then send that to a Microsoft.Extensions.AI <c>IChatClient</c> — see the
/// <c>Shiny.Blazor.Controls.Camera.Ai</c> package's <c>AiDocumentScanner</c>. Set
/// <see cref="CameraAnalyzer.ScanWindow"/> to require the document inside an aim region.
/// </summary>
public class DocumentAnalyzer : CameraAnalyzer
{
    /// <inheritdoc/>
    public override string Kind => "document";
}
