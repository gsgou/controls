using Microsoft.Maui.Layouts;
using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.Scheduler.Internal;

/// <summary>
/// The whole drag/resize gesture state machine for <see cref="SchedulerAgendaView"/>.
/// </summary>
/// <remarks>
/// The drag is owner-coordinated rather than panel-owned: a cross-day drag has to move an event out
/// of one <see cref="AgendaTimelinePanel"/> and into a sibling, so the panels detect and forward
/// while the view (through this controller) decides. Same split as TreeView's pointer drag.
/// </remarks>
class AgendaDragController(SchedulerAgendaView owner, ScrollView scrollView)
{
    /// <summary>How far a finger may travel while arming before the gesture is handed to the scroller.</summary>
    const double SlopPixels = 8;
    const double AutoScrollEdge = 48;
    const double AutoScrollStep = 12;

    AgendaTimelinePanel? sourcePanel;
    AgendaTimelinePanel? hostPanel;
    View? dragView;
    SchedulerEvent? evt;
    SchedulerEventChangeKind kind;
    SchedulerEventChange? candidate;
    DateTimeOffset originalStart;
    DateTimeOffset originalEnd;
    Rect originalBounds;
    IDispatcherTimer? armTimer;
    IDispatcherTimer? autoScrollTimer;
    BoxView? guide;
    bool pending;
    bool committable = true;
    double lastTotalX;
    double lastTotalY;
    double startScrollY;
    int dayDelta;

    public bool IsDragging { get; private set; }

    /// <summary>
    /// Set the moment a drag actually arms and cleared on the next gesture. Several platforms raise
    /// a Tapped on the release that ends a pan, which would otherwise select the event you just
    /// dropped; the panel's tap handler checks this and bails.
    /// </summary>
    public bool ConsumedLastGesture { get; private set; }


    public void Arm(AgendaTimelinePanel panel, View view, SchedulerEvent target, SchedulerEventChangeKind changeKind)
    {
        // Relayout would rebuild the panels and orphan the view this gesture just handed us.
        if (this.IsDragging)
            this.Cancel(relayout: false);

        this.StopArmTimer();

        this.sourcePanel = panel;
        this.hostPanel = panel;
        this.dragView = view;
        this.evt = target;
        this.kind = changeKind;
        this.pending = true;
        this.committable = true;
        this.candidate = null;
        this.dayDelta = 0;
        this.lastTotalX = 0;
        this.lastTotalY = 0;
        this.ConsumedLastGesture = false;

        var delay = owner.DragActivationDelay;

        // A mouse drag on a desktop calendar is expected to be instantaneous; only touch needs the
        // long-press, because only touch is ambiguous with a scroll.
        if (owner.HasPointerDevice || delay <= TimeSpan.Zero)
        {
            this.Activate();
            return;
        }

        this.armTimer = owner.Dispatcher.CreateTimer();
        this.armTimer.Interval = delay;
        this.armTimer.IsRepeating = false;
        this.armTimer.Tick += (_, _) => this.Activate();
        this.armTimer.Start();
    }


    public void Update(double totalX, double totalY)
    {
        if (!this.pending && !this.IsDragging)
            return;

        this.lastTotalX = totalX;
        this.lastTotalY = totalY;

        if (!this.IsDragging)
        {
            // Moved before the long-press elapsed - this was a scroll all along. Bail cheaply and
            // silently; nothing visible has happened yet.
            if (Math.Abs(totalX) > SlopPixels || Math.Abs(totalY) > SlopPixels)
                this.Abandon();
            return;
        }
        this.Apply();
    }


    public async Task CompleteAsync()
    {
        if (!this.IsDragging)
        {
            this.Abandon();
            return;
        }

        var change = this.candidate;
        var accepted = this.committable;

        // Restore scrolling and visuals *first* so the view is never stuck under a provider that hangs.
        this.RestoreChrome();
        this.IsDragging = false;

        if (change is null || !accepted)
        {
            owner.RelayoutDays();
            this.Reset();
            return;
        }

        // Optimistic: a provider that hits the network would otherwise leave the event visibly
        // pinned under the finger for the whole round trip.
        var target = change.Event;
        target.Start = change.NewStart;
        target.End = change.NewEnd;
        owner.RelayoutDays();

        var ok = false;
        try
        {
            ok = await (owner.Provider?.OnEventChanged(change) ?? Task.FromResult(false));
        }
        catch (Exception ex)
        {
            owner.RaiseChangeFailed(change, ex);
        }

        if (!ok)
        {
            target.Start = change.OriginalStart;
            target.End = change.OriginalEnd;
            owner.RelayoutDays();
        }
        else if (owner.UseFeedback)
        {
            FeedbackHelper.Execute(owner, "EventDropped");
        }
        this.Reset();
    }


