using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Flyout;

/// <summary>
/// One side of a <see cref="FlyoutView"/> — a panel that slides in from the start or end edge and
/// can rest in three states: hidden, a narrow rail (<see cref="RailContent"/>), or fully expanded.
/// </summary>
/// <remarks>
/// The panel owns what it looks like; the <see cref="FlyoutView"/> it is assigned to owns where it
/// is and how wide it currently is. Setting <see cref="State"/> from anywhere — markup, a binding,
/// code — is what drives the transition; the host animates to match.
/// </remarks>
[ContentProperty(nameof(PanelContent))]
public partial class FlyoutPanel : ContentView
{
    internal const double DefaultExpandedWidth = 280;
    internal const double DefaultCollapsedWidth = 64;
    internal const double DefaultAnimationDuration = 250;
    internal const double DefaultCompactWidth = 800;

    readonly Grid rootGrid;
    readonly Border surface;
    readonly BoxView divider;
    readonly Grid innerGrid;
    readonly Grid bodyGrid;
    readonly ContentView headerHost;
    readonly ContentView footerHost;
    readonly ContentView contentHost;
    readonly ContentView railHost;
    readonly ScrollView contentScroll;
    readonly Shadow surfaceShadow;

    FlyoutPresentation effectivePresentation = FlyoutPresentation.Overlay;
    bool isFloating;

    public FlyoutPanel()
    {
        // The panel is squeezed to the rail width mid-transition; without clipping, the expanded
        // content paints past the edge and smears over whatever is beside it.
        this.IsClippedToBounds = true;

        this.headerHost = new ContentView { IsVisible = false };
        this.footerHost = new ContentView { IsVisible = false };
        this.contentHost = new ContentView();
        this.railHost = new ContentView { IsVisible = false };
        this.contentScroll = new ScrollView { Content = this.contentHost };

        // Rail and expanded content are siblings rather than one host whose child is swapped:
        // re-parenting a view rebuilds its native tree, and this swap happens on every toggle.
        this.bodyGrid = new Grid();
        this.bodyGrid.Children.Add(this.contentScroll);
        this.bodyGrid.Children.Add(this.railHost);

        this.innerGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };
        this.innerGrid.Children.Add(this.headerHost);
        this.innerGrid.Children.Add(this.bodyGrid);
        this.innerGrid.Children.Add(this.footerHost);
        Grid.SetRow(this.headerHost, 0);
        Grid.SetRow(this.bodyGrid, 1);
        Grid.SetRow(this.footerHost, 2);

        // One Shadow instance, created here and toggled by opacity. Assigning a fresh Shadow later
        // re-creates the native layer, which on Android drops focus out of whatever is inside it.
        this.surfaceShadow = new Shadow
        {
            Brush = Brush.Black,
            Opacity = 0f,
            Radius = 16,
            Offset = new Point(0, 0)
        };

