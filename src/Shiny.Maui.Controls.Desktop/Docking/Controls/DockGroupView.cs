using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// A tabbed group of dockable panels. Internal building block of <see cref="DockHostView"/>.
/// Keeps every panel's View alive in the visual tree (hidden when inactive) so panel
/// state survives tab switches.
/// </summary>
public class DockGroupView : ContentView
{
    readonly DockTabStrip strip;
    readonly Grid content;
    readonly Dictionary<string, View> panelViews = new();

    public static readonly BindableProperty GroupIdProperty = BindableProperty.Create(
        nameof(GroupId), typeof(string), typeof(DockGroupView), string.Empty);

    public string GroupId
    {
        get => (string)GetValue(GroupIdProperty);
        set => SetValue(GroupIdProperty, value);
    }

    public DockGroup? Group { get; private set; }
    public DockTabStrip TabStrip => strip;
    public Grid ContentHost => content;

    public event EventHandler<DockTab>? TabActivateRequested;
    public event EventHandler<DockTab>? TabCloseRequested;
    public event EventHandler<(DockTab Tab, Border View, PanUpdatedEventArgs Pan)>? TabPan;
    public event EventHandler? CollapseRequested;

    public DockGroupView()
    {
        BackgroundColor = Colors.White;

        strip = new DockTabStrip { ShowCollapseButton = false };
        strip.TabTapped += (_, tab) => TabActivateRequested?.Invoke(this, tab);
        strip.TabCloseTapped += (_, tab) => TabCloseRequested?.Invoke(this, tab);
        strip.TabPan += (_, e) => TabPan?.Invoke(this, e);
        strip.CollapseTapped += (_, _) => CollapseRequested?.Invoke(this, EventArgs.Empty);

        content = new Grid();

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        grid.Add(strip, 0, 0);
        grid.Add(content, 0, 1);

        Content = new Border
        {
            Content = grid,
            Stroke = Color.FromArgb("#D1D5DB"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            BackgroundColor = Colors.White
        };
    }

    public void Apply(
        DockGroup group,
        Func<DockTab, View?> viewResolver,
        Func<DockTab, string> titleSelector,
        bool isLocked,
        bool showCollapse = false)
    {
        Group = group;
        GroupId = group.GroupId;

        strip.ShowCollapseButton = showCollapse && !isLocked;
        strip.SetTabs(group, titleSelector, CanClose, isLocked);

        content.Children.Clear();
        panelViews.Clear();
        var activeIndex = Math.Clamp(group.ActiveTabIndex, 0, Math.Max(0, group.Tabs.Count - 1));

        for (var i = 0; i < group.Tabs.Count; i++)
        {
            var tab = group.Tabs[i];
            var view = viewResolver(tab) ?? MissingPanelView(tab);
            view.IsVisible = i == activeIndex;
            panelViews[tab.PanelInstanceId] = view;
            content.Children.Add(view);

            if (view is IDockableContent dockable)
            {
                if (i == activeIndex) dockable.OnActivated();
                else dockable.OnDeactivated();
            }
        }

        bool CanClose(DockTab tab) =>
            viewResolver(tab) is not IDockableContent d || d.CanClose;
    }

    /// <summary>Switch the visible panel without rebuilding the whole group.</summary>
    public void SetActive(int index)
    {
        if (Group is null || index < 0 || index >= Group.Tabs.Count) return;
        for (var i = 0; i < Group.Tabs.Count; i++)
        {
            var tab = Group.Tabs[i];
            if (!panelViews.TryGetValue(tab.PanelInstanceId, out var view)) continue;
            var visible = i == index;
            if (view.IsVisible == visible) continue;
            view.IsVisible = visible;
            if (view is IDockableContent dockable)
            {
                if (visible) dockable.OnActivated();
                else dockable.OnDeactivated();
            }
        }
    }

    static View MissingPanelView(DockTab tab) => new Label
    {
        Text = $"Unknown panel type '{tab.PanelTypeId}'",
        FontSize = 12,
        TextColor = Color.FromArgb("#B91C1C"),
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center
    };
}
