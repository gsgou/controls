namespace Shiny.Maui.Controls;

/// <summary>
/// Shows the page-edge <see cref="ProgressLine"/> from code, with no markup on the page and no
/// view-model property to bind.
/// </summary>
/// <remarks>
/// Runs are reference-counted. Two overlapping operations produce one line that stays up until the
/// slower of them finishes, rather than two lines or a line that disappears when the first one is
/// done.
/// </remarks>
public interface IProgressLineService
{
    /// <summary>
    /// Shows the line and returns the handle that drives it. Disposing the handle completes the run,
    /// so <c>using</c> is the normal way to scope one to a method.
    /// </summary>
    IProgressLineHandle Start(Action<ProgressLineConfig>? configure = null);

    /// <summary>Whether any run is currently active.</summary>
    bool IsRunning { get; }

    /// <summary>Completes every active run — for a navigation or a cancel that abandons them all.</summary>
    void CompleteAll();
}


/// <summary>One run of the progress line. Disposing it is the same as calling <see cref="Complete"/>.</summary>
public interface IProgressLineHandle : IDisposable
{
    /// <summary>How far this run has reported, from 0 to 1.</summary>
    double Progress { get; }

    /// <summary>Whether this run has already finished.</summary>
    bool IsComplete { get; }

    /// <summary>
    /// Reports progress, from 0 to 1. Values below the current one are ignored: a progress line that
    /// goes backwards reads as a fault, and the usual cause is a second reporter with a stale number.
    /// </summary>
    void SetProgress(double progress);

    /// <summary>Runs the line out to 100% and fades it away once every other run has also finished.</summary>
    void Complete();

    /// <summary>Ends the run without the completion sweep — the work was abandoned, not finished.</summary>
    void Cancel();
}
