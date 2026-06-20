using System.ComponentModel;

namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Fans each camera frame out to the registered analyzers and aggregates the styled <see cref="OverlayBox"/>es
/// they currently see into a single set for the overlay. Owns the frame reference the platform handler hands
/// it: it retains per accepting analyzer and releases its own reference after dispatch, so the native buffer
/// frees once every analyzer that took the frame has finished. Keeps the last box set per analyzer so boxes
/// persist across dropped/slow frames until an analyzer replaces them (new list) or clears them (<c>null</c>).
/// An analyzer whose <see cref="FrameAnalyzer.IsEnabled"/> is <c>false</c> is skipped and its boxes cleared; the
/// pipeline reports <see cref="HasAnalyzers"/> from the count of <i>enabled</i> analyzers, so a collection full
/// of disabled analyzers reads as "none".
/// </summary>
sealed class CameraPipeline
{
    readonly Dictionary<string, IReadOnlyList<OverlayBox>> latest = new();
    readonly object gate = new();
    AnalyzerRunner[] runners = [];
    IFrameAnalyzer[] analyzers = [];
    volatile int enabledCount;

    /// <summary>Invoked (off the UI thread) with the aggregated boxes + the upright image size.</summary>
    public Action<IReadOnlyList<OverlayBox>, int, int>? OnOverlays;

    /// <summary>
    /// Invoked when the set of <i>enabled</i> analyzers may have changed (collection edit or an
    /// <see cref="FrameAnalyzer.IsEnabled"/> toggle). Platforms that bind use cases up-front (Android) use this
    /// to re-evaluate; those that gate frame delivery per-frame (Apple/Windows) can ignore it.
    /// </summary>
    public Action? OnActiveChanged;

    Action<Action>? dispatcher;
    int uprightW;
    int uprightH;

    public bool HasAnalyzers => this.enabledCount > 0;

    /// <summary>Dispatcher analyzers use to raise their typed events on the UI thread (re-applied on change).</summary>
    public void SetDispatcher(Action<Action>? post)
    {
        lock (this.gate)
        {
            this.dispatcher = post;
            foreach (var a in this.analyzers)
                (a as FrameAnalyzer)?.SetDispatcher(post);
        }
    }

    public void SetAnalyzers(IEnumerable<IFrameAnalyzer> analyzers)
    {
        var list = analyzers.ToArray();
        lock (this.gate)
        {
            foreach (var old in this.analyzers)
            {
                if (old is FrameAnalyzer fa)
                {
                    fa.SetDispatcher(null);
                    fa.PropertyChanged -= this.OnAnalyzerPropertyChanged;
                }
            }

            this.analyzers = list;
            this.runners = list.Select(a => new AnalyzerRunner(a, this.OnResult)).ToArray();
            foreach (var a in list)
            {
                if (a is FrameAnalyzer fa)
                {
                    fa.SetDispatcher(this.dispatcher);
                    fa.PropertyChanged += this.OnAnalyzerPropertyChanged;
                }
            }

            this.RecomputeEnabled();
            this.latest.Clear();
        }
        this.OnActiveChanged?.Invoke();
    }

    /// <summary>Submit one frame. The pipeline takes ownership of the passed reference.</summary>
    public void Process(CameraFrame frame, CancellationToken ct)
    {
        // upright dimensions account for a 90/270° sensor rotation so the overlay aspect is correct
        if (frame.Rotation is 90 or 270)
        {
            this.uprightW = frame.Height;
            this.uprightH = frame.Width;
        }
        else
        {
            this.uprightW = frame.Width;
            this.uprightH = frame.Height;
        }

        var current = this.runners;
        foreach (var runner in current)
        {
            if (runner.Enabled)
                runner.TrySubmit(frame, ct);
        }

        frame.Dispose();
    }

    // Refresh each runner's cached enabled flag + the enabled count. Call under gate.
    void RecomputeEnabled()
    {
        var count = 0;
        foreach (var runner in this.runners)
        {
            var enabled = (runner.Analyzer as FrameAnalyzer)?.IsEnabled ?? true;
            runner.Enabled = enabled;
            if (enabled)
                count++;
        }
        this.enabledCount = count;
    }

    void OnAnalyzerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != FrameAnalyzer.IsEnabledProperty.PropertyName)
            return;

        IReadOnlyList<OverlayBox> aggregated;
        lock (this.gate)
        {
            this.RecomputeEnabled();
            // a disabled analyzer should stop drawing immediately
            foreach (var runner in this.runners)
                if (!runner.Enabled)
                    this.latest.Remove(runner.Id);

            aggregated = this.latest.Values.SelectMany(x => x).ToList();
        }
        this.OnOverlays?.Invoke(aggregated, this.uprightW, this.uprightH);
        this.OnActiveChanged?.Invoke();
    }

    // Whether the runner for this id is currently disabled. Call under gate.
    bool IsDisabled(string analyzerId)
    {
        foreach (var runner in this.runners)
            if (runner.Id == analyzerId)
                return !runner.Enabled;
        return false;
    }

    void OnResult(string analyzerId, IReadOnlyList<OverlayBox>? boxes)
    {
        IReadOnlyList<OverlayBox> aggregated;
        lock (this.gate)
        {
            // null clears this analyzer's boxes; a non-null set replaces them. Skip the redraw when a
            // "nothing seen" result clears a key that was already empty (the common steady state). Treat a
            // result from a just-disabled analyzer (in flight when it was toggled off) as a clear.
            bool changed;
            if (boxes is null || this.IsDisabled(analyzerId))
                changed = this.latest.Remove(analyzerId);
            else
            {
                this.latest[analyzerId] = boxes;
                changed = true;
            }

            if (!changed)
                return;

            aggregated = this.latest.Values.SelectMany(x => x).ToList();
        }
        this.OnOverlays?.Invoke(aggregated, this.uprightW, this.uprightH);
    }
}
