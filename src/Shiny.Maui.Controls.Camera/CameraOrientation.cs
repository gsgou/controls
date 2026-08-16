namespace Shiny.Maui.Controls.Camera;

/// <summary>
/// How the camera orients what it captures — the live preview, still capture, recorded video, and the frames
/// handed to a <see cref="FrameAnalyzer"/>. Set via <see cref="CameraView.Orientation"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The landscape members are named for where the device's top edge physically points, and that is
/// deliberate.</b> Every platform ships a <c>LandscapeLeft</c>/<c>LandscapeRight</c> pair and they do not
/// agree with one another: <c>UIInterfaceOrientationLandscapeLeft</c> is
/// <c>UIDeviceOrientationLandscapeRight</c> — Apple's own headers note the inversion — and
/// <c>AVCaptureVideoOrientation</c> follows the *interface* convention while most code reading a rotation is
/// holding a *device* one. Inheriting either name would import that confusion into this API, and getting it
/// backwards is invisible until footage comes back rotated 180°. So these names describe the hardware, and
/// the mapping to each platform constant is written down once, here.
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Member</term><description>Apple (<c>AVCaptureVideoOrientation</c>) / Android (<c>Surface</c>)</description>
///   </listheader>
///   <item>
///     <term><see cref="Portrait"/></term>
///     <description><c>Portrait</c> / <c>ROTATION_0</c></description>
///   </item>
///   <item>
///     <term><see cref="PortraitUpsideDown"/></term>
///     <description><c>PortraitUpsideDown</c> / <c>ROTATION_180</c></description>
///   </item>
///   <item>
///     <term><see cref="LandscapeTopLeft"/></term>
///     <description><c>LandscapeRight</c> (yes, right) / <c>ROTATION_90</c></description>
///   </item>
///   <item>
///     <term><see cref="LandscapeTopRight"/></term>
///     <description><c>LandscapeLeft</c> (yes, left) / <c>ROTATION_270</c></description>
///   </item>
/// </list>
/// <para>
/// The Android column assumes a device whose natural orientation is portrait, which is every phone but not
/// every tablet. <see cref="Device"/> does not rely on that assumption — it reads the display rotation
/// directly — so prefer it unless the camera is deliberately being pinned.
/// </para>
/// </remarks>
public enum CameraOrientation
{
    /// <summary>
    /// Follow the device as it rotates. The default, and the right answer for almost everything.
    /// </summary>
    /// <remarks>
    /// A change that arrives while a recording is in progress is <b>deferred until that recording
    /// finishes</b> — see <see cref="CameraView.Orientation"/> for why re-orienting an encoder mid-file is
    /// not survivable.
    /// </remarks>
    Device = 0,

    /// <summary>Device upright — top edge up.</summary>
    Portrait,

    /// <summary>Device inverted — top edge down.</summary>
    PortraitUpsideDown,

    /// <summary>Device on its side with its top edge pointing <b>left</b>.</summary>
    LandscapeTopLeft,

    /// <summary>Device on its side with its top edge pointing <b>right</b>.</summary>
    LandscapeTopRight
}
