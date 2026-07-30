using System.Collections.ObjectModel;
using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls.FloatingPanel;

public partial class FloatingPanel
{
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen),
        typeof(bool),
        typeof(FloatingPanel),
        false,
        BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () => OnIsOpenChanged(b, o, n)));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly BindableProperty PositionProperty = BindableProperty.Create(
        nameof(Position),
        typeof(FloatingPanelPosition),
        typeof(FloatingPanel),
        FloatingPanelPosition.Bottom,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                ((FloatingPanel)b).UpdateLayoutForPosition();
            }));

    public FloatingPanelPosition Position
    {
        get => (FloatingPanelPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public static readonly BindableProperty PanelContentProperty = BindableProperty.Create(
        nameof(PanelContent),
        typeof(View),
        typeof(FloatingPanel),
        null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () => OnPanelContentChanged(b, o, n)));

    public View? PanelContent
    {
        get => (View?)GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }

    public static readonly BindableProperty HeaderTemplateProperty = BindableProperty.Create(
        nameof(HeaderTemplate),
        typeof(View),
        typeof(FloatingPanel),
        null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () => OnHeaderTemplateChanged(b, o, n)));

    public View? HeaderTemplate
    {
        get => (View?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public static readonly BindableProperty ShowHeaderWhenClosedProperty = BindableProperty.Create(
        nameof(ShowHeaderWhenClosed),
        typeof(bool),
        typeof(FloatingPanel),
        false,
        propertyChanged: (b, _, _) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                ((FloatingPanel)b).UpdateClosedState();
            }));

    public bool ShowHeaderWhenClosed
    {
        get => (bool)GetValue(ShowHeaderWhenClosedProperty);
        set => SetValue(ShowHeaderWhenClosedProperty, value);
    }

    public static readonly BindableProperty DetentsProperty = BindableProperty.Create(
        nameof(Detents),
        typeof(ObservableCollection<DetentValue>),
        typeof(FloatingPanel),
        null);

    public ObservableCollection<DetentValue> Detents
    {
        get => (ObservableCollection<DetentValue>)GetValue(DetentsProperty);
        set => SetValue(DetentsProperty, value);
    }

    public static readonly BindableProperty PanelBackgroundColorProperty = BindableProperty.Create(
        nameof(PanelBackgroundColor),
        typeof(Color),
        typeof(FloatingPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                var panel = (FloatingPanel)b;
                Tint(panel.sheetContainer, VisualElement.BackgroundColorProperty,
                    (Color?)n, ShinyThemeKeys.Color.SurfaceContainerLow);
            }));

    /// <summary>Leave unset to follow the active theme.</summary>
    public Color? PanelBackgroundColor
    {
        get => (Color?)GetValue(PanelBackgroundColorProperty);
        set => SetValue(PanelBackgroundColorProperty, value);
    }

    public static readonly BindableProperty HandleColorProperty = BindableProperty.Create(
        nameof(HandleColor),
        typeof(Color),
        typeof(FloatingPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                var panel = (FloatingPanel)b;
                Tint(panel.dragHandle, BoxView.ColorProperty, (Color?)n, ShinyThemeKeys.Color.OutlineVariant);
            }));

    /// <summary>Leave unset to follow the active theme.</summary>
    public Color? HandleColor
    {
        get => (Color?)GetValue(HandleColorProperty);
        set => SetValue(HandleColorProperty, value);
    }

    public static readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(
        nameof(HeaderBackgroundColor),
        typeof(Color),
        typeof(FloatingPanel),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
            var panel = (FloatingPanel)b;
            var color = n as Color;
            panel.dragHandleContainer.BackgroundColor = color ?? Colors.Transparent;
            panel.headerHost.BackgroundColor = color ?? Colors.Transparent;
            if (panel.safeAreaFill.IsVisible)
                Tint(panel.safeAreaFill, BoxView.ColorProperty,
                    color ?? panel.PanelBackgroundColor, ShinyThemeKeys.Color.SurfaceContainerLow);
        }));

    public Color? HeaderBackgroundColor
    {
        get => (Color?)GetValue(HeaderBackgroundColorProperty);
        set => SetValue(HeaderBackgroundColorProperty, value);
    }

    public static readonly BindableProperty PanelCornerRadiusProperty = BindableProperty.Create(
        nameof(PanelCornerRadius),
        typeof(double),
        typeof(FloatingPanel),
        16.0,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                ((FloatingPanel)b).UpdateCornerRadius((double)n);
            }));

    public double PanelCornerRadius
    {
        get => (double)GetValue(PanelCornerRadiusProperty);
        set => SetValue(PanelCornerRadiusProperty, value);
    }

    public static readonly BindableProperty HasBackdropProperty = BindableProperty.Create(
        nameof(HasBackdrop),
        typeof(bool),
        typeof(FloatingPanel),
        true);

    public bool HasBackdrop
    {
        get => (bool)GetValue(HasBackdropProperty);
        set => SetValue(HasBackdropProperty, value);
    }

    public static readonly BindableProperty CloseOnBackdropTapProperty = BindableProperty.Create(
        nameof(CloseOnBackdropTap),
        typeof(bool),
        typeof(FloatingPanel),
        true);

    public bool CloseOnBackdropTap
    {
        get => (bool)GetValue(CloseOnBackdropTapProperty);
        set => SetValue(CloseOnBackdropTapProperty, value);
    }

    public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
        nameof(AnimationDuration),
        typeof(double),
        typeof(FloatingPanel),
        DefaultAnimationDuration);

    public double AnimationDuration
    {
        get => (double)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public static readonly BindableProperty IsLockedProperty = BindableProperty.Create(
        nameof(IsLocked),
        typeof(bool),
        typeof(FloatingPanel),
        false,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                ((FloatingPanel)b).OnIsLockedChanged((bool)n);
            }));

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public static readonly BindableProperty FitContentProperty = BindableProperty.Create(
        nameof(FitContent),
        typeof(bool),
        typeof(FloatingPanel),
        false);

    public bool FitContent
    {
        get => (bool)GetValue(FitContentProperty);
        set => SetValue(FitContentProperty, value);
    }

    public static readonly BindableProperty UseFeedbackProperty = BindableProperty.Create(
        nameof(UseFeedback),
        typeof(bool),
        typeof(FloatingPanel),
        true);

    public bool UseFeedback
    {
        get => (bool)GetValue(UseFeedbackProperty);
        set => SetValue(UseFeedbackProperty, value);
    }

    public static readonly BindableProperty ShowHandleProperty = BindableProperty.Create(
        nameof(ShowHandle),
        typeof(bool),
        typeof(FloatingPanel),
        true,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                ((FloatingPanel)b).dragHandleContainer.IsVisible = (bool)n;
            }));

    public bool ShowHandle
    {
        get => (bool)GetValue(ShowHandleProperty);
        set => SetValue(ShowHandleProperty, value);
    }

    public static readonly BindableProperty ExpandOnInputFocusProperty = BindableProperty.Create(
        nameof(ExpandOnInputFocus),
        typeof(bool),
        typeof(FloatingPanel),
        true);

    public bool ExpandOnInputFocus
    {
        get => (bool)GetValue(ExpandOnInputFocusProperty);
        set => SetValue(ExpandOnInputFocusProperty, value);
    }

    public static readonly BindableProperty IsContentScrollEnabledProperty = BindableProperty.Create(
        nameof(IsContentScrollEnabled),
        typeof(bool),
        typeof(FloatingPanel),
        true,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(FloatingPanel), () =>
            {
                ((FloatingPanel)b).UpdateScrollEnabled((bool)n);
            }));

    public bool IsContentScrollEnabled
    {
        get => (bool)GetValue(IsContentScrollEnabledProperty);
        set => SetValue(IsContentScrollEnabledProperty, value);
    }

    // Events
    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public event EventHandler<DetentValue>? DetentChanged;

    // Static property change handlers

    static void OnIsOpenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var panel = (FloatingPanel)bindable;
        if ((bool)newValue)
            _ = panel.OpenAsync();
        else
            _ = panel.CloseAsync();
    }

    static void OnPanelContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var panel = (FloatingPanel)bindable;

        if (oldValue is View oldView)
            panel.UnhookInputViews(oldView);

        panel.contentHost.Content = newValue as View;

        if (newValue is View newView)
            panel.HookInputViews(newView);
    }

    static void OnHeaderTemplateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var panel = (FloatingPanel)bindable;
        panel.headerHost.Content = newValue as View;
        panel.UpdateClosedState();
    }

    /// <summary>Uses the explicit colour when one was supplied, otherwise binds to the theme token.</summary>
    internal static void Tint(Element target, BindableProperty property, Color? explicitColor, string themeKey)
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
