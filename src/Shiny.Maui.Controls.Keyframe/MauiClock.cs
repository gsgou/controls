using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Shiny.Maui.Controls.Keyframe;

/// <summary>
/// An <see cref="IClock"/> driven by the platform's frame signal.
/// </summary>
/// <remarks>
/// <para><b>Why this uses a dispatcher timer rather than the platform display link.</b> MAUI's
/// per-window <c>ITicker</c> exposes its callback as a single settable <c>Fire</c> property, and
/// MAUI's own animation manager already owns it. Assigning to it appears to work — right up until
/// it silently breaks every <c>FadeTo</c> in the app. Going through <c>IAnimationManager</c>
/// instead does not help: resolving it from <c>MauiContext.Services</c> returns a fresh instance
/// per call, and each new manager reassigns that same <c>Fire</c> callback, so animations end up
/// fighting each other for the one frame signal. A dispatcher timer is the honest choice: it is
/// not vsync-locked, but it is correct, and it leaves MAUI's own animations alone.</para>
/// <para><b>One clock per window.</b> <see cref="For"/> returns an instance shared across the
/// dispatcher, so twenty animated views cost one timer rather than twenty. It runs only while
/// something is actually listening — subscribing the first handler starts it and removing the last
/// one stops it, so a page of paused animations costs nothing.</para>
/// <para>Deltas come from a <see cref="Stopwatch"/> rather than the nominal interval, so a dropped
/// frame shows up as one larger step and the timeline stays on schedule. Assuming a fixed 16ms per
/// tick makes every hitch permanently retard the animation.</para>
/// </remarks>
public sealed class MauiClock : IClock, IDisposable
{
    static readonly ConditionalWeakTable<object, MauiClock> SharedClocks = [];
    static readonly TimeSpan FallbackInterval = TimeSpan.FromMilliseconds(16);

    // A frame this long means the app was suspended or the debugger was paused. Advancing by the
    // real gap would teleport every animation, so clamp it to a single frame.
    static readonly TimeSpan MaximumDelta = TimeSpan.FromMilliseconds(250);

    readonly IDispatcherTimer timer;
    readonly Stopwatch stopwatch = new();
    readonly bool isShared;
    readonly Lock gate = new();

    Action<TimeSpan>? handlers;
    TimeSpan lastTimestamp;
    bool disposed;

    MauiClock(IDispatcher dispatcher, bool isShared)
    {
        this.isShared = isShared;

        timer = dispatcher.CreateTimer();
        timer.Interval = FallbackInterval;
        timer.IsRepeating = true;
        timer.Tick += (_, _) => OnFrame();
    }

    /// <summary>Creates a clock driven by a dispatcher timer.</summary>
    public MauiClock(IDispatcher dispatcher)
        : this(dispatcher ?? throw new ArgumentNullException(nameof(dispatcher)), isShared: false) { }

    /// <summary>Gets the clock shared by every animation on the element's dispatcher.</summary>
    /// <remarks>
    /// The returned clock is shared and outlives any single animation, so callers should unsubscribe
    /// from <see cref="Tick"/> rather than dispose it. <see cref="Dispose"/> is a no-op on shared
    /// instances for exactly that reason.
    /// </remarks>
    public static MauiClock For(VisualElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        // Keyed on the dispatcher rather than on IAnimationManager. Resolving IAnimationManager
        // from MauiContext.Services hands back a *different* instance per call, so keying on it
        // silently produces one clock per animated view — and worse, each freshly constructed
        // manager reassigns the window's single ITicker.Fire callback, so only the last one ever
        // ticks. The dispatcher is genuinely one object per window, which is what "shared" needs.
        var dispatcher = element.Dispatcher
            ?? Application.Current?.Dispatcher
            ?? throw new InvalidOperationException(
                "No dispatcher is available for this element. Attach the animation after the " +
                "element has loaded, or supply your own IClock.");

        return SharedClocks.GetValue(dispatcher, static key => new MauiClock((IDispatcher)key, isShared: true));
    }

    /// <summary>
    /// Raised once per frame with the elapsed time since the previous tick. The underlying frame
    /// source runs only while at least one handler is attached.
    /// </summary>
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
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Ensures the frame source is running if anything is listening. Subscribing already does this,
    /// so an explicit call is rarely needed.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        lock (gate)
            Sync();
    }

    /// <summary>
    /// Stops the frame source.
    /// </summary>
    /// <remarks>
    /// On a shared clock this deliberately will not yank the frame source out from under other
    /// subscribers — it stops only once nothing is listening. A single view unloading must not be
    /// able to freeze every other animation on the window, and MAUI raises Unloaded far more often
    /// than one would expect during layout. To stop one animation, stop its player.
    /// </remarks>
    public void Stop()
    {
        lock (gate)
        {
            if (isShared)
                Sync();
            else
                StopSource();
        }
    }

    /// <summary>Starts or stops the frame source to match whether anyone is listening.</summary>
    void Sync()
    {
        if (disposed)
            return;

        if (handlers is not null && !IsRunning)
            StartSource();
        else if (handlers is null && IsRunning)
            StopSource();
    }

    void StartSource()
    {
        IsRunning = true;
        lastTimestamp = TimeSpan.Zero;
        stopwatch.Restart();

        timer.Start();
    }

    void StopSource()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        stopwatch.Stop();

        timer.Stop();
    }

    void OnFrame()
    {
        Action<TimeSpan>? snapshot;

        lock (gate)
        {
            if (!IsRunning)
                return;

            snapshot = handlers;
        }

        var now = stopwatch.Elapsed;
        var delta = now - lastTimestamp;
        lastTimestamp = now;

        if (delta <= TimeSpan.Zero)
            return;

        if (delta > MaximumDelta)
            delta = MaximumDelta;

        snapshot?.Invoke(delta);
    }

    /// <summary>
    /// Releases the frame subscription. Does nothing on a shared clock, which is owned by the
    /// window rather than by any one animation.
    /// </summary>
    public void Dispose()
    {
        if (isShared || disposed)
            return;

        lock (gate)
        {
            disposed = true;
            StopSource();
            handlers = null;
        }
    }
}
