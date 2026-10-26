using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Draggable splitter between two adjacent dock children. Reports its position
/// as a 0..1 ratio of the parent's extent so layouts survive resize.
/// </summary>
public class DockSplitter : ContentView
{
    public const double Thickness = 5;

    readonly BoxView bar;
    readonly FoldTracker? foldTracker;
    double startRatio;
    bool dragging;
    double? foldRatio;

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation), typeof(DockOrientation), typeof(DockSplitter), DockOrientation.Horizontal);

    public DockOrientation Orientation
    {
        get => (DockOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly BindableProperty RatioProperty = BindableProperty.Create(
        nameof(Ratio), typeof(double), typeof(DockSplitter), 0.5);

    public double Ratio
    {
        get => (double)GetValue(RatioProperty);
        set => SetValue(RatioProperty, value);
    }

    public static readonly BindableProperty IsLockedProperty = BindableProperty.Create(
        nameof(IsLocked), typeof(bool), typeof(DockSplitter), false);

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public static readonly BindableProperty FoldBehaviorProperty = BindableProperty.Create(
        nameof(FoldBehavior), typeof(FoldBehavior), typeof(DockSplitter), FoldBehavior.SnapToFold,
        propertyChanged: (b, _, _) => ((DockSplitter)b).foldTracker?.Refresh());

    /// <summary>
    /// On a foldable, line the splitter up with a fold that runs the same way (a vertical fold for a
    /// side-by-side split, a horizontal one for a stacked split) so neither pane sits across the hinge.
    /// The snap is display-only: <see cref="Ratio"/> and the saved layout keep the user's ratio, and
    /// dragging is suspended while snapped. Default <see cref="FoldBehavior.SnapToFold"/>; no effect without a fold posture source or
    /// while <see cref="IsLocked"/>.
    /// </summary>
    public FoldBehavior FoldBehavior
    {
        get => (FoldBehavior)GetValue(FoldBehaviorProperty);
        set => SetValue(FoldBehaviorProperty, value);
    }

    /// <summary>The ratio the splitter is shown at while lined up with a fold, or null when it is not.</summary>
    public double? FoldRatio => foldRatio;

    /// <summary>The ratio the panes are actually laid out at right now.</summary>
    public double EffectiveRatio => foldRatio ?? Ratio;

    /// <summary>Raised continuously while dragging with the live ratio.</summary>
    public event EventHandler<double>? RatioChanging;

    /// <summary>Raised once at the end of a drag with the final ratio.</summary>
    public event EventHandler<double>? RatioCommitted;

    /// <summary>The extent (width or height) of the area the splitter divides. Set by the host.</summary>
    public Func<double>? ExtentProvider { get; set; }

    public DockSplitter()
    {
        BackgroundColor = Colors.Transparent;
        bar = new BoxView
        {
            CornerRadius = 2,
            Opacity = 0
        }.Tint(BoxView.ColorProperty, Shiny.Maui.Controls.Themes.ShinyThemeKeys.Color.Primary);
        Content = bar;

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPan;
        GestureRecognizers.Add(pan);

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => { if (!IsLocked) bar.Opacity = 0.55; };
        pointer.PointerExited += (_, _) => { if (!dragging) bar.Opacity = 0; };
        GestureRecognizers.Add(pointer);

        // Measured against the split's grid (the parent), which is what the ratio divides. Null with
        // no fold posture source, so a desktop never pays for it.
        foldTracker = FoldTracker.Attach(this, OnFoldChanged, () => Parent as VisualElement);
    }

    void OnFoldChanged(FoldTracker tracker)
    {
        double? next = null;
        if (FoldBehavior == FoldBehavior.SnapToFold && !IsLocked && tracker.Fold is { } fold)
        {
            var horizontal = Orientation == DockOrientation.Horizontal;
            var extent = horizontal ? tracker.Size.Width : tracker.Size.Height;
            var vertical = FoldLayout.IsVertical(fold);

            // The splitter's own track takes Thickness out of the extent the two star rows share, so
            // its centre lands on the fold at (centre - Thickness/2) / (extent - Thickness).
            if (extent > Thickness && horizontal == vertical)
            {
                var centre = horizontal ? fold.Center.X : fold.Center.Y;
                var ratio = (centre - (Thickness / 2)) / (extent - Thickness);
                if (ratio > 0.02 && ratio < 0.98)
                    next = ratio;
            }
        }

        if (Nullable.Equals(next, foldRatio))
            return;

        foldRatio = next;
        RatioChanging?.Invoke(this, EffectiveRatio);
    }

    void OnPan(object? sender, PanUpdatedEventArgs e)
    {
        if (IsLocked || foldRatio is not null) return;
        var extent = ExtentProvider?.Invoke() ?? 0;
        if (extent <= 0) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                startRatio = Ratio;
                dragging = true;
                bar.Opacity = 0.55;
                break;
            case GestureStatus.Running:
            {
                var delta = Orientation == DockOrientation.Horizontal ? e.TotalX : e.TotalY;
                var ratio = Math.Clamp(startRatio + delta / extent, 0.08, 0.92);
                Ratio = ratio;
                RatioChanging?.Invoke(this, ratio);
                break;
            }
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                dragging = false;
                bar.Opacity = 0;
                RatioCommitted?.Invoke(this, Ratio);
                break;
        }
    }
}
