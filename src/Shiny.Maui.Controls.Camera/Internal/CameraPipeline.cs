using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Drives the single active <see cref="IFrameAnalyzer"/> against each camera frame and forwards the styled
/// <see cref="OverlayBox"/>es it currently sees (plus its <see cref="FrameAnalyzer.ScanWindow"/>) to the
/// overlay. Owns the frame reference the platform handler hands it: the runner retains it while analyzing and
/// the pipeline releases its own reference after dispatch, so the native buffer frees once analysis finishes.
/// Keeps the last box set so boxes persist across dropped/slow frames until the analyzer replaces them (new
/// list) or clears them (<c>null</c>). An analyzer whose <see cref="FrameAnalyzer.IsEnabled"/> is <c>false</c>
/// (or no analyzer at all) reads as <see cref="HasAnalyzer"/> false, so the camera behaves as if it had none.
/// </summary>
sealed class CameraPipeline
{
    readonly object gate = new();
    AnalyzerRunner? runner;
    IFrameAnalyzer? analyzer;
    IReadOnlyList<OverlayBox> latest = [];
    RectF? scanWindow;
    volatile bool enabled;

    /// <summary>Invoked (off the UI thread) with the analyzer's boxes, its scan window, and the upright image size.</summary>
    public Action<IReadOnlyList<OverlayBox>, RectF?, int, int>? OnOverlays;

    /// <summary>
    /// Invoked when the active analyzer may have changed (assignment or an <see cref="FrameAnalyzer.IsEnabled"/>
    /// toggle). Platforms that bind use cases up-front (Android) use this to re-evaluate; those that gate frame
    /// delivery per-frame (Apple/Windows) can ignore it.
    /// </summary>
    public Action? OnActiveChanged;

    Action<Action>? dispatcher;
    int uprightW;
    int uprightH;
    int emittedW = -1;
    int emittedH = -1;

    public bool HasAnalyzer => this.enabled;

    /// <summary>Dispatcher the analyzer uses to raise its typed events on the UI thread (re-applied on change).</summary>
    public void SetDispatcher(Action<Action>? post)
    {
        lock (this.gate)
        {
            this.dispatcher = post;
            (this.analyzer as FrameAnalyzer)?.SetDispatcher(post);
        }
    }

    public void SetAnalyzer(IFrameAnalyzer? analyzer)
    {
        RectF? window;
        lock (this.gate)
        {
            if (this.analyzer is FrameAnalyzer old)
            {
                old.SetDispatcher(null);
                old.PropertyChanged -= this.OnAnalyzerPropertyChanged;
            }

            this.analyzer = analyzer;
            this.runner = analyzer is null ? null : new AnalyzerRunner(analyzer, this.OnResult);

            if (analyzer is FrameAnalyzer fa)
            {
                fa.SetDispatcher(this.dispatcher);
                fa.PropertyChanged += this.OnAnalyzerPropertyChanged;
            }

            this.Recompute();
            this.latest = [];
            this.emittedW = -1; // force the next frame to re-publish dims for the reticle
            this.emittedH = -1;
            window = this.scanWindow;
        }
        // clear any prior boxes and surface the new analyzer's scan window so the reticle draws before the
        // first detection
        this.OnOverlays?.Invoke([], window, this.uprightW, this.uprightH);
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

        // A standing scan-window reticle is drawn even with no detections, but its position depends on the
        // upright image aspect — which the overlay only learns when we publish. When the dimensions change (the
        // first frame after the analyzer is set, or a rotation) and a window is active, re-publish so the reticle
        // lands correctly without waiting for a detection.
        if ((this.uprightW != this.emittedW || this.uprightH != this.emittedH) && this.scanWindow is not null)
        {
            RectF? window = null;
            IReadOnlyList<OverlayBox> boxes = [];
            var emit = false;
            lock (this.gate)
            {
                if (this.enabled && this.scanWindow is not null)
                {
                    this.emittedW = this.uprightW;
                    this.emittedH = this.uprightH;
                    window = this.scanWindow;
                    boxes = this.latest;
                    emit = true;
                }
            }
            if (emit)
                this.OnOverlays?.Invoke(boxes, window, this.uprightW, this.uprightH);
        }

        var current = this.runner;
        if (current is not null && this.enabled)
            current.TrySubmit(frame, ct);

        frame.Dispose();
    }

    // Refresh the cached enabled flag + scan window. Call under gate.
    void Recompute()
    {
        var fa = this.analyzer as FrameAnalyzer;
        this.enabled = this.analyzer is not null && (fa?.IsEnabled ?? true);
        this.scanWindow = this.enabled ? fa?.ScanWindow : null;
    }

    void OnAnalyzerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != FrameAnalyzer.IsEnabledProperty.PropertyName &&
            e.PropertyName != FrameAnalyzer.ScanWindowProperty.PropertyName)
            return;

        var enabledChanged = e.PropertyName == FrameAnalyzer.IsEnabledProperty.PropertyName;
        IReadOnlyList<OverlayBox> boxes;
        RectF? window;
        lock (this.gate)
        {
            this.Recompute();
            if (!this.enabled)
                this.latest = []; // a disabled analyzer stops drawing immediately
            boxes = this.latest;
            window = this.scanWindow;
        }
        this.OnOverlays?.Invoke(boxes, window, this.uprightW, this.uprightH);
        if (enabledChanged)
            this.OnActiveChanged?.Invoke();
    }

    void OnResult(string analyzerId, IReadOnlyList<OverlayBox>? boxes)
    {
        IReadOnlyList<OverlayBox> emit;
        RectF? window;
        lock (this.gate)
        {
            if (!this.enabled)
                return; // a result in flight when the analyzer was disabled/swapped — drop it

            var next = boxes ?? [];
            // skip the redraw when a "nothing seen" result clears an already-empty set (the common steady state)
            if (next.Count == 0 && this.latest.Count == 0)
                return;

            this.latest = next;
            emit = next;
            window = this.scanWindow;
        }
        this.OnOverlays?.Invoke(emit, window, this.uprightW, this.uprightH);
    }
}
