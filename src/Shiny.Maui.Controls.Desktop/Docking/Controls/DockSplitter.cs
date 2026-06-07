using Microsoft.Maui.Controls;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Draggable splitter between two adjacent dock children. Reports its position
/// as a 0..1 ratio of the parent's extent so layouts survive resize.
/// </summary>
public class DockSplitter : ContentView
{
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
}
