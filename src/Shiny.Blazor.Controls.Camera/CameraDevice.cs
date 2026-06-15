namespace Shiny.Blazor.Controls.Camera;

/// <summary>
/// One video input device the browser exposes (from <see cref="CameraView.GetAvailableCamerasAsync"/>).
/// Pass <see cref="Id"/> to <see cref="CameraView.CameraId"/> to use that exact camera.
/// </summary>
/// <remarks>
/// Init properties (not a positional record) so System.Text.Json deserializes it via property setters —
/// constructor-parameter names get trimmed in published WASM, which would otherwise throw.
/// </remarks>
public record CameraDevice
{
    /// <summary>Browser <c>MediaDeviceInfo.deviceId</c>.</summary>
    public string Id { get; init; } = "";

    /// <summary>Human-readable label (only populated after camera permission is granted).</summary>
    public string Name { get; init; } = "";
}
