using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using IImage = Microsoft.Maui.Graphics.IImage;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

/// <summary>
/// The layer contract. Nothing here needs a device — what it pins is the part of the contract a platform
/// compositor <i>relies</i> on, and whose failure mode is a wrong or stale overlay burned into a recording
/// rather than an exception.
/// </summary>
public class CompositedVideoOverlayTests
{
    static VideoOverlayContext Ctx => new(TimeSpan.FromSeconds(2), 5, 1920, 1080, CameraFacing.Back);

    sealed class StubImage : IImage
    {
        public float Width => 100;
        public float Height => 50;
        public void Dispose() { }
        public void Draw(ICanvas canvas, RectF dirtyRect) { }
        public IImage Downsize(float maxWidthOrHeight, bool disposeOriginal = false) => this;
        public IImage Downsize(float maxWidth, float maxHeight, bool disposeOriginal = false) => this;
        public IImage Resize(float width, float height, ResizeMode resizeMode = ResizeMode.Fit, bool disposeOriginal = false) => this;
        public void Save(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1) { }
        public Task SaveAsync(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1) => Task.CompletedTask;
        public IImage ToPlatformImage() => this;
    }

    sealed class StubOverlay(IReadOnlyList<VideoOverlayLayer>? layers) : ICompositedVideoOverlayRenderer
    {
        public int Draws { get; private set; }

        public void DrawOverlay(ICanvas canvas, RectF frame, VideoOverlayContext context) => this.Draws++;

        public IReadOnlyList<VideoOverlayLayer>? GetLayers(VideoOverlayContext context) => layers;
    }

    /// <summary>
    /// ⚠️ The distinction the whole fallback rests on. Null means "I cannot describe myself this frame, draw
    /// me"; empty means "there is genuinely nothing on this frame". Conflating them either loses the overlay
    /// entirely or costs a pointless draw on every frame.
    /// </summary>
    [Fact]
    public void Null_layers_and_empty_layers_are_different_answers()
    {
        new StubOverlay(null).GetLayers(Ctx).ShouldBeNull();
        new StubOverlay([]).GetLayers(Ctx).ShouldBeEmpty();
    }

    [Fact]
    public void A_composited_renderer_is_still_an_ordinary_renderer()
    {
        // The platform falls back to DrawOverlay whenever it cannot take the layer path, so implementing the
        // richer interface must never be a way to end up with no drawing at all.
        var overlay = new StubOverlay(null);

        ((IVideoOverlayRenderer)overlay).DrawOverlay(null!, new RectF(0, 0, 1920, 1080), Ctx);

        overlay.Draws.ShouldBe(1);
    }

    [Fact]
    public void A_layer_carries_where_it_goes_and_which_version_it_is()
    {
        var layer = new VideoOverlayLayer(new StubImage(), new RectF(32, 32, 100, 50), 7);

        layer.Destination.ShouldBe(new RectF(32, 32, 100, 50));
        layer.Version.ShouldBe(7);
    }

    /// <summary>
    /// Layers are compared by value, which is what lets a compositor keep the previous frame's list and skip
    /// work when nothing moved.
    /// </summary>
    [Fact]
    public void Layers_with_the_same_image_position_and_version_are_equal()
    {
        var image = new StubImage();
        var a = new VideoOverlayLayer(image, new RectF(0, 0, 100, 50), 3);
        var b = new VideoOverlayLayer(image, new RectF(0, 0, 100, 50), 3);
        var moved = a with { Destination = new RectF(0, 1, 100, 50) };
        var repainted = a with { Version = 4 };

        a.ShouldBe(b);
        a.ShouldNotBe(moved);
        a.ShouldNotBe(repainted);
    }
}
