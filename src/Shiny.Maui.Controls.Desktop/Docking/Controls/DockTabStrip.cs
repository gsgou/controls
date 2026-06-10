using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Tab strip rendered at the top of a <see cref="DockGroupView"/>.
/// Handles tab activation, close buttons, and originates tab drags.
/// </summary>
public class DockTabStrip : ContentView
{
    readonly HorizontalStackLayout stack;
    readonly List<(DockTab Tab, Border View)> tabViews = new();

    public event EventHandler<DockTab>? TabTapped;
    public event EventHandler<DockTab>? TabCloseTapped;
    public event EventHandler<(DockTab Tab, Border View, PanUpdatedEventArgs Pan)>? TabPan;
    public event EventHandler? CollapseTapped;

    public DockTabStrip()
    {
        BackgroundColor = Color.FromArgb("#E5E7EB");
        stack = new HorizontalStackLayout { Spacing = 2, Padding = new Thickness(6, 4, 6, 0) };

        var collapseButton = new Label
        {
            Text = "−",
            FontSize = 15,
            TextColor = Color.FromArgb("#4B5563"),
            Padding = new Thickness(8, 2),
            VerticalOptions = LayoutOptions.Center
        };
        var collapseTap = new TapGestureRecognizer();
        collapseTap.Tapped += (_, _) => CollapseTapped?.Invoke(this, EventArgs.Empty);
        collapseButton.GestureRecognizers.Add(collapseTap);
        collapse = collapseButton;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = stack
        }, 0, 0);
        grid.Add(collapseButton, 1, 0);
        Content = grid;
    }

    readonly Label collapse;

    public bool ShowCollapseButton
    {
        get => collapse.IsVisible;
        set => collapse.IsVisible = value;
    }

    public IReadOnlyList<(DockTab Tab, Border View)> TabViews => tabViews;

    public void SetTabs(DockGroup group, Func<DockTab, string> titleSelector, Func<DockTab, bool> canClose, bool isLocked)
    {
        stack.Children.Clear();
        tabViews.Clear();
        var activeIndex = Math.Clamp(group.ActiveTabIndex, 0, Math.Max(0, group.Tabs.Count - 1));

        for (var i = 0; i < group.Tabs.Count; i++)
        {
            var tab = group.Tabs[i];
            var isActive = i == activeIndex;

            var title = new Label
            {
                Text = titleSelector(tab),
                FontSize = 12,
                FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isActive ? Color.FromArgb("#111827") : Color.FromArgb("#4B5563"),
                VerticalOptions = LayoutOptions.Center
            };

            var row = new HorizontalStackLayout { Spacing = 6, Children = { title } };

            if (!isLocked && canClose(tab))
            {
                var close = new Label
                {
                    Text = "×",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#9CA3AF"),
                    VerticalOptions = LayoutOptions.Center,
                    Padding = new Thickness(2, 0)
                };
                var closeTap = new TapGestureRecognizer();
                closeTap.Tapped += (_, _) => TabCloseTapped?.Invoke(this, tab);
                close.GestureRecognizers.Add(closeTap);
                row.Children.Add(close);
            }

            var border = new Border
            {
                Content = row,
                Padding = new Thickness(10, 5),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(5, 5, 0, 0) },
                BackgroundColor = isActive ? Colors.White : Colors.Transparent
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => TabTapped?.Invoke(this, tab);
            border.GestureRecognizers.Add(tap);

            if (!isLocked)
            {
                var pan = new PanGestureRecognizer();
                pan.PanUpdated += (_, e) => TabPan?.Invoke(this, (tab, border, e));
                border.GestureRecognizers.Add(pan);
            }

            stack.Children.Add(border);
            tabViews.Add((tab, border));
        }
    }
}
