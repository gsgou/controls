using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;
using Microsoft.Maui.Layouts;

namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Root dock surface. Attaches to an existing <see cref="ContentPage"/> — does not
/// subclass it, so consumers keep control of their Shell / page architecture.
/// </summary>
/// <remarks>
/// Place inside a page like any other <see cref="View"/>:
/// <code>
/// &lt;ContentPage ...&gt;
///     &lt;docking:DockHostView InitialLayout="{Binding StartupLayout}" /&gt;
/// &lt;/ContentPage&gt;
/// </code>
/// </remarks>
public class DockHostView : ContentView, IDockHost
{
    const double EdgeBand = 0.28;
    const double RailSize = 230;
    const double RailBarSize = 170;

    DockRoot? layout;
    string? pristineJson;
    bool loadAttempted;
    DockableContentRegistry? registry;
    CancellationTokenSource? saveCts;

    readonly Dictionary<string, View> views = new();             // panelInstanceId -> content view
    readonly Dictionary<string, DockGroupView> groupViews = new(); // groupId -> rendered group
    readonly Dictionary<string, DockArea?> groupRails = new();   // groupId -> owning rail (null = document area)
    readonly Dictionary<string, bool> groupInSplit = new();      // groupId -> rendered inside a split
    View? emptyDocView;                                          // rendered DockEmpty doc area, a valid drop target
    readonly Grid mainGrid;
    readonly AbsoluteLayout overlay;
    readonly DockEventsImpl events = new();
    readonly DockCommandScopeImpl commandScope = new();

    TabDragState? drag;

    public static readonly BindableProperty InitialLayoutProperty = BindableProperty.Create(
        nameof(InitialLayout), typeof(DockRoot), typeof(DockHostView),
        propertyChanged: (b, _, _) => ((DockHostView)b).TryInitialLoad());

    public DockRoot? InitialLayout
    {
        get => (DockRoot?)GetValue(InitialLayoutProperty);
        set => SetValue(InitialLayoutProperty, value);
    }

    public static readonly BindableProperty IsLockedProperty = BindableProperty.Create(
        nameof(IsLocked), typeof(bool), typeof(DockHostView), false,
        propertyChanged: (b, _, _) => ((DockHostView)b).RebuildAll());

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public static readonly BindableProperty LayoutStoreProperty = BindableProperty.Create(
        nameof(LayoutStore), typeof(IDockLayoutStore), typeof(DockHostView));

    public IDockLayoutStore? LayoutStore
    {
        get => (IDockLayoutStore?)GetValue(LayoutStoreProperty);
        set => SetValue(LayoutStoreProperty, value);
    }

    public IDockEvents Events => events;
    public IDockCommandScope CommandScope => commandScope;

    public DockHostView()
    {
        // rows: top-rail, top-resizer, center, bottom-resizer, bottom-rail
        // cols: left-rail, left-resizer, doc, right-resizer, right-rail
        mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            Padding = 4,
            BackgroundColor = Color.FromArgb("#F3F4F6")
        };

        // overlay passes input through itself but its children (ghost/zones are
        // input-transparent anyway, floating panes are interactive) still receive it
        overlay = new AbsoluteLayout
        {
            InputTransparent = true,
            CascadeInputTransparent = false
        };

        var root = new Grid();
        root.Add(mainGrid);
        root.Add(overlay);
        Content = root;

