using Microsoft.Maui.Controls;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// A tabbed group of dockable panels. Internal building block of <see cref="DockHostView"/>.
/// </summary>
public class DockGroupView : ContentView
{
    public static readonly BindableProperty GroupIdProperty = BindableProperty.Create(
        nameof(GroupId), typeof(string), typeof(DockGroupView), string.Empty);

    public string GroupId
    {
        get => (string)GetValue(GroupIdProperty);
        set => SetValue(GroupIdProperty, value);
    }
}
