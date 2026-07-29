using Shiny.Maui.Controls.Themes;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class Slider : ContentView
{
    readonly BoxView trackBackground;
    readonly BoxView trackFill;
    readonly Frame thumb;
    readonly Border tooltipBadge;
    readonly Label tooltipLabel;
    readonly ContentView tooltipContainer;
    readonly AbsoluteLayout trackLayout;
    readonly Grid rootGrid;

    double trackWidth;
    bool isDragging;
    double dragStartThumbX;

    public Slider()
    {
        // Tooltip label (default)
        tooltipLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };
        // Theme default — overridden if the consumer sets TooltipTextColor explicitly.
        tooltipLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        tooltipBadge = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(10, 4),
            Content = tooltipLabel,
            HorizontalOptions = LayoutOptions.Center
        };
        // Theme default — overridden if the consumer sets TooltipBackgroundColor explicitly.
        tooltipBadge.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceVariant);

        tooltipContainer = new ContentView
        {
            Content = tooltipBadge,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 4),
            IsVisible = true
        };

        // Track background (full gradient)
        trackBackground = new BoxView
        {
            HeightRequest = 8,
            CornerRadius = 4,
            VerticalOptions = LayoutOptions.Center
        };

        // Track fill (partial gradient up to value)
        trackFill = new BoxView
        {
            HeightRequest = 8,
            CornerRadius = 4,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start
        };

        // Thumb
        thumb = new Frame
        {
            WidthRequest = 24,
            HeightRequest = 24,
            CornerRadius = 12,
            BorderColor = ColdColor,
            HasShadow = true,
            Padding = 0,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center
        };
        // Theme default — overridden if the consumer sets ThumbColor explicitly.
        thumb.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.OnPrimary);

        // Track layout
        trackLayout = new AbsoluteLayout
        {
            HeightRequest = 32,
            VerticalOptions = LayoutOptions.Center
        };

        AbsoluteLayout.SetLayoutBounds(trackBackground, new Rect(0, 0.5, 1, 8));
        AbsoluteLayout.SetLayoutFlags(trackBackground, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional | Microsoft.Maui.Layouts.AbsoluteLayoutFlags.WidthProportional);

        AbsoluteLayout.SetLayoutBounds(trackFill, new Rect(0, 0.5, 0, 8));
        AbsoluteLayout.SetLayoutFlags(trackFill, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.YProportional);

        AbsoluteLayout.SetLayoutBounds(thumb, new Rect(0, 0.5, 24, 24));
        AbsoluteLayout.SetLayoutFlags(thumb, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.YProportional);

        trackLayout.Children.Add(trackBackground);
        trackLayout.Children.Add(trackFill);
        trackLayout.Children.Add(thumb);

        rootGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            RowSpacing = 0
        };

        rootGrid.Add(tooltipContainer, 0, 0);
        rootGrid.Add(trackLayout, 0, 1);

        Content = rootGrid;

        // Set initial tooltip text
        tooltipLabel.Text = FormatValue(Value);

        // Gesture recognizers
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        trackLayout.GestureRecognizers.Add(panGesture);

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTrackTapped;
        trackLayout.GestureRecognizers.Add(tapGesture);

        UpdateVisuals();

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(Slider));
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

    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!IsEnabled) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                isDragging = true;
                dragStartThumbX = AbsoluteLayout.GetLayoutBounds(thumb).X;
                break;

            case GestureStatus.Running:
                if (isDragging && trackWidth > 0)
                {
                    var currentX = dragStartThumbX + e.TotalX;
                    var percent = Math.Clamp(currentX / (trackWidth - ThumbSize), 0, 1);
                    SetValueFromPercent(percent);
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                isDragging = false;
                break;
        }
    }

    void OnTrackTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled || trackWidth <= 0) return;

        var point = e.GetPosition(trackLayout);
        if (point is null) return;

        var percent = Math.Clamp(point.Value.X / trackWidth, 0, 1);
        SetValueFromPercent(percent);
    }

    void SetValueFromPercent(double percent)
    {
        var rawValue = Minimum + (percent * (Maximum - Minimum));

        if (Step > 0)
            rawValue = Math.Round(rawValue / Step) * Step;

        rawValue = Math.Clamp(rawValue, Minimum, Maximum);

        if (Math.Abs(rawValue - Value) > double.Epsilon)
        {
            Value = rawValue;
            ValueChangedCommand?.Execute(rawValue);
            ValueChangedEvent?.Invoke(this, rawValue);
        }
    }

    void UpdateVisuals()
    {
        if (trackWidth <= 0) return;

        var percent = Maximum > Minimum
            ? (Value - Minimum) / (Maximum - Minimum)
            : 0;

        var blended = BlendColors(ColdColor, HotColor, percent);

        // Update track background - solid blended color
        trackBackground.Background = new SolidColorBrush(blended);
        trackBackground.HeightRequest = TrackHeight;
        trackBackground.CornerRadius = new CornerRadius(TrackHeight / 2);

        // Hide track fill - not needed with solid color approach
        AbsoluteLayout.SetLayoutBounds(trackFill, new Rect(0, 0.5, 0, TrackHeight));

        // Update thumb position and color
        var thumbX = percent * (trackWidth - ThumbSize);
        AbsoluteLayout.SetLayoutBounds(thumb, new Rect(thumbX, 0.5, ThumbSize, ThumbSize));
        thumb.BorderColor = blended;
        thumb.CornerRadius = (float)(ThumbSize / 2);
        thumb.WidthRequest = ThumbSize;
        thumb.HeightRequest = ThumbSize;

        // Update tooltip
        UpdateTooltip(percent, blended);
    }

    void UpdateTooltip(double percent, Color blended)
    {
        tooltipContainer.IsVisible = ShowTooltip;
        if (!ShowTooltip) return;

        // Update tooltip content first so we can measure
        if (TooltipTemplate is not null)
        {
            tooltipContainer.Content = CreateTooltipFromTemplate();
        }
        else
        {
            tooltipLabel.Text = FormatValue(Value);
            if (TooltipTextColor is Color ttText)
                tooltipLabel.TextColor = ttText;
            else
                tooltipLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);
            tooltipLabel.FontSize = TooltipFontSize;
            if (TooltipBackgroundColor is Color ttBg)
                tooltipBadge.BackgroundColor = ttBg;
            else
                tooltipBadge.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceVariant);
        }

        // Position tooltip centered on thumb, clamped to track bounds.
        // Use TranslationX on the badge to avoid layout squeeze at edges.
        var tooltipWidth = tooltipBadge.Width > 0 ? tooltipBadge.Width : 50;
        var thumbCenter = percent * (trackWidth - ThumbSize) + (ThumbSize / 2);
        var halfTooltip = tooltipWidth / 2;
        var tooltipX = Math.Clamp(thumbCenter - halfTooltip, 0, Math.Max(0, trackWidth - tooltipWidth));
        tooltipBadge.TranslationX = tooltipX;
        tooltipBadge.HorizontalOptions = LayoutOptions.Start;
    }

    View? CreateTooltipFromTemplate()
    {
        if (TooltipTemplate is null) return null;
        var content = TooltipTemplate.CreateContent();
        if (content is View view)
        {
            view.BindingContext = Value;
            return view;
        }
        return null;
    }

    string FormatValue(double val)
    {
        if (!string.IsNullOrEmpty(ValueFormat))
            return val.ToString(ValueFormat);
        return val % 1 == 0 ? val.ToString("0") : val.ToString("0.#");
    }

    static Color BlendColors(Color color1, Color color2, double ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        var r = color1.Red + (color2.Red - color1.Red) * (float)ratio;
        var g = color1.Green + (color2.Green - color1.Green) * (float)ratio;
        var b = color1.Blue + (color2.Blue - color1.Blue) * (float)ratio;
        return new Color(r, g, b);
    }
}
