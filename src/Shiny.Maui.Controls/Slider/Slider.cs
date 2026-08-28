using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Shiny.Maui.Controls.Themes;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

public partial class Slider : ContentView
{
    // Gap between the track and the tooltip, and between the track and the mark captions.
    const double TooltipGap = 6;
    const double MarkLabelGap = 4;

    readonly ObservableCollection<SliderMark> marks = new();
    readonly List<MarkVisual> markVisuals = new();

    readonly BoxView trackBackground;
    readonly Border thumb;
    readonly RoundRectangle thumbShape;
    readonly Border tooltipBadge;
    readonly Label tooltipLabel;
    readonly ContentView tooltipContainer;
    readonly AbsoluteLayout rootLayout;

    double layoutWidth;
    double layoutHeight;
    bool isDragging;
    double dragStartCenter;

    public Slider()
    {
        // Tooltip label (default)
        tooltipLabel = new Label
        {
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontAttributes = FontAttributes.Bold
        }.WithFontSize(ShinyThemeKeys.Type.BodySmallSize);
        // Theme default — overridden if the consumer sets TooltipTextColor explicitly.
        tooltipLabel.SetDynamicResource(Label.TextColorProperty, ShinyThemeKeys.Color.OnSurfaceVariant);

        tooltipBadge = new Border
        {
            StrokeShape = new RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerExtraSmallRadius),
            Stroke = Colors.Transparent,
            Padding = new Thickness(10, 4),
            Content = tooltipLabel
        };
        // Theme default — overridden if the consumer sets TooltipBackgroundColor explicitly.
        tooltipBadge.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.SurfaceVariant);

        tooltipContainer = new ContentView
        {
            Content = tooltipBadge,
            InputTransparent = true
        };

        // Track background (solid blended color)
        trackBackground = new BoxView
        {
            CornerRadius = 4
        };

        // Thumb
        thumbShape = new RoundRectangle().WithCornerRadius(ShinyThemeKeys.Shape.CornerMediumRadius);
        thumb = new Border
        {
            WidthRequest = 24,
            HeightRequest = 24,
            StrokeShape = thumbShape,
            Stroke = ColdColor,
            Shadow = CreateThumbShadow(),
            Padding = 0,
            InputTransparent = true
        }.WithStrokeThickness(ShinyThemeKeys.Border.Thin);
        // Theme default — overridden if the consumer sets ThumbColor explicitly.
        thumb.SetDynamicResource(VisualElement.BackgroundColorProperty, ShinyThemeKeys.Color.OnPrimary);

        // Everything — track, thumb, marks and tooltip — lives in one absolute layout. The orientations
        // differ only in how the two axes are read, so a single coordinate space keeps them one code path.
        rootLayout = new AbsoluteLayout();

        AbsoluteLayout.SetLayoutBounds(trackBackground, new Rect(0, 0, 0, 0));
        AbsoluteLayout.SetLayoutBounds(thumb, new Rect(0, 0, 24, 24));
        AbsoluteLayout.SetLayoutBounds(tooltipContainer, new Rect(0, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));

        rootLayout.Children.Add(trackBackground);
        rootLayout.Children.Add(thumb);
        rootLayout.Children.Add(tooltipContainer);

        Content = rootLayout;

        // Set initial tooltip text
        tooltipLabel.Text = FormatValue(Value);

        // The tooltip and the mark badges size themselves, so their true extents only arrive after a
        // layout pass. Reposition then — this only moves them, so it cannot loop back into a resize.
        tooltipContainer.SizeChanged += (_, _) => UpdateVisuals();

        rootLayout.SizeChanged += (_, _) => SetLayoutSize(rootLayout.Width, rootLayout.Height);

        marks.CollectionChanged += OnMarksChanged;

        // Gesture recognizers
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        rootLayout.GestureRecognizers.Add(panGesture);

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTrackTapped;
        rootLayout.GestureRecognizers.Add(tapGesture);

        UpdateLayoutRequests();
        UpdateVisuals();

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(Slider));
    }


    /// <summary>
    /// The stop points drawn on the track. Add <see cref="SliderMark"/>s in XAML or code; set
    /// <see cref="SnapToMarks"/> to make the thumb come to rest on them.
    /// </summary>
    public IList<SliderMark> Marks => this.marks;


    /// <summary>
    /// The subtle drop shadow the thumb used to get from <c>Frame.HasShadow</c>. Border has no
    /// equivalent flag, so it is recreated explicitly (and per-thumb — a Shadow instance cannot
    /// be shared across elements).
    /// </summary>
    internal static Shadow CreateThumbShadow() => new()
    {
        Brush = Brush.Black,
        Opacity = 0.24f,
        Radius = 3,
        Offset = new Point(0, 1)
    };


    bool IsVertical => this.Orientation == SliderOrientation.Vertical;


    /// <summary>
    /// Records the size the track was laid out at and redraws. The layout pass calls it; tests call it
    /// in place of one, since nothing is arranged headlessly.
    /// </summary>
    internal void SetLayoutSize(double width, double height)
    {
        this.layoutWidth = width;
        this.layoutHeight = height;
        this.UpdateVisuals();
    }


    /// <summary>Where the thumb was placed, for tests to read.</summary>
    internal Rect ThumbBounds => AbsoluteLayout.GetLayoutBounds(this.thumb);


    /// <summary>The paint order of the slider's own layer, for tests to read.</summary>
    internal int IndexOf(IView view) => this.rootLayout.Children.IndexOf(view);

    internal View ThumbView => this.thumb;


    /// <summary>Every drawn mark and the box it was placed in, for tests to read.</summary>
    internal IReadOnlyList<(SliderMark Mark, View Marker, View? Caption)> DrawnMarks
        => this.markVisuals.Select(v => (v.Mark, v.Marker, v.Caption)).ToList();


    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Marks are BindableObjects rather than elements, so nothing hands them a binding context.
        // Seeding it is what lets a mark bind its Text or Value to the page's view-model.
        foreach (var mark in this.marks)
            SetInheritedBindingContext(mark, this.BindingContext);
    }


    // ---------------------------------------------------------------------------------------------
    // Interaction
    // ---------------------------------------------------------------------------------------------

    void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!IsEnabled) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                isDragging = true;
                dragStartCenter = CenterFor(Percent);
                break;

            case GestureStatus.Running:
                if (isDragging && Travel > 0)
                {
                    var current = dragStartCenter + (IsVertical ? e.TotalY : e.TotalX);
                    SetValueFromPercent(PercentForCenter(current));
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
        if (!IsEnabled || Travel <= 0) return;

        var point = e.GetPosition(rootLayout);
        if (point is null) return;

        SetValueFromPercent(PercentForCenter(IsVertical ? point.Value.Y : point.Value.X));
    }


    internal void SetValueFromPercent(double percent)
    {
        percent = Math.Clamp(percent, 0, 1);
        var rawValue = Minimum + (percent * (Maximum - Minimum));

        var snapped = SnapToNearestMark(rawValue);
        if (snapped is double markValue)
        {
            rawValue = markValue;
        }
        else if (Step > 0)
        {
            rawValue = Math.Round(rawValue / Step) * Step;
        }

        rawValue = Math.Clamp(rawValue, Minimum, Maximum);

        if (Math.Abs(rawValue - Value) > double.Epsilon)
        {
            Value = rawValue;
            ValueChangedCommand?.Execute(rawValue);
            ValueChangedEvent?.Invoke(this, rawValue);
        }
    }


    /// <summary>The mark the value comes to rest on, or null when marks are not snap targets.</summary>
    double? SnapToNearestMark(double rawValue)
    {
        if (!SnapToMarks)
            return null;

        double? best = null;
        var bestDistance = double.MaxValue;

        foreach (var mark in this.marks)
        {
            if (!mark.IsVisible)
                continue;

            var distance = Math.Abs(mark.Value - rawValue);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = mark.Value;
            }
        }
        return best;
    }


    // ---------------------------------------------------------------------------------------------
    // Geometry
    // ---------------------------------------------------------------------------------------------

    internal double Percent => Maximum > Minimum
        ? Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1)
        : 0;

    /// <summary>How far the thumb's centre can travel, which is the track less one thumb.</summary>
    double Travel => Math.Max(0, (IsVertical ? layoutHeight : layoutWidth) - ThumbSize);

    /// <summary>
    /// Where the thumb's centre sits for a given fraction. Vertical runs bottom-to-top, so the minimum
    /// is at the largest coordinate.
    /// </summary>
    internal double CenterFor(double percent) => IsVertical
        ? layoutHeight - (ThumbSize / 2) - (percent * Travel)
        : (ThumbSize / 2) + (percent * Travel);

    internal double PercentForCenter(double center)
    {
        if (Travel <= 0)
            return 0;

        var percent = IsVertical
            ? (layoutHeight - (ThumbSize / 2) - center) / Travel
            : (center - (ThumbSize / 2)) / Travel;

        return Math.Clamp(percent, 0, 1);
    }


    void UpdateVisuals()
    {
        if (layoutWidth <= 0 || layoutHeight <= 0) return;

        var percent = Percent;
        var blended = BlendColors(ColdColor, HotColor, percent);
        var tooltipBand = TooltipBand();
        var trackBand = TrackBand();

        // Track — solid blended color. Color is set alongside Background because the macOS/AppKit BoxView
        // handler paints from Color only and ignores the Background brush, which left the track invisible.
        trackBackground.Background = new SolidColorBrush(blended);
        trackBackground.Color = blended;
        trackBackground.CornerRadius = new CornerRadius(TrackHeight / 2);

        var trackAcross = tooltipBand + ((trackBand - TrackHeight) / 2);
        AbsoluteLayout.SetLayoutBounds(trackBackground, IsVertical
            ? new Rect(trackAcross, 0, TrackHeight, layoutHeight)
            : new Rect(0, trackAcross, layoutWidth, TrackHeight));

        // Thumb
        var center = CenterFor(percent);
        var thumbAcross = tooltipBand + ((trackBand - ThumbSize) / 2);
        AbsoluteLayout.SetLayoutBounds(thumb, IsVertical
            ? new Rect(thumbAcross, center - (ThumbSize / 2), ThumbSize, ThumbSize)
            : new Rect(center - (ThumbSize / 2), thumbAcross, ThumbSize, ThumbSize));

        thumb.Stroke = blended;
        thumbShape.CornerRadius = ThumbSize / 2;
        thumb.WidthRequest = ThumbSize;
        thumb.HeightRequest = ThumbSize;

        LayoutMarks(tooltipBand, trackBand);
        UpdateTooltip(center, tooltipBand);
    }


    void UpdateTooltip(double center, double tooltipBand)
    {
        tooltipContainer.IsVisible = ShowTooltip;
        if (!ShowTooltip) return;

        // Update tooltip content first so it can size itself
        if (TooltipTemplate is not null)
        {
            tooltipContainer.Content = CreateTooltipFromTemplate();
        }
        else
        {
            if (!ReferenceEquals(tooltipContainer.Content, tooltipBadge))
                tooltipContainer.Content = tooltipBadge;

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

        var width = tooltipContainer.Width > 0 ? tooltipContainer.Width : EstimatedTooltipWidth();
        var height = tooltipContainer.Height > 0 ? tooltipContainer.Height : EstimatedTooltipHeight();

        // AutoSize keeps the badge sized to its own content; only the origin is placed here.
        if (IsVertical)
        {
            var y = Math.Clamp(center - (height / 2), 0, Math.Max(0, layoutHeight - height));
            var x = Math.Max(0, tooltipBand - TooltipGap - width);
            AbsoluteLayout.SetLayoutBounds(tooltipContainer, new Rect(x, y, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        }
        else
        {
            var x = Math.Clamp(center - (width / 2), 0, Math.Max(0, layoutWidth - width));
            AbsoluteLayout.SetLayoutBounds(tooltipContainer, new Rect(x, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        }
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
