using Foundation;
using UIKit;

namespace Shiny.Maui.Controls;

public partial class ShinyNavigationPage
{
    // UIKit refers to its delegates weakly, so the one handed to the recognizer has to be kept alive
    // here or it is collected and the gesture silently reverts to UIKit's own (disabled) behaviour.
    readonly List<NSObject> gestureDelegates = new();

    /// <summary>
    /// Puts iOS's edge-swipe pop back. <c>UINavigationController</c> disables
    /// <c>interactivePopGestureRecognizer</c> whenever its bar is hidden — which is exactly what this
    /// page does to make room for the drawn bar — so a stock <c>NavigationPage</c>'s swipe-back would
    /// otherwise be lost simply by adopting it.
    /// </summary>
    /// <remarks>
    /// The recognizer's delegate is replaced rather than nulled. Nulling it is the widely-copied
    /// trick and it does re-enable the gesture, but it also lets a swipe start on the root page,
    /// where UIKit pops nothing and leaves the controller unable to respond to touches at all. The
    /// replacement answers the one question the default delegate exists to answer — is there anything
    /// to pop — and nothing else.
    /// </remarks>
    partial void ApplySwipeBack()
    {
        if (!this.EnableSwipeBackGesture)
            return;

        if (this.Handler?.PlatformView is not UIResponder responder)
            return;

        var controller = FindNavigationController(responder);
        if (controller?.InteractivePopGestureRecognizer is not { } recognizer)
            return;

        if (recognizer.Delegate is PopGestureDelegate)
        {
            recognizer.Enabled = true;
            return;
        }

        var behaviour = new PopGestureDelegate(controller);
        this.gestureDelegates.Add(behaviour);
        recognizer.Delegate = behaviour;
        recognizer.Enabled = true;
    }


    static UINavigationController? FindNavigationController(UIResponder? responder)
    {
        // Walked rather than cast: which type MAUI's iOS navigation handler exposes as its platform
        // view has changed across releases, and the responder chain has not.
        while (responder is not null)
        {
            if (responder is UINavigationController controller)
                return controller;

            responder = responder.NextResponder;
        }
        return null;
    }


    sealed class PopGestureDelegate(UINavigationController controller) : UIGestureRecognizerDelegate
    {
        public override bool ShouldBegin(UIGestureRecognizer recognizer)
            => controller.ViewControllers is { Length: > 1 };

        /// <summary>
        /// Never alongside another gesture. Letting the edge swipe run with a scroll view's pan is how
        /// a horizontal flick inside a carousel ends up popping the page.
        /// </summary>
        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer recognizer, UIGestureRecognizer other)
            => false;
    }
}
