using Microsoft.Maui.Graphics;

namespace Shiny.Blazor.Controls.Camera;

/// <summary>
/// Flat, primitives-only DTO marshaled from the camera JS module into [JSInvokable] callbacks. Kept
/// separate from <see cref="Shiny.Controls.Camera.Detection"/> (which uses <see cref="RectF"/>) so JS
/// interop never has to (de)serialize a struct — see the repo rule against anonymous/complex interop types.
/// Coordinates are normalized 0..1 in upright video space.
/// </summary>
public class CameraDetection
{
    public string Type { get; set; } = "Barcode";
    public float X { get; set; }
    public float Y { get; set; }
    public float W { get; set; }
    public float H { get; set; }
    public string? Label { get; set; }
    public string? Value { get; set; }
    public float Confidence { get; set; } = 1f;

    /// <summary>Project to the shared <see cref="Shiny.Controls.Camera.Detection"/> record.</summary>
    public Shiny.Controls.Camera.Detection ToDetection()
    {
        var type = Enum.TryParse<Shiny.Controls.Camera.DetectionType>(this.Type, ignoreCase: true, out var t)
            ? t
            : Shiny.Controls.Camera.DetectionType.Custom;

        return new Shiny.Controls.Camera.Detection(
            type,
            new RectF(this.X, this.Y, this.W, this.H),
            this.Label,
            this.Value,
            this.Confidence
        );
    }
}
