using System.Windows.Input;
using Shiny.Maui.Controls.Collections;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.CarouselGallery;

public class CarouselGallery : CollectionControlBase
{
    readonly FoldTracker? foldTracker;

    public CarouselGallery()
    {
        // On a foldable in book posture the focused item centres in the larger segment rather than on
        // the hinge. Null with no posture source: the focus point is then the middle, as always.
        foldTracker = FoldTracker.Attach(this, _ =>
        {
            Handler?.UpdateValue(nameof(PeekAreaInsets));
            Handler?.UpdateValue(nameof(CurrentPosition));
        });
    }

    /// <summary>
    /// The segment the focused item centres in while a vertical fold crosses the carousel, in its own
    /// coordinates; null without one.
    /// </summary>
    internal Rect? FoldSegment => foldTracker is { Fold: { } fold } tracker && FoldLayout.IsVertical(fold)
        ? FoldLayout.SegmentFor(new Rect(0, 0, tracker.Size.Width, tracker.Size.Height), fold, FoldPreference.Larger,
            ((IView)this).FlowDirection == FlowDirection.RightToLeft)
        : null;

    /// <summary>Where along the viewport the focused item's centre sits: the middle, or the fold segment's middle.</summary>
    internal double FocusCentre(double viewportWidth) => FoldSegment?.Center.X ?? viewportWidth / 2;

    public static readonly BindableProperty FocusedItemScaleProperty = BindableProperty.Create(
        nameof(FocusedItemScale),
        typeof(double),
        typeof(CarouselGallery),
        1.0);

    public static readonly BindableProperty UnfocusedItemScaleProperty = BindableProperty.Create(
        nameof(UnfocusedItemScale),
        typeof(double),
        typeof(CarouselGallery),
        0.8);

    public static readonly BindableProperty ItemWidthProperty = BindableProperty.Create(
        nameof(ItemWidth),
        typeof(double),
        typeof(CarouselGallery),
        200.0);

    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight),
        typeof(double),
        typeof(CarouselGallery),
        300.0);

    public static readonly BindableProperty CurrentPositionProperty = BindableProperty.Create(
        nameof(CurrentPosition),
        typeof(int),
        typeof(CarouselGallery),
        0,
        BindingMode.TwoWay);

    public static readonly BindableProperty PeekAreaInsetsProperty = BindableProperty.Create(
        nameof(PeekAreaInsets),
        typeof(Thickness),
        typeof(CarouselGallery),
        new Thickness(0));

    public static readonly BindableProperty IsInfiniteProperty = BindableProperty.Create(
        nameof(IsInfinite),
        typeof(bool),
        typeof(CarouselGallery),
        false);

    public static readonly BindableProperty SnapCountProperty = BindableProperty.Create(
        nameof(SnapCount),
        typeof(int),
        typeof(CarouselGallery),
        1,
        validateValue: (_, v) => (int)v >= 0);

    public static readonly BindableProperty PositionChangedCommandProperty = BindableProperty.Create(
        nameof(PositionChangedCommand),
        typeof(ICommand),
        typeof(CarouselGallery));

    public double FocusedItemScale
    {
        get => (double)GetValue(FocusedItemScaleProperty);
        set => SetValue(FocusedItemScaleProperty, value);
    }

    public double UnfocusedItemScale
    {
        get => (double)GetValue(UnfocusedItemScaleProperty);
        set => SetValue(UnfocusedItemScaleProperty, value);
    }

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public int CurrentPosition
    {
        get => (int)GetValue(CurrentPositionProperty);
        set => SetValue(CurrentPositionProperty, value);
    }

    public Thickness PeekAreaInsets
    {
        get => (Thickness)GetValue(PeekAreaInsetsProperty);
        set => SetValue(PeekAreaInsetsProperty, value);
    }

    public bool IsInfinite
    {
        get => (bool)GetValue(IsInfiniteProperty);
        set => SetValue(IsInfiniteProperty, value);
    }

    public int SnapCount
    {
        get => (int)GetValue(SnapCountProperty);
        set => SetValue(SnapCountProperty, value);
    }

    public ICommand? PositionChangedCommand
    {
        get => (ICommand?)GetValue(PositionChangedCommandProperty);
        set => SetValue(PositionChangedCommandProperty, value);
    }

    public event EventHandler<int>? PositionChanged;

    internal void RaisePositionChanged(int position)
    {
        CurrentPosition = position;
        PositionChanged?.Invoke(this, position);
        if (PositionChangedCommand?.CanExecute(position) == true)
            PositionChangedCommand.Execute(position);
    }
}
