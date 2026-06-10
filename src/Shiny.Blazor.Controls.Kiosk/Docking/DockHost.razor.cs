using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Shiny.Blazor.Controls.Kiosk.Docking;

public partial class DockHost : ComponentBase, IDockHost, IAsyncDisposable
{
    DockRoot? layout;
    string? pristineJson;
    bool loadAttempted;
    bool lastLocked;
    DockableContentRegistry? registry;
    ElementReference hostRef;
    IJSObjectReference? module;
    DotNetObjectReference<DockHost>? dotnetRef;
    CancellationTokenSource? saveCts;
    readonly Dictionary<string, RenderFragment> fragments = new();
    readonly DockEventsImpl events = new();
    readonly DockCommandScopeImpl commandScope = new();

    [Inject] IServiceProvider Services { get; set; } = null!;
    [Inject] IJSRuntime JS { get; set; } = null!;

    [Parameter] public DockRoot? InitialLayout { get; set; }
    [Parameter] public bool IsLocked { get; set; }
    [Parameter] public IDockLayoutStore? LayoutStore { get; set; }

    [Parameter] public string? BackgroundColor { get; set; }

    string HostStyle => string.IsNullOrEmpty(BackgroundColor)
        ? string.Empty
        : $"--shiny-dock-host-bg: {BackgroundColor};";

    public IDockEvents Events => events;
    public IDockCommandScope CommandScope => commandScope;

    protected override void OnInitialized()
        => registry = Services.GetService<DockableContentRegistry>();

    protected override async Task OnParametersSetAsync()
    {
        if (layout is null && !loadAttempted)
        {
            loadAttempted = true;
            DockRoot? stored = null;
            if (LayoutStore is not null)
            {
                try { stored = await LayoutStore.LoadAsync(); }
                catch { /* fall back to InitialLayout on any store failure */ }
            }
            var root = stored ?? InitialLayout;
            if (root is not null)
                await LoadCoreAsync(root, captureAsPristine: stored is null, CancellationToken.None);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            lastLocked = IsLocked;
            module = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Shiny.Blazor.Controls.Kiosk/docking.js");
            dotnetRef = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("init", hostRef, dotnetRef, new { locked = IsLocked });
        }
        else if (module is not null)
        {
            if (lastLocked != IsLocked)
            {
                lastLocked = IsLocked;
                await module.InvokeVoidAsync("setLocked", hostRef, IsLocked);
            }
            await module.InvokeVoidAsync("refreshFloating", hostRef);
        }
    }

    // ----------------------------------------------------------------- IDockHost
    public Task LoadAsync(DockRoot root, CancellationToken ct = default)
    {
        loadAttempted = true;
        return LoadCoreAsync(root, captureAsPristine: true, ct);
    }

    async Task LoadCoreAsync(DockRoot root, bool captureAsPristine, CancellationToken ct)
    {
        root = Migrate(root);
        var json = DockSerialization.Serialize(root);
        if (captureAsPristine || pristineJson is null)
            pristineJson = InitialLayout is not null ? DockSerialization.Serialize(Migrate(InitialLayout)) : json;
        layout = DockSerialization.Deserialize(json)!;
        fragments.Clear();
        await ResolveFragmentsAsync(ct);
        events.RaiseLayoutChanged(new LayoutChangedEventArgs { Snapshot = Snapshot(), Reason = "load" });
        StateHasChanged();
    }

    DockRoot Migrate(DockRoot root)
    {
        var migrators = Services.GetService<IEnumerable<IDockLayoutMigrator>>()?.ToList();
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

        var existing = AllGroups()
            .SelectMany(g => g.Tabs)
            .FirstOrDefault(t => t.PanelTypeId == panelTypeId);
        if (existing is not null)
        {
            await ActivatePanelAsync(existing.PanelInstanceId, ct);
            return;
        }

        var tab = new DockTab { PanelTypeId = panelTypeId };
        await ResolveFragmentAsync(tab, ct);

        var win = layout.MainWindow;
        var rail = preferredArea switch
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
            rail = new DockGroup { Tabs = { tab } };
            switch (preferredArea)
            {
                case DockArea.Top: win.TopRail = rail; break;
                case DockArea.Right: win.RightRail = rail; break;
                case DockArea.Bottom: win.BottomRail = rail; break;
                default: win.LeftRail = rail; break;
            }
        }

