using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.Flyout;

public partial class FlyoutView
{
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content),
        typeof(View),
        typeof(FlyoutView),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutView), () =>
        {
            ((FlyoutView)b).contentHost.Content = (View?)n;
        }));

    /// <summary>What the panels sit beside. One view — put a layout here if you need more.</summary>
    public View? Content
    {
        get => (View?)this.GetValue(ContentProperty);
        set => this.SetValue(ContentProperty, value);
    }

    public static readonly BindableProperty StartProperty = BindableProperty.Create(
        nameof(Start),
        typeof(FlyoutPanel),
        typeof(FlyoutView),
        null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(FlyoutView), () =>
        {
            ((FlyoutView)b).OnPanelAssigned(FlyoutSide.Start, (FlyoutPanel?)o, (FlyoutPanel?)n);
        }));

    /// <summary>The panel on the leading edge — left in a left-to-right layout.</summary>
    public FlyoutPanel? Start
    {
        get => (FlyoutPanel?)this.GetValue(StartProperty);
        set => this.SetValue(StartProperty, value);
    }

    public static readonly BindableProperty EndProperty = BindableProperty.Create(
        nameof(End),
        typeof(FlyoutPanel),
        typeof(FlyoutView),
        null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(FlyoutView), () =>
        {
            ((FlyoutView)b).OnPanelAssigned(FlyoutSide.End, (FlyoutPanel?)o, (FlyoutPanel?)n);
        }));

    /// <summary>The panel on the trailing edge — right in a left-to-right layout.</summary>
    public FlyoutPanel? End
    {
        get => (FlyoutPanel?)this.GetValue(EndProperty);
        set => this.SetValue(EndProperty, value);
    }

    /// <summary>Backing store for <see cref="PushMode"/>.</summary>
    public static readonly BindableProperty PushModeProperty = BindableProperty.Create(
        nameof(PushMode), typeof(FlyoutPushMode), typeof(FlyoutView), FlyoutPushMode.Shift,
        propertyChanged: (b, _, _) => ((FlyoutView)b).InvalidateMeasure());

    /// <summary>
    /// What a pushing panel does to the content: shifts it whole (the default) or narrows it.
    /// </summary>
    /// <remarks>
    /// On the view rather than on the panel because the content is shared — two panels cannot
    /// disagree about whether the thing between them is being resized.
    /// </remarks>
    public FlyoutPushMode PushMode
    {
        get => (FlyoutPushMode)this.GetValue(PushModeProperty);
        set => this.SetValue(PushModeProperty, value);
    }

    public static readonly BindableProperty ScrimColorProperty = BindableProperty.Create(
        nameof(ScrimColor),
        typeof(Color),
        typeof(FlyoutView),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FlyoutView), () =>
        {
            Tint(((FlyoutView)b).scrim, BoxView.ColorProperty, (Color?)n, ShinyThemeKeys.Color.Scrim);
        }));

    /// <summary>Leave unset to follow the active theme's scrim.</summary>
    public Color? ScrimColor
    {
        get => (Color?)this.GetValue(ScrimColorProperty);
        set => this.SetValue(ScrimColorProperty, value);
    }

    public static readonly BindableProperty ScrimOpacityProperty = BindableProperty.Create(
        nameof(ScrimOpacity),
        typeof(double),
        typeof(FlyoutView),
        0.4,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FlyoutView), () =>
        {
            ((FlyoutView)b).UpdateVisuals();
        }));

    /// <summary>How dark the scrim gets at full strength.</summary>
    public double ScrimOpacity
    {
        get => (double)this.GetValue(ScrimOpacityProperty);
        set => this.SetValue(ScrimOpacityProperty, value);
    }

    public static readonly BindableProperty IsAnimationEnabledProperty = BindableProperty.Create(
        nameof(IsAnimationEnabled),
        typeof(bool),
        typeof(FlyoutView),
        true);

    /// <summary>Turn off to have every state change snap. Individual durations live on the panels.</summary>
    public bool IsAnimationEnabled
    {
        get => (bool)this.GetValue(IsAnimationEnabledProperty);
        set => this.SetValue(IsAnimationEnabledProperty, value);
    }


    void OnPanelAssigned(FlyoutSide side, FlyoutPanel? oldPanel, FlyoutPanel? newPanel)
    {
        var runtime = this.Runtime(side);

        if (oldPanel is not null)
        {
            oldPanel.Host = null;
            oldPanel.TranslationX = 0;
            this.Children.Remove(oldPanel);
        }

        runtime.Panel = newPanel;
        runtime.PanelWidth = 0;
        runtime.Visible = 0;
        runtime.Inset = 0;
        runtime.LastRestingInset = 0;
        runtime.CompactedFrom = null;
        runtime.IsDragging = false;

        if (newPanel is not null)
        {
            newPanel.Host = this;
            newPanel.Side = side;
            newPanel.ZIndex = ZOrder.Panel;
            newPanel.IsVisible = false;

            // Seeded rather than left at the default: a panel that starts out expanded has not
            // "changed" to expanded, and firing StateChanged for the initial layout would have every
            // handler run once before the user has touched anything.
            runtime.AppliedState = newPanel.State;
            newPanel.ApplyStateVisuals(newPanel.State);
            this.Children.Add(newPanel);
        }

        this.Retarget(animate: false);
    }
}
