using CoreImage;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

// Maps the cross-platform CameraFilter enum to a configured Core Image filter (null = passthrough).
static class AppleCameraFilters
{
    public static CIFilter? Create(CameraFilter filter)
    {
        switch (filter)
        {
            case CameraFilter.Mono:
                return CIFilter.FromName("CIPhotoEffectMono");

            case CameraFilter.Noir:
                return CIFilter.FromName("CIPhotoEffectNoir");

            case CameraFilter.Invert:
                return CIFilter.FromName("CIColorInvert");

            case CameraFilter.Sepia:
            {
                var f = CIFilter.FromName("CISepiaTone");
                f?.SetValueForKey(NSNumber.FromDouble(1.0), new NSString("inputIntensity"));
                return f;
            }

            case CameraFilter.Vivid:
            {
                var f = CIFilter.FromName("CIColorControls");
                f?.SetValueForKey(NSNumber.FromDouble(1.5), new NSString("inputSaturation"));
                f?.SetValueForKey(NSNumber.FromDouble(1.1), new NSString("inputContrast"));
                return f;
            }

            case CameraFilter.Cool:
            {
                var f = CIFilter.FromName("CITemperatureAndTint");
                // neutral 6500K -> target a cooler 4800K
                f?.SetValueForKey(new CIVector(6500, 0), new NSString("inputNeutral"));
                f?.SetValueForKey(new CIVector(4800, 0), new NSString("inputTargetNeutral"));
                return f;
            }

            case CameraFilter.Warm:
            {
                var f = CIFilter.FromName("CITemperatureAndTint");
                f?.SetValueForKey(new CIVector(6500, 0), new NSString("inputNeutral"));
                f?.SetValueForKey(new CIVector(8500, 0), new NSString("inputTargetNeutral"));
                return f;
            }

            default:
                return null;
        }
    }
}
