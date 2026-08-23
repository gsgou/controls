using System.ComponentModel;
using Shiny.Maui.Controls.FloatingPanel;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Presents the quick entry popup as an overlay on the current page. The only presentation
/// available on iOS, Android and Mac Catalyst, and an option on desktop for a popup that should stay
/// inside the app rather than float over the whole machine.
/// </summary>
/// <remarks>
/// <para>
/// Built on the library's own <see cref="Overlay"/> control rather than a hand-rolled scrim. That
/// brings the backdrop, the optional blur, close-on-backdrop-tap and the show/hide worker that
/// already survives a rapid open→close flip — none of which is worth reimplementing, and all of
/// which is already proven on iOS and Android.
/// </para>
/// <para>
/// It also means the popup shares the page's <see cref="OverlayHost"/> backdrop with everything else
/// that uses one, so a quick entry opened over a floating panel dims the page once rather than
/// twice. When the page has no host — a plain <c>ContentPage</c> — one is installed into the shared
/// page overlay so the control still works with no cooperation from the app.
/// </para>
/// <para>
/// Unlike the desktop presenter this does not have to size a window to its content: the overlay is
/// laid out by the page, so the content sizes itself and <see cref="QuickEntryOptions.MaxHeight"/>
/// is a maximum-height request. That is why none of the measuring machinery
/// <see cref="IQuickEntryAutoSize"/> exists for is needed here.
/// </para>
/// </remarks>
sealed class InAppQuickEntryPresenter : IQuickEntryPresenter
{
    ContentPage? page;
    OverlayHost? host;
    Overlay? overlay;
    ContentView? contentHost;
    View? content;
    bool opened;

    public QuickEntryPresentation Kind => QuickEntryPresentation.InApp;

    /// <summary>Needs a <see cref="ContentPage"/> to draw on, which only exists once the app has a window.</summary>
    public bool IsSupported => PageOverlay.CurrentPage() != null;

    public Action? Deactivated { get; set; }

    public Func<QuickEntryKey, bool>? KeyPressed { get; set; }

    /// <summary>Never raised: the overlay is laid out by the page, so nothing here has to size itself.</summary>
    public Action<double>? ContentHeightChanged { get; set; }

    public Task PrepareAsync(QuickEntryOptions options, View content)
    {
        this.content = content;
        this.Attach(options);
        return Task.CompletedTask;
    }

    public void SetContent(View content)
    {
        this.content = content;
        if (this.contentHost != null)
            this.contentHost.Content = content;
    }

    public void Show(QuickEntryOptions options, double width, double height)
    {
        // Re-attach every open. The page underneath changes as the user navigates, and an overlay
        // pinned to whichever page happened to be showing the first time is the classic way one of
        // these silently stops appearing.
        this.Attach(options);
        if (this.overlay == null)
            return;

        this.ApplyLayout(options, width);
        this.opened = true;
        this.overlay.IsShown = true;
    }

    public void Hide()
    {
        this.opened = false;
        if (this.overlay != null)
            this.overlay.IsShown = false;
    }

    public void Resize(QuickEntryOptions options, double width, double height)
        => this.ApplyLayout(options, width);

    public void Teardown()
    {
        if (this.overlay != null)
        {
            this.overlay.PropertyChanged -= this.OnOverlayPropertyChanged;
            this.overlay.IsShown = false;
            this.host?.Children.Remove(this.overlay);
        }

        // Release the content so another presenter can take it — MAUI refuses to add a view that
        // still has a parent, and switching presentation hands this same view across.
        if (this.contentHost != null)
            this.contentHost.Content = null;

        this.overlay = null;
        this.contentHost = null;
        this.host = null;
        this.page = null;
        this.opened = false;
    }

    // -------------------------------------------------------------------------------------

    void Attach(QuickEntryOptions options)
    {
        var current = PageOverlay.CurrentPage();
        if (current == null)
            return;

        if (ReferenceEquals(current, this.page) && this.overlay != null)
            return;

        // Moving to a new page: let the old host go, and detach the content before it is re-parented.
        if (this.overlay != null)
        {
            this.overlay.PropertyChanged -= this.OnOverlayPropertyChanged;
            this.host?.Children.Remove(this.overlay);
        }
        if (this.contentHost != null)
            this.contentHost.Content = null;

        this.page = current;
        this.host = FindHost(current) ?? InstallHost(current);

        this.contentHost = new ContentView { Content = this.content };
        this.overlay = new Overlay
        {
            CloseOnBackdropTap = options.DismissOnScrimTap,
            OverlayContentTemplate = new DataTemplate(() => this.contentHost)
        };
        this.overlay.PropertyChanged += this.OnOverlayPropertyChanged;
        this.host.Children.Add(this.overlay);
        this.ApplyLayout(options, options.Width);
    }

    /// <summary>
    /// A backdrop tap sets <c>IsShown</c> false on the overlay itself, so watching the property is how
    /// the host hears about a dismissal it did not initiate.
    /// </summary>
    void OnOverlayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != Overlay.IsShownProperty.PropertyName)
            return;

        if (this.opened && this.overlay?.IsShown == false)
        {
            this.opened = false;
            this.Deactivated?.Invoke();
        }
    }

    static OverlayHost? FindHost(Element root)
    {
        if (root is OverlayHost host)
            return host;

        foreach (var child in root.LogicalChildren)
        {
            if (child is Element element && FindHost(element) is { } found)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Installs a host for a page that has none, in the shared page overlay so it layers correctly
    /// against tooltips, walkthroughs and dialogs rather than fighting them.
    /// </summary>
    static OverlayHost InstallHost(ContentPage page)
    {
        var layer = PageOverlay.GetOrCreateLayer<PageOverlay.QuickEntryLayer>(page, PageOverlay.Layers.QuickEntry);
        if (layer.Children.OfType<OverlayHost>().FirstOrDefault() is { } existing)
            return existing;

        var host = new OverlayHost();
        layer.Children.Add(host);
        return host;
    }

    /// <summary>
    /// Places the popup with alignment and a margin. Height is never requested: the overlay is laid
    /// out by the page, so the content sizes itself and <c>MaxHeight</c> is simply a ceiling.
    /// </summary>
    void ApplyLayout(QuickEntryOptions options, double width)
    {
        if (this.contentHost == null || this.overlay == null)
            return;

        this.contentHost.WidthRequest = width;
        this.contentHost.MaximumHeightRequest = options.MaxHeight;

        var available = this.host?.Height > 0 ? this.host.Height : 0;

        switch (options.Placement)
        {
            case QuickEntryPlacement.BottomCenter:
                this.overlay.ContentAlignment = LayoutOptions.End;
                this.overlay.ContentMargin = new Thickness(0, 0, 0, available * options.BottomMarginRatio);
                break;

            // A touch screen has no pointer to sit near, and on desktop the overlay is inside the
            // app rather than at the cursor, so NearCursor reads as centred in-app rather than as a
            // silently ignored setting.
            case QuickEntryPlacement.Center:
            case QuickEntryPlacement.NearCursor:
                this.overlay.ContentAlignment = LayoutOptions.Center;
                this.overlay.ContentMargin = new Thickness(0);
                break;

            case QuickEntryPlacement.Manual:
                this.overlay.ContentAlignment = LayoutOptions.Start;
                this.overlay.ContentMargin = new Thickness(options.X, options.Y, 0, 0);
                break;

            default:
                this.overlay.ContentAlignment = LayoutOptions.Start;
                this.overlay.ContentMargin = new Thickness(0, available * options.TopMarginRatio, 0, 0);
                break;
        }
    }
}
