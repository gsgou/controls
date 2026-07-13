using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class VideoOverlayTests
{
    static readonly RectF Frame = new(0, 0, 1920, 1080);
    static VideoOverlayContext Ctx => new(TimeSpan.FromSeconds(2), 5, 1920, 1080, CameraFacing.Front);

    [Fact]
    public void DelegateVideoOverlay_forwards_all_arguments()
    {
        RectF? seenFrame = null;
        VideoOverlayContext? seenCtx = null;
        var overlay = new DelegateVideoOverlay((canvas, frame, ctx) =>
        {
            seenFrame = frame;
            seenCtx = ctx;
        });

        overlay.DrawOverlay(null!, Frame, Ctx);

        seenFrame.ShouldBe(Frame);
        seenCtx.ShouldBe(Ctx);
    }

    [Fact]
    public void DelegateVideoOverlay_null_delegate_throws()
        => Should.Throw<ArgumentNullException>(() => new DelegateVideoOverlay(null!));

    [Fact]
    public void DrawableVideoOverlay_draws_the_drawable_over_the_full_frame()
    {
        var drawable = new RecordingDrawable();
        var overlay = new DrawableVideoOverlay(drawable);

        overlay.DrawOverlay(null!, Frame, Ctx);

        drawable.Calls.ShouldBe(1);
        drawable.LastRect.ShouldBe(Frame);
    }

    [Fact]
    public void DrawableVideoOverlay_null_drawable_throws()
        => Should.Throw<ArgumentNullException>(() => new DrawableVideoOverlay(null!));

    [Fact]
    public void VideoOverlayContext_is_a_value_with_structural_equality()
    {
        var a = new VideoOverlayContext(TimeSpan.FromSeconds(1), 3, 640, 480, CameraFacing.Back);
        var b = new VideoOverlayContext(TimeSpan.FromSeconds(1), 3, 640, 480, CameraFacing.Back);
        a.ShouldBe(b);
        a.Width.ShouldBe(640);
        a.FrameIndex.ShouldBe(3);
        a.Facing.ShouldBe(CameraFacing.Back);
    }

    sealed class RecordingDrawable : IDrawable
    {
        public int Calls { get; private set; }
        public RectF LastRect { get; private set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            this.Calls++;
            this.LastRect = dirtyRect;
        }
    }
}
