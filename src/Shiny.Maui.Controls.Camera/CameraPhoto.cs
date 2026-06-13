namespace Shiny.Maui.Controls.Camera;

/// <summary>The result of <see cref="CameraView.CapturePhotoAsync"/>.</summary>
/// <param name="Data">Encoded image bytes (JPEG).</param>
/// <param name="Width">Pixel width of the captured image.</param>
/// <param name="Height">Pixel height of the captured image.</param>
public record CameraPhoto(byte[] Data, int Width, int Height)
{
    /// <summary>Open a stream over the encoded bytes.</summary>
    public Stream OpenRead() => new MemoryStream(this.Data, false);
}
