namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// The single wrapper a page grows so controls can paint above its content — tooltips, walkthrough
/// scrims, dialogs.
/// </summary>
/// <remarks>
/// A page's <c>Content</c> is one view. To put anything on top of it, that view has to become a child
/// of a layout the extra layer can also live in. Every control that needs this used to wrap the page
/// itself, so two of them wrapped it twice and the second one's layer sat inside the first one's
/// grid — which is fine until they disagree about z-order. Wrapping is done once here, into a marked
/// <see cref="ShinyOverlayRoot"/>, and every layer after that is a sibling inside it.
/// </remarks>
static class PageOverlay
{
    /// <summary>Marks the grid this class installed, so it is reused rather than nested.</summary>
    internal sealed class ShinyOverlayRoot : Grid;

    /// <summary>
    /// Marker layers. Each control gets its own type because the layer is looked up by type — asking
    /// for a bare <see cref="AbsoluteLayout"/> would match any other control's layer that happened to
    /// be added first, and two controls would end up sharing (and clearing) each other's children.
    /// </summary>
    internal sealed class TooltipLayer : AbsoluteLayout;

    internal sealed class WalkthroughLayer : AbsoluteLayout;

    /// <summary>
    /// Z-order for the layers, so the intent is stated once rather than guessed at each call site.
    /// A tooltip sits above page content, a walkthrough dims everything including tooltips, and a
    /// modal dialog wins outright.
    /// </summary>
    public static class Layers
    {
        public const int Tooltip = 9_000;
        public const int Walkthrough = 9_500;
        public const int Dialog = 10_000;
    }


    /// <summary>
    /// Returns the page's overlay root, installing it on first use. Null when the element is not on a
    /// <see cref="ContentPage"/> — a control that is offscreen, mid-navigation, or hosted somewhere
    /// that has no page content to wrap at all.
    /// </summary>
    public static ShinyOverlayRoot? GetOrCreateRoot(Element anchor)
    {
        var page = FindPage(anchor);
        return page is null ? null : GetOrCreateRoot(page);
    }


    public static ShinyOverlayRoot GetOrCreateRoot(ContentPage page)
    {
        if (page.Content is ShinyOverlayRoot existing)
            return existing;

        var root = new ShinyOverlayRoot();
        if (page.Content is View content)
        {
            // Detach before re-parenting: MAUI throws if a view already has a parent.
            page.Content = null;
            root.Children.Add(content);
        }
        page.Content = root;
        return root;
    }


    /// <summary>
    /// Returns the page's layer of type <typeparamref name="T"/>, adding one at <paramref name="zIndex"/>
    /// on first use. The layer is input-transparent so it never eats a tap meant for the page; the
    /// children a control puts in it are not, so they still receive their own.
    /// </summary>
    public static T? GetOrCreateLayer<T>(Element anchor, int zIndex) where T : Layout, new()
    {
        var root = GetOrCreateRoot(anchor);
        return root is null ? null : GetOrCreateLayer<T>(root, zIndex);
    }


    public static T GetOrCreateLayer<T>(ShinyOverlayRoot root, int zIndex) where T : Layout, new()
    {
        if (root.Children.OfType<T>().FirstOrDefault() is { } existing)
            return existing;

        var layer = new T
        {
            InputTransparent = true,
            CascadeInputTransparent = false,
            ZIndex = zIndex
        };
        root.Children.Add(layer);
        return layer;
    }


    /// <summary>The <see cref="ContentPage"/> an element is sitting on, or null if it is not on one yet.</summary>
    public static ContentPage? FindPage(Element? element)
    {
        while (element is not null)
        {
            if (element is ContentPage page)
                return page;

            element = element.Parent;
        }
        return null;
    }


    /// <summary>
    /// The page currently on screen, for callers with no element to start from (a service, say).
    /// Walks the navigation containers down to the leaf that is actually showing.
    /// </summary>
    public static ContentPage? CurrentPage()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is null ? null : LeafPage(page);
    }


    public static ContentPage? LeafPage(Page page) => page switch
    {
        ContentPage cp => cp,
        NavigationPage np when np.CurrentPage is not null => LeafPage(np.CurrentPage),
        Shell shell when shell.CurrentPage is not null => LeafPage(shell.CurrentPage),
        TabbedPage tp when tp.CurrentPage is not null => LeafPage(tp.CurrentPage),
        FlyoutPage fp when fp.Detail is not null => LeafPage(fp.Detail),
        _ => null
    };
}
