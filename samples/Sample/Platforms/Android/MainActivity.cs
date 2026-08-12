using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Shiny.Maui.Controls.Media;

namespace Sample;

// SupportsPictureInPicture is what makes MediaElement.TryEnterPictureInPictureAsync work on Android:
// Activity.EnterPictureInPictureMode throws without it, and only the app can declare it. PictureInPicture
// is added to ConfigurationChanges so entering PiP resizes the activity instead of recreating it.
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    SupportsPictureInPicture = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Android reports PiP transitions only to the activity, so forward them or the control never learns
    // that the user collapsed the floating window.
    public override void OnPictureInPictureModeChanged(bool isInPictureInPictureMode, Configuration? newConfig)
    {
        base.OnPictureInPictureModeChanged(isInPictureInPictureMode, newConfig);
        AndroidMediaIntegration.NotifyPictureInPictureModeChanged(isInPictureInPictureMode);
    }
}
