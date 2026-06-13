namespace Shiny.Controls.Camera;

/// <summary>Built-in live color filters applied to the camera preview (and captured output).</summary>
public enum CameraFilter
{
    /// <summary>No filter (default).</summary>
    None,

    /// <summary>Black &amp; white (luminance).</summary>
    Mono,

    /// <summary>High-contrast black &amp; white film look.</summary>
    Noir,

    /// <summary>Warm brown vintage tone.</summary>
    Sepia,

    /// <summary>Inverted (negative) colors.</summary>
    Invert,

    /// <summary>Boosted saturation/contrast.</summary>
    Vivid,

    /// <summary>Cool blue color cast.</summary>
    Cool,

    /// <summary>Warm orange color cast.</summary>
    Warm
}
