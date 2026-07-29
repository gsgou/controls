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
    readonly BoxView shimmerBand;

    bool isAnimating;
    double containerWidth;

    public SkeletonView()
    {
        this.realContentHost = new ContentView();
        this.placeholderHost = new ContentView();

        this.shimmerBand = new BoxView
        {
            IsVisible = false,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill
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
                this.ConfigureShimmerBand();
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
        => this.placeholderHost.Content = this.SkeletonTemplate != null
            ? this.SkeletonTemplate.CreateContent() as View
            : this.BuildDefaultSkeleton();

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
