using Shiny.Controls.Camera;

namespace Shiny.Blazor.Controls.Camera;

// Maps the cross-platform CameraFilter enum to a CSS `filter` string applied to the <video> element.
// Temperature filters (Cool/Warm) are approximated — CSS has no true white-balance control.
static class BlazorCameraFilters
{
    public static string ToCss(CameraFilter filter) => filter switch
    {
        CameraFilter.Mono => "grayscale(1)",
        CameraFilter.Noir => "grayscale(1) contrast(1.4) brightness(0.9)",
        CameraFilter.Sepia => "sepia(1)",
        CameraFilter.Invert => "invert(1)",
        CameraFilter.Vivid => "saturate(1.6) contrast(1.1)",
        CameraFilter.Cool => "saturate(1.15) hue-rotate(-12deg) brightness(1.03)",
        CameraFilter.Warm => "sepia(0.3) saturate(1.4) hue-rotate(-10deg)",
        _ => "none"
    };
}
