using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

/// <summary>
/// The thin determinate/indeterminate line that runs across the top or bottom of a page while
/// something is loading.
/// </summary>
/// <remarks>
/// <para>
/// Distinct from <see cref="ProgressBar"/>, which is an inline view that fills a slot you gave it in
/// a layout. This one is chrome: it has no slot, it moves itself onto the page edge, and it knows
/// about the navigation bar, the tab bar and the safe area so it lands against them rather than
/// under them. The drawing is <see cref="ProgressBar"/>'s — the gradient, the shimmer sweep, the
/// animated fill and the platform paint fixes are all shared rather than reimplemented.
/// </para>
/// <para>
/// It can be driven two ways: declared in markup with <see cref="Value"/> bound, or created for you
/// by <see cref="IProgressLineService"/> when the thing you are reporting on is a code path rather
/// than a view-model property.
/// </para>
/// </remarks>
public partial class ProgressLine : ContentView, IDisposable
{
    const string FadeAnimationName = "ShinyProgressLineFade";

    readonly ProgressBar bar;

    ContentPage? subscribedPage;
    Element? watchedAncestor;
    bool dockPending;
    bool disposed;

    public ProgressLine()
    {
        this.bar = new ProgressBar
        {
            ShowText = false,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };

        // Bound rather than pushed by a propertyChanged handler on each of the fifteen passthroughs:
        // one source of truth, and a property added to ProgressBar later cannot silently go unwired
        // on this side.
        Forward(ProgressBar.ValueProperty, nameof(this.Value));
        Forward(ProgressBar.MinimumProperty, nameof(this.Minimum));
        Forward(ProgressBar.MaximumProperty, nameof(this.Maximum));
        Forward(ProgressBar.IsIndeterminateProperty, nameof(this.IsIndeterminate));
        Forward(ProgressBar.BarColorProperty, nameof(this.BarColor));
        Forward(ProgressBar.TrackColorProperty, nameof(this.TrackColor));
        Forward(ProgressBar.TrackHeightProperty, nameof(this.LineHeight));
        Forward(ProgressBar.CornerRadiusProperty, nameof(this.CornerRadius));
        Forward(ProgressBar.UseGradientProperty, nameof(this.UseGradient));
        Forward(ProgressBar.AnimateProgressProperty, nameof(this.AnimateProgress));
        Forward(ProgressBar.ProgressAnimationDurationProperty, nameof(this.ProgressAnimationDuration));
        Forward(ProgressBar.ProgressAnimationEasingProperty, nameof(this.ProgressAnimationEasing));
        Forward(ProgressBar.PulseEnabledProperty, nameof(this.PulseEnabled));
        Forward(ProgressBar.PulseColorProperty, nameof(this.PulseColor));
        Forward(ProgressBar.PulseLengthProperty, nameof(this.PulseLength));
        Forward(ProgressBar.PulseSpeedProperty, nameof(this.PulseSpeed));

        this.Content = this.bar;
        this.HorizontalOptions = LayoutOptions.Fill;

        // The line reports, it is never the thing being touched — and it spans the full width, so
        // leaving it hit-testable would swallow taps along a whole edge of the page.
        this.InputTransparent = true;

        this.ApplyGradientColors();

        StyleGuard.MarkReady(this, typeof(ProgressLine));

        void Forward(BindableProperty target, string source)
            => this.bar.SetBinding(target, new Binding(source, source: this));
    }


    /// <summary>The inner bar, for the styling <see cref="ProgressLine"/> does not surface.</summary>
    public ProgressBar Bar => this.bar;


    protected override void OnParentSet()
    {
        base.OnParentSet();

        this.Unsubscribe();

        if (this.Parent is null)
            return;

        if (!this.Dock || this.Parent is PageOverlay.ProgressLineLayer)
        {
            this.RefreshLayout();
            return;
        }

        this.ScheduleDock();
    }


    /// <summary>
    /// Docking is deferred a tick because XAML sets properties in document order: at the moment the
    /// line's parent is set, the page around it is often still being built and has no content to
    /// wrap into an overlay root.
    /// </summary>
    internal void ScheduleDock()
    {
        if (this.dockPending)
            return;

        this.dockPending = true;
        this.Dispatcher.Dispatch(() =>
        {
            this.dockPending = false;
            this.TryDock();
        });
    }


    void TryDock()
    {
        if (this.disposed || !this.Dock || this.Parent is PageOverlay.ProgressLineLayer)
            return;

        var page = PageOverlay.FindPage(this);
        if (page is null)
        {
            this.WatchForPage();
            return;
        }

        this.UnwatchAncestor();

        // Before reading Parent: creating the root re-parents the page's content, and this line is
        // currently somewhere inside it.
        var layer = PageOverlay.GetOrCreateLayer<PageOverlay.ProgressLineLayer>(page, PageOverlay.Layers.ProgressLine);

        if (!this.DetachFromParent())
            return;

        layer.Children.Add(this);
        this.RefreshLayout();

        // Second pass once the page has laid out: the nav/tab bar's measured height is what the inset
        // is taken from, and on the first pass it is still zero.
        this.Dispatcher.Dispatch(this.RefreshLayout);
    }


