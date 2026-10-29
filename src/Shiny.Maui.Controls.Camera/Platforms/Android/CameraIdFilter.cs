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

    // getIdentifier is a Java default method. With minSdk < 24, D8 desugars it out of the interface, the
    // generated Java wrapper still forwards it to .NET, and the binding's default body then throws
    // AbstractMethodError - the same failure that left FrameAnalyzerBridge's preview black. Return what the
    // Java default returns.
    public AndroidX.Camera.Core.Impl.Identifier Identifier => ICameraFilter.DefaultId;
}
