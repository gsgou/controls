using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls;

/// <summary>
/// Shows exactly one of several named branches, chosen by a string. It is the declarative form of
/// the <c>IsVisible</c> ladder every app grows — bind <see cref="CurrentState"/> to a view-model
/// property and the matching <see cref="StateViewState"/> is the one on screen.
/// </summary>
/// <example>
/// <code language="xaml">
/// &lt;shiny:StateView CurrentState="{Binding CurrentState}" Transition="Slide"&gt;
///     &lt;shiny:StateViewState Name="Loading"&gt;
///         &lt;ActivityIndicator IsRunning="True" /&gt;
///     &lt;/shiny:StateViewState&gt;
///     &lt;shiny:StateViewState Name="Loaded"&gt;
///         &lt;Label Text="Done" /&gt;
///     &lt;/shiny:StateViewState&gt;
/// &lt;/shiny:StateView&gt;
/// </code>
/// </example>
[ContentProperty(nameof(States))]
public class StateView : ContentView
{
    readonly ObservableCollection<StateViewState> states = new();
    readonly List<StateViewState> subscribed = new();
    readonly Grid root;
    readonly ContentView hostA;
    readonly ContentView hostB;

    bool showingA;
    bool initialized;
    int transitionToken;

    public StateView()
    {
        this.hostA = new ContentView();
        this.hostB = new ContentView { IsVisible = false };

        // Both hosts occupy the same cell so one can animate over the other. hostB is added last and
        // therefore paints on top; every transition fades the outgoing host as well as moving it, so
        // the fixed stacking order never reads as a glitch when the incoming host is the lower one.
        this.root = new Grid();
        this.root.Children.Add(this.hostA);
        this.root.Children.Add(this.hostB);

        this.states.CollectionChanged += this.OnStatesChanged;

        base.Content = this.root;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(StateView));
    }


    public static readonly BindableProperty CurrentStateProperty = BindableProperty.Create(
        nameof(CurrentState), typeof(string), typeof(StateView), null, BindingMode.TwoWay,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(StateView), () =>
            ((StateView)b).Sync(animate: true)));

    public static readonly BindableProperty DefaultStateProperty = BindableProperty.Create(
        nameof(DefaultState), typeof(string), typeof(StateView), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(StateView), () =>
            ((StateView)b).Sync(animate: false)));

    public static readonly BindableProperty TransitionProperty = BindableProperty.Create(
        nameof(Transition), typeof(StateTransition), typeof(StateView), StateTransition.Fade);

    public static readonly BindableProperty TransitionDurationProperty = BindableProperty.Create(
        nameof(TransitionDuration), typeof(uint), typeof(StateView), 200u);

    public static readonly BindableProperty TransitionEasingProperty = BindableProperty.Create(
        nameof(TransitionEasing), typeof(Easing), typeof(StateView), Easing.CubicOut);

    public static readonly BindableProperty CacheContentProperty = BindableProperty.Create(
        nameof(CacheContent), typeof(bool), typeof(StateView), true);

    public static readonly BindableProperty EmptyViewProperty = BindableProperty.Create(
        nameof(EmptyView), typeof(View), typeof(StateView), null,
        propertyChanged: (b, o, n) => StyleGuard.WhenReady(b, typeof(StateView), () =>
            ((StateView)b).Sync(animate: false)));

    public static readonly BindableProperty StateChangedCommandProperty = BindableProperty.Create(
        nameof(StateChangedCommand), typeof(ICommand), typeof(StateView), null);


    /// <summary>
    /// The name of the state to show. Matched against <see cref="StateViewState.Name"/> ordinally and
    /// case-insensitively. Empty or unmatched falls back to <see cref="DefaultState"/>, then to the
    /// first state, then to <see cref="EmptyView"/>.
    /// </summary>
    public string? CurrentState
    {
        get => (string?)this.GetValue(CurrentStateProperty);
        set => this.SetValue(CurrentStateProperty, value);
    }

    /// <summary>Shown when <see cref="CurrentState"/> is empty or names a state that does not exist.</summary>
    public string? DefaultState
    {
        get => (string?)this.GetValue(DefaultStateProperty);
        set => this.SetValue(DefaultStateProperty, value);
    }

    /// <summary>How the swap is animated. Defaults to <see cref="StateTransition.Fade"/>.</summary>
    public StateTransition Transition
    {
        get => (StateTransition)this.GetValue(TransitionProperty);
        set => this.SetValue(TransitionProperty, value);
    }

    /// <summary>Transition length in milliseconds. Zero swaps instantly.</summary>
    public uint TransitionDuration
    {
        get => (uint)this.GetValue(TransitionDurationProperty);
        set => this.SetValue(TransitionDurationProperty, value);
    }

    public Easing TransitionEasing
    {
        get => (Easing)this.GetValue(TransitionEasingProperty);
        set => this.SetValue(TransitionEasingProperty, value);
    }

    /// <summary>
    /// Keep a view built from <see cref="StateViewState.ContentTemplate"/> alive after its state is
    /// left, so returning to it is instant and any scroll position or entry text survives. Turn it
    /// off to rebuild the branch — and reset it — every time it is entered.
    /// </summary>
    public bool CacheContent
    {
        get => (bool)this.GetValue(CacheContentProperty);
        set => this.SetValue(CacheContentProperty, value);
    }

    /// <summary>Shown when nothing matches and there are no states to fall back to.</summary>
    public View? EmptyView
    {
        get => (View?)this.GetValue(EmptyViewProperty);
        set => this.SetValue(EmptyViewProperty, value);
    }

    /// <summary>Invoked with the new state name after every change.</summary>
    public ICommand? StateChangedCommand
    {
        get => (ICommand?)this.GetValue(StateChangedCommandProperty);
        set => this.SetValue(StateChangedCommandProperty, value);
    }

    /// <summary>The declared states, in markup order.</summary>
    public IList<StateViewState> States => this.states;

    /// <summary>The state currently on screen, or null when nothing matched.</summary>
    public StateViewState? CurrentStateView { get; private set; }

    /// <summary>Index of <see cref="CurrentStateView"/> within <see cref="States"/>, or -1.</summary>
    public int CurrentStateIndex => this.CurrentStateView == null ? -1 : this.states.IndexOf(this.CurrentStateView);

    /// <summary>Raised after the new state is on screen.</summary>
    public event EventHandler<StateChangedEventArgs>? StateChanged;


    /// <summary>Show the named state. Returns false (and changes nothing) when no state has that name.</summary>
    public bool GoTo(string name)
    {
        if (this.FindState(name) == null)
            return false;

        this.CurrentState = name;
        return true;
    }

    /// <summary>Show the state at <paramref name="index"/>. Returns false when out of range.</summary>
    public bool GoTo(int index)
    {
        if (index < 0 || index >= this.states.Count)
            return false;

        this.CurrentState = this.states[index].Name;
        return true;
    }


    // -------------------------------------------------------------------------------------------
    // Resolution
    // -------------------------------------------------------------------------------------------

    StateViewState? FindState(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (var state in this.states)
        {
            if (string.Equals(state.Name, name, StringComparison.OrdinalIgnoreCase))
                return state;
        }
        return null;
    }

    /// <summary>Which state a given name lands on, after the fallback chain.</summary>
    StateViewState? Resolve(string? name)
        => this.FindState(name)
           ?? this.FindState(this.DefaultState)
           ?? (this.states.Count > 0 ? this.states[0] : null);


    void OnStatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // A Reset carries neither OldItems nor NewItems, so the subscriptions are tracked separately
        // rather than inferred from the event - otherwise Clear() leaves every state still wired up.
        foreach (var state in this.subscribed)
            state.Changed -= this.OnStateContentChanged;
        this.subscribed.Clear();

        foreach (var state in this.states)
        {
            state.Changed += this.OnStateContentChanged;
            this.subscribed.Add(state);
            SetInheritedBindingContext(state, this.BindingContext);
        }

        if (this.CurrentStateView != null && !this.states.Contains(this.CurrentStateView))
            this.CurrentStateView = null;

        this.Sync(animate: false);
    }

    void OnStateContentChanged(object? sender, EventArgs e)
    {
        // Only a change to the state on screen needs re-hosting; the rest are picked up when entered.
        if (ReferenceEquals(sender, this.CurrentStateView))
            this.Sync(animate: false, force: true);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        foreach (var state in this.states)
            SetInheritedBindingContext(state, this.BindingContext);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // The first state is hosted without animation, but only once there is a tree to host it in.
        if (!this.initialized)
            this.Sync(animate: false);
    }


    // -------------------------------------------------------------------------------------------
    // Hosting + transition
    // -------------------------------------------------------------------------------------------

    void Sync(bool animate, bool force = false)
    {
        var next = this.Resolve(this.CurrentState);
        if (!force && this.initialized && ReferenceEquals(next, this.CurrentStateView))
            return;

        var previous = this.CurrentStateView;
        var previousName = previous?.Name;
        var direction = Direction(previous, next);

        var outgoing = this.showingA ? this.hostA : this.hostB;
        var incoming = this.showingA ? this.hostB : this.hostA;

        var view = next?.ResolveContent() ?? this.EmptyView;

        // A state can be re-entered with the same view instance still parented in the outgoing host
        // (force sync, or EmptyView on both sides); unparent it first or the assignment is dropped.
        if (view != null && ReferenceEquals(outgoing.Content, view))
            outgoing.Content = null;

        incoming.Content = view;
        this.CurrentStateView = next;
        this.showingA = !this.showingA;

        var first = !this.initialized;
        this.initialized = true;

        this.BeginTransition(outgoing, incoming, animate && !first, direction);

        if (previous != null && !this.CacheContent)
            previous.ReleaseTemplatedContent();

        if (!first || next != null)
        {
            var args = new StateChangedEventArgs(previousName, next?.Name);
            this.StateChanged?.Invoke(this, args);

            var command = this.StateChangedCommand;
            if (command?.CanExecute(next?.Name) == true)
                command.Execute(next?.Name);
        }
    }

    int Direction(StateViewState? from, StateViewState? to)
    {
        if (from == null || to == null)
            return 1;

        var fromIndex = this.states.IndexOf(from);
        var toIndex = this.states.IndexOf(to);
        return toIndex >= fromIndex ? 1 : -1;
    }

    void BeginTransition(ContentView outgoing, ContentView incoming, bool animate, int direction)
    {
        var token = ++this.transitionToken;
        incoming.IsVisible = true;

        var transition = this.Transition;
        var duration = this.TransitionDuration;

        // No handler means nothing is on screen yet (and, in a headless host, no animation manager
        // at all) - there is nothing to animate between, so land on the final frame directly.
        if (!animate || transition == StateTransition.None || duration == 0 || this.Handler == null)
        {
            Reset(incoming);
            outgoing.Content = null;
            outgoing.IsVisible = false;
            Reset(outgoing);
            return;
        }

        _ = this.AnimateAsync(token, outgoing, incoming, transition, direction, duration);
    }

    async Task AnimateAsync(int token, ContentView outgoing, ContentView incoming, StateTransition transition, int direction, uint duration)
    {
        var easing = this.TransitionEasing ?? Easing.CubicOut;
        var width = this.Width > 0 ? this.Width : 320d;
        var height = this.Height > 0 ? this.Height : 480d;

        var (dx, dy, scale) = transition switch
        {
            StateTransition.Slide => (direction * width, 0d, 1d),
            StateTransition.SlideLeft => (width, 0d, 1d),
            StateTransition.SlideRight => (-width, 0d, 1d),
            StateTransition.SlideUp => (0d, height, 1d),
            StateTransition.SlideDown => (0d, -height, 1d),
            StateTransition.Scale => (0d, 0d, 0.92d),
            _ => (0d, 0d, 1d)
        };

        incoming.Opacity = 0;
        incoming.TranslationX = dx;
        incoming.TranslationY = dy;
        incoming.Scale = scale;

        try
        {
            var animations = new List<Task>
            {
                incoming.FadeToAsync(1, duration, easing),
                outgoing.FadeToAsync(0, duration, easing)
            };

            if (dx != 0 || dy != 0)
            {
                animations.Add(incoming.TranslateToAsync(0, 0, duration, easing));
                animations.Add(outgoing.TranslateToAsync(-dx, -dy, duration, easing));
            }

            if (scale != 1d)
            {
                animations.Add(incoming.ScaleToAsync(1, duration, easing));
                animations.Add(outgoing.ScaleToAsync(scale, duration, easing));
            }

            await Task.WhenAll(animations).ConfigureAwait(true);
        }
        catch (Exception)
        {
            // A host torn down mid-flight (page popped, handler disconnected) aborts the animation;
            // the finishing frame below still runs so the tree is never left half-transitioned.
        }

        // A newer transition already owns these hosts - it will do its own cleanup.
        if (token != this.transitionToken)
            return;

        Reset(incoming);
        outgoing.Content = null;
        outgoing.IsVisible = false;
        Reset(outgoing);
    }

    static void Reset(ContentView host)
    {
        host.Opacity = 1;
        host.TranslationX = 0;
        host.TranslationY = 0;
        host.Scale = 1;
    }
}
