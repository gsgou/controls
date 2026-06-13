namespace Shiny.Maui.Controls.Camera.Internal;

/// <summary>
/// Wraps one <see cref="IFrameAnalyzer"/> with a max-in-flight of one. A frame submitted while the
/// analyzer is still working is dropped for this analyzer, giving each analyzer independent backpressure
/// (a slow OCR pass never stalls a fast barcode pass). On accept it retains the shared frame and disposes
/// it when the analysis completes. Forwards every completed result — including <c>null</c> (clear) — to the
/// pipeline.
/// </summary>
sealed class AnalyzerRunner(IFrameAnalyzer analyzer, Action<string, IReadOnlyList<OverlayBox>?> onResult)
{
    int busy;

    public string Id => analyzer.Id;

    /// <summary>Submit a frame. Returns false (frame untouched) when the analyzer is busy.</summary>
    public bool TrySubmit(CameraFrame frame, CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref this.busy, 1, 0) != 0)
            return false;

        frame.Retain();
        _ = this.RunAsync(frame, ct);
        return true;
    }

    async Task RunAsync(CameraFrame frame, CancellationToken ct)
    {
        try
        {
            var result = await analyzer.AnalyzeAsync(frame, ct).ConfigureAwait(false);
            onResult(analyzer.Id, result);
        }
        catch
        {
            // a misbehaving analyzer must not tear down the pipeline
        }
        finally
        {
            frame.Dispose();
            Interlocked.Exchange(ref this.busy, 0);
        }
    }
}
