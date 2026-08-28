using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Shiny.Blazor.Controls;

/// <summary>What a <see cref="SliderMark"/> registers itself with.</summary>
public interface ISliderMarkHost
{
    void RegisterMark(SliderMark mark);
    void UnregisterMark(SliderMark mark);
    void NotifyMarkChanged(SliderMark mark);
}


/// <summary>
/// A stop point on a <see cref="Slider"/>: a dot, tick or labelled bubble drawn at a fixed
/// <see cref="Value"/> on the track.
/// </summary>
/// <remarks>
/// Renders nothing itself — it registers with the slider, which draws every mark inside the track. That
/// is what keeps the mark markup inside the slider's own CSS isolation scope, and it lets each mark carry
/// its own text and colour. Set <see cref="Slider.SnapToMarks"/> to make the thumb come to rest on them.
/// </remarks>
public class SliderMark : ComponentBase, IDisposable
{
    ISliderMarkHost? registeredWith;
    bool seen;

    // What the slider draws each mark from. Tracked so a parameter set that changed none of it does not
    // ask the host for another render — a child that notifies unconditionally from OnParametersSet spins
    // the renderer forever.
    double lastValue;
    string? lastText;
    string? lastColor;
    string? lastTextColor;
    SliderMarkShape? lastShape;
    double lastSize = -1;
    bool lastVisible = true;

    /// <summary>
    /// Supplied by the slider. Must be public — a private cascading parameter compiles, runs, and is
    /// silently skipped, which leaves the mark orphaned and the track a stop short.
    /// </summary>
    [CascadingParameter] public ISliderMarkHost? Owner { get; set; }

    /// <summary>Where on the track the mark sits, in the slider's own units.</summary>
    [Parameter] public double Value { get; set; }

    /// <summary>
    /// The label. It is the caption under a <see cref="SliderMarkShape.Dot"/> or
    /// <see cref="SliderMarkShape.Line"/>, and the content of a <see cref="SliderMarkShape.Bubble"/>.
    /// Leave it out for an unlabelled tick.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Fill colour of the dot, tick or bubble. Null falls back to <see cref="Slider.MarkColor"/>.</summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>Colour of <see cref="Text"/>. Null falls back to <see cref="Slider.MarkTextColor"/>.</summary>
    [Parameter] public string? TextColor { get; set; }

    /// <summary>Overrides the slider's <see cref="Slider.MarkShape"/> for this one mark. Null inherits.</summary>
    [Parameter] public SliderMarkShape? Shape { get; set; }

    /// <summary>
    /// Dot diameter, or tick thickness, in px. The default, <c>-1</c>, inherits <see cref="Slider.MarkSize"/>.
    /// Ignored by <see cref="SliderMarkShape.Bubble"/>, which sizes to its text.
    /// </summary>
    [Parameter] public double Size { get; set; } = -1;

    /// <summary>Whether the mark is drawn. A hidden mark is not a snap target either.</summary>
    [Parameter] public bool IsVisible { get; set; } = true;


    protected override void OnInitialized()
    {
        this.registeredWith = this.Owner;
        this.registeredWith?.RegisterMark(this);
    }


    protected override void OnParametersSet()
    {
        // This runs again every time the host re-renders us — including the re-render a notification
        // itself causes — so notifying unconditionally is an infinite render loop that reads exactly
        // like a hung browser. Only speak up when something the host draws from has moved.
        if (this.HasHostRelevantChange())
            this.registeredWith?.NotifyMarkChanged(this);
    }


    /// <summary>
    /// Whether anything the slider reads off this mark has changed. Every comparison has to run —
    /// <c>|=</c>, never <c>||</c> — because each also records the value it read.
    /// </summary>
    bool HasHostRelevantChange()
    {
        var changed = !this.seen;
        this.seen = true;

        changed |= Moved(ref this.lastValue, this.Value);
        changed |= Moved(ref this.lastText, this.Text);
        changed |= Moved(ref this.lastColor, this.Color);
        changed |= Moved(ref this.lastTextColor, this.TextColor);
        changed |= Moved(ref this.lastSize, this.Size);
        changed |= Moved(ref this.lastVisible, this.IsVisible);

        if (this.lastShape != this.Shape)
        {
            this.lastShape = this.Shape;
            changed = true;
        }
        return changed;
    }


    static bool Moved(ref string? tracked, string? value)
    {
        if (string.Equals(tracked, value, StringComparison.Ordinal))
            return false;

        tracked = value;
        return true;
    }


    static bool Moved(ref double tracked, double value)
    {
        if (tracked.Equals(value))
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


    // Deliberately empty: the slider draws every mark inside its own track, which is what keeps the
    // markup inside the slider's CSS isolation scope.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }


    public void Dispose()
    {
        this.registeredWith?.UnregisterMark(this);
        GC.SuppressFinalize(this);
    }
}
