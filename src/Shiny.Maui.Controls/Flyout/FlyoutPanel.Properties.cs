using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Flyout;

public partial class FlyoutPanel
{
    /// <summary>Raised once the panel has settled into a new state.</summary>
    public event EventHandler<FlyoutStateChangedEventArgs>? StateChanged;

    internal void RaiseStateChanged(FlyoutStateChangedEventArgs args)
    {
        if (this.UseFeedback)
            FeedbackHelper.Execute(this, nameof(StateChanged), args.NewState);

        this.StateChanged?.Invoke(this, args);
    }


    #region content

    public static readonly BindableProperty PanelContentProperty = BindableProperty.Create(
        nameof(PanelContent),
        typeof(View),
        typeof(FlyoutPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.contentHost.Content = (View?)n;
            panel.ApplyStateVisuals(panel.State);
        }));

    /// <summary>The expanded body. Scrolls on its own unless <see cref="IsContentScrollEnabled"/> is false.</summary>
    public View? PanelContent
    {
        get => (View?)this.GetValue(PanelContentProperty);
        set => this.SetValue(PanelContentProperty, value);
    }

    public static readonly BindableProperty RailContentProperty = BindableProperty.Create(
        nameof(RailContent),
        typeof(View),
        typeof(FlyoutPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.railHost.Content = (View?)n;
            panel.ApplyStateVisuals(panel.State);
        }));

    /// <summary>
    /// What the rail shows in <see cref="FlyoutPanelState.Collapsed"/>. Anything at all — it is not
    /// restricted to icons. Leave it unset and the collapsed panel simply shows the leading edge of
    /// <see cref="PanelContent"/> instead.
    /// </summary>
    public View? RailContent
    {
        get => (View?)this.GetValue(RailContentProperty);
        set => this.SetValue(RailContentProperty, value);
    }

    public static readonly BindableProperty HeaderContentProperty = BindableProperty.Create(
        nameof(HeaderContent),
        typeof(View),
        typeof(FlyoutPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.headerHost.Content = (View?)n;
            panel.ApplyStateVisuals(panel.State);
        }));

    /// <summary>Pinned above the body — it does not scroll with it.</summary>
    public View? HeaderContent
    {
        get => (View?)this.GetValue(HeaderContentProperty);
        set => this.SetValue(HeaderContentProperty, value);
    }

    public static readonly BindableProperty FooterContentProperty = BindableProperty.Create(
        nameof(FooterContent),
        typeof(View),
        typeof(FlyoutPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.footerHost.Content = (View?)n;
            panel.ApplyStateVisuals(panel.State);
        }));

    /// <summary>Pinned below the body — it does not scroll with it.</summary>
    public View? FooterContent
    {
        get => (View?)this.GetValue(FooterContentProperty);
        set => this.SetValue(FooterContentProperty, value);
    }

    public static readonly BindableProperty IsContentScrollEnabledProperty = BindableProperty.Create(
        nameof(IsContentScrollEnabled),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.UpdateScrollHost();
            panel.ApplyStateVisuals(panel.State);
        }));

    public bool IsContentScrollEnabled
    {
        get => (bool)this.GetValue(IsContentScrollEnabledProperty);
        set => this.SetValue(IsContentScrollEnabledProperty, value);
    }

    public static readonly BindableProperty ShowHeaderWhenCollapsedProperty = BindableProperty.Create(
        nameof(ShowHeaderWhenCollapsed),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.ApplyStateVisuals(panel.State);
        }));

    public bool ShowHeaderWhenCollapsed
    {
        get => (bool)this.GetValue(ShowHeaderWhenCollapsedProperty);
        set => this.SetValue(ShowHeaderWhenCollapsedProperty, value);
    }

    public static readonly BindableProperty ShowFooterWhenCollapsedProperty = BindableProperty.Create(
        nameof(ShowFooterWhenCollapsed),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.ApplyStateVisuals(panel.State);
        }));

    public bool ShowFooterWhenCollapsed
    {
        get => (bool)this.GetValue(ShowFooterWhenCollapsedProperty);
        set => this.SetValue(ShowFooterWhenCollapsedProperty, value);
    }

    #endregion


    #region state and geometry

    public static readonly BindableProperty SideProperty = BindableProperty.Create(
        nameof(Side),
        typeof(FlyoutSide),
        typeof(FlyoutPanel),
        FlyoutSide.Start,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.ApplySideLayout();
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>
    /// Which edge the panel belongs to. Assigning the panel to <see cref="FlyoutView.Start"/> or
    /// <see cref="FlyoutView.End"/> sets this for you; setting it by hand only matters for a panel
    /// built before it is handed to a host.
    /// </summary>
    public FlyoutSide Side
    {
        get => (FlyoutSide)this.GetValue(SideProperty);
        set => this.SetValue(SideProperty, value);
    }

    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State),
        typeof(FlyoutPanelState),
        typeof(FlyoutPanel),
        FlyoutPanelState.Expanded,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.ApplyStateVisuals((FlyoutPanelState)n);
            panel.Host?.OnPanelStateChanged(panel, (FlyoutPanelState)o, (FlyoutPanelState)n);
        }));

    /// <summary>Hidden, the rail, or the full panel. Two-way — the panel writes back when it settles.</summary>
    public FlyoutPanelState State
    {
        get => (FlyoutPanelState)this.GetValue(StateProperty);
        set => this.SetValue(StateProperty, value);
    }

    public static readonly BindableProperty CollapsedStateProperty = BindableProperty.Create(
        nameof(CollapsedState),
        typeof(FlyoutPanelState),
        typeof(FlyoutPanel),
        FlyoutPanelState.Collapsed);

    /// <summary>
    /// Where an expanded panel goes when it is dismissed — by <see cref="ToggleAsync"/>, a scrim tap,
    /// a swipe, or <see cref="CollapseBelow"/>. Set it to <see cref="FlyoutPanelState.Hidden"/> for a
    /// drawer that leaves nothing behind.
    /// </summary>
    public FlyoutPanelState CollapsedState
    {
        get => (FlyoutPanelState)this.GetValue(CollapsedStateProperty);
        set => this.SetValue(CollapsedStateProperty, value);
    }

    public static readonly BindableProperty ExpandedWidthProperty = BindableProperty.Create(
        nameof(ExpandedWidth),
        typeof(double),
        typeof(FlyoutPanel),
        DefaultExpandedWidth,
        BindingMode.TwoWay,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>Two-way: a resize drag writes the new width back here.</summary>
    public double ExpandedWidth
    {
        get => (double)this.GetValue(ExpandedWidthProperty);
        set => this.SetValue(ExpandedWidthProperty, value);
    }

    public static readonly BindableProperty CollapsedWidthProperty = BindableProperty.Create(
        nameof(CollapsedWidth),
        typeof(double),
        typeof(FlyoutPanel),
        DefaultCollapsedWidth,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>The rail's width.</summary>
    public double CollapsedWidth
    {
        get => (double)this.GetValue(CollapsedWidthProperty);
        set => this.SetValue(CollapsedWidthProperty, value);
    }

    public static readonly BindableProperty MinExpandedWidthProperty = BindableProperty.Create(
        nameof(MinExpandedWidth),
        typeof(double),
        typeof(FlyoutPanel),
        160d);

    /// <summary>Lower clamp for a resize drag.</summary>
    public double MinExpandedWidth
    {
        get => (double)this.GetValue(MinExpandedWidthProperty);
        set => this.SetValue(MinExpandedWidthProperty, value);
    }

    public static readonly BindableProperty MaxExpandedWidthProperty = BindableProperty.Create(
        nameof(MaxExpandedWidth),
        typeof(double),
        typeof(FlyoutPanel),
        480d);

    /// <summary>Upper clamp for a resize drag.</summary>
    public double MaxExpandedWidth
    {
        get => (double)this.GetValue(MaxExpandedWidthProperty);
        set => this.SetValue(MaxExpandedWidthProperty, value);
    }

    public static readonly BindableProperty IsResizableProperty = BindableProperty.Create(
        nameof(IsResizable),
        typeof(bool),
        typeof(FlyoutPanel),
        false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>
    /// A drag handle on the panel's inner edge that resizes <see cref="ExpandedWidth"/>. Only while
    /// the panel is expanded and pushing — a floating panel's inner edge is a drag-to-close gesture
    /// instead, and the two cannot share one strip.
    /// </summary>
    public bool IsResizable
    {
        get => (bool)this.GetValue(IsResizableProperty);
        set => this.SetValue(IsResizableProperty, value);
    }

    #endregion


    #region presentation

    public static readonly BindableProperty PresentationProperty = BindableProperty.Create(
        nameof(Presentation),
        typeof(FlyoutPresentation),
        typeof(FlyoutPanel),
        FlyoutPresentation.Auto,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: true);
        }));

    /// <summary>Overlay, push, or <see cref="FlyoutPresentation.Auto"/> — width decides.</summary>
    public FlyoutPresentation Presentation
    {
        get => (FlyoutPresentation)this.GetValue(PresentationProperty);
        set => this.SetValue(PresentationProperty, value);
    }

    public static readonly BindableProperty CompactWidthProperty = BindableProperty.Create(
        nameof(CompactWidth),
        typeof(double),
        typeof(FlyoutPanel),
        DefaultCompactWidth,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: true);
        }));

    /// <summary>Host width at or above which <see cref="FlyoutPresentation.Auto"/> pushes instead of floating.</summary>
    public double CompactWidth
    {
        get => (double)this.GetValue(CompactWidthProperty);
        set => this.SetValue(CompactWidthProperty, value);
    }

    public static readonly BindableProperty CollapseBelowProperty = BindableProperty.Create(
        nameof(CollapseBelow),
        typeof(double),
        typeof(FlyoutPanel),
        0d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: true);
        }));

    /// <summary>
    /// Host width under which an expanded panel drops to <see cref="CollapsedState"/>, restoring what
    /// it was when the host grows back. 0 turns it off. This is a response to the viewport, not a
    /// preference — it is deliberately not remembered anywhere.
    /// </summary>
    public double CollapseBelow
    {
        get => (double)this.GetValue(CollapseBelowProperty);
        set => this.SetValue(CollapseBelowProperty, value);
    }

    public static readonly BindableProperty HasScrimProperty = BindableProperty.Create(
        nameof(HasScrim),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>Dim the content behind a floating panel. Ignored while pushing — nothing is behind it.</summary>
    public bool HasScrim
    {
        get => (bool)this.GetValue(HasScrimProperty);
        set => this.SetValue(HasScrimProperty, value);
    }

    public static readonly BindableProperty CloseOnScrimTapProperty = BindableProperty.Create(
        nameof(CloseOnScrimTap),
        typeof(bool),
        typeof(FlyoutPanel),
        true);

    /// <summary>A tap outside a floating panel returns it to <see cref="CollapsedState"/>.</summary>
    public bool CloseOnScrimTap
    {
        get => (bool)this.GetValue(CloseOnScrimTapProperty);
        set => this.SetValue(CloseOnScrimTapProperty, value);
    }

    public static readonly BindableProperty IsSwipeEnabledProperty = BindableProperty.Create(
        nameof(IsSwipeEnabled),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>Swipe in from the edge to open, and drag the scrim to close. Floating panels only.</summary>
    public bool IsSwipeEnabled
    {
        get => (bool)this.GetValue(IsSwipeEnabledProperty);
        set => this.SetValue(IsSwipeEnabledProperty, value);
    }

    public static readonly BindableProperty EdgeSwipeWidthProperty = BindableProperty.Create(
        nameof(EdgeSwipeWidth),
        typeof(double),
        typeof(FlyoutPanel),
        20d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            panel.Host?.OnPanelInvalidated(panel, animate: false);
        }));

    /// <summary>
    /// How wide the edge-swipe catch strip is. The strip sits over the content, so taps landing in
    /// that band go to it rather than to what is under it — set it to 0 for content that needs the
    /// very edge.
    /// </summary>
    public double EdgeSwipeWidth
    {
        get => (double)this.GetValue(EdgeSwipeWidthProperty);
        set => this.SetValue(EdgeSwipeWidthProperty, value);
    }

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration),
        typeof(double),
        typeof(FlyoutPanel),
        DefaultAnimationDuration);

    /// <summary>Milliseconds for a state transition. 0 snaps.</summary>
    public double AnimationDuration
    {
        get => (double)this.GetValue(AnimationDurationProperty);
        set => this.SetValue(AnimationDurationProperty, value);
    }

    public static readonly BindableProperty UseFeedbackProperty = BindableProperty.Create(
        nameof(UseFeedback),
        typeof(bool),
        typeof(FlyoutPanel),
        true);

    /// <summary>Route state changes through the app's <c>IFeedbackService</c> (haptics, sound).</summary>
    public bool UseFeedback
    {
        get => (bool)this.GetValue(UseFeedbackProperty);
        set => this.SetValue(UseFeedbackProperty, value);
    }

    #endregion


    #region appearance

    public static readonly BindableProperty PanelBackgroundColorProperty = BindableProperty.Create(
        nameof(PanelBackgroundColor),
        typeof(Color),
        typeof(FlyoutPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            Tint(panel.surface, VisualElement.BackgroundColorProperty, (Color?)n, ShinyThemeKeys.Color.SurfaceContainerLow);
        }));

    /// <summary>Leave unset to follow the active theme.</summary>
    public Color? PanelBackgroundColor
    {
        get => (Color?)this.GetValue(PanelBackgroundColorProperty);
        set => this.SetValue(PanelBackgroundColorProperty, value);
    }

    public static readonly BindableProperty DividerColorProperty = BindableProperty.Create(
        nameof(DividerColor),
        typeof(Color),
        typeof(FlyoutPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            var panel = (FlyoutPanel)b;
            Tint(panel.divider, BoxView.ColorProperty, (Color?)n, ShinyThemeKeys.Color.OutlineVariant);
        }));

    /// <summary>Leave unset to follow the active theme.</summary>
    public Color? DividerColor
    {
        get => (Color?)this.GetValue(DividerColorProperty);
        set => this.SetValue(DividerColorProperty, value);
    }

    public static readonly BindableProperty DividerWidthProperty = BindableProperty.Create(
        nameof(DividerWidth),
        typeof(double),
        typeof(FlyoutPanel),
        1d,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            ((FlyoutPanel)b).divider.WidthRequest = (double)n;
        }));

    public double DividerWidth
    {
        get => (double)this.GetValue(DividerWidthProperty);
        set => this.SetValue(DividerWidthProperty, value);
    }

    public static readonly BindableProperty HasDividerProperty = BindableProperty.Create(
        nameof(HasDivider),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            ((FlyoutPanel)b).ApplyEdgeTreatment();
        }));

    /// <summary>The hairline on the edge facing the content. A floating panel drops it for a shadow.</summary>
    public bool HasDivider
    {
        get => (bool)this.GetValue(HasDividerProperty);
        set => this.SetValue(HasDividerProperty, value);
    }

    public static readonly BindableProperty HasShadowProperty = BindableProperty.Create(
        nameof(HasShadow),
        typeof(bool),
        typeof(FlyoutPanel),
        true,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            ((FlyoutPanel)b).ApplyEdgeTreatment();
        }));

    /// <summary>Drop a shadow while floating over the content.</summary>
    public bool HasShadow
    {
        get => (bool)this.GetValue(HasShadowProperty);
        set => this.SetValue(HasShadowProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius),
        typeof(double),
        typeof(FlyoutPanel),
        0d,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutPanel), () =>
        {
            ((FlyoutPanel)b).ApplyCornerRadius();
        }));

    /// <summary>Rounds the two corners on the panel's inner edge; the outer edge stays square.</summary>
    public double CornerRadius
    {
        get => (double)this.GetValue(CornerRadiusProperty);
        set => this.SetValue(CornerRadiusProperty, value);
    }

    #endregion
}
