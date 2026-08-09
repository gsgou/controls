using Microsoft.Maui.Controls;
using Shiny.Maui.Controls.Scheduler;
using Shiny.Maui.Controls.Scheduler.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>
/// The gesture state machine, as opposed to the arithmetic in <see cref="AgendaDragMathTests"/>.
/// These cover the boundary between a tap and a drag - the place where the two ways of pressing an
/// event (the native touch hook on touch-down, the pan when it finally starts) meet.
///
/// Every test here is async even where there is nothing to await: the view kicks off a
/// fire-and-forget scroll to "now" when it loads, and xUnit blocks a *synchronous* test until
/// everything posted to its sync context has finished - which a ScrollToAsync with no handler
/// behind it never does.
/// </summary>
public class AgendaDragGestureTests
{
    static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    sealed class Provider : ISchedulerEventProvider
    {
        public int Saves { get; private set; }

        public Task<IReadOnlyList<SchedulerEvent>> GetEvents(DateTimeOffset start, DateTimeOffset end)
            => Task.FromResult<IReadOnlyList<SchedulerEvent>>([]);

        public void OnEventSelected(SchedulerEvent selectedEvent) { }
        public bool CanCalendarSelect(DateOnly selectedDate) => true;
        public void OnCalendarDateSelected(DateOnly selectedDate) { }
        public void OnAgendaTimeSelected(DateTimeOffset selectedTime) { }
        public bool CanSelectAgendaTime(DateTimeOffset selectedTime) => true;
        public bool CanChangeEvent(SchedulerEvent evt) => true;

        public Task<bool> OnEventChanged(SchedulerEventChange change)
        {
            this.Saves++;
            return Task.FromResult(true);
        }
    }

    static (AgendaDragController Controller, Provider Provider, AgendaTimelinePanel Panel, View DragView, SchedulerEvent Event) Build()
    {
        TestDispatcherProvider.Install();
        _ = new Application();

        var provider = new Provider();
        var owner = new SchedulerAgendaView
        {
            // Zero delay arms on the press itself, which is what the mouse path does anyway and
            // keeps the dispatcher out of these tests.
            DragActivationDelay = TimeSpan.Zero,
            UseFeedback = false,
            AllowEventDrag = true,
            Provider = provider
        };

        var evt = new SchedulerEvent
        {
            Title = "Standup",
            Start = new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0))),
            End = new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 30)))
        };

        var panel = new AgendaTimelinePanel();
        panel.Build(Today, [evt], null, showTimeMarker: false);

        var controller = new AgendaDragController(owner, new ScrollView());
        return (controller, provider, panel, new ContentView(), evt);
    }


    /// <summary>
    /// The hook presses on touch-down and the pan presses again when it starts. The second press is
    /// the same finger on the same view; restarting there would throw away the origin the drag has
    /// been measuring from.
    /// </summary>
    [Fact]
    public async Task PressingTwiceForOneGestureDoesNotRestartTheDrag()
    {
        await Task.CompletedTask;
        var (controller, _, panel, dragView, evt) = Build();

        controller.Press(panel, dragView, evt, SchedulerEventChangeKind.Move, null);
        controller.Update(0, 60);

        controller.Press(panel, dragView, evt, SchedulerEventChangeKind.Move, null);

        controller.IsDragging.ShouldBeTrue("the second press must not cancel the gesture in flight");
        controller.ConsumedLastGesture.ShouldBeTrue();
    }


    /// <summary>
    /// A finger held on an event for longer than the activation delay and then lifted is a slow tap.
    /// It must not save anything, and it must not swallow the trailing Tapped - otherwise selecting
    /// an event becomes a matter of how fast you can stab at the screen.
    /// </summary>
    [Fact]
    public async Task AnArmedPressThatNeverMovedIsStillATap()
    {
        var (controller, provider, panel, dragView, evt) = Build();

        controller.Press(panel, dragView, evt, SchedulerEventChangeKind.Move, null);
        await controller.CompleteAsync();

        provider.Saves.ShouldBe(0, "nothing changed, so there is nothing to save");
        controller.ConsumedLastGesture.ShouldBeFalse("the tap has to reach the event");
        evt.Start.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
    }


    /// <summary>A drag that travelled far enough to snap commits, and does eat the trailing tap.</summary>
    [Fact]
    public async Task ADragThatMovedCommitsAndSwallowsTheTap()
    {
        var (controller, provider, panel, dragView, evt) = Build();

        controller.Press(panel, dragView, evt, SchedulerEventChangeKind.Move, null);
        controller.Update(0, 60);   // one hour at the default 60px slot height
        await controller.CompleteAsync();

        provider.Saves.ShouldBe(1);
        controller.ConsumedLastGesture.ShouldBeTrue();
        evt.Start.TimeOfDay.ShouldBe(TimeSpan.FromHours(10));
    }


    /// <summary>
    /// The touch can end without the pan ever reporting a thing (a press that armed and never
    /// moved produces no pan at all), so the release has to be able to finish the gesture itself.
    /// </summary>
    [Fact]
    public async Task ReleasingEndsAGestureThePanNeverReported()
    {
        await Task.CompletedTask;
        var (controller, _, panel, dragView, evt) = Build();

        controller.Press(panel, dragView, evt, SchedulerEventChangeKind.Move, null);
        controller.IsDragging.ShouldBeTrue();

        controller.Release();

        controller.IsDragging.ShouldBeFalse("a released touch must never leave the drag armed");
    }
}
