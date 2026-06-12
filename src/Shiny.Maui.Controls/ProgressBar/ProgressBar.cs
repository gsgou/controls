using Shiny.Maui.Controls.Themes;

namespace Shiny.Maui.Controls;

public partial class ProgressBar : ContentView, IDisposable
{
    readonly BoxView trackBackground;
    readonly BoxView trackFill;
    readonly BoxView pulseOverlay;
    readonly Label progressLabel;
    readonly Grid fillGrid;
    readonly Grid trackGrid;

    IDispatcherTimer? pulseTimer;
    bool isAnimatingPulse;
    bool isAnimatingIndeterminate;
    double trackWidth;

    public ProgressBar()
    {
        trackBackground = new BoxView
        {
            HeightRequest = 8,
            CornerRadius = new CornerRadius(4),
            VerticalOptions = LayoutOptions.Center
        };
        // Theme default — overridden if the consumer sets TrackColor explicitly.
        trackBackground.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceContainerHighest);

        trackFill = new BoxView
        {
            HeightRequest = 8,
            CornerRadius = new CornerRadius(4),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = 0
        };

        pulseOverlay = new BoxView
        {
            HeightRequest = 8,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Start,
            BackgroundColor = Colors.White,
            Opacity = 0,
            InputTransparent = true
        };

