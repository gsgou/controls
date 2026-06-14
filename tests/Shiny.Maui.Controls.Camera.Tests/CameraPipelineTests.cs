using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Internal;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class CameraPipelineTests
{
    // The analyzers here return already-completed ValueTasks, so the runner completes synchronously and the
    // pipeline publishes overlays before Process() returns — making these assertions deterministic.

    [Fact]
    public void Null_clears_and_a_new_list_replaces()
    {
        var pipeline = new CameraPipeline();
        IReadOnlyList<OverlayBox> latest = [];
        pipeline.OnOverlays = (boxes, _, _) => latest = boxes;

        var a = new OverlayBox(new RectF(0, 0, 0.5f, 0.5f));
        var b = new OverlayBox(new RectF(0.5f, 0.5f, 0.5f, 0.5f));
        pipeline.SetAnalyzers([new ScriptedAnalyzer("x", [a], null, [b])]);

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([a]);                 // sees A

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBeEmpty();               // null -> cleared

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([b]);                 // replaced with B
    }

    [Fact]
    public void Aggregates_analyzers_and_one_persists_while_another_clears()
    {
        var pipeline = new CameraPipeline();
        IReadOnlyList<OverlayBox> latest = [];
        pipeline.OnOverlays = (boxes, _, _) => latest = boxes;

        var a = new OverlayBox(new RectF(0, 0, 0.4f, 0.4f));
        var b = new OverlayBox(new RectF(0.6f, 0.6f, 0.4f, 0.4f));
        pipeline.SetAnalyzers(
        [
            new ScriptedAnalyzer("a", [a], null),   // frame 1: A, frame 2: clear
            new ScriptedAnalyzer("b", [b], [b])     // frame 1: B, frame 2: still B
        ]);

        pipeline.Process(new FakeFrame(), default);
        latest.Count.ShouldBe(2);               // A + B aggregated

        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([b]);                   // A cleared, B retained
    }


    [Fact]
    public void Disabled_analyzer_is_skipped_its_boxes_cleared_and_resumes_on_enable()
    {
        var pipeline = new CameraPipeline();
        IReadOnlyList<OverlayBox> latest = [];
        pipeline.OnOverlays = (boxes, _, _) => latest = boxes;

        var box = new OverlayBox(new RectF(0, 0, 0.5f, 0.5f));
        var analyzer = new ToggleAnalyzer("t", box);
        pipeline.SetAnalyzers([analyzer]);

        pipeline.HasAnalyzers.ShouldBeTrue();
        pipeline.Process(new FakeFrame(), default);
        latest.ShouldBe([box]);
        analyzer.Calls.ShouldBe(1);

        analyzer.IsEnabled = false;
        pipeline.HasAnalyzers.ShouldBeFalse();   // enabled count is now 0 -> behaves as "no analyzers"
        latest.ShouldBeEmpty();                  // disabling cleared its boxes immediately

        pipeline.Process(new FakeFrame(), default);
        analyzer.Calls.ShouldBe(1);              // skipped while disabled

        analyzer.IsEnabled = true;
        pipeline.HasAnalyzers.ShouldBeTrue();
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
        pipeline.SetAnalyzers([analyzer]);
        fires.ShouldBe(1);                       // collection edit

        analyzer.IsEnabled = false;
        fires.ShouldBe(2);                       // toggled off

        analyzer.IsEnabled = true;
        fires.ShouldBe(3);                       // toggled on
    }


    // A FrameAnalyzer (so it carries the IsEnabled bindable) that counts how often it actually runs.
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
