using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Motion;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class MotionAnalyzerTests
{
    const int W = 8, H = 8;
    static FakeLumFrame Still => new(new byte[W * H]);                       // all zero
    static FakeLumFrame Moved => new(Enumerable.Repeat((byte)255, W * H).ToArray());

    // First frame seeds the reference; the second (fully changed) trips motion.
    static async Task<IReadOnlyList<OverlayBox>?> RunMotion(MotionAnalyzer a)
    {
        await a.AnalyzeAsync(Still, default);
        return await a.AnalyzeAsync(Moved, default);
    }

    [Fact]
    public async Task ShowBoundingBox_false_draws_nothing_even_on_motion()
    {
        var a = new MotionAnalyzer { ShowBoundingBox = false };
        (await RunMotion(a)).ShouldBeNull();
    }

    [Fact]
    public async Task OverlayProvider_overrides_the_default_box()
    {
        var marker = new OverlayBox(new RectF(0, 0, 1, 1), Colors.Red);
        var a = new MotionAnalyzer { OverlayProvider = _ => new[] { marker } };

        var boxes = await RunMotion(a);
        boxes.ShouldNotBeNull();
        boxes!.ShouldContain(marker);
    }

    [Fact]
    public async Task OverlayProvider_returning_null_draws_nothing()
    {
        var a = new MotionAnalyzer { OverlayProvider = _ => null };
        (await RunMotion(a)).ShouldBeNull();
    }

    [Fact]
    public async Task Default_draws_a_box_on_motion()
    {
        var a = new MotionAnalyzer();
        var boxes = await RunMotion(a);
        boxes.ShouldNotBeNull();
        boxes!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Two_separated_motion_areas_yield_two_boxes()
    {
        const int n = 64;
        var still = new byte[n * n];
        var moved = new byte[n * n];
        FillBlock(moved, n, 0, 0, 16);    // top-left changes
        FillBlock(moved, n, 48, 48, 16);  // bottom-right changes; the gap between stays still

        var a = new MotionAnalyzer();
        await a.AnalyzeAsync(new SizedLumFrame(still, n, n), default);
        var boxes = await a.AnalyzeAsync(new SizedLumFrame(moved, n, n), default);

        boxes.ShouldNotBeNull();
        boxes!.Count.ShouldBe(2);   // one box per region, not a single box spanning both
    }

    [Fact]
    public async Task Regions_are_reported_on_the_event_args()
    {
        const int n = 64;
        var still = new byte[n * n];
        var moved = new byte[n * n];
        FillBlock(moved, n, 0, 0, 16);
        FillBlock(moved, n, 48, 48, 16);

        MotionEventArgs? captured = null;
        // EnterFrames=1 so a single transition reports immediately (debounce covered separately)
        var a = new MotionAnalyzer { EnterFrames = 1, MotionChangedCommand = new Command<MotionEventArgs>(e => captured = e) };
        await a.AnalyzeAsync(new SizedLumFrame(still, n, n), default);
        await a.AnalyzeAsync(new SizedLumFrame(moved, n, n), default);

        captured.ShouldNotBeNull();
        captured!.Regions.Count.ShouldBe(2);
        captured.Region.ShouldNotBeNull();   // union still provided for back-compat
    }

    static void FillBlock(byte[] buf, int w, int x0, int y0, int size)
    {
        for (var y = y0; y < y0 + size; y++)
            for (var x = x0; x < x0 + size; x++)
                buf[y * w + x] = 255;
    }

    [Fact]
    public async Task MotionChangedCommand_executes_on_a_transition()
    {
        MotionEventArgs? captured = null;
        var a = new MotionAnalyzer
        {
            EnterFrames = 1,   // fire on the first transition
            MotionChangedCommand = new Command<MotionEventArgs>(e => captured = e)
        };

        await RunMotion(a);

        captured.ShouldNotBeNull();
        captured!.InMotion.ShouldBeTrue();
    }


    [Fact]
    public async Task Motion_event_is_debounced_by_EnterFrames()
    {
        var events = new List<bool>();
        var a = new MotionAnalyzer
        {
            EnterFrames = 3,
            MotionChangedCommand = new Command<MotionEventArgs>(e => events.Add(e.InMotion))
        };

        // alternate fully-different frames so every comparison registers change
        await a.AnalyzeAsync(Still, default);   // seed
        await a.AnalyzeAsync(Moved, default);   // change #1
        events.Count.ShouldBe(0);
        await a.AnalyzeAsync(Still, default);   // change #2
        events.Count.ShouldBe(0);
        await a.AnalyzeAsync(Moved, default);   // change #3 -> motion started
        events.Count.ShouldBe(1);
        events[0].ShouldBeTrue();
        await a.AnalyzeAsync(Still, default);   // still moving -> no re-fire
        events.Count.ShouldBe(1);
    }


    [Fact]
    public async Task Motion_stops_only_after_ExitFrames_of_stillness()
    {
        var events = new List<bool>();
        var a = new MotionAnalyzer
        {
            EnterFrames = 1,
            ExitFrames = 2,
            MotionChangedCommand = new Command<MotionEventArgs>(e => events.Add(e.InMotion))
        };

        await a.AnalyzeAsync(Still, default);   // seed
        await a.AnalyzeAsync(Moved, default);   // change -> started
        events.Count.ShouldBe(1);
        events[0].ShouldBeTrue();
        await a.AnalyzeAsync(Moved, default);   // identical to prev -> still #1, not yet stopped
        events.Count.ShouldBe(1);
        await a.AnalyzeAsync(Moved, default);   // still #2 -> stopped
        events.Count.ShouldBe(2);
        events[1].ShouldBeFalse();
    }


    sealed class FakeLumFrame(byte[] luminance) : CameraFrame
    {
        public override int Width => W;
        public override int Height => H;
        public override int Rotation => 0;
        public override bool IsMirrored => false;
        public override CameraFrameFormat Format => CameraFrameFormat.Grayscale8;
        protected override byte[] MaterializeLuminance() => luminance;
    }

    sealed class SizedLumFrame(byte[] luminance, int width, int height) : CameraFrame
    {
        public override int Width => width;
        public override int Height => height;
        public override int Rotation => 0;
        public override bool IsMirrored => false;
        public override CameraFrameFormat Format => CameraFrameFormat.Grayscale8;
        protected override byte[] MaterializeLuminance() => luminance;
    }
}
