namespace Shiny.Maui.Controls.Infrastructure;

/// <summary>
/// The one place the controls read the device posture from. Mirrors <see cref="SafeAreaInsets"/>:
/// a static the controls ask, fed by whatever <see cref="IFoldPostureSource"/> the app registered.
/// </summary>
/// <remarks>
/// <para>With no source, <see cref="Current"/> is <see cref="FoldInfo.None"/> and
/// <see cref="HasSource"/> is false, and every fold-aware control skips its fold handling entirely —
/// it does not even subscribe — so an app that never registers a source behaves exactly as before.</para>
/// <para>Listeners are held weakly. A static event would root every control that ever subscribed, and
/// MAUI never disposes views.</para>
/// </remarks>
static class FoldPosture
{
    static readonly object Gate = new();
    static readonly List<WeakReference<FoldTracker>> Listeners = [];
    static IFoldPostureSource? source;
    static bool sourceIsExplicit;

    /// <summary>Whether anything is reporting posture. False means "no fold, ever".</summary>
    public static bool HasSource => source is not null;

    /// <summary>The posture right now; <see cref="FoldInfo.None"/> with no source.</summary>
    public static FoldInfo Current => source?.Current ?? FoldInfo.None;


    /// <summary>
    /// Set the source. An explicit source (<c>UseFoldPosture</c>) always wins; a default one (the
    /// Foldables package's built-in providers) only fills an empty slot, so the two can be registered
    /// in either order and the app's own choice is the one that sticks.
    /// </summary>
    public static void SetSource(IFoldPostureSource? newSource, bool isDefault = false)
    {
        lock (Gate)
        {
            if (isDefault && sourceIsExplicit && source is not null)
                return;

            if (source is not null)
                source.Changed -= OnSourceChanged;

            source = newSource;
            sourceIsExplicit = !isDefault && newSource is not null;

            if (source is not null)
                source.Changed += OnSourceChanged;
        }

        RaiseChanged();
    }


    internal static void Register(FoldTracker tracker)
    {
        lock (Gate)
        {
            Listeners.RemoveAll(static w => !w.TryGetTarget(out _));
            Listeners.Add(new WeakReference<FoldTracker>(tracker));
        }
    }


    static void OnSourceChanged(object? sender, EventArgs e) => RaiseChanged();


    /// <summary>Tells every live tracker the posture moved. Exposed for tests driving a fake source.</summary>
    internal static void RaiseChanged()
    {
        List<FoldTracker> live;
        lock (Gate)
        {
            live = new List<FoldTracker>(Listeners.Count);
            foreach (var weak in Listeners)
            {
                if (weak.TryGetTarget(out var tracker))
                    live.Add(tracker);
            }
        }

        foreach (var tracker in live)
            tracker.OnPostureChanged();
    }
}


/// <summary>
/// Follows the fold for one control: where it falls in the control's (or a reference view's)
/// coordinates, recomputed when the posture changes and when the view is resized or moved.
/// </summary>
/// <remarks>
/// A fold that does not move can still start or stop crossing a control that moved, which is why the
/// view's own size changes count as much as a posture change. The recompute is posted rather than run
/// inline because the native position is only final once the platform has laid the view out.
/// </remarks>
sealed class FoldTracker
{
    readonly WeakReference<VisualElement> owner;
    readonly Func<VisualElement?>? reference;
    readonly Action<FoldTracker> changed;
    static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(450);
    bool queued;
    bool settleQueued;

    FoldTracker(VisualElement owner, Func<VisualElement?>? reference, Action<FoldTracker> changed)
    {
        this.owner = new WeakReference<VisualElement>(owner);
        this.reference = reference;
        this.changed = changed;
    }


    /// <summary>
    /// Starts tracking, or returns null when no posture source is registered — in which case the
    /// caller has nothing to do, ever, and keeps its pre-foldables behaviour.
    /// </summary>
    /// <param name="owner">The control. Its size changes trigger a recompute; it is held weakly.</param>
    /// <param name="changed">Called (on the UI thread) when <see cref="Fold"/> changes.</param>
    /// <param name="reference">
    /// The view whose coordinates the fold is reported in, when that is not the owner — a
    /// FloatingPanel measures its detents against its OverlayHost, not itself.
    /// </param>
    public static FoldTracker? Attach(VisualElement owner, Action<FoldTracker> changed, Func<VisualElement?>? reference = null)
    {
        if (!FoldPosture.HasSource)
            return null;

        var tracker = new FoldTracker(owner, reference, changed);
        owner.SizeChanged += tracker.OnSizeChanged;
        owner.Loaded += tracker.OnSizeChanged;
        FoldPosture.Register(tracker);
        tracker.Recompute(raise: false);
        return tracker;
    }


    /// <summary>The fold (grown by the hinge margin) in the reference view's coordinates, or null when none crosses it.</summary>
    public Rect? Fold { get; private set; }

    /// <summary>The posture the last recompute saw.</summary>
    public FoldInfo Info { get; private set; } = FoldInfo.None;

    /// <summary>The reference view's size at the last recompute.</summary>
    public Size Size { get; private set; }

    /// <summary>The fold runs top to bottom through the view.</summary>
    public bool IsVertical => this.Fold is { } f && FoldLayout.IsVertical(f);

    /// <summary>The fold runs side to side through the view.</summary>
    public bool IsHorizontal => this.Fold is { } f && !FoldLayout.IsVertical(f);


    /// <summary>
    /// Recompute now, without waiting for a size change. Raises the callback when the answer moved,
    /// unless <paramref name="raise"/> is false — for a caller about to use the answer itself.
    /// </summary>
    public void Refresh(bool raise = true) => this.Recompute(raise);


    internal void OnPostureChanged() => this.Queue();


    void OnSizeChanged(object? sender, EventArgs e) => this.Queue();


    void Queue()
    {
        if (!this.owner.TryGetTarget(out var view))
            return;

        // A view built off any dispatcher (a unit test, a view constructed on a worker) throws from
        // Dispatcher rather than returning null; either way there is nothing to post to.
        IDispatcher? dispatcher;
        try
        {
            dispatcher = view.Dispatcher;
        }
        catch (InvalidOperationException)
        {
            dispatcher = null;
        }

        if (dispatcher is null)
        {
            this.Recompute(raise: true);
            return;
        }

        if (this.queued)
            return;

        this.queued = true;
        dispatcher.Dispatch(() =>
        {
            this.queued = false;
            this.Recompute(raise: true);
        });

        // And once more after things settle. A page that is still sliding in (a Shell or navigation
        // transition translates an ancestor) reports its in-flight position, and nothing re-lays it out
        // when the animation ends - so a fold measured at the first pass is off by however far the page
        // still had to travel.
        if (!this.settleQueued)
        {
            this.settleQueued = true;
            dispatcher.DispatchDelayed(SettleDelay, () =>
            {
                this.settleQueued = false;
                this.Recompute(raise: true);
            });
        }
    }


    void Recompute(bool raise)
    {
        if (!this.owner.TryGetTarget(out var view))
            return;

        var target = this.reference?.Invoke() ?? view;
        var info = FoldPosture.Current;
        Rect? fold = null;
        var size = new Size(Math.Max(0, target.Width), Math.Max(0, target.Height));

        if (info.Avoid is { } avoid && FoldGeometry.WindowBounds(target) is { } bounds)
            fold = FoldLayout.FoldIn(bounds, avoid);

        var moved = !Equals(fold, this.Fold) || !Equals(info, this.Info) || size != this.Size;
        this.Fold = fold;
        this.Info = info;
        this.Size = size;

        if (raise && moved)
            this.changed(this);
    }
}
