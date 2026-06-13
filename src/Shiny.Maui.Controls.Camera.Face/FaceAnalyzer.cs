using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Face;

/// <summary>
/// Detects faces in each frame (Apple Vision on iOS/macOS, Android MLKit, Windows.Media.FaceAnalysis;
/// a no-op on bare net10.0). Raises <see cref="FacesDetected"/> with the located faces and draws a box
/// around each. The per-platform <c>AnalyzeAsync</c> builds the <see cref="DetectedFace"/> list and calls
/// <see cref="Report"/>, which raises the event (on the UI thread) and produces the overlay boxes.
/// </summary>
public partial class FaceAnalyzer : FrameAnalyzer
{
    /// <inheritdoc/>
    public override string Id => "shiny.camera.face";

    /// <summary>Color used for the face boxes and labels. Default a teal accent.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#22D3EE");

    /// <summary>Caption drawn on each face box (null/empty for none). Default "Face".</summary>
    public string? Label { get; set; } = "Face";

    /// <summary>Command invoked (with the <see cref="FacesDetectedEventArgs"/>) when faces are detected.</summary>
    public static readonly BindableProperty FacesDetectedCommandProperty = BindableProperty.Create(
        nameof(FacesDetectedCommand), typeof(ICommand), typeof(FaceAnalyzer));

    /// <inheritdoc cref="FacesDetectedCommandProperty"/>
    public ICommand? FacesDetectedCommand
    {
        get => (ICommand?)this.GetValue(FacesDetectedCommandProperty);
        set => this.SetValue(FacesDetectedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw for the detected faces; return <c>null</c> for no overlay.
    /// When unset the analyzer draws one <see cref="BoxColor"/> box per face.
    /// </summary>
    public Func<FacesDetectedEventArgs, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>Raised on the UI thread when one or more faces are detected in a frame.</summary>
    public event EventHandler<FacesDetectedEventArgs>? FacesDetected;

    /// <summary>Raise <see cref="FacesDetected"/>/command and turn the faces into overlay boxes (null clears).</summary>
    protected IReadOnlyList<OverlayBox>? Report(IReadOnlyList<DetectedFace> faces)
    {
        if (faces.Count == 0)
            return null;

        var args = new FacesDetectedEventArgs(faces);
        this.Emit(() => this.FacesDetected?.Invoke(this, args), this.FacesDetectedCommand, args);

        return this.ResolveOverlay(args, this.OverlayProvider, () =>
        {
            var boxes = new OverlayBox[faces.Count];
            for (var i = 0; i < faces.Count; i++)
                boxes[i] = new OverlayBox(faces[i].Bounds, this.BoxColor, this.Label, this.BoxColor);
            return boxes;
        });
    }
}
