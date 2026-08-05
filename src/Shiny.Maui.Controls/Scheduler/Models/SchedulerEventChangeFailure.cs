namespace Shiny.Maui.Controls.Scheduler;

/// <summary>
/// Raised when <see cref="ISchedulerEventProvider.OnEventChanged"/> threw. The change has already
/// been reverted by the time this is surfaced - a silent revert is the worst failure mode here, so
/// the exception is always reported rather than swallowed.
/// </summary>
public class SchedulerEventChangeFailure(SchedulerEventChange change, Exception exception)
{
    public SchedulerEventChange Change { get; } = change;
    public Exception Exception { get; } = exception;
}
