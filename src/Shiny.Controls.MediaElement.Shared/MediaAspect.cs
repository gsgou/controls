namespace Shiny.Controls.Media;

/// <summary>How the video image is scaled into the bounds of the media surface.</summary>
public enum MediaAspect
{
    /// <summary>Scale to fit entirely inside the bounds, preserving aspect ratio. Letterboxes/pillarboxes.</summary>
    AspectFit,

    /// <summary>Scale to fill the bounds, preserving aspect ratio. Crops the overflowing edge.</summary>
    AspectFill,

    /// <summary>Stretch to fill the bounds exactly, ignoring aspect ratio.</summary>
    Fill
}
