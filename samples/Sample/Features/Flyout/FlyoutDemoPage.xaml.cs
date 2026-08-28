using Shiny.Maui.Controls.Flyout;

namespace Sample.Features.Flyout;

public partial class FlyoutDemoPage : ShinyFlyoutPage
{
    public FlyoutDemoPage()
    {
        InitializeComponent();
        SampleSourceCode.Attach(this);
        this.UpdateStatus();
    }

    void OnToggleStart(object? sender, EventArgs e) => _ = this.Nav.ToggleAsync();

    void OnToggleEnd(object? sender, EventArgs e) => _ = this.Inspector.ToggleAsync();

    void OnStartHidden(object? sender, EventArgs e) => _ = this.Nav.HideAsync();

    void OnStartCollapsed(object? sender, EventArgs e) => _ = this.Nav.CollapseAsync();

    void OnStartExpanded(object? sender, EventArgs e) => _ = this.Nav.ExpandAsync();

    void OnPushShift(object? sender, EventArgs e) => this.SetPushMode(FlyoutPushMode.Shift);

    void OnPushResize(object? sender, EventArgs e) => this.SetPushMode(FlyoutPushMode.Resize);

    void SetPushMode(FlyoutPushMode mode)
    {
        this.PushMode = mode;
        this.SetPresentation(FlyoutPresentation.Push);
    }

    void OnOverlay(object? sender, EventArgs e) => this.SetPresentation(FlyoutPresentation.Overlay);

    void OnPush(object? sender, EventArgs e) => this.SetPresentation(FlyoutPresentation.Push);

    void OnAuto(object? sender, EventArgs e) => this.SetPresentation(FlyoutPresentation.Auto);

    void SetPresentation(FlyoutPresentation presentation)
    {
        this.Nav.Presentation = presentation;
        this.UpdateStatus();
    }

    void OnPanelStateChanged(object? sender, FlyoutStateChangedEventArgs e) => this.UpdateStatus();

    void UpdateStatus()
        => this.Status.Text = $"Start: {this.Nav.State} - {this.Nav.Presentation} (currently {this.FlyoutView.GetEffectivePresentation(FlyoutSide.Start)})";
}
