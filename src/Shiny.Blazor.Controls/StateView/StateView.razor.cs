using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls;

/// <summary>
/// Shows exactly one of several named branches, chosen by a string — the declarative form of the
/// <c>@if/else</c> ladder every app grows.
/// </summary>
/// <example>
/// <code>
/// &lt;StateView CurrentState="@state" Transition="StateTransition.Slide"&gt;
///     &lt;States&gt;
///         &lt;StateViewState Name="Loading"&gt;&lt;Spinner /&gt;&lt;/StateViewState&gt;
///         &lt;StateViewState Name="Loaded"&gt;&lt;Report /&gt;&lt;/StateViewState&gt;
///     &lt;/States&gt;
/// &lt;/StateView&gt;
/// </code>
/// </example>
public partial class StateView
{
    readonly List<StateViewState> states = new();

    StateViewState? current;
    int currentIndex = -1;
    int direction = 1;

    /// <summary>The states, as markup. Children are <c>StateViewState</c> components.</summary>
    [Parameter] public RenderFragment? States { get; set; }

    /// <summary>Alias for <see cref="States"/>, so the states can be declared without the wrapper tag.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Rendered when nothing matches and there is nothing to fall back to.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// Which state to show, matched against <c>StateViewState.Name</c> ordinally and
    /// case-insensitively. Empty or unmatched falls back to <see cref="DefaultState"/>, then to the
    /// first declared state.
    /// </summary>
    [Parameter] public string? CurrentState { get; set; }

    /// <summary>Fires when the state view settles on a different state (including its first).</summary>
    [Parameter] public EventCallback<string?> CurrentStateChanged { get; set; }

    /// <summary>Shown when <see cref="CurrentState"/> is empty or names a state that does not exist.</summary>
    [Parameter] public string? DefaultState { get; set; }

    /// <summary>How the incoming state animates in. Defaults to <see cref="StateTransition.Fade"/>.</summary>
    [Parameter] public StateTransition Transition { get; set; } = StateTransition.Fade;

    /// <summary>Transition length in milliseconds. Zero swaps instantly.</summary>
    [Parameter] public int TransitionDuration { get; set; } = 200;

    /// <summary>Extra classes for the root element.</summary>
    [Parameter] public string? CssClass { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    // A splat clobbers attributes the component wrote itself, so a consumer passing style= would drop
    // the transition-duration custom property. Pull class/style out of the splat and merge them.
    IDictionary<string, object>? ExtraAttributes { get; set; }
    string? UserClass { get; set; }
    string? UserStyle { get; set; }


    /// <summary>The state on screen, or null when nothing matched.</summary>
    public StateViewState? Current => this.current;

    /// <summary>Index of <see cref="Current"/> among the declared states, or -1.</summary>
    public int CurrentIndex => this.currentIndex;

    /// <summary>The declared states, in markup order.</summary>
    public IReadOnlyList<StateViewState> StateItems => this.states;


    /// <summary>Show the named state. Returns false when no state carries that name.</summary>
    public bool GoTo(string name)
    {
        if (this.Find(name) == null)
            return false;

        return this.SetCurrent(name);
    }

    /// <summary>Show the state at <paramref name="index"/>. Returns false when out of range.</summary>
    public bool GoTo(int index)
        => index >= 0 && index < this.states.Count && this.SetCurrent(this.states[index].Name);


    protected override void OnParametersSet()
    {
        this.ExtraAttributes = LayoutAttributes.Split(this.AdditionalAttributes, out var userClass, out var userStyle);
        this.UserClass = userClass;
        this.UserStyle = userStyle;

        this.Resolve();
    }


    // -------------------------------------------------------------------------------------------
    // Host
    // -------------------------------------------------------------------------------------------

    void IStateViewHost.RegisterState(StateViewState state)
    {
        if (this.states.Contains(state))
            return;

        this.states.Add(state);

        // Registration happens while this component is already rendering its children, so the newly
        // resolvable state cannot be shown in this pass - queue another one.
        _ = this.InvokeAsync(() =>
        {
            this.Resolve();
            this.StateHasChanged();
        });
    }

    void IStateViewHost.UnregisterState(StateViewState state)
    {
        if (!this.states.Remove(state))
            return;

        if (ReferenceEquals(state, this.current))
            this.current = null;

        _ = this.InvokeAsync(() =>
        {
            this.Resolve();
            this.StateHasChanged();
        });
    }

    void IStateViewHost.NotifyStateChanged(StateViewState state)
    {
        // A state's Name can arrive after registration (or change), which can move which one wins.
        if (this.Resolve())
            this.StateHasChanged();
    }


    // -------------------------------------------------------------------------------------------
    // Resolution + rendering
    // -------------------------------------------------------------------------------------------

    StateViewState? Find(string? name)
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

    bool Resolve()
    {
        var next = this.Find(this.CurrentState)
                   ?? this.Find(this.DefaultState)
                   ?? this.states.FirstOrDefault();

        if (ReferenceEquals(next, this.current))
            return false;

        var nextIndex = next == null ? -1 : this.states.IndexOf(next);
        this.direction = nextIndex >= this.currentIndex ? 1 : -1;
        this.current = next;
        this.currentIndex = nextIndex;

        if (next?.Name != this.CurrentState && this.CurrentStateChanged.HasDelegate)
        {
            this.CurrentState = next?.Name;
            _ = this.CurrentStateChanged.InvokeAsync(next?.Name);
        }

        return true;
    }

    bool SetCurrent(string? name)
    {
        this.CurrentState = name;
        var changed = this.Resolve();

        if (this.CurrentStateChanged.HasDelegate)
            _ = this.CurrentStateChanged.InvokeAsync(name);

        if (changed)
            this.StateHasChanged();

        return true;
    }

    string RootStyle => LayoutAttributes.Append(
        $"--shiny-stateview-duration:{Math.Max(0, this.TransitionDuration)}ms;",
        this.UserStyle
    );

    string PaneClass => this.TransitionDuration <= 0
        ? string.Empty
        : this.Transition switch
        {
            StateTransition.Fade => "is-fade",
            StateTransition.Slide => this.direction >= 0 ? "is-slide-left" : "is-slide-right",
            StateTransition.SlideLeft => "is-slide-left",
            StateTransition.SlideRight => "is-slide-right",
            StateTransition.SlideUp => "is-slide-up",
            StateTransition.SlideDown => "is-slide-down",
            StateTransition.Scale => "is-scale",
            _ => string.Empty
        };
}
