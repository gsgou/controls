namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Fans each camera frame out to the registered analyzers and aggregates their latest results into a
/// single detection set. Owns the frame reference the platform handler hands it: it retains per accepting
/// analyzer and releases its own reference after dispatch, so the native buffer frees once every analyzer
/// that took the frame has finished.
/// </summary>
sealed class CameraPipeline
{
    readonly Dictionary<string, IReadOnlyList<Detection>> latest = new();
    readonly object gate = new();
    AnalyzerRunner[] runners = [];

    /// <summary>Invoked (off the UI thread) with the aggregated detections + the upright image size.</summary>
    public Action<IReadOnlyList<Detection>, int, int>? OnDetections;

    int uprightW;
    int uprightH;

    public bool HasAnalyzers => this.runners.Length > 0;

    public void SetAnalyzers(IEnumerable<IFrameAnalyzer> analyzers)
    {
        lock (this.gate)
        {
            this.runners = analyzers.Select(a => new AnalyzerRunner(a, this.OnResult)).ToArray();
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

    void OnResult(DetectionResult result)
    {
        IReadOnlyList<Detection> aggregated;
        lock (this.gate)
        {
            this.latest[result.AnalyzerId] = result.Detections;
            aggregated = this.latest.Values.SelectMany(x => x).ToList();
        }
        this.OnDetections?.Invoke(aggregated, this.uprightW, this.uprightH);
    }
}
