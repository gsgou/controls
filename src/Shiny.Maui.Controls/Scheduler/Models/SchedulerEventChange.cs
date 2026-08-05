namespace Shiny.Maui.Controls.Scheduler;

/// <summary>Whether a proposed change moves the whole event or drags one of its edges.</summary>
public enum SchedulerEventChangeKind
{
    /// <summary>Both edges shifted by the same amount - the duration is preserved.</summary>
    Move,

    /// <summary>The start edge moved; the end is fixed.</summary>
    ResizeStart,

    /// <summary>The end edge moved; the start is fixed.</summary>
    ResizeEnd
}


/// <summary>Describes a proposed change to an event's time, produced by a drag or resize.</summary>
public class SchedulerEventChange
{
    public required SchedulerEvent Event { get; init; }

    /// <summary>The event's Start before the gesture began.</summary>
    public required DateTimeOffset OriginalStart { get; init; }

    /// <summary>The event's End before the gesture began.</summary>
    public required DateTimeOffset OriginalEnd { get; init; }

    /// <summary>The proposed new Start (already snapped to <c>DragSnapMinutes</c>).</summary>
    public required DateTimeOffset NewStart { get; init; }

    /// <summary>The proposed new End (already snapped; never closer than <c>MinEventDuration</c>).</summary>
    public required DateTimeOffset NewEnd { get; init; }

    /// <summary>Move (both edges shifted) vs. resize (one edge moved).</summary>
    public required SchedulerEventChangeKind Kind { get; init; }
}
