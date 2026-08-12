namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The modal page a <see cref="MediaElement"/> pushes for fullscreen. It carries a second
/// <see cref="MediaElement"/> sharing the owner's <see cref="IMediaPlayerBackend"/>, so entering and
/// leaving fullscreen re-points the video output instead of re-opening the stream.
/// </summary>
/// <remarks>
/// A modal page rather than reparenting the control: MAUI recreates a view's platform view when it moves
/// in the tree, which for a video surface means a visible stall and a fresh buffer on a remote stream.
/// It also avoids having to lift the element out of whatever layout the consumer put it in and put it
/// back exactly right afterwards.
/// </remarks>
class MediaFullScreenPage : ContentPage
{
    readonly MediaElement owner;

    public MediaFullScreenPage(MediaElement owner)
    {
        this.owner = owner;
        this.BackgroundColor = Colors.Black;
        this.Padding = 0;

        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
        NavigationPage.SetHasNavigationBar(this, false);

        this.Content = new MediaElement(owner);
    }

    // Android's hardware/gesture back should collapse fullscreen, not pop the page out from under the
    // owner — routing it through IsFullScreen keeps the property, the event, and the page in step.
    protected override bool OnBackButtonPressed()
    {
        this.owner.IsFullScreen = false;
        return true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.owner.OnFullScreenPageDismissed();
    }
}
