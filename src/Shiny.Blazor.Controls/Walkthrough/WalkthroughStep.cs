using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Shiny.Blazor.Controls;

/// <summary>What a <see cref="WalkthroughStep"/> registers itself with.</summary>
public interface IWalkthroughHost
{
    void RegisterStep(WalkthroughStep step);
    void UnregisterStep(WalkthroughStep step);
    void NotifyStepChanged(WalkthroughStep step);
}


/// <summary>
/// One stop on a <see cref="Walkthrough"/>: what to highlight, what to say about it, and how that
/// arrives and leaves.
/// </summary>
/// <remarks>
/// Renders nothing itself — it registers with the walkthrough, which draws the step when the tour
/// reaches it. Declaring the steps together, in order, is the point: on a busy page an attached
/// per-element approach scatters the sequence across the markup, where nothing can see it as a whole.
/// </remarks>
public class WalkthroughStep : ComponentBase, IDisposable
{
    IWalkthroughHost? registeredWith;
    bool seen;

    // What the walkthrough draws each step from. Tracked so a parameter set that changed none of it
    // does not ask the host for another render — a child that notifies unconditionally from
    // OnParametersSet spins the renderer forever.
    string? lastTarget;
    string? lastTitle;
    string? lastText;
    bool lastVisible = true;

    /// <summary>
    /// Supplied by the walkthrough. Must be public — a private cascading parameter compiles, runs, and
    /// is silently skipped, which leaves the step orphaned and the tour a step short.
    /// </summary>
    [CascadingParameter] public IWalkthroughHost? Owner { get; set; }

    /// <summary>A CSS selector for the element to highlight. Leave it out for a centred, targetless step.</summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>Identifies the step for <c>GoToAsync</c> and for the walkthrough's current-step name.</summary>
    [Parameter] public string? Name { get; set; }

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? Text { get; set; }

    /// <summary>Your own markup in place of the title/text pair.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the step is part of the run. Bind it to drop steps that do not apply.</summary>
    [Parameter] public bool IsVisible { get; set; } = true;

    [Parameter] public WalkthroughDisplay Display { get; set; } = WalkthroughDisplay.Popover;

    /// <summary>Which side of the target to prefer. Auto picks the roomiest.</summary>
    [Parameter] public TooltipPlacement Placement { get; set; } = TooltipPlacement.Auto;

    /// <summary>
    /// Milliseconds to hold this step before advancing on its own. Zero waits for the user. This is
    /// dwell time, not animation time — see <see cref="DurationIn"/> for that.
    /// </summary>
    [Parameter] public int Duration { get; set; }

    /// <summary>How long the callout takes to arrive, in milliseconds.</summary>
    [Parameter] public int DurationIn { get; set; } = 260;

    /// <summary>How long it takes to leave.</summary>
    [Parameter] public int DurationOut { get; set; } = 180;

    [Parameter] public WalkthroughAnimation AnimationIn { get; set; } = WalkthroughAnimation.Zoom;

    [Parameter] public WalkthroughAnimation AnimationOut { get; set; } = WalkthroughAnimation.Fade;

    /// <summary>Overrides the walkthrough's cut-out shape for this step. Null inherits.</summary>
    [Parameter] public WalkthroughHighlight? Highlight { get; set; }

    /// <summary>Breathing room around the target inside the cut-out, in pixels. Null inherits.</summary>
    [Parameter] public double? HighlightPadding { get; set; }

    [Parameter] public double? HighlightCornerRadius { get; set; }

    /// <summary>
    /// Let clicks inside the cut-out reach the real control, so the user can try the thing being
    /// explained. The backdrop outside the hole still catches everything else.
    /// </summary>
    [Parameter] public bool AllowTargetInteraction { get; set; }

    /// <summary>
    /// Advance when the user actually uses the highlighted control. Implies
    /// <see cref="AllowTargetInteraction"/>, since the click has to reach the control to count.
    /// </summary>
    [Parameter] public bool AdvanceOnTargetClick { get; set; }

    /// <summary>Bring the target into view first. Null inherits from the walkthrough.</summary>
    [Parameter] public bool? ScrollToTarget { get; set; }

    /// <summary>Runs as the step arrives — for setting the screen up so the step makes sense.</summary>
    [Parameter] public EventCallback Entered { get; set; }

    /// <summary>Runs as the step leaves, in either direction.</summary>
    [Parameter] public EventCallback Left { get; set; }


    /// <summary>What the callout labels this step, falling back to its name.</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(this.Title) ? (this.Name ?? string.Empty) : this.Title!;


    protected override void OnInitialized()
    {
        this.registeredWith = this.Owner;
        this.registeredWith?.RegisterStep(this);
    }


    protected override void OnParametersSet()
    {
        // This runs again every time the host re-renders us — including the re-render a notification
        // itself causes — so notifying unconditionally is an infinite render loop that reads exactly
        // like a hung browser. Only speak up when something the host draws from has moved.
        if (this.HasHostRelevantChange())
            this.registeredWith?.NotifyStepChanged(this);
    }


    /// <summary>
    /// Whether anything the walkthrough reads off this step has changed. Every comparison has to run —
    /// <c>|=</c>, never <c>||</c> — because each also records the value it read.
    /// </summary>
    bool HasHostRelevantChange()
    {
        var changed = !this.seen;
        this.seen = true;

        changed |= Moved(ref this.lastTarget, this.Target);
        changed |= Moved(ref this.lastTitle, this.Title);
        changed |= Moved(ref this.lastText, this.Text);
        changed |= Moved(ref this.lastVisible, this.IsVisible);

        return changed;
    }


    static bool Moved(ref string? tracked, string? value)
    {
        if (string.Equals(tracked, value, StringComparison.Ordinal))
            return false;

        tracked = value;
        return true;
    }


    static bool Moved(ref bool tracked, bool value)
    {
        if (tracked == value)
            return false;

        tracked = value;
        return true;
    }


    // Deliberately empty: the walkthrough decides when, and where, this step is drawn.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }


    public void Dispose()
    {
        this.registeredWith?.UnregisterStep(this);
        GC.SuppressFinalize(this);
    }
}
