using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class CameraPipelineTests
{
    // The analyzer here returns already-completed ValueTasks, so the runner completes synchronously and the
    // pipeline publishes overlays before Process() returns — making these assertions deterministic.

    [Fact]
    public void Null_clears_and_a_new_list_replaces()
    {
        var pipeline = new CameraPipeline();
        IReadOnlyList<OverlayBox> latest = [];
        pipeline.OnOverlays = (boxes, _, _, _) => latest = boxes;

        var a = new OverlayBox(new RectF(0, 0, 0.5f, 0.5f));
        var b = new OverlayBox(new RectF(0.5f, 0.5f, 0.5f, 0.5f));
        pipeline.SetAnalyzer(new ScriptedAnalyzer("x", [a], null, [b]));

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([a]);                 // sees A

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBeEmpty();               // null -> cleared

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([b]);                 // replaced with B
    }


    [Fact]
    public void Swapping_the_analyzer_clears_prior_boxes()
    {
        var pipeline = new CameraPipeline();
        IReadOnlyList<OverlayBox> latest = [new OverlayBox(new RectF(0, 0, 1, 1))];
        pipeline.OnOverlays = (boxes, _, _, _) => latest = boxes;

        var a = new OverlayBox(new RectF(0, 0, 0.4f, 0.4f));
        pipeline.SetAnalyzer(new ScriptedAnalyzer("a", [a], [a]));
        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([a]);

        pipeline.SetAnalyzer(null);           // swapping/clearing the analyzer wipes its boxes immediately
        latest.ShouldBeEmpty();
    }


    [Fact]
    public void Scan_window_is_surfaced_to_the_overlay()
    {
        var pipeline = new CameraPipeline();
        RectF? window = null;
        pipeline.OnOverlays = (_, w, _, _) => window = w;

        var analyzer = new ToggleAnalyzer("t", new OverlayBox(new RectF(0, 0, 1, 1)))
        {
            ScanWindow = new RectF(0.1f, 0.4f, 0.8f, 0.2f)
        };
        pipeline.SetAnalyzer(analyzer);       // emits an initial overlay carrying the scan window
        window.ShouldBe(new RectF(0.1f, 0.4f, 0.8f, 0.2f));

        analyzer.ScanWindow = new RectF(0.2f, 0.2f, 0.6f, 0.6f);
        window.ShouldBe(new RectF(0.2f, 0.2f, 0.6f, 0.6f));   // a change re-surfaces it
    }


    [Fact]
    public void Scan_window_reticle_gets_correct_dims_on_the_first_frame_without_a_detection()
    {
        var pipeline = new CameraPipeline();
        RectF? window = null;
        int w = 0, h = 0;
        pipeline.OnOverlays = (_, win, iw, ih) => { window = win; w = iw; h = ih; };

        var analyzer = new QuietAnalyzer("q") { ScanWindow = new RectF(0.1f, 0.4f, 0.8f, 0.2f) };
        pipeline.SetAnalyzer(analyzer);      // initial emit carries the window but no frame dims yet
        w.ShouldBe(0);

        pipeline.Process(new FakeFrame(), default);   // first frame establishes dims -> reticle re-published
        window.ShouldBe(new RectF(0.1f, 0.4f, 0.8f, 0.2f));
        w.ShouldBe(8);                       // FakeFrame is 8x8 -> overlay now has the real aspect
        h.ShouldBe(8);
    }

    [Fact]
    public void Disabled_analyzer_is_skipped_its_boxes_cleared_and_resumes_on_enable()
    {
        var pipeline = new CameraPipeline();
        IReadOnlyList<OverlayBox> latest = [];
        pipeline.OnOverlays = (boxes, _, _, _) => latest = boxes;

        var box = new OverlayBox(new RectF(0, 0, 0.5f, 0.5f));
        var analyzer = new ToggleAnalyzer("t", box);
        pipeline.SetAnalyzer(analyzer);

        pipeline.HasAnalyzer.ShouldBeTrue();
        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([box]);
        analyzer.Calls.ShouldBe(1);

        analyzer.IsEnabled = false;
        pipeline.HasAnalyzer.ShouldBeFalse();    // disabled -> behaves as "no analyzer"
        latest.ShouldBeEmpty();                  // disabling cleared its boxes immediately

        pipeline.Process(new FakeFrame(), default);
        analyzer.Calls.ShouldBe(1);              // skipped while disabled

        analyzer.IsEnabled = true;
        pipeline.HasAnalyzer.ShouldBeTrue();
        pipeline.Process(new FakeFrame(), default);
        analyzer.Calls.ShouldBe(2);              // runs again with state intact
        latest.ShouldBe([box]);
    }

    [Fact]
    public void OnActiveChanged_fires_on_set_and_on_enabled_toggle()
    {
        var pipeline = new CameraPipeline();
        var fires = 0;
        pipeline.OnActiveChanged = () => fires++;

        var analyzer = new ToggleAnalyzer("t", new OverlayBox(new RectF(0, 0, 1, 1)));
        pipeline.SetAnalyzer(analyzer);
        fires.ShouldBe(1);                       // analyzer assigned

        analyzer.IsEnabled = false;
        fires.ShouldBe(2);                       // toggled off

        analyzer.IsEnabled = true;
        fires.ShouldBe(3);                       // toggled on
    }


    // A FrameAnalyzer (so it carries the IsEnabled/ScanWindow bindables) that counts how often it actually runs.
    sealed class ToggleAnalyzer(string id, OverlayBox box) : FrameAnalyzer
    {
        public int Calls;

        public override string Id => id;

        public override ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
        {
            this.Calls++;
            return new(new[] { box });
        }
    }


    // A FrameAnalyzer (carries ScanWindow) that never reports a detection — for the standing-reticle case.
    sealed class QuietAnalyzer(string id) : FrameAnalyzer
    {
        public override string Id => id;
        public override ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
            => new((IReadOnlyList<OverlayBox>?)null);
    }


    sealed class ScriptedAnalyzer(string id, params IReadOnlyList<OverlayBox>?[] results) : IFrameAnalyzer
    {
        readonly Queue<IReadOnlyList<OverlayBox>?> queue = new(results);

        public string Id { get; } = id;

        public ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
            => new(this.queue.Count > 0 ? this.queue.Dequeue() : null);
    }


    sealed class FakeFrame : CameraFrame
    {
        public override int Width => 8;
        public override int Height => 8;
        public override int Rotation => 0;
        public override bool IsMirrored => false;
        public override CameraFrameFormat Format => CameraFrameFormat.Grayscale8;
        protected override byte[] MaterializeLuminance() => new byte[this.Width * this.Height];
    }
}
