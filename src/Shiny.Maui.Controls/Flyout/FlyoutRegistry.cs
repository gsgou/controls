using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Flyout;

/// <summary>
/// Every live <see cref="FlyoutView"/>, so a service (or a view model) can reach the one on the page
/// that is actually showing without the page having to hand it over.
/// </summary>
/// <remarks>
/// References are weak and pruned on read: a flyout belongs to a page, pages come and go with
/// navigation, and a registry that kept them alive would keep every page they are on alive too.
/// </remarks>
static class FlyoutRegistry
{
    static readonly List<WeakReference<FlyoutView>> views = new();

    public static event EventHandler<FlyoutStateChangedEventArgs>? StateChanged;

    public static void Register(FlyoutView view)
    {
        lock (views)
        {
            Prune();
            views.Add(new WeakReference<FlyoutView>(view));
        }
    }


    internal static void RaiseStateChanged(FlyoutView view, FlyoutStateChangedEventArgs args)
        => StateChanged?.Invoke(view, args);


    /// <summary>Newest first — the most recently constructed flyout is the one most likely on screen.</summary>
    public static IReadOnlyList<FlyoutView> Live()
    {
        lock (views)
        {
            Prune();
            var live = new List<FlyoutView>(views.Count);
            for (var i = views.Count - 1; i >= 0; i--)
            {
                if (views[i].TryGetTarget(out var view))
                    live.Add(view);
            }
            return live;
        }
    }


    /// <summary>
    /// The flyout to act on: the one on the page currently showing, falling back to the most recent
    /// live one for hosts where the page cannot be resolved.
    /// </summary>
    public static FlyoutView? Current()
    {
        var live = Live();
        if (live.Count == 0)
            return null;

        var page = PageOverlay.CurrentPage();
        if (page is not null)
        {
            foreach (var view in live)
            {
                if (ReferenceEquals(PageOverlay.FindPage(view), page))
                    return view;
            }
        }

        return live[0];
    }


    static void Prune() => views.RemoveAll(w => !w.TryGetTarget(out _));
}
