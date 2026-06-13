using AndroidX.Camera.Core;

namespace Shiny.Maui.Controls.Camera;

// A CameraX CameraFilter that keeps only the camera whose Camera2 id matches the requested one,
// letting callers select an exact device (e.g. a specific back lens) by id.
sealed class CameraIdFilter(string cameraId) : Java.Lang.Object, ICameraFilter
{
    public IList<ICameraInfo>? Filter(IList<ICameraInfo>? cameraInfos)
        => cameraInfos?
            .Where(i => AndroidX.Camera.Camera2.InterOp.Camera2CameraInfo.From(i).CameraId == cameraId)
            .ToList();
}
