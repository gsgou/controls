using Microsoft.Maui.Controls;

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
    public static readonly BindableProperty InitialLayoutProperty = BindableProperty.Create(
        nameof(InitialLayout), typeof(DockRoot), typeof(DockHostView));

    public DockRoot? InitialLayout
    {
        get => (DockRoot?)GetValue(InitialLayoutProperty);
        set => SetValue(InitialLayoutProperty, value);
    }

    public static readonly BindableProperty IsLockedProperty = BindableProperty.Create(
        nameof(IsLocked), typeof(bool), typeof(DockHostView), false);

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public IDockEvents Events => events;
    public IDockCommandScope CommandScope => commandScope;

    readonly DockEventsImpl events = new();
    readonly DockCommandScopeImpl commandScope = new();

    public DockHostView()
    {
        // v0.1 skeleton — placeholder content until layout rendering lands.
        Content = new Grid
        {
            Children =
            {
                new Label
                {
                    Text = "DockHostView (v0.1 skeleton)",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };
    }

    public Task LoadAsync(DockRoot root, CancellationToken ct = default)
    {
        InitialLayout = root;
        return Task.CompletedTask;
    }

    public DockRoot Snapshot() => InitialLayout ?? new DockRoot();

    public Task ShowPanelAsync(string panelTypeId, DockArea preferredArea = DockArea.Left, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task HidePanelAsync(string panelInstanceId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ActivatePanelAsync(string panelInstanceId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ResetLayoutAsync(CancellationToken ct = default)
        => Task.CompletedTask;

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