        // fillGrid clips the pulse sheen inside the fill area
        fillGrid = new Grid
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start,
            IsClippedToBounds = true,
            WidthRequest = 0
        };
        fillGrid.Children.Add(trackFill);
        fillGrid.Children.Add(pulseOverlay);

        progressLabel = new Label
        {
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            IsVisible = false
        };
        // Theme default — overridden if the consumer sets TextColor explicitly.
        progressLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnPrimary);

        trackGrid = new Grid
        {
            VerticalOptions = LayoutOptions.Center
        };

        trackGrid.Children.Add(trackBackground);
        trackGrid.Children.Add(fillGrid);
        trackGrid.Children.Add(progressLabel);

        Content = trackGrid;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
        {
            trackWidth = width;
            UpdateVisuals();
        }
    }

    void OnValueChanged(double oldValue, double newValue)
    {
        UpdateVisuals();

        if (PulseEnabled && PulseOnValueChange && Math.Abs(newValue - oldValue) > double.Epsilon)
            TriggerPulse();

        ValueChangedCommand?.Execute(newValue);
        ValueChangedEvent?.Invoke(this, newValue);
    }

    void UpdateVisuals()
    {
        if (trackWidth <= 0) return;
        if (IsIndeterminate) return;

        var percent = Maximum > Minimum
            ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1)
            : 0;

        var fillWidth = percent * trackWidth;

        // Track
        trackBackground.HeightRequest = TrackHeight;
        trackBackground.CornerRadius = new CornerRadius(CornerRadius);

        // Fill container (clips the pulse)
        fillGrid.WidthRequest = fillWidth;
        fillGrid.HeightRequest = TrackHeight;

        // Fill bar — fill the container
        trackFill.WidthRequest = fillWidth;
        trackFill.HeightRequest = TrackHeight;
        trackFill.CornerRadius = new CornerRadius(CornerRadius);
        trackFill.HorizontalOptions = LayoutOptions.Fill;

        if (UseGradient)
        {
            trackFill.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                GradientStops =
                {
                    new GradientStop(GradientStartColor, 0),
                    new GradientStop(GradientEndColor, 1)
                }
            };
        }
        else
        {
            trackFill.Background = null;
            if (BarColor is Color barColor)
                trackFill.BackgroundColor = barColor;
            else
                trackFill.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        }

        // Pulse sheen sizing
        UpdatePulseOverlaySize();

        // Text
        progressLabel.IsVisible = ShowText;
        if (ShowText)
        {
            var displayPercent = percent * 100;
            progressLabel.Text = string.Format(TextFormat, displayPercent);
        }
    }

    void UpdatePulseOverlaySize()
    {
        var percent = Maximum > Minimum
            ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1)
            : 0;
        var fillWidth = percent * trackWidth;
        var sheenWidth = fillWidth * Math.Clamp(PulseLength, 0.05, 1.0);

        pulseOverlay.WidthRequest = Math.Max(sheenWidth, 4);
        pulseOverlay.HeightRequest = TrackHeight;
    }

    void TriggerPulse()
    {
        if (isAnimatingPulse) return;
        isAnimatingPulse = true;

        var percent = Maximum > Minimum
            ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1)
            : 0;
        var fillWidth = percent * trackWidth;
        var sheenWidth = fillWidth * Math.Clamp(PulseLength, 0.05, 1.0);

        pulseOverlay.WidthRequest = Math.Max(sheenWidth, 4);
        pulseOverlay.HeightRequest = TrackHeight;
        pulseOverlay.BackgroundColor = PulseColor;

        // Build a gradient sheen: transparent -> color -> transparent
        pulseOverlay.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(PulseColor.WithAlpha((float)PulseOpacity), 0.5f),
                new GradientStop(Colors.Transparent, 1)
            }
        };
        pulseOverlay.Opacity = 1;

        // Start off-screen left, sweep to off-screen right
        pulseOverlay.TranslationX = -sheenWidth;

        var animation = new Animation(
            v => pulseOverlay.TranslationX = v,
            -sheenWidth,
            fillWidth,
            Easing.CubicInOut);

        animation.Commit(this, "PulseSweep",
            length: (uint)PulseSpeed,
            finished: (_, _) =>
            {
                pulseOverlay.Opacity = 0;
                pulseOverlay.TranslationX = 0;
                isAnimatingPulse = false;
            });
    }

    void OnPulseEnabledChanged(bool enabled)
    {
        if (enabled)
            ConfigurePulseTimer();
        else
            StopPulseTimer();
    }

    void ConfigurePulseTimer()
    {
        StopPulseTimer();

        if (!PulseEnabled || PulseInterval <= TimeSpan.Zero)
            return;

        pulseTimer = Dispatcher.CreateTimer();
        pulseTimer.Interval = PulseInterval;
        pulseTimer.Tick += (_, _) => TriggerPulse();
        pulseTimer.Start();
    }

    void StopPulseTimer()
    {
        pulseTimer?.Stop();
        pulseTimer = null;
    }

    void OnIndeterminateChanged(bool indeterminate)
    {
        if (indeterminate)
            StartIndeterminateAnimation();
        else
            StopIndeterminateAnimation();
    }

    void StartIndeterminateAnimation()
    {
        if (isAnimatingIndeterminate) return;
        isAnimatingIndeterminate = true;

        fillGrid.WidthRequest = trackWidth;
        fillGrid.HeightRequest = TrackHeight;

        trackFill.HeightRequest = TrackHeight;
        trackFill.CornerRadius = new CornerRadius(CornerRadius);
        trackFill.HorizontalOptions = LayoutOptions.Start;

        if (UseGradient)
        {
            trackFill.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                GradientStops =
                {
                    new GradientStop(GradientStartColor, 0),
                    new GradientStop(GradientEndColor, 1)
                }
            };
        }
        else
        {
            trackFill.Background = null;
            if (BarColor is Color barColor)
                trackFill.BackgroundColor = barColor;
            else
                trackFill.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.Primary);
        }

        progressLabel.IsVisible = false;
        RunIndeterminateLoop();
    }

    async void RunIndeterminateLoop()
    {
        while (isAnimatingIndeterminate && trackWidth > 0)
        {
            var barWidth = trackWidth * 0.3;
            trackFill.WidthRequest = barWidth;
            trackFill.TranslationX = -barWidth;

            await trackFill.TranslateToAsync(trackWidth, 0, 1200, Easing.CubicInOut);

            if (!isAnimatingIndeterminate) break;

            trackFill.TranslationX = -barWidth;
        }
    }

    void StopIndeterminateAnimation()
    {
        isAnimatingIndeterminate = false;
        Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(trackFill);
        trackFill.TranslationX = 0;
        UpdateVisuals();
    }

    public void Dispose()
    {
        StopPulseTimer();
        isAnimatingIndeterminate = false;
        Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(trackFill);
        this.AbortAnimation("PulseSweep");
        GC.SuppressFinalize(this);
    }
}
