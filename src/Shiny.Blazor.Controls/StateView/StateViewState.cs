using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Shiny.Blazor.Controls;

/// <summary>
/// What a <see cref="StateViewState"/> registers itself with. Implemented by <see cref="StateView"/>
/// and by <see cref="Wizard"/>, so a step does not need to know which of the two is hosting it.
/// </summary>
public interface IStateViewHost
{
    void RegisterState(StateViewState state);
    void UnregisterState(StateViewState state);
    void NotifyStateChanged(StateViewState state);
}


/// <summary>
/// One named branch of a <see cref="StateView"/>.
/// </summary>
/// <remarks>
/// This component renders nothing itself — it registers with the owning state view and hands over its
/// <see cref="ChildContent"/>, which the host renders only while this is the state on screen. That is
/// what makes the branches lazy: content for a state you never reach is never built.
/// </remarks>
public class StateViewState : ComponentBase, IDisposable
{
    IStateViewHost? registeredWith;

    /// <summary>
    /// Supplied by the owning host. Must be public — a private cascading parameter compiles and is
    /// then silently skipped, which leaves the state orphaned and invisible.
    /// </summary>
    [CascadingParameter] public IStateViewHost? Owner { get; set; }

    /// <summary>The value the host's current-state string is matched against.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>Rendered by the host while this is the current state.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        this.registeredWith = this.Owner;
        this.registeredWith?.RegisterState(this);
    }

    protected override void OnParametersSet() => this.registeredWith?.NotifyStateChanged(this);

    // Deliberately empty: the host decides when, and where, ChildContent is rendered.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    public virtual void Dispose()
    {
        this.registeredWith?.UnregisterState(this);
        GC.SuppressFinalize(this);
    }
}
