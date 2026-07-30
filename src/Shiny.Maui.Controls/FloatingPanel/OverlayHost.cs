using Shiny.Maui.Controls.Infrastructure;
using Shiny.Maui.Controls.Themes;
namespace Shiny.Maui.Controls.FloatingPanel;

public class OverlayHost : Grid
{
    const double DefaultBackdropMaxOpacity = 0.5;

    readonly BoxView backdrop;
    readonly List<object> activeClients = new();

    public OverlayHost()
    {
        InputTransparent = true;
        CascadeInputTransparent = false;

        backdrop = new BoxView
        {
            Opacity = 0,
            IsVisible = false,
            InputTransparent = true
        };
        FloatingPanel.Tint(backdrop, BoxView.ColorProperty, this.BackdropColor, ShinyThemeKeys.Color.Scrim);

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnBackdropTapped;
        backdrop.GestureRecognizers.Add(tap);
        Children.Add(backdrop);

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(OverlayHost));
    }

    public static readonly BindableProperty BackdropColorProperty = BindableProperty.Create(
        nameof(BackdropColor),
        typeof(Color),
        typeof(OverlayHost),
        null,
        propertyChanged: (b, _, n) => StyleGuard.WhenReady(b, typeof(OverlayHost), () =>
            {
                var host = (OverlayHost)b;
                FloatingPanel.Tint(host.backdrop, BoxView.ColorProperty, (Color?)n, ShinyThemeKeys.Color.Scrim);
            }));

    /// <summary>Leave unset to follow the active theme's scrim.</summary>
    public Color? BackdropColor
    {
        get => (Color?)GetValue(BackdropColorProperty);
        set => SetValue(BackdropColorProperty, value);
    }

    public static readonly BindableProperty BackdropMaxOpacityProperty = BindableProperty.Create(
        nameof(BackdropMaxOpacity),
        typeof(double),
        typeof(OverlayHost),
        DefaultBackdropMaxOpacity);

    public double BackdropMaxOpacity
    {
        get => (double)GetValue(BackdropMaxOpacityProperty);
        set => SetValue(BackdropMaxOpacityProperty, value);
    }

    internal async void ShowBackdrop(object client, uint animationDuration)
    {
        if (!activeClients.Contains(client))
            activeClients.Add(client);

        backdrop.InputTransparent = false;
        backdrop.IsVisible = true;
        await backdrop.FadeToAsync(BackdropMaxOpacity, animationDuration);
    }

    internal async void HideBackdrop(object client, uint animationDuration)
    {
        activeClients.Remove(client);

        if (activeClients.Count > 0)
            return;

        await backdrop.FadeToAsync(0, animationDuration);
        backdrop.IsVisible = false;
        backdrop.InputTransparent = true;
    }

    void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        foreach (var client in activeClients.ToList())
        {
            if (client is FloatingPanel panel)
            {
                if (panel.CloseOnBackdropTap && !panel.IsLocked)
                    panel.IsOpen = false;
            }
            else if (client is Overlay overlay)
            {
                if (overlay.CloseOnBackdropTap)
                    overlay.IsShown = false;
            }
        }
    }
}
