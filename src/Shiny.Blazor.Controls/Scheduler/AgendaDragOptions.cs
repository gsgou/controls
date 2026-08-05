namespace Shiny.Blazor.Controls.Scheduler;

/// <summary>How often <see cref="ISchedulerEventProvider.CanChangeEventTo"/> is consulted during a drag.</summary>
public enum AgendaDragValidationMode
{
    /// <summary>
    /// The default. The provider is asked once, when the gesture completes - no interop happens
    /// while the pointer is moving.
    /// </summary>
    OnCommit,

    /// <summary>
    /// The provider is asked every time the drag crosses a snap boundary, so a rejected position can
    /// be shown live. One interop round trip per boundary; expect visible lag on WASM.
    /// </summary>
    PerPosition
}


/// <summary>
/// The drag settings handed to <c>scheduler-agenda.js</c>.
/// </summary>
/// <remarks>
/// A named DTO with primitive members only: anonymous types break trimmed/published WASM
/// (ConstructorContainsNullParameterNames), and array-typed members lose their trim annotation at
/// the element type.
/// </remarks>
public class AgendaDragOptions
{
    public double TimeSlotHeight { get; set; }
    public int SnapMinutes { get; set; }
    public double MinDurationMinutes { get; set; }
    public double ActivationDelayMs { get; set; }
    public bool AllowDrag { get; set; }
    public bool AllowResize { get; set; }
    public bool AllowCrossDay { get; set; }
    public bool PerPositionValidation { get; set; }
    public int Days { get; set; }
}
