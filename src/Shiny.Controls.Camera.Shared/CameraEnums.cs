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


/// <summary>The category of a <see cref="Detection"/> produced by an analyzer.</summary>
public enum DetectionType
{
    /// <summary>A 1D/2D barcode or QR code. <see cref="Detection.Value"/> holds the decoded payload.</summary>
    Barcode,

    /// <summary>A detected face. <see cref="Detection.Landmarks"/> may hold eye/nose/mouth points.</summary>
    Face,

    /// <summary>A recognized block/line/word of text. <see cref="Detection.Value"/> holds the text.</summary>
    Text,

    /// <summary>A region where motion was detected between frames.</summary>
    Motion,

    /// <summary>A structured field extracted by an <c>IDocumentAnalyzer</c> (e.g. invoice total).
    /// <see cref="Detection.Label"/> is the field name, <see cref="Detection.Value"/> the field value.</summary>
    DocumentField,

    /// <summary>A detection produced by a custom analyzer.</summary>
    Custom
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
