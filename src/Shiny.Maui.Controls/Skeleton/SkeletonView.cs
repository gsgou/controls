using Shiny.Maui.Controls.Themes;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

/// <summary>
/// Wraps a content area and, while <see cref="IsBusy"/> is true, replaces it with animated
/// shimmer placeholders — conceptually similar to <c>RefreshView</c>. Provide a custom
/// placeholder layout via <see cref="SkeletonTemplate"/> or rely on the built-in line placeholders.
/// </summary>
[ContentProperty(nameof(Content))]
public partial class SkeletonView : Grid, IDisposable
{
    readonly ContentView realContentHost;
    readonly Grid skeletonHost;
    readonly ContentView placeholderHost;
    readonly Border shimmerBand;

    bool isAnimating;
    double containerWidth;

    public SkeletonView()
    {
        this.realContentHost = new ContentView();
        this.placeholderHost = new ContentView();

        // The band is drawn entirely by its gradient Background, which constrains what it can be.
        // Not a BoxView: one with no Color of its own picks up whatever the host app's implicit
        // Style TargetType="BoxView" says, and an opaque fill over the gradient shows up as a solid
        // bar sweeping across the placeholders. Not a bare layout either: a Grid maps to WinUI's
        // LayoutPanel, which only paints a solid BackgroundColor - a gradient Background on it is
        // silently dropped and the shimmer is invisible on Windows. Border is drawn on every
        // platform; the stroke properties are set explicitly so an implicit Border style cannot
        // outline the band.
        this.shimmerBand = new Border
        {
            IsVisible = false,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill,
            Stroke = Brush.Transparent,
            StrokeThickness = 0,
            Padding = 0
        };

        this.skeletonHost = new Grid
        {
            IsVisible = false,
            InputTransparent = true,
            IsClippedToBounds = true
        };
        this.skeletonHost.Children.Add(this.placeholderHost);
        this.skeletonHost.Children.Add(this.shimmerBand);

        // Both layers occupy the single (0,0) cell so the skeleton overlays the same area.
        this.Children.Add(this.realContentHost);
        this.Children.Add(this.skeletonHost);

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(SkeletonView));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0 && Math.Abs(width - this.containerWidth) > 0.5)
        {
            this.containerWidth = width;
            if (this.isAnimating)
            {
                this.ConfigureShimmerBand();
                this.UpdateShimmerMask();
            }
        }
    }

    void OnContentChanged() => this.realContentHost.Content = this.Content;

    void OnIsBusyChanged(bool busy)
    {
        if (busy)
        {
            this.RebuildSkeleton();
            this.realContentHost.IsVisible = false;
            this.skeletonHost.IsVisible = true;
            this.StartShimmer();
        }
        else
        {
            this.StopShimmer();
            this.skeletonHost.IsVisible = false;
            this.realContentHost.IsVisible = true;
        }
    }

    void OnSkeletonAppearanceChanged()
    {
        if (this.IsBusy)
            this.RebuildSkeleton();
    }

    void RebuildSkeleton()
    {
        // Drop the mask first: it describes the shapes being replaced, and since it clips the host it
        // would hide the new ones until the next sweep recomputes it.
        this.skeletonHost.Clip = null;
        this.placeholderHost.Content = this.SkeletonTemplate != null
            ? this.SkeletonTemplate.CreateContent() as View
            : this.BuildDefaultSkeleton();
    }

    View BuildDefaultSkeleton()
    {
        var count = Math.Max(this.ItemCount, 1);
        var stack = new VerticalStackLayout { Spacing = this.ItemSpacing };
        for (var i = 0; i < count; i++)
        {
            // Shorten the last line for a more natural paragraph look.
            var fraction = i == count - 1 && count > 1 ? 0.6 : 1.0;
            stack.Children.Add(this.BuildLine(fraction));
        }
        return stack;
    }

    View BuildLine(double widthFraction)
    {
        var bar = new BoxView
        {
            HeightRequest = this.ItemHeight,
            CornerRadius = new CornerRadius(this.CornerRadius),
            HorizontalOptions = LayoutOptions.Fill
        };
        // Theme default — overridden if the consumer sets BaseColor explicitly.
        if (this.BaseColor is Color baseColor)
            bar.Color = baseColor;
        else
            bar.SetDynamicResource(BoxView.ColorProperty, ShinyThemeKeys.Color.SurfaceContainerHigh);

        if (widthFraction >= 1.0)
            return bar;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(widthFraction, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(1 - widthFraction, GridUnitType.Star))
            }
        };
        grid.Children.Add(bar);
        return grid;
    }

    /// <summary>
    /// Masks the sweeping band to the placeholder shapes, so the highlight lights up the shapes
    /// themselves instead of painting a rectangle across the gaps between them. The clip goes on the
    /// host rather than the band because the band translates - a clip set on it would travel with it.
    /// Clipping the host is free for the placeholders (the geometry is their own bounds) and is what
    /// confines the band. Custom <see cref="SkeletonTemplate"/> content is measured the same way, so
    /// templates are masked without being modified.
    /// </summary>
    void UpdateShimmerMask()
    {
        var group = new Microsoft.Maui.Controls.Shapes.GeometryGroup();
        CollectShapes(this.placeholderHost, this.placeholderHost.X, this.placeholderHost.Y, group);

        // Before the first layout pass nothing has bounds yet. Leaving the clip off shows an unmasked
        // band for one sweep, which beats clipping everything away; the loop re-runs this each sweep.
        this.skeletonHost.Clip = group.Children.Count > 0 ? group : null;
    }

    static void CollectShapes(Element element, double offsetX, double offsetY, Microsoft.Maui.Controls.Shapes.GeometryGroup group)
    {
        foreach (var child in VisualChildren(element))
        {
            if (child is not VisualElement { IsVisible: true } ve)
                continue;

            var x = offsetX + ve.X;
            var y = offsetY + ve.Y;

            if (VisualChildren(ve).Any())
            {
                CollectShapes(ve, x, y, group);
            }
            else if (ve.Width > 0 && ve.Height > 0)
            {
                group.Children.Add(new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry(
                    CornerRadiusFor(ve),
                    new Rect(x, y, ve.Width, ve.Height)));
            }
        }
    }

    static IEnumerable<Element> VisualChildren(Element element)
        => element is IVisualTreeElement v
            ? v.GetVisualChildren().OfType<Element>()
            : [];

    static CornerRadius CornerRadiusFor(VisualElement ve) => ve switch
    {
        BoxView box => box.CornerRadius,
        Border { StrokeShape: Microsoft.Maui.Controls.Shapes.RoundRectangle rr } => rr.CornerRadius,
        _ => new CornerRadius(0)
    };

    void ConfigureShimmerBand()
    {
        if (this.containerWidth <= 0)
            return;

        var bandWidth = Math.Max(this.containerWidth * 0.4, 40);
        this.shimmerBand.WidthRequest = bandWidth;

        // Middle highlight stop — falls back to the theme token when ShimmerColor is unset.
        var highlightStop = new GradientStop { Offset = 0.5f };
        if (this.ShimmerColor is Color shimmerColor)
            highlightStop.Color = shimmerColor;
        else
            highlightStop.SetDynamicResource(GradientStop.ColorProperty, ShinyThemeKeys.Color.SurfaceContainerHighest);

        this.shimmerBand.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0f),
                highlightStop,
                new GradientStop(Colors.Transparent, 1f)
            }
        };
    }

    async void StartShimmer()
    {
        if (!this.ShimmerEnabled)
        {
            this.shimmerBand.IsVisible = false;
            return;
        }
        if (this.isAnimating)
            return;

        this.isAnimating = true;
        this.shimmerBand.IsVisible = true;
        this.ConfigureShimmerBand();

        while (this.isAnimating && this.IsBusy)
        {
            if (this.containerWidth <= 0)
            {
                await Task.Delay(50);
                continue;
            }

            // Re-measured every sweep: the placeholders may not have been laid out when the loop
            // started, and a template can reflow (text wrapping, rotation) between sweeps.
            this.UpdateShimmerMask();

            var bandWidth = this.shimmerBand.WidthRequest;
            this.shimmerBand.TranslationX = -bandWidth;
            try
            {
                await this.shimmerBand.TranslateToAsync(this.containerWidth, 0, this.AnimationDuration, Easing.Linear);
            }
            catch
            {
                // The view can be detached mid-animation (e.g. navigation); the loop guard tears down.
            }

            if (!this.isAnimating || !this.IsBusy)
                break;
        }

        this.shimmerBand.TranslationX = 0;
    }

    void StopShimmer()
    {
        this.isAnimating = false;
        Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(this.shimmerBand);
        this.shimmerBand.IsVisible = false;
        this.shimmerBand.TranslationX = 0;
        // Nothing left to mask, and a stale clip would hide placeholders shown without shimmer.
        this.skeletonHost.Clip = null;
    }

    void OnShimmerEnabledChanged()
    {
        if (!this.IsBusy)
            return;

        if (this.ShimmerEnabled)
            this.StartShimmer();
        else
            this.StopShimmer();
    }

    void OnShimmerColorChanged()
    {
        if (this.isAnimating)
            this.ConfigureShimmerBand();
    }

    public void Dispose()
    {
        this.StopShimmer();
        GC.SuppressFinalize(this);
    }
}
