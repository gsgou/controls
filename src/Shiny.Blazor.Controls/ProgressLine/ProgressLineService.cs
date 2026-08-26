namespace Shiny.Blazor.Controls;

/// <inheritdoc cref="IProgressLineService"/>
public sealed class ProgressLineService : IProgressLineService, IDisposable
{
    readonly List<Run> runs = new();
    readonly object gate = new();

    Timer? trickleTimer;
    int generation;
    bool disposed;

    /// <summary>Raised whenever the host needs to re-render. Subscribed by <c>ProgressLineHost</c>.</summary>
    public event Action? OnChanged;

    /// <summary>The line's settings while it is up, or null when nothing is running.</summary>
    public ProgressLineConfig? Config { get; private set; }

    /// <summary>Whether the host should render the line at all, fade included.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>False while the line is fading out, which is what drives the fade.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Current progress from 0 to 100, for the line's <c>Value</c>.</summary>
    public double Value { get; private set; }

    public bool IsRunning
    {
        get { lock (this.gate) return this.runs.Count > 0; }
    }


    public IProgressLineHandle Start(Action<ProgressLineConfig>? configure = null)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        var cfg = new ProgressLineConfig();
        configure?.Invoke(cfg);

        var run = new Run(this, cfg)
        {
            Progress = Math.Clamp(cfg.StartProgress, 0, 1)
        };

        lock (this.gate)
        {
            this.runs.Add(run);
            this.generation++;

            // The last caller's look wins: overlapping runs share one line, and there is no honest
            // way to show two configurations at once.
            this.Config = cfg;
            this.IsVisible = true;
            this.IsActive = true;
            this.RecomputeValue();
            this.StartTimer(cfg);
        }

        this.OnChanged?.Invoke();
        return run;
    }


    public void CompleteAll()
    {
        Run[] snapshot;
        lock (this.gate)
            snapshot = this.runs.ToArray();

        foreach (var run in snapshot)
            run.Complete();
    }


    void StartTimer(ProgressLineConfig cfg)
    {
        this.trickleTimer?.Dispose();

        var interval = cfg.TrickleInterval > TimeSpan.Zero
            ? cfg.TrickleInterval
            : TimeSpan.FromMilliseconds(400);

        this.trickleTimer = new Timer(_ => this.OnTick(), null, interval, interval);
    }


    void StopTimer()
    {
        this.trickleTimer?.Dispose();
        this.trickleTimer = null;
    }


    void OnTick()
    {
        lock (this.gate)
        {
            if (this.disposed || this.runs.Count == 0)
                return;

            foreach (var run in this.runs)
                run.Trickle();

            this.RecomputeValue();
        }

        this.OnChanged?.Invoke();
    }


    /// <summary>
    /// Takes the slowest active run, not the average: the line's job is to say "not finished yet",
    /// and averaging lets one quick run drag the bar most of the way across while the slow one it is
    /// waiting on has barely started.
    /// </summary>
    void RecomputeValue()
    {
        if (this.runs.Count == 0)
            return;

        this.Value = Math.Clamp(this.runs.Min(r => r.Progress), 0, 1) * 100;
    }


    internal void NotifyProgress()
    {
        lock (this.gate)
            this.RecomputeValue();

        this.OnChanged?.Invoke();
    }


    internal void OnRunFinished(Run run, bool sweep)
    {
        int token;

        lock (this.gate)
        {
            this.runs.Remove(run);

            if (this.runs.Count > 0)
            {
                this.RecomputeValue();
                this.OnChanged?.Invoke();
                return;
            }

            this.StopTimer();
            token = ++this.generation;
        }

        _ = this.FinishAsync(sweep, token);
    }


    async Task FinishAsync(bool sweep, int token)
    {
        var cfg = this.Config ?? new ProgressLineConfig();

        if (sweep)
        {
            this.Value = 100;
            this.OnChanged?.Invoke();

            // Let the fill reach the end before it starts fading, so the run visibly completes rather
            // than dissolving somewhere short of it.
            await Task.Delay(Math.Max(cfg.ProgressAnimationDuration, 0));
            if (token != this.generation || this.disposed)
                return;
        }

        this.IsActive = false;
        this.OnChanged?.Invoke();

        await Task.Delay(Math.Max(cfg.FadeDuration, 0) + 16);
        if (token != this.generation || this.disposed)
            return;

        this.IsVisible = false;
        this.Value = 0;
        this.OnChanged?.Invoke();
    }


    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.StopTimer();

        lock (this.gate)
            this.runs.Clear();

        this.IsVisible = false;
        this.OnChanged = null;
    }


    internal sealed class Run(ProgressLineService owner, ProgressLineConfig config) : IProgressLineHandle
    {
        public double Progress { get; set; }

        public bool IsComplete { get; private set; }

        public void SetProgress(double progress)
        {
            if (this.IsComplete)
                return;

            var clamped = Math.Clamp(progress, 0, 1);
            if (clamped <= this.Progress)
                return;

            this.Progress = clamped;
            owner.NotifyProgress();
        }


        /// <summary>
        /// Advances a fraction of the distance still to run, so the line decelerates as it approaches
        /// the ceiling and never quite arrives — which is the honest shape for "still working".
        /// </summary>
        public void Trickle()
        {
            if (this.IsComplete || !config.Trickle || config.Indeterminate)
                return;

            var ceiling = Math.Clamp(config.TrickleCeiling, 0, 1);
            if (this.Progress >= ceiling)
                return;

            this.Progress = Math.Min(ceiling, this.Progress + (ceiling - this.Progress) * config.TrickleRate);
        }


        public void Complete() => this.Finish(sweep: true);

        public void Cancel() => this.Finish(sweep: false);

        public void Dispose() => this.Complete();

        void Finish(bool sweep)
        {
            if (this.IsComplete)
                return;

            this.IsComplete = true;
            this.Progress = 1;
            owner.OnRunFinished(this, sweep);
        }
    }
}
