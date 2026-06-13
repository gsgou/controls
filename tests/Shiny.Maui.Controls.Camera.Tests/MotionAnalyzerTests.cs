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
    public async Task MotionChangedCommand_executes_on_a_transition()
    {
        MotionEventArgs? captured = null;
        var a = new MotionAnalyzer
        {
            MotionChangedCommand = new Command<MotionEventArgs>(e => captured = e)
        };

        await RunMotion(a);

        captured.ShouldNotBeNull();
        captured!.InMotion.ShouldBeTrue();
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
}