        this.surface = new Border
        {
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = 0,
            Content = this.innerGrid,
            Shadow = this.surfaceShadow,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 }
        };

        this.divider = new BoxView { WidthRequest = 1 };

        this.rootGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        this.rootGrid.Children.Add(this.surface);
        this.rootGrid.Children.Add(this.divider);

        Tint(this.surface, VisualElement.BackgroundColorProperty, null, ShinyThemeKeys.Color.SurfaceContainerLow);
        Tint(this.divider, BoxView.ColorProperty, null, ShinyThemeKeys.Color.OutlineVariant);

        base.Content = this.rootGrid;
        this.ApplySideLayout();

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(FlyoutPanel));
    }


    /// <summary>The host this panel is assigned to, or null while it is unparented.</summary>
    public FlyoutView? Host { get; internal set; }

    /// <summary>
    /// What <see cref="FlyoutPresentation.Auto"/> currently resolves to, or the explicit
    /// <see cref="Presentation"/> when one is set. Only meaningful once the host has been measured.
    /// </summary>
    public FlyoutPresentation EffectivePresentation => this.effectivePresentation;

    /// <summary>The panel's width right now, mid-animation included.</summary>
    public double CurrentWidth => this.Host?.GetCurrentWidth(this.Side) ?? 0;

    /// <summary>
    /// Hides the base <see cref="ContentView.Content"/> — the panel builds its own tree, so use
    /// <see cref="PanelContent"/> for the body.
    /// </summary>
    public new View? Content
    {
        get => this.PanelContent;
        set => this.PanelContent = value;
    }

    /// <summary>Expands the panel, or returns it to <see cref="CollapsedState"/> if it is already expanded.</summary>
    public Task ToggleAsync()
        => this.SetStateAsync(this.State == FlyoutPanelState.Expanded ? this.CollapsedState : FlyoutPanelState.Expanded);

    public Task ExpandAsync() => this.SetStateAsync(FlyoutPanelState.Expanded);

    public Task CollapseAsync() => this.SetStateAsync(FlyoutPanelState.Collapsed);

    public Task HideAsync() => this.SetStateAsync(FlyoutPanelState.Hidden);

    /// <summary>
    /// Moves to <paramref name="state"/> and completes when the transition has finished. Without a
    /// host the state is still recorded, so a panel configured before it is shown is not lost.
    /// </summary>
    public Task SetStateAsync(FlyoutPanelState state)
    {
        this.State = state;
        return this.Host?.WaitForTransitionAsync(this.Side) ?? Task.CompletedTask;
    }


    #region internals driven by the host

    /// <summary>Where the panel rests in a given state, before any animation.</summary>
    internal double RestingWidth(FlyoutPanelState state) => state switch
    {
        FlyoutPanelState.Expanded => Math.Max(0, this.ExpandedWidth),
        FlyoutPanelState.Collapsed => Math.Max(0, this.CollapsedWidth),
        _ => 0
    };

    /// <summary>
    /// Told by the host what <see cref="FlyoutPresentation.Auto"/> came out as, so the panel can
    /// dress itself for it: a floating panel casts a shadow and drops the divider it would otherwise
    /// share with the content beside it.
    /// </summary>
    internal void ApplyEffectivePresentation(FlyoutPresentation presentation, bool floating)
    {
        this.effectivePresentation = presentation;
        this.isFloating = floating;
        this.ApplyEdgeTreatment();
    }


    /// <summary>
    /// The divider and the shadow are the same decision seen from two sides: a panel that is inset
    /// into the layout shares an edge with the content and draws a hairline there, and one floating
    /// over it casts a shadow instead. Doing both reads as a seam.
    /// </summary>
    void ApplyEdgeTreatment()
    {
        this.surfaceShadow.Opacity = this.isFloating && this.HasShadow ? 0.25f : 0f;
        this.divider.IsVisible = this.HasDivider && !this.isFloating;
    }

    internal void ApplyStateVisuals(FlyoutPanelState state)
    {
        var railing = state == FlyoutPanelState.Collapsed && this.RailContent is not null;

        this.railHost.IsVisible = railing;
        this.BodyView.IsVisible = !railing;

        this.headerHost.IsVisible = this.HeaderContent is not null && (!railing || this.ShowHeaderWhenCollapsed);
        this.footerHost.IsVisible = this.FooterContent is not null && (!railing || this.ShowFooterWhenCollapsed);
    }

    View BodyView => this.IsContentScrollEnabled ? this.contentScroll : this.contentHost;

    #endregion


    void ApplySideLayout()
    {
        // The divider always faces the content: on a start-side panel that is its right edge.
        var panelColumn = this.Side == FlyoutSide.Start ? 0 : 1;
        var dividerColumn = this.Side == FlyoutSide.Start ? 1 : 0;

        this.rootGrid.ColumnDefinitions[panelColumn].Width = GridLength.Star;
        this.rootGrid.ColumnDefinitions[dividerColumn].Width = GridLength.Auto;
        Grid.SetColumn(this.surface, panelColumn);
        Grid.SetColumn(this.divider, dividerColumn);

        this.ApplyCornerRadius();
    }


    void ApplyCornerRadius()
    {
        var r = this.CornerRadius;
        var corners = this.Side == FlyoutSide.Start
            ? new CornerRadius(0, r, r, 0)   // outer edge square, inner edge rounded
            : new CornerRadius(r, 0, 0, r);

        this.surface.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = corners };
    }


    void UpdateScrollHost()
    {
        var wanted = this.BodyView;
        var other = ReferenceEquals(wanted, this.contentScroll) ? (View)this.contentHost : this.contentScroll;

        if (this.bodyGrid.Children.Contains(wanted))
            return;

        this.bodyGrid.Children.Remove(other);
        this.contentScroll.Content = ReferenceEquals(wanted, this.contentScroll) ? this.contentHost : null;
        this.bodyGrid.Children.Insert(0, wanted);
    }


    static void Tint(Element target, BindableProperty property, Color? explicitColor, string themeKey)
    {
        if (explicitColor is null)
        {
            target.SetDynamicResource(property, themeKey);
        }
        else
        {
            target.RemoveDynamicResource(property);
            target.SetValue(property, explicitColor);
        }
    }
}
