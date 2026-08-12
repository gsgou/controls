using System.Diagnostics;
using System.Runtime.CompilerServices;
using Shiny.Controls.Keyframe;

namespace Shiny.Maui.Controls.MotionIcons;

/// <summary>
/// The frame signal every animating icon on a window shares, as an <see cref="IClock"/> the
/// keyframe engine's <c>Player</c> can drive from.
/// </summary>
/// <remarks>
/// <para><b>One timer per window, not per icon.</b> A list with an animated icon in every row is a
/// completely ordinary thing to build, and giving each of them its own <c>IDispatcherTimer</c> would
/// mean twenty timers waking the UI thread on twenty slightly different schedules. Instances are
/// keyed on the dispatcher — genuinely one object per window — and the underlying timer runs only
/// while something is subscribed, so a page of resting icons costs nothing at all.</para>
/// <para><b>Why not the platform frame signal.</b> MAUI's per-window ticker exposes its callback as
/// a single settable property that MAUI's own animation manager already owns; assigning to it
/// appears to work right up until every <c>FadeTo</c> in the app stops firing. A dispatcher timer
/// is not vsync-locked, but it is correct and it leaves MAUI's animations alone.</para>
/// <para>Deltas come from a stopwatch rather than the nominal interval, so a dropped frame is one
/// larger step and the animation stays on schedule instead of falling permanently behind.</para>
/// </remarks>
sealed class MotionTicker : IClock
{
    static readonly ConditionalWeakTable<IDispatcher, MotionTicker> Shared = [];
    static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(16);

    // A gap this large means the app was backgrounded or sitting at a breakpoint. Advancing by the
    // real elapsed time would teleport every icon on the page, so it is clamped to one frame.
    static readonly TimeSpan MaximumDelta = TimeSpan.FromMilliseconds(250);

    readonly IDispatcherTimer timer;
    readonly Stopwatch stopwatch = new();
    readonly Lock gate = new();

    Action<TimeSpan>? handlers;
    TimeSpan lastTimestamp;

    MotionTicker(IDispatcher dispatcher)
    {
        timer = dispatcher.CreateTimer();
        timer.Interval = Interval;
        timer.IsRepeating = true;
        timer.Tick += (_, _) => OnFrame();
    }

    /// <summary>
    /// Gets the ticker shared by everything on the element's dispatcher, or null when there is no
    /// dispatcher to drive one.
    /// </summary>
    /// <remarks>
    /// Returning null rather than throwing is deliberate. An element can be constructed long before
    /// it is attached to anything — a headless test host, or an implicit <c>Style</c> that sets
    /// properties from inside the base constructor — and "there is nowhere to draw yet" is a normal
    /// state for a view to be in, not an error worth taking the app down for.
    /// </remarks>
    public static MotionTicker? For(VisualElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var dispatcher = element.Dispatcher ?? Application.Current?.Dispatcher;

        if (dispatcher is null)
            return null;

        return Shared.GetValue(dispatcher, static key => new MotionTicker(key));
    }

    /// <inheritdoc />
    public event Action<TimeSpan>? Tick
    {
        add
        {
            lock (gate)
            {
                handlers += value;
                Sync();
            }
        }
        remove
        {
            lock (gate)
            {
                handlers -= value;
                Sync();
            }
        }
    }

    /// <inheritdoc />
    public bool IsRunning => timer.IsRunning;

    /// <inheritdoc />
    public void Start()
    {
        lock (gate)
            Sync();
    }

    /// <summary>Stops the frame source if nothing is listening any more.</summary>
    /// <remarks>
    /// The clock is shared, so an unconditional stop would freeze every other icon on the window.
    /// Unsubscribing is what actually stops it; this only settles the timer to match.
    /// </remarks>
    public void Stop()
    {
        lock (gate)
            Sync();
    }

    void Sync()
    {
        if (handlers is null)
        {
            if (!timer.IsRunning)
                return;

            timer.Stop();
            stopwatch.Reset();
            return;
        }

        if (timer.IsRunning)
            return;

        lastTimestamp = TimeSpan.Zero;
        stopwatch.Restart();
        timer.Start();
    }

    void OnFrame()
    {
        Action<TimeSpan>? snapshot;
        TimeSpan delta;

        lock (gate)
        {
            snapshot = handlers;

            var now = stopwatch.Elapsed;
            delta = now - lastTimestamp;
            lastTimestamp = now;

            if (delta > MaximumDelta)
                delta = MaximumDelta;
        }

        if (delta > TimeSpan.Zero)
            snapshot?.Invoke(delta);
    }
}