    public void Cancel(bool relayout = true)
    {
        var wasDragging = this.IsDragging;
        this.RestoreChrome();
        this.IsDragging = false;
        this.pending = false;

        if (wasDragging && relayout)
            owner.RelayoutDays();

        this.Reset();
    }


    void Activate()
    {
        this.StopArmTimer();
        if (!this.pending || this.evt is null || this.dragView is null)
            return;

        this.pending = false;

        if (owner.Provider?.CanChangeEvent(this.evt) != true)
        {
            this.Reset();
            return;
        }

        this.IsDragging = true;
        this.ConsumedLastGesture = true;
        this.originalStart = this.evt.Start;
        this.originalEnd = this.evt.End;
        this.originalBounds = AbsoluteLayout.GetLayoutBounds(this.dragView);
        this.startScrollY = scrollView.ScrollY;

        scrollView.Orientation = ScrollOrientation.Neither;
        this.dragView.Opacity = 0.75;
        this.dragView.ZIndex = 1;

        if (owner.UseFeedback)
            FeedbackHelper.Execute(owner, "EventDragStarted");

        this.StartAutoScroll();
        this.Apply();
    }


    /// <summary>Recomputes the candidate change from the last gesture totals and repositions the view.</summary>
    void Apply()
    {
        if (this.evt is null || this.dragView is null || this.sourcePanel is null)
            return;

        // The event tracks the finger in *content* coordinates: when auto-scroll moves the content
        // under a stationary finger, the target time still has to keep moving.
        var contentDeltaY = this.lastTotalY + (scrollView.ScrollY - this.startScrollY);
        var rawMinutes = AgendaGeometry.YToMinutes(contentDeltaY, owner.TimeSlotHeight);
        var snapped = AgendaGeometry.SnapMinutes(rawMinutes, owner.DragSnapMinutes);

        this.dayDelta = this.ResolveDayDelta();

        var (newStart, newEnd) = AgendaGeometry.Apply(
            this.originalStart,
            this.originalEnd,
            this.kind,
            snapped,
            this.dayDelta,
            owner.MinEventDuration);

        this.candidate = new SchedulerEventChange
        {
            Event = this.evt,
            OriginalStart = this.originalStart,
            OriginalEnd = this.originalEnd,
            NewStart = newStart,
            NewEnd = newEnd,
            Kind = this.kind
        };
        this.committable = owner.Provider?.CanChangeEventTo(this.candidate) ?? false;

        var sourceIndex = owner.IndexOfPanel(this.sourcePanel);
        var target = owner.PanelAt(sourceIndex + this.dayDelta) ?? this.sourcePanel;
        this.MoveTo(target, newStart, newEnd);
    }


    int ResolveDayDelta()
    {
        if (this.kind != SchedulerEventChangeKind.Move || !owner.AllowCrossDayDrag || owner.DaysToShow <= 1)
            return 0;

        var width = this.sourcePanel!.Width;
        if (width <= 0)
            return 0;

        var sourceIndex = owner.IndexOfPanel(this.sourcePanel);
        var raw = (int)Math.Round(this.lastTotalX / width, MidpointRounding.AwayFromZero);
        return Math.Clamp(raw, -sourceIndex, owner.DaysToShow - 1 - sourceIndex);
    }