        win.ActivePanelId = tab.PanelInstanceId;
        OnLayoutMutated("show-panel");
        events.RaisePanelActivated(new PanelActivatedEventArgs
        {
            PanelInstanceId = tab.PanelInstanceId,
            PanelTypeId = tab.PanelTypeId
        });
        StateHasChanged();
    }

    public Task HidePanelAsync(string panelInstanceId, CancellationToken ct = default)
    {
        if (layout is null) return Task.CompletedTask;

        foreach (var group in AllGroups())
        {
            var tab = group.Tabs.FirstOrDefault(t => t.PanelInstanceId == panelInstanceId);
            if (tab is null) continue;

            group.Tabs.Remove(tab);
            group.ActiveTabIndex = Math.Clamp(group.ActiveTabIndex, 0, Math.Max(0, group.Tabs.Count - 1));
            fragments.Remove(panelInstanceId);
            SimplifyAll();
            if (layout.MainWindow.ActivePanelId == panelInstanceId)
                layout.MainWindow.ActivePanelId = null;

            OnLayoutMutated("hide-panel");
            StateHasChanged();
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
        fragments.Clear();
        await ResolveFragmentsAsync(ct);
        OnLayoutMutated("reset");
        StateHasChanged();
    }

    public Task SetRailCollapsedAsync(DockArea area, bool collapsed, CancellationToken ct = default)
    {
        if (layout is null) return Task.CompletedTask;
        var rails = layout.MainWindow.CollapsedRails;
        if (collapsed && !rails.Contains(area))
            rails.Add(area);
        else if (!collapsed)
            rails.Remove(area);
        else
            return Task.CompletedTask;
        OnLayoutMutated(collapsed ? "rail-collapse" : "rail-expand");
        StateHasChanged();
        return Task.CompletedTask;
    }

    bool IsRailCollapsed(DockArea area)
        => layout?.MainWindow.CollapsedRails.Contains(area) == true;

    async Task ExpandRailToPanelAsync(DockArea area, string panelInstanceId)
    {
        await SetRailCollapsedAsync(area, false);
        await ActivatePanelAsync(panelInstanceId);
    }

    // ----------------------------------------------------------------- JS callbacks
    [JSInvokable]
    public void OnSplitterRatioChangedJs(string splitId, double ratio)
    {
        if (IsLocked || layout is null) return;
        var split = AllSplits().FirstOrDefault(s => s.RuntimeId == splitId);
        if (split is null) return;
        split.Ratio = Math.Clamp(ratio, 0.08, 0.92);
        OnLayoutMutated("splitter");
        StateHasChanged();
    }

    [JSInvokable]
    public void OnDragStartedJs(string instanceId)
    {
        if (IsLocked) return;
        var tab = FindTab(instanceId);
        if (tab is null) return;
        events.RaiseDragStarted(new DockDragEventArgs { SourcePanelInstanceId = instanceId });
    }

    [JSInvokable]
    public void OnDragCancelledJs(string instanceId)
        => events.RaiseDragCancelled(new DockDragEventArgs { SourcePanelInstanceId = instanceId });

    [JSInvokable]
    public async Task OnTabDroppedJs(string instanceId, string? targetGroupId, string zoneName, int index, double x, double y)
    {
        if (IsLocked || layout is null) return;
        if (!Enum.TryParse<DockZone>(zoneName, out var zone)) return;

        var sourceGroup = AllGroups().FirstOrDefault(g => g.Tabs.Any(t => t.PanelInstanceId == instanceId));
        var tab = sourceGroup?.Tabs.First(t => t.PanelInstanceId == instanceId);
        if (sourceGroup is null || tab is null) return;

        var targetGroup = targetGroupId is null
            ? null
            : AllGroups().FirstOrDefault(g => g.GroupId == targetGroupId);

        switch (zone)
        {
            case DockZone.TabStrip when targetGroup is not null:
            {
                var oldIndex = sourceGroup.Tabs.IndexOf(tab);
                sourceGroup.Tabs.Remove(tab);
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
            case DockZone.Left or DockZone.Right or DockZone.Top or DockZone.Bottom when targetGroup is not null:
            {
                // splitting yourself when you're the only tab is a no-op
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
            case DockZone.TearOff:
            {
                if (ReferenceEquals(sourceGroup, FindFloatingGroup(sourceGroup)) && sourceGroup.Tabs.Count == 1)
                {
                    // dragging the only tab of a floating window: just move the window
                    var fw = layout.FloatingWindows.FirstOrDefault(w => GroupsIn(w.DocumentArea).Contains(sourceGroup));
                    if (fw is not null)
                    {
                        var b = fw.Bounds ?? new DockRect(x, y, 360, 260);
                        fw.Bounds = new DockRect(x, y, b.Width, b.Height);
                        break;
                    }
                }
                sourceGroup.Tabs.Remove(tab);
                layout.FloatingWindows.Add(new DockWindowState
                {
                    Bounds = new DockRect(x, y, 360, 260),
                    DocumentArea = new DockGroup { Tabs = { tab }, ActiveTabIndex = 0 }
                });
                break;
            }
            default:
                return;
        }

        sourceGroup.ActiveTabIndex = Math.Clamp(sourceGroup.ActiveTabIndex, 0, Math.Max(0, sourceGroup.Tabs.Count - 1));
        SimplifyAll();
        OnLayoutMutated("drag-drop");
        events.RaiseDragCompleted(new DockDragEventArgs
        {
            SourcePanelInstanceId = instanceId,
            TargetGroupId = targetGroupId,
            TargetZone = zone
        });
        await ActivatePanelAsync(instanceId);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnFloatingMovedJs(int index, double x, double y)
    {
        if (IsLocked || layout is null || index < 0 || index >= layout.FloatingWindows.Count) return;
        var fw = layout.FloatingWindows[index];
        var b = fw.Bounds ?? new DockRect(x, y, 360, 260);
        // no-change guard: re-observation echoes from JS must not loop into
        // LayoutChanged → render → re-observe → echo, which starves the debounced save
        if (Math.Abs(b.X - x) < 0.5 && Math.Abs(b.Y - y) < 0.5) return;
        fw.Bounds = new DockRect(x, y, b.Width, b.Height);
        OnLayoutMutated("float-move");
    }

    [JSInvokable]
    public void OnFloatingResizedJs(int index, double width, double height)
    {
        if (IsLocked || layout is null || index < 0 || index >= layout.FloatingWindows.Count) return;
        var fw = layout.FloatingWindows[index];
        var b = fw.Bounds ?? new DockRect(60, 40, width, height);
        if (Math.Abs(b.Width - width) < 0.5 && Math.Abs(b.Height - height) < 0.5) return;
        fw.Bounds = new DockRect(b.X, b.Y, width, height);
        OnLayoutMutated("float-resize");
    }

    // ----------------------------------------------------------------- floating
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

        OnLayoutMutated("dock-floating");
        if (tabs.Count > 0)
            await ActivatePanelAsync(tabs[^1].PanelInstanceId);
        StateHasChanged();
    }

    Task CloseFloatingAsync(int index)
    {
        if (layout is null || index < 0 || index >= layout.FloatingWindows.Count) return Task.CompletedTask;
        var fw = layout.FloatingWindows[index];
        foreach (var t in GroupsIn(fw.DocumentArea).SelectMany(g => g.Tabs))
            fragments.Remove(t.PanelInstanceId);
        layout.FloatingWindows.RemoveAt(index);
        OnLayoutMutated("close-floating");
        StateHasChanged();
        return Task.CompletedTask;
    }

    string FloatTitle(DockWindowState fw)
    {
        var group = GroupsIn(fw.DocumentArea).FirstOrDefault();
        if (group is null || group.Tabs.Count == 0) return "Floating";
        var active = group.Tabs[Math.Clamp(group.ActiveTabIndex, 0, group.Tabs.Count - 1)];
        return GetTabTitle(active);
    }

    static string FloatStyle(DockRect b) => string.Create(CultureInfo.InvariantCulture,
        $"left:{b.X:0.##}px;top:{b.Y:0.##}px;width:{b.Width:0.##}px;height:{b.Height:0.##}px;");

    // ----------------------------------------------------------------- internals
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
        var token = saveCts.Token;
        _ = DebouncedSaveAsync(snapshot, debounce, token);
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

    void ActivateTab(DockGroup group, int index)
    {
        if (index < 0 || index >= group.Tabs.Count) return;
        group.ActiveTabIndex = index;
        group.FocusHistory.Remove(index);
        group.FocusHistory.Add(index);

        var tab = group.Tabs[index];
        if (layout is not null)
            layout.MainWindow.ActivePanelId = tab.PanelInstanceId;

        commandScope.IsInScope = true;
        commandScope.ActiveGroupId = group.GroupId;
        commandScope.ActivePanelInstanceId = tab.PanelInstanceId;

        events.RaisePanelActivated(new PanelActivatedEventArgs
        {
            PanelInstanceId = tab.PanelInstanceId,
            PanelTypeId = tab.PanelTypeId
        });
        StateHasChanged();
    }

    string GetTabTitle(DockTab tab)
        => registry?.Resolve(tab.PanelTypeId)?.DisplayName ?? tab.PanelTypeId;

    static string FlexStyle(double ratio)
        => $"flex:{ratio.ToString("0.####", CultureInfo.InvariantCulture)} 1 0%;";

    async Task ResolveFragmentsAsync(CancellationToken ct)
    {
        foreach (var group in AllGroups())
            foreach (var tab in group.Tabs)
                await ResolveFragmentAsync(tab, ct);
    }

    async Task ResolveFragmentAsync(DockTab tab, CancellationToken ct)
    {
        if (fragments.ContainsKey(tab.PanelInstanceId)) return;
        var factory = registry?.Resolve(tab.PanelTypeId);
        if (factory is null) return; // renders the "unknown panel" placeholder
        fragments[tab.PanelInstanceId] = await factory.CreateAsync(tab.PanelInstanceId, ct);
    }

    DockTab? FindTab(string instanceId)
        => AllGroups().SelectMany(g => g.Tabs).FirstOrDefault(t => t.PanelInstanceId == instanceId);

    DockGroup? FindFloatingGroup(DockGroup group)
        => layout?.FloatingWindows.SelectMany(w => GroupsIn(w.DocumentArea)).FirstOrDefault(g => ReferenceEquals(g, group));

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

    IEnumerable<DockSplit> AllSplits()
    {
        if (layout is null) yield break;
        var win = layout.MainWindow;
        var roots = new List<DockNode?> { win.DocumentArea, win.LeftRail, win.TopRail, win.RightRail, win.BottomRail };
        roots.AddRange(layout.FloatingWindows.Select(w => (DockNode?)w.DocumentArea));
        foreach (var node in roots)
            foreach (var split in SplitsIn(node))
                yield return split;
    }

    static IEnumerable<DockSplit> SplitsIn(DockNode? node)
    {
        if (node is not DockSplit s) yield break;
        yield return s;
        foreach (var c in SplitsIn(s.First)) yield return c;
        foreach (var c in SplitsIn(s.Second)) yield return c;
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

    public async ValueTask DisposeAsync()
    {
        saveCts?.Cancel();
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync("dispose", hostRef);
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        dotnetRef?.Dispose();
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
