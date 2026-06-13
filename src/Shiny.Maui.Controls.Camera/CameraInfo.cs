namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// Describes one selectable physical camera. Pass <see cref="Id"/> to <see cref="CameraView.CameraId"/>
/// to use that exact device (e.g. a specific USB webcam on macOS, or the ultra-wide lens on a phone).
/// </summary>
/// <param name="Id">Stable platform device identifier (AVFoundation uniqueID, Camera2 id, Windows device id).</param>
/// <param name="Name">Human-readable device name.</param>
/// <param name="Facing">Best-known position; <see cref="CameraFacing.External"/> for USB/unknown.</param>
/// <param name="IsDefault">Whether this is the system default for its facing.</param>
public record CameraInfo(string Id, string Name, CameraFacing Facing, bool IsDefault = false);
