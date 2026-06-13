namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Fans each camera frame out to the registered analyzers and aggregates the styled <see cref="OverlayBox"/>es
/// they currently see into a single set for the overlay. Owns the frame reference the platform handler hands
/// it: it retains per accepting analyzer and releases its own reference after dispatch, so the native buffer
/// frees once every analyzer that took the frame has finished. Keeps the last box set per analyzer so boxes
/// persist across dropped/slow frames until an analyzer replaces them (new list) or clears them (<c>null</c>).
/// </summary>
sealed class CameraPipeline
{
    readonly Dictionary<string, IReadOnlyList<OverlayBox>> latest = new();
    readonly object gate = new();
    AnalyzerRunner[] runners = [];
    IFrameAnalyzer[] analyzers = [];

    /// <summary>Invoked (off the UI thread) with the aggregated boxes + the upright image size.</summary>
    public Action<IReadOnlyList<OverlayBox>, int, int>? OnOverlays;

    Action<Action>? dispatcher;
    int uprightW;
    int uprightH;

    public bool HasAnalyzers => this.runners.Length > 0;

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
                (old as FrameAnalyzer)?.SetDispatcher(null);

            this.analyzers = list;
            this.runners = list.Select(a => new AnalyzerRunner(a, this.OnResult)).ToArray();
            foreach (var a in list)
                (a as FrameAnalyzer)?.SetDispatcher(this.dispatcher);

            this.latest.Clear();
        }
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
            runner.TrySubmit(frame, ct);

        frame.Dispose();
    }

    void OnResult(string analyzerId, IReadOnlyList<OverlayBox>? boxes)
    {
        IReadOnlyList<OverlayBox> aggregated;
        lock (this.gate)
        {
            // null clears this analyzer's boxes; a non-null set replaces them. Skip the redraw when a
            // "nothing seen" result clears a key that was already empty (the common steady state).
            bool changed;
            if (boxes is null)
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
