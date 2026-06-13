using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>A single detected face in normalized, upright image space.</summary>
/// <param name="Bounds">Normalized bounds (0..1) of the face in upright image space.</param>
/// <param name="Confidence">Detector confidence 0..1 (1 when the backend doesn't report one).</param>
/// <param name="Landmarks">Optional normalized feature points (eyes/nose/mouth) when available.</param>
public record DetectedFace(RectF Bounds, float Confidence = 1f, IReadOnlyList<PointF>? Landmarks = null);


/// <summary>Carries the faces found in a frame to <see cref="FaceAnalyzer.FacesDetected"/> subscribers.</summary>
public class FacesDetectedEventArgs(IReadOnlyList<DetectedFace> faces) : EventArgs
{
    /// <summary>The faces detected in the frame (never empty when the event is raised).</summary>
    public IReadOnlyList<DetectedFace> Faces { get; } = faces;
}
