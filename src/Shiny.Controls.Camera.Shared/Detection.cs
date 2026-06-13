using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Camera;

/// <summary>
/// A single detection produced by an <see cref="IFrameAnalyzer"/>. All geometry is expressed in
/// <b>normalized, upright image space</b> (0..1 on each axis, origin top-left, already corrected for
/// sensor rotation and front-camera mirroring). The overlay maps these into view space.
/// </summary>
/// <param name="Type">The category of detection.</param>
/// <param name="BoundingBox">Normalized bounds (0..1) of the detection in upright image space.</param>
/// <param name="Label">Optional human label / field name (e.g. "QR", "Face", "Total").</param>
/// <param name="Value">Optional decoded payload / recognized text / field value.</param>
/// <param name="Confidence">Detector confidence in the range 0..1 (1 when unknown).</param>
/// <param name="Landmarks">Optional normalized feature points (face landmarks, barcode corners).</param>
public record Detection(
    DetectionType Type,
    RectF BoundingBox,
    string? Label = null,
    string? Value = null,
    float Confidence = 1f,
    IReadOnlyList<PointF>? Landmarks = null
);