    /// <summary>
    /// Waits for the page to appear above the line, by watching the top of the chain it is currently
    /// in for a parent of its own.
    /// </summary>
    /// <remarks>
    /// <see cref="OnParentSet"/> alone is not enough, and the reason is the shape of every XAML page:
    /// the line is constructed and added to its layout first, and only then is that layout handed to
    /// the page. The line's own parent never changes at that second step, so nothing re-fires — the
    /// line sits inline forever and the control looks like it simply does not work.
    /// </remarks>
    void WatchForPage()
    {
        Element root = this;
        while (root.Parent is not null)
            root = root.Parent;

        if (ReferenceEquals(root, this.watchedAncestor))
            return;

        this.UnwatchAncestor();
        this.watchedAncestor = root;
        root.ParentChanged += this.OnAncestorParentChanged;
    }


    void OnAncestorParentChanged(object? sender, EventArgs e)
    {
        this.UnwatchAncestor();
        this.ScheduleDock();
    }


    void UnwatchAncestor()
    {
        if (this.watchedAncestor is null)
            return;

        this.watchedAncestor.ParentChanged -= this.OnAncestorParentChanged;
        this.watchedAncestor = null;
    }


    /// <summary>
    /// Removes the line from whatever it was declared in. False when the parent is not something a
    /// child can be pulled out of — a templated item, chiefly — in which case the line stays inline
    /// rather than throwing.
    /// </summary>
    bool DetachFromParent()
    {
        switch (this.Parent)
        {
            case Layout layout:
                return layout.Children.Remove(this);

            case ContentView view when ReferenceEquals(view.Content, this):
                view.Content = null;
                return true;

            case ScrollView scroll when ReferenceEquals(scroll.Content, this):
                scroll.Content = null;
                return true;

            case ContentPage page when ReferenceEquals(page.Content, this):
                page.Content = null;
                return true;

            default:
                return false;
        }
    }


    /// <summary>
    /// Re-resolves the edge, the inset and the fade state. Called automatically on docking, on a
    /// placement property change and when the page resizes; call it directly after changing the
    /// height of a bar the line is sitting against.
    /// </summary>
    public void RefreshLayout()
    {
        if (this.disposed)
            return;

        var page = PageOverlay.FindPage(this);
        this.Subscribe(page);

        var top = this.Position == ProgressLinePosition.Top;
        this.VerticalOptions = top ? LayoutOptions.Start : LayoutOptions.End;

        var inset = this.AutoInset && page is not null
            ? ProgressLineInsets.Resolve(page, this.OverlayRoot(), this.Position)
            : 0;

        this.Margin = top
            ? new Thickness(this.Offset.Left, inset + this.Offset.Top, this.Offset.Right, 0)
            : new Thickness(this.Offset.Left, 0, this.Offset.Right, inset + this.Offset.Bottom);

        this.HeightRequest = this.LineHeight;
    }


    /// <summary>
    /// The overlay root the line sits in — the coordinate space its margin is measured against, and
    /// the thing the inset rule is expressed in terms of. Null when the line is inline.
    /// </summary>
    Element? OverlayRoot()
    {
        for (Element? element = this; element is not null; element = element.Parent)
        {
            if (element is PageOverlay.ShinyOverlayRoot root)
                return root;
        }
        return null;
    }


    void Subscribe(ContentPage? page)
    {
        if (ReferenceEquals(page, this.subscribedPage))
            return;

        this.Unsubscribe();

        if (page is null)
            return;

        this.subscribedPage = page;
        page.SizeChanged += this.OnPageSizeChanged;
    }


    void Unsubscribe()
    {
        if (this.subscribedPage is null)
            return;

        this.subscribedPage.SizeChanged -= this.OnPageSizeChanged;
        this.subscribedPage = null;
    }


    // A rotation or a window resize changes the safe area and can change the chrome's height with it.
    void OnPageSizeChanged(object? sender, EventArgs e) => this.RefreshLayout();


    void OnActiveChanged(bool active)
    {
        this.AbortAnimation(FadeAnimationName);

        if (this.FadeDuration <= 0)
        {
            this.Opacity = active ? 1 : 0;
            this.IsVisible = active;
            return;
        }

        if (active)
            this.IsVisible = true;

        new Animation(v => this.Opacity = v, this.Opacity, active ? 1 : 0)
            .Commit(
                this,
                FadeAnimationName,
                length: (uint)this.FadeDuration,
                finished: (_, cancelled) =>
                {
                    if (!cancelled && !active)
                        this.IsVisible = false;
                }
            );
    }


    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.Unsubscribe();
        this.UnwatchAncestor();
        this.AbortAnimation(FadeAnimationName);
        this.bar.Dispose();
        GC.SuppressFinalize(this);
    }
}
