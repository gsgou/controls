namespace Shiny.Controls.Camera;

/// <summary>Which physical camera to use.</summary>
public enum CameraFacing
{
    /// <summary>The rear/world-facing camera (default).</summary>
    Back,

    /// <summary>The front/user-facing (selfie) camera.</summary>
    Front,

    /// <summary>An external/USB camera where supported (desktop, Android UVC).</summary>
    External
}


/// <summary>Flash behaviour used when capturing a photo.</summary>
public enum CameraFlashMode
{
    /// <summary>Flash never fires.</summary>
    Off,

    /// <summary>Flash always fires on capture.</summary>
    On,

    /// <summary>The system decides based on the scene.</summary>
    Auto
}


/// <summary>The pixel layout of a <see cref="CameraFrame"/>'s native buffer.</summary>
public enum CameraFrameFormat
{
    /// <summary>Unknown/unspecified.</summary>
    Unknown,

    /// <summary>32-bit BGRA (Apple 32BGRA, Windows BGRA8).</summary>
    Bgra32,

    /// <summary>Planar YUV 4:2:0 (Android YUV_420_888); plane 0 is luminance.</summary>
    Yuv420,

    /// <summary>Single 8-bit luminance plane.</summary>
    Grayscale8
}