    void MoveTo(AgendaTimelinePanel target, DateTimeOffset newStart, DateTimeOffset newEnd)
    {
        if (this.dragView is null)
            return;

        if (!ReferenceEquals(target, this.hostPanel))
        {
            this.hostPanel?.EventsLayer.Remove(this.dragView);
            target.EventsLayer.Children.Add(this.dragView);
            AbsoluteLayout.SetLayoutFlags(this.dragView,
                AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
            this.hostPanel = target;
        }

        var date = target.BuildDate;
        var localStart = newStart.LocalDateTime;
        var localEnd = newEnd.LocalDateTime;

        var startMinutes = DateOnly.FromDateTime(localStart) < date ? 0 : localStart.TimeOfDay.TotalMinutes;
        var endMinutes = DateOnly.FromDateTime(localEnd) > date ? AgendaGeometry.MinutesPerDay : localEnd.TimeOfDay.TotalMinutes;

        var y = AgendaGeometry.MinutesToY(startMinutes, owner.TimeSlotHeight);
        var h = AgendaGeometry.MinutesToY(Math.Max(endMinutes - startMinutes, 15), owner.TimeSlotHeight);

        // A cross-day drop lands in a column whose overlap clustering is not known until the
        // relayout, so the preview simply takes the full width of the destination day.
        var x = this.dayDelta == 0 ? this.originalBounds.X : 0;
        var width = this.dayDelta == 0 ? this.originalBounds.Width : 1;

        AbsoluteLayout.SetLayoutBounds(this.dragView, new Rect(x, y, width, h));
        this.dragView.Opacity = this.committable ? 0.75 : 0.5;

        this.UpdateGuide(target, y);
    }


    void UpdateGuide(AgendaTimelinePanel target, double y)
    {
        this.guide ??= new BoxView { HeightRequest = 1, InputTransparent = true };
        this.guide.Color = this.committable
            ? owner.DragSnapGuideColor ?? Color.FromRgba(120, 120, 120, 153)
            : Color.FromRgba(239, 68, 68, 200);

        if (this.guide.Parent is Layout parent && !ReferenceEquals(parent, target.EventsLayer))
            parent.Remove(this.guide);

        if (!target.EventsLayer.Children.Contains(this.guide))
            target.EventsLayer.Children.Add(this.guide);

        AbsoluteLayout.SetLayoutFlags(this.guide, AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(this.guide, new Rect(0, y, 1, 1));
    }


    void StartAutoScroll()
    {
        this.autoScrollTimer?.Stop();
        this.autoScrollTimer = owner.Dispatcher.CreateTimer();
        this.autoScrollTimer.Interval = TimeSpan.FromMilliseconds(16);
        this.autoScrollTimer.Tick += (_, _) => this.AutoScrollTick();
        this.autoScrollTimer.Start();
    }


    /// <summary>
    /// Without this you cannot drag an event from 09:00 to 18:00 on a phone, because the
    /// destination is simply not on screen.
    /// </summary>
    void AutoScrollTick()
    {
        if (!this.IsDragging || this.dragView is null || this.hostPanel is null)
            return;

        var viewport = scrollView.Height;
        if (viewport <= 0)
            return;

        var bounds = AbsoluteLayout.GetLayoutBounds(this.dragView);
        var contentTop = this.hostPanel.Y + bounds.Y;
        var contentBottom = contentTop + bounds.Height;
        var top = scrollView.ScrollY;

        var step = 0.0;
        if (contentTop < top + AutoScrollEdge)
            step = -AutoScrollStep;
        else if (contentBottom > top + viewport - AutoScrollEdge)
            step = AutoScrollStep;

        if (step == 0)
            return;

        var max = Math.Max(0, scrollView.ContentSize.Height - viewport);
        var next = Math.Clamp(top + step, 0, max);
        if (Math.Abs(next - top) < 0.5)
            return;

        _ = scrollView.ScrollToAsync(0, next, false);
        this.Apply();
    }


    /// <summary>The arming window lost - hand the gesture back to the scroller with nothing to undo.</summary>
    void Abandon()
    {
        this.StopArmTimer();
        this.pending = false;
        this.Reset();
    }


    void RestoreChrome()
    {
        this.StopArmTimer();
        this.autoScrollTimer?.Stop();
        this.autoScrollTimer = null;

        scrollView.Orientation = owner.AllowPan ? ScrollOrientation.Vertical : ScrollOrientation.Neither;

        if (this.dragView is not null)
        {
            this.dragView.Opacity = 1;
            this.dragView.ZIndex = 0;
        }

        if (this.guide?.Parent is Layout parent)
            parent.Remove(this.guide);
        this.guide = null;
    }


    void StopArmTimer()
    {
        this.armTimer?.Stop();
        this.armTimer = null;
    }


    void Reset()
    {
        this.sourcePanel = null;
        this.hostPanel = null;
        this.dragView = null;
        this.evt = null;
        this.candidate = null;
        this.dayDelta = 0;
        this.committable = true;
    }
}