        ShowPlaceholder();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext?.Services is { } services)
        {
            registry ??= services.GetService<DockableContentRegistry>();
            TryInitialLoad();
        }
    }

    void ShowPlaceholder()
    {
        mainGrid.Children.Clear();
        var placeholder = new Border
        {
            Stroke = Color.FromArgb("#CCCCCC"),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 4 },
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            Margin = 8,
            Content = new Label
            {
                Text = "No dock layout loaded",
                FontSize = 14,
                FontAttributes = FontAttributes.Italic,
                TextColor = Color.FromArgb("#888888"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        mainGrid.Add(placeholder, 1, 1);
    }

    async void TryInitialLoad()
    {
        if (layout is not null || loadAttempted || registry is null) return;
        loadAttempted = true;

        DockRoot? stored = null;
        if (LayoutStore is not null)
        {
            try { stored = await LayoutStore.LoadAsync(); }
            catch { /* fall back to InitialLayout on store failure */ }
        }
        var root = stored ?? InitialLayout;
        if (root is null)
        {
            loadAttempted = false; // a later InitialLayout assignment may retry
            return;
        }
        await LoadCoreAsync(root, CancellationToken.None);
    }

    // ------------------------------------------------------------------ IDockHost
    public Task LoadAsync(DockRoot root, CancellationToken ct = default)
    {
        loadAttempted = true;
        return LoadCoreAsync(root, ct);
    }

    async Task LoadCoreAsync(DockRoot root, CancellationToken ct)
    {
        root = Migrate(root);
        var json = DockSerialization.Serialize(root);
        pristineJson ??= InitialLayout is not null
            ? DockSerialization.Serialize(Migrate(InitialLayout))
            : json;
        layout = DockSerialization.Deserialize(json)!;
        ConvertLegacyCollapsedRails();
        views.Clear();
        await ResolveViewsAsync(ct);
        RebuildAll();
        events.RaiseLayoutChanged(new LayoutChangedEventArgs { Snapshot = Snapshot(), Reason = "load" });
    }

    // older layouts collapsed whole rails via CollapsedRails — convert to per-panel entries
    void ConvertLegacyCollapsedRails()
    {
        var win = layout!.MainWindow;
        foreach (var area in win.CollapsedRails.ToList())
        {
            var node = area switch
            {
                DockArea.Top => win.TopRail,
                DockArea.Right => win.RightRail,
                DockArea.Bottom => win.BottomRail,
                _ => win.LeftRail
            };
            foreach (var tab in GroupsIn(node).SelectMany(g => g.Tabs))
                win.CollapsedTabs.Add(new DockCollapsedPanel { Area = area, Tab = tab });
            switch (area)
            {
                case DockArea.Top: win.TopRail = null; break;
                case DockArea.Right: win.RightRail = null; break;
                case DockArea.Bottom: win.BottomRail = null; break;
                default: win.LeftRail = null; break;
            }
        }
        win.CollapsedRails.Clear();
    }

    DockRoot Migrate(DockRoot root)
    {
        var migrators = Handler?.MauiContext?.Services
            .GetService<IEnumerable<IDockLayoutMigrator>>()?.ToList();
        if (migrators is null || migrators.Count == 0) return root;
        while (root.SchemaVersion < DockRoot.CurrentSchemaVersion)
        {
            var step = migrators.FirstOrDefault(m => m.FromVersion == root.SchemaVersion);
            if (step is null) break;
            root = step.Migrate(root);
            root.SchemaVersion = step.ToVersion;
        }
        return root;
    }

    public DockRoot Snapshot()
    {
        var current = layout ?? InitialLayout;
        if (current is null) return new DockRoot();
        return DockSerialization.Deserialize(DockSerialization.Serialize(current))!;
    }

    public async Task ShowPanelAsync(string panelTypeId, DockArea preferredArea = DockArea.Left, CancellationToken ct = default)
    {
        layout ??= new DockRoot();

        var existing = AllGroups().SelectMany(g => g.Tabs).FirstOrDefault(t => t.PanelTypeId == panelTypeId);
        if (existing is not null)
        {
            await ActivatePanelAsync(existing.PanelInstanceId, ct);
            return;
        }

        var collapsedExisting = layout.MainWindow.CollapsedTabs
            .FirstOrDefault(c => c.Tab.PanelTypeId == panelTypeId);
        if (collapsedExisting is not null)
        {
            await RestoreCollapsedAsync(collapsedExisting);
            return;
        }

        var tab = new DockTab { PanelTypeId = panelTypeId };
        await ResolveViewAsync(tab, ct);

        DockIntoRail(preferredArea, tab);
        layout.MainWindow.ActivePanelId = tab.PanelInstanceId;
        RebuildAll();
        OnLayoutMutated("show-panel");
        events.RaisePanelActivated(new PanelActivatedEventArgs
        {
            PanelInstanceId = tab.PanelInstanceId,
            PanelTypeId = tab.PanelTypeId
        });
    }

    void DockIntoRail(DockArea area, DockTab tab)
    {
        var win = layout!.MainWindow;
        var rail = area switch
        {
            DockArea.Top => win.TopRail,
            DockArea.Right => win.RightRail,
            DockArea.Bottom => win.BottomRail,
            _ => win.LeftRail
        };

        if (GroupsIn(rail).FirstOrDefault() is { } group)
        {
            group.Tabs.Add(tab);
            group.ActiveTabIndex = group.Tabs.Count - 1;
        }
        else
        {
            DockNode newRail = new DockGroup { Tabs = { tab } };
            switch (area)
            {
                case DockArea.Top: win.TopRail = newRail; break;
                case DockArea.Right: win.RightRail = newRail; break;
                case DockArea.Bottom: win.BottomRail = newRail; break;
                default: win.LeftRail = newRail; break;
            }
        }
    }

    public Task HidePanelAsync(string panelInstanceId, CancellationToken ct = default)
    {
        if (layout is null) return Task.CompletedTask;

        var collapsedEntry = layout.MainWindow.CollapsedTabs
            .FirstOrDefault(c => c.Tab.PanelInstanceId == panelInstanceId);
        if (collapsedEntry is not null)
        {
            layout.MainWindow.CollapsedTabs.Remove(collapsedEntry);
            if (views.TryGetValue(panelInstanceId, out var collapsedView) && collapsedView is IDisposable d)
                d.Dispose();
            views.Remove(panelInstanceId);
            RebuildAll();
            OnLayoutMutated("hide-panel");
            return Task.CompletedTask;
        }

        foreach (var group in AllGroups())
        {
            var tab = group.Tabs.FirstOrDefault(t => t.PanelInstanceId == panelInstanceId);
            if (tab is null) continue;

            if (views.TryGetValue(panelInstanceId, out var view) && view is IDisposable disposable)
                disposable.Dispose();
            views.Remove(panelInstanceId);

            group.Tabs.Remove(tab);
            group.ActiveTabIndex = Math.Clamp(group.ActiveTabIndex, 0, Math.Max(0, group.Tabs.Count - 1));
            SimplifyAll();
            if (layout.MainWindow.ActivePanelId == panelInstanceId)
                layout.MainWindow.ActivePanelId = null;

            RebuildAll();
            OnLayoutMutated("hide-panel");
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    public Task ActivatePanelAsync(string panelInstanceId, CancellationToken ct = default)
    {
        foreach (var group in AllGroups())
        {
            var idx = group.Tabs.FindIndex(t => t.PanelInstanceId == panelInstanceId);
            if (idx < 0) continue;
            ActivateTab(group, idx);
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    public async Task ResetLayoutAsync(CancellationToken ct = default)
    {
        if (pristineJson is null) return;
        layout = DockSerialization.Deserialize(pristineJson)!;
        views.Clear();
        await ResolveViewsAsync(ct);
        RebuildAll();
        OnLayoutMutated("reset");
    }

    /// <summary>Collapse or restore every panel on a rail at once. Individual panels
    /// collapse via their tab-strip button; this is the bulk/programmatic form.</summary>
    public async Task SetRailCollapsedAsync(DockArea area, bool collapsed, CancellationToken ct = default)
    {
        if (layout is null) return;
        var win = layout.MainWindow;

        if (collapsed)
        {
            var node = area switch
            {
                DockArea.Top => win.TopRail,
                DockArea.Right => win.RightRail,
                DockArea.Bottom => win.BottomRail,
                _ => win.LeftRail
            };
            var tabs = GroupsIn(node).SelectMany(g => g.Tabs).ToList();
            if (tabs.Count == 0) return;
            foreach (var tab in tabs)
                win.CollapsedTabs.Add(new DockCollapsedPanel { Area = area, Tab = tab });
            switch (area)
            {
                case DockArea.Top: win.TopRail = null; break;
                case DockArea.Right: win.RightRail = null; break;
                case DockArea.Bottom: win.BottomRail = null; break;
                default: win.LeftRail = null; break;
            }
            RebuildAll();
            OnLayoutMutated("rail-collapse");
        }
        else
        {
            var items = CollapsedFor(area);
            if (items.Count == 0) return;
            foreach (var item in items)
            {
                win.CollapsedTabs.Remove(item);
                await ResolveViewAsync(item.Tab, ct);
                DockIntoRail(area, item.Tab);
            }
            RebuildAll();
            OnLayoutMutated("rail-expand");
        }
    }

    void CollapseActiveTab(DockArea area, DockGroup group)
    {
        if (layout is null || group.Tabs.Count == 0) return;
        var idx = Math.Clamp(group.ActiveTabIndex, 0, group.Tabs.Count - 1);
        var tab = group.Tabs[idx];
        group.Tabs.RemoveAt(idx);
        // the next tab becomes active; the group stays expanded while tabs remain
        group.ActiveTabIndex = Math.Clamp(idx, 0, Math.Max(0, group.Tabs.Count - 1));
        layout.MainWindow.CollapsedTabs.Add(new DockCollapsedPanel { Area = area, Tab = tab });
        SimplifyAll();
        RebuildAll();
        OnLayoutMutated("panel-collapse");
    }

    async Task RestoreCollapsedAsync(DockCollapsedPanel item)
    {
        if (layout is null) return;
        layout.MainWindow.CollapsedTabs.Remove(item);
        await ResolveViewAsync(item.Tab, CancellationToken.None);
        DockIntoRail(item.Area, item.Tab);
        RebuildAll();
        OnLayoutMutated("panel-expand");
        await ActivatePanelAsync(item.Tab.PanelInstanceId);
    }

    List<DockCollapsedPanel> CollapsedFor(DockArea area)
        => layout?.MainWindow.CollapsedTabs.Where(c => c.Area == area).ToList() ?? new();

    bool HasCollapsed(DockArea area)
        => layout?.MainWindow.CollapsedTabs.Any(c => c.Area == area) == true;

    public Task SetGroupCollapsedAsync(string groupId, bool collapsed, CancellationToken ct = default)
    {
        var group = AllGroups().FirstOrDefault(g => g.GroupId == groupId);
        if (group is null || group.IsCollapsed == collapsed) return Task.CompletedTask;
        group.IsCollapsed = collapsed;
        RebuildAll(); // split sizing changes with collapse state
        OnLayoutMutated(collapsed ? "group-collapse" : "group-expand");
        return Task.CompletedTask;
    }

    // ------------------------------------------------------------------ rendering
    void RebuildAll()
    {
        if (layout is null) return;

        mainGrid.Children.Clear();
        groupViews.Clear();
        groupRails.Clear();
        groupInSplit.Clear();
        overlay.Children.Clear();

        var win = layout.MainWindow;

        if (win.TopRail is not null || HasCollapsed(DockArea.Top))
        {
            var v = BuildRail(win.TopRail, DockArea.Top, out var rsz);
            mainGrid.Add(v, 0, 0);
            Grid.SetColumnSpan(v, 5);
            if (rsz is not null)
            {
                mainGrid.Add(rsz, 0, 1);
                Grid.SetColumnSpan(rsz, 5);
            }
        }
        if (win.BottomRail is not null || HasCollapsed(DockArea.Bottom))
        {
            var v = BuildRail(win.BottomRail, DockArea.Bottom, out var rsz);
            mainGrid.Add(v, 0, 4);
            Grid.SetColumnSpan(v, 5);
            if (rsz is not null)
            {
                mainGrid.Add(rsz, 0, 3);
                Grid.SetColumnSpan(rsz, 5);
            }
        }
        if (win.LeftRail is not null || HasCollapsed(DockArea.Left))
        {
            var v = BuildRail(win.LeftRail, DockArea.Left, out var rsz);
            mainGrid.Add(v, 0, 2);
            if (rsz is not null) mainGrid.Add(rsz, 1, 2);
        }
        if (win.RightRail is not null || HasCollapsed(DockArea.Right))
        {
            var v = BuildRail(win.RightRail, DockArea.Right, out var rsz);
            mainGrid.Add(v, 4, 2);
            if (rsz is not null) mainGrid.Add(rsz, 3, 2);
        }

        var docView = BuildNode(win.DocumentArea);
        emptyDocView = win.DocumentArea is DockEmpty ? docView : null;
        mainGrid.Add(docView, 2, 2);

        for (var i = 0; i < layout.FloatingWindows.Count; i++)
            overlay.Children.Add(BuildFloating(i, layout.FloatingWindows[i]));
    }

    View BuildRail(DockNode? node, DockArea area, out View? resizer)
    {
        resizer = null;
        var vertical = area is DockArea.Left or DockArea.Right;
        var collapsed = CollapsedFor(area);

        View? content = null;
        if (node is not null)
        {
            content = BuildNode(node, area);
            if (vertical)
                content.WidthRequest = GetRailSize(area);
            else
                content.HeightRequest = GetRailSize(area);
            resizer = IsLocked
                ? new BoxView { Color = Colors.Transparent, WidthRequest = 4, HeightRequest = 4 }
                : BuildRailResizer(area, content, vertical);
        }

        if (collapsed.Count == 0)
            return content!;

        var bar = BuildCollapsedRailBar(collapsed, area);
        if (content is null)
            return bar;

        // collapsed edge bar hugs the outer edge, docked content sits beside it
        var wrap = new Grid { ColumnSpacing = 4, RowSpacing = 4 };
        if (vertical)
        {
            wrap.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            wrap.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            var barFirst = area == DockArea.Left;
            wrap.Add(bar, barFirst ? 0 : 1, 0);
            wrap.Add(content, barFirst ? 1 : 0, 0);
        }
        else
        {
            wrap.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            wrap.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var barFirst = area == DockArea.Top;
            wrap.Add(bar, 0, barFirst ? 0 : 1);
            wrap.Add(content, 0, barFirst ? 1 : 0);
        }
        return wrap;
    }

    double GetRailSize(DockArea area)
    {
        var win = layout!.MainWindow;
        return area switch
        {
            DockArea.Left => win.LeftRailSize ?? RailSize,
            DockArea.Right => win.RightRailSize ?? RailSize,
            DockArea.Top => win.TopRailSize ?? RailBarSize,
            _ => win.BottomRailSize ?? RailBarSize
        };
    }

    View BuildRailResizer(DockArea area, View rail, bool vertical)
    {
        var handle = new BoxView { Color = Colors.Transparent };
        if (vertical) handle.WidthRequest = 5;
        else handle.HeightRequest = 5;

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => handle.Color = Color.FromRgba(59, 130, 246, 110);
        pointer.PointerExited += (_, _) => handle.Color = Colors.Transparent;
        handle.GestureRecognizers.Add(pointer);

        double startSize = 0;
        var pan = new PanGestureRecognizer();
        pan.PanUpdated += (_, e) =>
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    startSize = GetRailSize(area);
                    break;
                case GestureStatus.Running:
                {
                    var delta = area switch
                    {
                        DockArea.Left => e.TotalX,
                        DockArea.Right => -e.TotalX,
                        DockArea.Top => e.TotalY,
                        _ => -e.TotalY
                    };
                    var size = Math.Clamp(startSize + delta, 80, 1200);
                    if (vertical) rail.WidthRequest = size;
                    else rail.HeightRequest = size;
                    break;
                }
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                {
                    var final = vertical ? rail.WidthRequest : rail.HeightRequest;
                    var win = layout!.MainWindow;
                    switch (area)
                    {
                        case DockArea.Left: win.LeftRailSize = final; break;
                        case DockArea.Right: win.RightRailSize = final; break;
                        case DockArea.Top: win.TopRailSize = final; break;
                        default: win.BottomRailSize = final; break;
                    }
                    OnLayoutMutated("rail-resize");
                    break;
                }
            }
        };
        handle.GestureRecognizers.Add(pan);
        return handle;
    }

    View BuildCollapsedRailBar(List<DockCollapsedPanel> items, DockArea area)
    {
        var vertical = area is DockArea.Left or DockArea.Right;
        Microsoft.Maui.Controls.Layout stack = vertical
            ? new VerticalStackLayout { Spacing = 4, Padding = new Thickness(2, 6) }
            : new HorizontalStackLayout { Spacing = 4, Padding = new Thickness(6, 2) };

        foreach (var entry in items)
        {
            var captured = entry;
            var icon = GetTabIcon(entry.Tab);

            var label = new Label
            {
                Text = GetTabTitle(entry.Tab),
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#4B5563"),
                LineBreakMode = LineBreakMode.TailTruncation,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            View item;
            if (vertical)
            {
                // icon stays upright at the reading start; the title rotates along the bar
                label.Rotation = area == DockArea.Left ? 270 : 90;
                label.WidthRequest = 110;
                var cell = new VerticalStackLayout { Spacing = 2, WidthRequest = 26 };
                if (icon is not null)
                    cell.Children.Add(new Label
                    {
                        Text = icon,
                        FontSize = 12,
                        HorizontalOptions = LayoutOptions.Center
                    });
                cell.Children.Add(new Grid { HeightRequest = 112, Children = { label } });
                item = cell;
            }
            else
            {
                var row = new HorizontalStackLayout { Spacing = 4, Padding = new Thickness(8, 2) };
                if (icon is not null)
                    row.Children.Add(new Label { Text = icon, FontSize = 12, VerticalOptions = LayoutOptions.Center });
                row.Children.Add(label);
                item = row;
            }

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => _ = RestoreCollapsedAsync(captured);
            item.GestureRecognizers.Add(tap);
            stack.Children.Add(item);
        }

        var bar = new Border
        {
            Stroke = Color.FromArgb("#D1D5DB"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            BackgroundColor = Color.FromArgb("#E5E7EB"),
            Content = stack
        };
        if (vertical) bar.WidthRequest = 32;
        else bar.HeightRequest = 32;
        return bar;
    }

    View BuildNode(DockNode node, DockArea? rail = null, bool inSplit = false) => node switch
    {
        DockSplit split => BuildSplit(split, rail),
        DockGroup group => BuildGroup(group, rail, inSplit),
        _ => BuildEmpty()
    };

    View BuildSplit(DockSplit split, DockArea? rail = null)
    {
        var grid = new Grid();
        var horizontal = split.Orientation == DockOrientation.Horizontal;
        var firstCollapsed = split.First is DockGroup { IsCollapsed: true };
        var secondCollapsed = split.Second is DockGroup { IsCollapsed: true };
        var anyCollapsed = firstCollapsed || secondCollapsed;

        // a collapsed child sizes to its tab strip; the other side takes the rest
        var firstLength = firstCollapsed ? GridLength.Auto
            : anyCollapsed ? GridLength.Star
            : new GridLength(split.Ratio, GridUnitType.Star);
        var secondLength = secondCollapsed ? GridLength.Auto
            : anyCollapsed ? GridLength.Star
            : new GridLength(1 - split.Ratio, GridUnitType.Star);

        if (horizontal)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(firstLength));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(DockSplitter.Thickness)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(secondLength));
        }
        else
        {
            grid.RowDefinitions.Add(new RowDefinition(firstLength));
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(DockSplitter.Thickness)));
            grid.RowDefinitions.Add(new RowDefinition(secondLength));
        }

        var first = BuildNode(split.First, rail, inSplit: true);
        var second = BuildNode(split.Second, rail, inSplit: true);
        var splitter = new DockSplitter
        {
            Orientation = split.Orientation,
            Ratio = split.Ratio,
            IsLocked = IsLocked || anyCollapsed, // no resizing against a collapsed pane
            ExtentProvider = () => horizontal ? grid.Width : grid.Height
        };
        splitter.RatioChanging += (_, ratio) =>
        {
            if (horizontal)
            {
                grid.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
                grid.ColumnDefinitions[2].Width = new GridLength(1 - ratio, GridUnitType.Star);
            }
            else
            {
                grid.RowDefinitions[0].Height = new GridLength(ratio, GridUnitType.Star);
                grid.RowDefinitions[2].Height = new GridLength(1 - ratio, GridUnitType.Star);
            }
        };
        splitter.RatioCommitted += (_, ratio) =>
        {
            split.Ratio = ratio;
            OnLayoutMutated("splitter");
        };

        if (horizontal)
        {
            grid.Add(first, 0, 0);
            grid.Add(splitter, 1, 0);
            grid.Add(second, 2, 0);
        }
        else
        {
            grid.Add(first, 0, 0);
            grid.Add(splitter, 0, 1);
            grid.Add(second, 0, 2);
        }
        return grid;
    }

    View BuildGroup(DockGroup group, DockArea? rail = null, bool inSplit = false)
    {
        var gv = new DockGroupView();
        gv.TabActivateRequested += (_, tab) =>
        {
            var idx = group.Tabs.IndexOf(tab);
            if (idx >= 0) ActivateTab(group, idx);
        };
        gv.TabCloseRequested += (_, tab) => _ = HidePanelAsync(tab.PanelInstanceId);
        gv.TabPan += (_, e) => HandleTabPan(group, e.Tab, e.View, e.Pan);

        // one collapse button, collapsing toward where the panel is docked:
        // rail groups collapse the rail to its edge bar; split document groups
        // shrink to their tab strip; a lone document group has no button
        if (rail is { } railArea)
            gv.CollapseRequested += (_, _) => CollapseActiveTab(railArea, group);
        else if (inSplit || group.IsCollapsed)
            gv.CollapseRequested += (_, _) => _ = SetGroupCollapsedAsync(group.GroupId, !group.IsCollapsed);

        gv.Apply(group, ResolveCachedView, GetTabTitle, IsLocked, CollapseGlyphFor(group, rail, inSplit), GetTabIcon);
        groupViews[group.GroupId] = gv;
        groupRails[group.GroupId] = rail;
        groupInSplit[group.GroupId] = inSplit;
        return gv;
    }

    static string? CollapseGlyphFor(DockGroup group, DockArea? rail, bool inSplit)
    {
        if (rail is { } area)
            return area switch
            {
                DockArea.Left => "◂",
                DockArea.Right => "▸",
                DockArea.Top => "▴",
                _ => "▾"
            };
        if (inSplit || group.IsCollapsed)
            return group.IsCollapsed ? "▸" : "▾";
        return null;
    }

    static View BuildEmpty() => new Border
    {
        Stroke = Color.FromArgb("#CCCCCC"),
        StrokeThickness = 1,
        StrokeDashArray = new DoubleCollection { 4, 4 },
        StrokeShape = new RoundRectangle { CornerRadius = 6 },
        BackgroundColor = Colors.Transparent,
        Content = new Label
        {
            Text = "Drop a panel here",
            FontSize = 12,
            FontAttributes = FontAttributes.Italic,
            TextColor = Color.FromArgb("#9CA3AF"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        }
    };

    View BuildFloating(int index, DockWindowState fw)
    {
        var bounds = fw.Bounds ?? new DockRect(60 + index * 30, 40 + index * 30, 360, 260);

        var title = new Label
        {
            Text = FloatTitle(fw),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var dockBtn = FloatButton("⇤", () => _ = DockFloatingAsync(index));
        var closeBtn = FloatButton("×", () => CloseFloating(index));

        var header = new Grid
        {
            BackgroundColor = Color.FromArgb("#374151"),
            Padding = new Thickness(10, 5, 6, 5),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(title, 0, 0);
        header.Add(dockBtn, 1, 0);
        header.Add(closeBtn, 2, 0);

        var pane = new Border
        {
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#9CA3AF"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            }
        };
        var paneGrid = (Grid)pane.Content;
        paneGrid.Add(header, 0, 0);
        var body = BuildNode(fw.DocumentArea);
        paneGrid.Add(body, 0, 1);

        AbsoluteLayout.SetLayoutBounds(pane, new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));

        if (!IsLocked)
        {
            double startX = 0, startY = 0;
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += (_, e) =>
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        var b = AbsoluteLayout.GetLayoutBounds(pane);
                        startX = b.X;
                        startY = b.Y;
                        break;
                    case GestureStatus.Running:
                    {
                        var x = Math.Max(0, startX + e.TotalX);
                        var y = Math.Max(0, startY + e.TotalY);
                        var cur = AbsoluteLayout.GetLayoutBounds(pane);
                        AbsoluteLayout.SetLayoutBounds(pane, new Rect(x, y, cur.Width, cur.Height));
                        break;
                    }
                    case GestureStatus.Completed:
                    case GestureStatus.Canceled:
                    {
                        var final = AbsoluteLayout.GetLayoutBounds(pane);
                        var old = fw.Bounds ?? new DockRect(final.X, final.Y, final.Width, final.Height);
                        fw.Bounds = new DockRect(final.X, final.Y, old.Width, old.Height);
                        OnLayoutMutated("float-move");
                        break;
                    }
                }
            };
            header.GestureRecognizers.Add(pan);

            // corner grip — mirrors the Blazor pane's CSS resize
            var grip = new Label
            {
                Text = "◢",
                FontSize = 11,
                TextColor = Color.FromArgb("#9CA3AF"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                Padding = new Thickness(6, 2)
            };
            double startW = 0, startH = 0;
            var resizePan = new PanGestureRecognizer();
            resizePan.PanUpdated += (_, e) =>
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        var b = AbsoluteLayout.GetLayoutBounds(pane);
                        startW = b.Width;
                        startH = b.Height;
                        break;
                    case GestureStatus.Running:
                    {
                        var cur = AbsoluteLayout.GetLayoutBounds(pane);
                        var w = Math.Max(180, startW + e.TotalX);
                        var h = Math.Max(120, startH + e.TotalY);
                        AbsoluteLayout.SetLayoutBounds(pane, new Rect(cur.X, cur.Y, w, h));
                        break;
                    }
                    case GestureStatus.Completed:
                    case GestureStatus.Canceled:
                    {
                        var final = AbsoluteLayout.GetLayoutBounds(pane);
                        fw.Bounds = new DockRect(final.X, final.Y, final.Width, final.Height);
                        OnLayoutMutated("float-resize");
                        break;
                    }
                }
            };
            grip.GestureRecognizers.Add(resizePan);
            Grid.SetRow(grip, 1);
            paneGrid.Add(grip);
        }

        return pane;
    }

    static Button FloatButton(string text, Action onClick) => new()
    {
        Text = text,
        FontSize = 13,
        Padding = new Thickness(6, 0),
        Margin = new Thickness(2, 0, 0, 0),
        BackgroundColor = Colors.Transparent,
        TextColor = Colors.White,
        MinimumWidthRequest = 28,
        MinimumHeightRequest = 22,
        Command = new Command(onClick)
    };

    // ------------------------------------------------------------------ tab drag
    sealed class TabDragState
    {
        public required DockGroup SourceGroup { get; init; }
        public required DockTab Tab { get; init; }
        public required Point Origin { get; init; }
        public required Border Ghost { get; init; }
        public required BoxView ZoneBox { get; init; }
        public DockGroup? TargetGroup { get; set; }
        public DockZone? Zone { get; set; }
        public int Index { get; set; } = -1;
        public Point DropPoint { get; set; }
    }

    void HandleTabPan(DockGroup group, DockTab tab, Border tabView, PanUpdatedEventArgs e)
    {
        if (IsLocked) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
            {
                var origin = AbsBounds(tabView);
                var ghost = new Border
                {
                    BackgroundColor = Color.FromArgb("#312E81"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 5 },
                    Padding = new Thickness(12, 5),
                    Opacity = 0.92,
                    Content = new Label
                    {
                        Text = GetTabTitle(tab),
                        TextColor = Colors.White,
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold
                    }
                };
                var zoneBox = new BoxView
                {
                    Color = Color.FromRgba(59, 130, 246, 70),
                    IsVisible = false
                };
                AbsoluteLayout.SetLayoutBounds(ghost, new Rect(origin.X, origin.Y, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
                overlay.Children.Add(zoneBox);
                overlay.Children.Add(ghost);
                drag = new TabDragState
                {
                    SourceGroup = group,
                    Tab = tab,
                    Origin = new Point(origin.X, origin.Y),
                    Ghost = ghost,
                    ZoneBox = zoneBox
                };
                events.RaiseDragStarted(new DockDragEventArgs { SourcePanelInstanceId = tab.PanelInstanceId });
                break;
            }
            case GestureStatus.Running when drag is not null:
            {
                var x = drag.Origin.X + e.TotalX;
                var y = drag.Origin.Y + e.TotalY;
                AbsoluteLayout.SetLayoutBounds(drag.Ghost, new Rect(x + 14, y + 10, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
                HitTest(new Point(x, y));
                break;
            }
            case GestureStatus.Completed when drag is not null:
            {
                var d = drag;
                drag = null;
                overlay.Children.Remove(d.Ghost);
                overlay.Children.Remove(d.ZoneBox);
                _ = CompleteDropAsync(d);
                break;
            }
            case GestureStatus.Canceled when drag is not null:
            {
                var d = drag;
                drag = null;
                overlay.Children.Remove(d.Ghost);
                overlay.Children.Remove(d.ZoneBox);
                events.RaiseDragCancelled(new DockDragEventArgs { SourcePanelInstanceId = d.Tab.PanelInstanceId });
                break;
            }
        }
    }

    void HitTest(Point p)
    {
        if (drag is null) return;
        drag.TargetGroup = null;
        drag.Zone = null;
        drag.Index = -1;
        drag.DropPoint = p;

        // tab strips first — the most specific target
        foreach (var (_, gv) in groupViews)
        {
            var group = gv.Group;
            if (group is null) continue;

            var stripRect = AbsRect(gv.TabStrip);
            if (!stripRect.Contains(p)) continue;

            var index = group.Tabs.Count;
            var tabViews = gv.TabStrip.TabViews;
            for (var i = 0; i < tabViews.Count; i++)
            {
                var tr = AbsRect(tabViews[i].View);
                if (p.X < tr.X + tr.Width / 2) { index = i; break; }
            }
            drag.TargetGroup = group;
            drag.Zone = DockZone.TabStrip;
            drag.Index = index;
            ShowZone(stripRect);
            return;
        }

        var insideHost = p.X >= 0 && p.Y >= 0 && p.X <= Width && p.Y <= Height;

        // host edge bands before group bodies — the extreme edge always means
        // "dock to this side of the window", even when a group's content touches
        // the host edge (where its own split zone would no-op on itself)
        const double edge = 28;
        if (insideHost)
        {
            if (p.X < edge)
            {
                drag.Zone = DockZone.Left;
                ShowZone(new Rect(0, 0, Width / 5, Height));
                return;
            }
            if (p.X > Width - edge)
            {
                drag.Zone = DockZone.Right;
                ShowZone(new Rect(Width - Width / 5, 0, Width / 5, Height));
                return;
            }
            if (p.Y < edge)
            {
                drag.Zone = DockZone.Top;
                ShowZone(new Rect(0, 0, Width, Height / 5));
                return;
            }
            if (p.Y > Height - edge)
            {
                drag.Zone = DockZone.Bottom;
                ShowZone(new Rect(0, Height - Height / 5, Width, Height / 5));
                return;
            }
        }

        foreach (var (_, gv) in groupViews)
        {
            var group = gv.Group;
            if (group is null) continue;

            var contentRect = AbsRect(gv.ContentHost);
            if (!contentRect.Contains(p)) continue;

            var fx = (p.X - contentRect.X) / contentRect.Width;
            var fy = (p.Y - contentRect.Y) / contentRect.Height;
            var (zone, rect) = (fx, fy) switch
            {
                var (x, _) when x < EdgeBand => (DockZone.Left, new Rect(contentRect.X, contentRect.Y, contentRect.Width / 2, contentRect.Height)),
                var (x, _) when x > 1 - EdgeBand => (DockZone.Right, new Rect(contentRect.X + contentRect.Width / 2, contentRect.Y, contentRect.Width / 2, contentRect.Height)),
                var (_, y) when y < EdgeBand => (DockZone.Top, new Rect(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height / 2)),
                var (_, y) when y > 1 - EdgeBand => (DockZone.Bottom, new Rect(contentRect.X, contentRect.Y + contentRect.Height / 2, contentRect.Width, contentRect.Height / 2)),
                _ => (DockZone.Center, contentRect)
            };
            drag.TargetGroup = group;
            drag.Zone = zone;
            ShowZone(rect);
            return;
        }

        // empty document well → dock into it
        if (emptyDocView is not null)
        {
            var er = AbsRect(emptyDocView);
            if (er.Contains(p))
            {
                drag.Zone = DockZone.Center; // null TargetGroup = empty doc area
                ShowZone(er);
                return;
            }
        }

        if (!insideHost)
        {
            drag.ZoneBox.IsVisible = false;
            return;
        }

        // anywhere else inside the host → tear-off
        drag.Zone = DockZone.TearOff;
        drag.ZoneBox.IsVisible = false;
    }

    void ShowZone(Rect rect)
    {
        if (drag is null) return;
        AbsoluteLayout.SetLayoutBounds(drag.ZoneBox, rect);
        drag.ZoneBox.IsVisible = true;
    }

    async Task CompleteDropAsync(TabDragState d)
    {
        if (layout is null || d.Zone is null)
        {
            events.RaiseDragCancelled(new DockDragEventArgs { SourcePanelInstanceId = d.Tab.PanelInstanceId });
            return;
        }

        var sourceGroup = d.SourceGroup;
        var tab = d.Tab;
        var zone = d.Zone.Value;
        var targetGroup = d.TargetGroup;

        switch (zone)
        {
            case DockZone.TabStrip when targetGroup is not null:
            {
                var oldIndex = sourceGroup.Tabs.IndexOf(tab);
                sourceGroup.Tabs.Remove(tab);
                var index = d.Index;
                if (ReferenceEquals(sourceGroup, targetGroup) && oldIndex < index)
                    index--;
                index = Math.Clamp(index, 0, targetGroup.Tabs.Count);
                targetGroup.Tabs.Insert(index, tab);
                targetGroup.ActiveTabIndex = index;
                break;
            }
            case DockZone.Center when targetGroup is not null:
            {
                if (ReferenceEquals(sourceGroup, targetGroup)) return;
                sourceGroup.Tabs.Remove(tab);
                targetGroup.Tabs.Add(tab);
                targetGroup.ActiveTabIndex = targetGroup.Tabs.Count - 1;
                break;
            }
            // dropped on the empty document well
            case DockZone.Center:
            {
                if (layout.MainWindow.DocumentArea is not DockEmpty) return;
                sourceGroup.Tabs.Remove(tab);
                layout.MainWindow.DocumentArea = new DockGroup { Tabs = { tab } };
                break;
            }
            case DockZone.Left or DockZone.Right or DockZone.Top or DockZone.Bottom when targetGroup is not null:
            {
                if (ReferenceEquals(sourceGroup, targetGroup) && sourceGroup.Tabs.Count == 1) return;
                sourceGroup.Tabs.Remove(tab);
                var newGroup = new DockGroup { Tabs = { tab } };
                var split = new DockSplit
                {
                    Orientation = zone is DockZone.Left or DockZone.Right
                        ? DockOrientation.Horizontal
                        : DockOrientation.Vertical,
                    Ratio = 0.5
                };
                if (zone is DockZone.Left or DockZone.Top)
                {
                    split.First = newGroup;
                    split.Second = targetGroup;
                }
                else
                {
                    split.First = targetGroup;
                    split.Second = newGroup;
                }
                ReplaceNode(targetGroup, split);
                break;
            }
            // dropped on a host edge band → dock into (or re-create) that rail
            case DockZone.Left or DockZone.Right or DockZone.Top or DockZone.Bottom:
            {
                sourceGroup.Tabs.Remove(tab);
                DockIntoRail(zone switch
                {
                    DockZone.Top => DockArea.Top,
                    DockZone.Right => DockArea.Right,
                    DockZone.Bottom => DockArea.Bottom,
                    _ => DockArea.Left
                }, tab);
                break;
            }
            case DockZone.TearOff:
            {
                // dragging the only tab of a floating window just moves the window,
                // preserving its size instead of recreating it at defaults
                var owningFloat = layout.FloatingWindows
                    .FirstOrDefault(w => GroupsIn(w.DocumentArea).Contains(sourceGroup));
                if (owningFloat is not null && sourceGroup.Tabs.Count == 1)
                {
                    var b = owningFloat.Bounds ?? new DockRect(d.DropPoint.X, d.DropPoint.Y, 360, 260);
                    owningFloat.Bounds = new DockRect(d.DropPoint.X, d.DropPoint.Y, b.Width, b.Height);
                    break;
                }
                sourceGroup.Tabs.Remove(tab);
                layout.FloatingWindows.Add(new DockWindowState
                {
                    Bounds = new DockRect(d.DropPoint.X, d.DropPoint.Y, 360, 260),
                    DocumentArea = new DockGroup { Tabs = { tab }, ActiveTabIndex = 0 }
                });
                break;
            }
            default:
                events.RaiseDragCancelled(new DockDragEventArgs { SourcePanelInstanceId = tab.PanelInstanceId });
                return;
        }

        sourceGroup.ActiveTabIndex = Math.Clamp(sourceGroup.ActiveTabIndex, 0, Math.Max(0, sourceGroup.Tabs.Count - 1));
        SimplifyAll();
        RebuildAll();
        OnLayoutMutated("drag-drop");
        events.RaiseDragCompleted(new DockDragEventArgs
        {
            SourcePanelInstanceId = tab.PanelInstanceId,
            TargetGroupId = targetGroup?.GroupId,
            TargetZone = zone
        });
        await ActivatePanelAsync(tab.PanelInstanceId);
    }

    // ------------------------------------------------------------------ floating ops
    async Task DockFloatingAsync(int index)
    {
        if (layout is null || index < 0 || index >= layout.FloatingWindows.Count) return;
        var fw = layout.FloatingWindows[index];
        var tabs = GroupsIn(fw.DocumentArea).SelectMany(g => g.Tabs).ToList();
        layout.FloatingWindows.RemoveAt(index);

        var win = layout.MainWindow;
        var target = GroupsIn(win.LeftRail).FirstOrDefault();
        if (target is null)
        {
            target = new DockGroup();
            win.LeftRail = target;
        }
        target.Tabs.AddRange(tabs);
        target.ActiveTabIndex = target.Tabs.Count - 1;

        RebuildAll();
        OnLayoutMutated("dock-floating");
        if (tabs.Count > 0)
            await ActivatePanelAsync(tabs[^1].PanelInstanceId);
    }

    void CloseFloating(int index)
    {
        if (layout is null || index < 0 || index >= layout.FloatingWindows.Count) return;
        var fw = layout.FloatingWindows[index];
        foreach (var t in GroupsIn(fw.DocumentArea).SelectMany(g => g.Tabs))
        {
            if (views.TryGetValue(t.PanelInstanceId, out var view) && view is IDisposable disposable)
                disposable.Dispose();
            views.Remove(t.PanelInstanceId);
        }
        layout.FloatingWindows.RemoveAt(index);
        RebuildAll();
        OnLayoutMutated("close-floating");
    }

    string FloatTitle(DockWindowState fw)
    {
        var group = GroupsIn(fw.DocumentArea).FirstOrDefault();
        if (group is null || group.Tabs.Count == 0) return "Floating";
        var active = group.Tabs[Math.Clamp(group.ActiveTabIndex, 0, group.Tabs.Count - 1)];
        return GetTabTitle(active);
    }

    // ------------------------------------------------------------------ internals
    void ActivateTab(DockGroup group, int index)
    {
        if (index < 0 || index >= group.Tabs.Count) return;
        var expanding = group.IsCollapsed;
        if (expanding)
        {
            group.IsCollapsed = false;
            RebuildAll();
            OnLayoutMutated("group-expand");
        }
        group.ActiveTabIndex = index;
        group.FocusHistory.Remove(index);
        group.FocusHistory.Add(index);

        var tab = group.Tabs[index];
        if (layout is not null)
            layout.MainWindow.ActivePanelId = tab.PanelInstanceId;

        commandScope.IsInScope = true;
        commandScope.ActiveGroupId = group.GroupId;
        commandScope.ActivePanelInstanceId = tab.PanelInstanceId;

        // refresh just this group's chrome + visible panel
        if (groupViews.TryGetValue(group.GroupId, out var gv))
        {
            groupRails.TryGetValue(group.GroupId, out var rail);
            groupInSplit.TryGetValue(group.GroupId, out var inSplit);
            gv.Apply(group, ResolveCachedView, GetTabTitle, IsLocked, CollapseGlyphFor(group, rail, inSplit), GetTabIcon);
        }

        events.RaisePanelActivated(new PanelActivatedEventArgs
        {
            PanelInstanceId = tab.PanelInstanceId,
            PanelTypeId = tab.PanelTypeId
        });
    }

    void OnLayoutMutated(string reason)
    {
        events.RaiseLayoutChanged(new LayoutChangedEventArgs { Snapshot = Snapshot(), Reason = reason });
        QueueSave();
    }

    void QueueSave()
    {
        if (LayoutStore is null || layout is null) return;
        saveCts?.Cancel();
        var snapshot = Snapshot();
        var debounce = Math.Max(0, LayoutStore.SaveDebounceMs);
        if (debounce == 0)
        {
            _ = SaveSafeAsync(snapshot, CancellationToken.None);
            return;
        }
        saveCts = new CancellationTokenSource();
        _ = DebouncedSaveAsync(snapshot, debounce, saveCts.Token);
    }

    async Task DebouncedSaveAsync(DockRoot snapshot, int debounce, CancellationToken token)
    {
        try
        {
            await Task.Delay(debounce, token);
            await SaveSafeAsync(snapshot, token);
        }
        catch (OperationCanceledException) { }
    }

    async Task SaveSafeAsync(DockRoot snapshot, CancellationToken ct)
    {
        try { await LayoutStore!.SaveAsync(snapshot, ct); }
        catch { /* persistence must never take the host down */ }
    }

    View? ResolveCachedView(DockTab tab)
        => views.TryGetValue(tab.PanelInstanceId, out var view) ? view : null;

    string GetTabTitle(DockTab tab)
    {
        if (views.TryGetValue(tab.PanelInstanceId, out var view) && view is IDockableContent dockable)
            return dockable.Title;
        return registry?.Resolve(tab.PanelTypeId)?.DisplayName ?? tab.PanelTypeId;
    }

    string? GetTabIcon(DockTab tab)
    {
        if (views.TryGetValue(tab.PanelInstanceId, out var view)
            && view is IDockableContent { Icon: string contentIcon })
            return contentIcon;
        return registry?.Resolve(tab.PanelTypeId)?.Icon;
    }

    async Task ResolveViewsAsync(CancellationToken ct)
    {
        foreach (var group in AllGroups())
            foreach (var tab in group.Tabs)
                await ResolveViewAsync(tab, ct);
    }

    async Task ResolveViewAsync(DockTab tab, CancellationToken ct)
    {
        if (views.ContainsKey(tab.PanelInstanceId)) return;
        var factory = registry?.Resolve(tab.PanelTypeId);
        if (factory is null) return; // group renders the "unknown panel" label
        views[tab.PanelInstanceId] = await factory.CreateAsync(tab.PanelInstanceId, ct);
    }

    Point AbsBounds(VisualElement element)
    {
        double x = 0, y = 0;
        Element? current = element;
        while (current is VisualElement ve && !ReferenceEquals(current, this))
        {
            x += ve.Frame.X;
            y += ve.Frame.Y;
            if (ve.Parent is ScrollView sv)
            {
                x -= sv.ScrollX;
                y -= sv.ScrollY;
            }
            current = current.Parent;
        }
        return new Point(x, y);
    }

    Rect AbsRect(VisualElement element)
    {
        var p = AbsBounds(element);
        return new Rect(p.X, p.Y, element.Width, element.Height);
    }

    IEnumerable<DockGroup> AllGroups()
    {
        if (layout is null) yield break;
        var win = layout.MainWindow;
        foreach (var node in new[] { win.DocumentArea, win.LeftRail, win.TopRail, win.RightRail, win.BottomRail })
            foreach (var group in GroupsIn(node))
                yield return group;
        foreach (var fw in layout.FloatingWindows)
            foreach (var group in GroupsIn(fw.DocumentArea))
                yield return group;
    }

    static IEnumerable<DockGroup> GroupsIn(DockNode? node)
    {
        switch (node)
        {
            case DockGroup g:
                yield return g;
                break;
            case DockSplit s:
                foreach (var g in GroupsIn(s.First)) yield return g;
                foreach (var g in GroupsIn(s.Second)) yield return g;
                break;
        }
    }

    void ReplaceNode(DockNode target, DockNode replacement)
    {
        if (layout is null) return;
        var win = layout.MainWindow;
        win.DocumentArea = ReplaceIn(win.DocumentArea, target, replacement) ?? new DockEmpty();
        win.LeftRail = win.LeftRail is null ? null : ReplaceIn(win.LeftRail, target, replacement);
        win.TopRail = win.TopRail is null ? null : ReplaceIn(win.TopRail, target, replacement);
        win.RightRail = win.RightRail is null ? null : ReplaceIn(win.RightRail, target, replacement);
        win.BottomRail = win.BottomRail is null ? null : ReplaceIn(win.BottomRail, target, replacement);
        foreach (var fw in layout.FloatingWindows)
            fw.DocumentArea = ReplaceIn(fw.DocumentArea, target, replacement) ?? new DockEmpty();
    }

    static DockNode? ReplaceIn(DockNode? node, DockNode target, DockNode replacement)
    {
        if (node is null) return null;
        if (ReferenceEquals(node, target)) return replacement;
        if (node is DockSplit s)
        {
            s.First = ReplaceIn(s.First, target, replacement) ?? new DockEmpty();
            s.Second = ReplaceIn(s.Second, target, replacement) ?? new DockEmpty();
        }
        return node;
    }

    void SimplifyAll()
    {
        if (layout is null) return;
        var win = layout.MainWindow;
        win.DocumentArea = Simplify(win.DocumentArea) ?? new DockEmpty();
        win.LeftRail = Simplify(win.LeftRail);
        win.TopRail = Simplify(win.TopRail);
        win.RightRail = Simplify(win.RightRail);
        win.BottomRail = Simplify(win.BottomRail);
        for (var i = layout.FloatingWindows.Count - 1; i >= 0; i--)
        {
            var area = Simplify(layout.FloatingWindows[i].DocumentArea);
            if (area is null)
                layout.FloatingWindows.RemoveAt(i);
            else
                layout.FloatingWindows[i].DocumentArea = area;
        }

        // a group left alone in a document well has nothing to collapse against —
        // auto-expand so it can't get stuck as a strip-only sliver
        if (win.DocumentArea is DockGroup lone)
            lone.IsCollapsed = false;
        foreach (var fw in layout.FloatingWindows)
            if (fw.DocumentArea is DockGroup floatLone)
                floatLone.IsCollapsed = false;
    }

    static DockNode? Simplify(DockNode? node)
    {
        switch (node)
        {
            case null:
            case DockEmpty:
                return null;
            case DockGroup g:
                return g.Tabs.Count == 0 ? null : g;
            case DockSplit s:
                var first = Simplify(s.First);
                var second = Simplify(s.Second);
                if (first is null && second is null) return null;
                if (first is null) return second;
                if (second is null) return first;
                s.First = first;
                s.Second = second;
                return s;
            default:
                return node;
        }
    }

    sealed class DockEventsImpl : IDockEvents
    {
        public event EventHandler<LayoutChangedEventArgs>? LayoutChanged;
        public event EventHandler<PanelActivatedEventArgs>? PanelActivated;
        public event EventHandler<DockDragEventArgs>? DragStarted;
        public event EventHandler<DockDragEventArgs>? DragCompleted;
        public event EventHandler<DockDragEventArgs>? DragCancelled;

        internal void RaiseLayoutChanged(LayoutChangedEventArgs e) => LayoutChanged?.Invoke(this, e);
        internal void RaisePanelActivated(PanelActivatedEventArgs e) => PanelActivated?.Invoke(this, e);
        internal void RaiseDragStarted(DockDragEventArgs e) => DragStarted?.Invoke(this, e);
        internal void RaiseDragCompleted(DockDragEventArgs e) => DragCompleted?.Invoke(this, e);
        internal void RaiseDragCancelled(DockDragEventArgs e) => DragCancelled?.Invoke(this, e);
    }

    sealed class DockCommandScopeImpl : IDockCommandScope
    {
        public bool IsInScope { get; internal set; }
        public string? ActiveGroupId { get; internal set; }
        public string? ActivePanelInstanceId { get; internal set; }
    }
}
