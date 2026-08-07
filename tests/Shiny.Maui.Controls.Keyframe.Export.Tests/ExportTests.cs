using System.Text;
using Shiny.Maui.Controls.Keyframe.Export;
using Shiny.Controls.Keyframe.Graphics;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Shiny.Maui.Controls.Keyframe.Export.Tests;

public class ExportTests
{
    /// <summary>A red square that fades to blue over one second, filling the whole frame.</summary>
    static KeyframeScene FadingSquare(double seconds = 1d)
    {
        var scene = new KeyframeScene(64, 64) { Stretch = SceneStretch.Fill };
        var square = scene.Add(new RectangleLayer
        {
            Id = "square",
            Size = new SizeF(64, 64),
            Fill = Colors.Red
        });

        scene.Animation = TimelineBuilder
            .Create(TimeSpan.FromSeconds(seconds))
            .Fill(FillMode.Both)
            .AnimateFill(square, k => k.From(Colors.Red).To(Colors.Blue))
            .Build();

        return scene;
    }

    // --- Frame pump ---------------------------------------------------------------------

    [Fact]
    public void FrameCountIncludesTheClosingFrame()
    {
        var exporter = new FrameExporter(FadingSquare());
        var frames = exporter.Frames(new ExportOptions { Fps = 10 }).ToList();

        // One second at 10fps: ten intervals, eleven frames, ending exactly on the final pose.
        Assert.Equal(11, frames.Count);
        Assert.Equal(TimeSpan.Zero, frames[0].Time);
        Assert.Equal(TimeSpan.FromSeconds(1), frames[^1].Time);
    }

    [Fact]
    public void FrameTimesAreDerivedFromTheIndexSoTheyCannotDrift()
    {
        var exporter = new FrameExporter(FadingSquare(seconds: 3d));
        var frames = exporter.Frames(new ExportOptions { Fps = 60 }).ToList();

        // 1/60 of a second is not representable in TimeSpan's 100ns ticks, so the achievable
        // accuracy is one tick per frame — not the millisecond quantisation TimeSpan.FromSeconds
        // would impose, which at 60fps is a sixteenth of a frame and visibly uneven.
        for (var i = 0; i < frames.Count; i++)
        {
            var expected = Math.Min(i / 60d, 3d);
            var error = Math.Abs(frames[i].Time.TotalSeconds - expected);

            Assert.True(error <= 1e-7d, $"Frame {i} was off by {error:E3} seconds, more than one tick.");
        }
    }

    [Fact]
    public void ProgressRunsZeroToOne()
    {
        var frames = new FrameExporter(FadingSquare()).Frames(new ExportOptions { Fps = 4 }).ToList();

        Assert.Equal(0d, frames[0].Progress, 6);
        Assert.Equal(1d, frames[^1].Progress, 6);
    }

    [Fact]
    public void ExportIsDeterministic()
    {
        // Two runs of the same export must produce byte-identical frames. This is the property the
        // whole stateless-evaluation design exists to guarantee.
        var first = new FrameExporter(FadingSquare()).Frames(new ExportOptions { Fps = 5 }).ToList();
        var second = new FrameExporter(FadingSquare()).Frames(new ExportOptions { Fps = 5 }).ToList();

        Assert.Equal(first.Count, second.Count);

        for (var i = 0; i < first.Count; i++)
            Assert.Equal(first[i].Pixels, second[i].Pixels);
    }

    [Fact]
    public void TheAnimationActuallyChangesBetweenFrames()
    {
        var frames = new FrameExporter(FadingSquare()).Frames(new ExportOptions { Fps = 4 }).ToList();

        var start = PixelAt(frames[0], 32, 32);
        var end = PixelAt(frames[^1], 32, 32);

        Assert.True(start.R > 128, $"Expected the first frame to be red, got {start}.");
        Assert.True(end.B > 128, $"Expected the last frame to be blue, got {end}.");
    }

    [Fact]
    public void ScaleMultipliesTheOutputSize()
    {
        var frame = new FrameExporter(FadingSquare())
            .Frames(new ExportOptions { Fps = 1, Scale = 2d })
            .First();

        Assert.Equal(128, frame.Width);
        Assert.Equal(128, frame.Height);
        Assert.Equal(128 * 128 * 4, frame.Pixels.Length);
    }

    [Fact]
    public void ExplicitSizeOverridesTheDesignSize()
    {
        var frame = new FrameExporter(FadingSquare())
            .Frames(new ExportOptions { Fps = 1, Size = new SizeF(200, 100) })
            .First();

        Assert.Equal(200, frame.Width);
        Assert.Equal(100, frame.Height);
    }

    [Fact]
    public void InfiniteAnimationsNeedAnExplicitDuration()
    {
        var scene = new KeyframeScene(32, 32);
        var square = scene.Add(new RectangleLayer { Size = new SizeF(32, 32), Fill = Colors.Red });

        scene.Animation = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .RepeatForever()
            .AnimateOpacity(square, k => k.From(0f).To(1f))
            .Build();

        var exporter = new FrameExporter(scene);

        var error = Assert.Throws<InvalidOperationException>(
            () => exporter.Frames(new ExportOptions { Fps = 10 }).ToList());
        Assert.Contains("repeats forever", error.Message, StringComparison.OrdinalIgnoreCase);

        // ...but it exports happily once told how much to render.
        var frames = exporter.Frames(new ExportOptions { Fps = 10, Duration = TimeSpan.FromSeconds(2) }).ToList();
        Assert.Equal(21, frames.Count);
    }

    [Fact]
    public void ExportCanBeCancelledBetweenFrames()
    {
        using var cts = new CancellationTokenSource();
        var exporter = new FrameExporter(FadingSquare(seconds: 10d));

        var produced = 0;

        Assert.Throws<OperationCanceledException>(() =>
        {
            foreach (var _ in exporter.Frames(new ExportOptions { Fps = 30 }, cts.Token))
            {
                if (++produced == 3)
                    cts.Cancel();
            }
        });

        Assert.Equal(3, produced);
    }

    [Fact]
    public void FrameAtRendersASinglePosition()
    {
        var frame = new FrameExporter(FadingSquare()).FrameAt(0d);
        Assert.True(PixelAt(frame, 32, 32).R > 128);
    }

    // --- GIF encoding -------------------------------------------------------------------

    [Fact]
    public void GifStartsWithTheExpectedHeaderAndEndsWithTheTrailer()
    {
        var bytes = EncodeGif(fps: 10);

        Assert.Equal("GIF89a", Encoding.ASCII.GetString(bytes, 0, 6));
        Assert.Equal(0x3B, bytes[^1]);

        // Logical screen descriptor carries the frame size, little-endian.
        Assert.Equal(64, bytes[6] | (bytes[7] << 8));
        Assert.Equal(64, bytes[8] | (bytes[9] << 8));
    }

    [Fact]
    public void GifDeclaresTheNetscapeLoopExtension()
    {
        var bytes = EncodeGif(fps: 10);
        Assert.Contains("NETSCAPE2.0", Encoding.ASCII.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void GifDecodesToTheExpectedNumberOfFrames()
    {
        // The real test of the encoder: hand the bytes to an independent decoder and see whether it
        // agrees. A hand-rolled LZW implementation that is subtly wrong still produces a file.
        var bytes = EncodeGif(fps: 10);

        using var codec = SKCodec.Create(new SKMemoryStream(bytes));

        Assert.NotNull(codec);
        Assert.Equal(11, codec.FrameCount);
        Assert.Equal(64, codec.Info.Width);
        Assert.Equal(64, codec.Info.Height);
    }

    [Fact]
    public void DecodedGifFramesCarryTheAnimatedColors()
    {
        var bytes = EncodeGif(fps: 4);

        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        Assert.NotNull(codec);

        var first = DecodeFrame(codec, 0);
        var last = DecodeFrame(codec, codec.FrameCount - 1);

        Assert.True(first.Red > 128, $"Expected the first decoded frame to be red, got {first}.");
        Assert.True(last.Blue > 128, $"Expected the last decoded frame to be blue, got {last}.");
    }

    [Fact]
    public void DecodedGifFrameDelaysMatchTheRequestedRate()
    {
        var bytes = EncodeGif(fps: 25);

        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        Assert.NotNull(codec);

        // 25fps divides 100 exactly, so every frame should be 4 hundredths — 40ms.
        Assert.All(codec.FrameInfo, info => Assert.Equal(40, info.Duration));
    }

    [Fact]
    public void TransparencyIsPreservedThroughQuantisation()
    {
        var scene = new KeyframeScene(32, 32) { Background = null };
        var square = scene.Add(new RectangleLayer
        {
            Size = new SizeF(16, 16),
            Position = new PointF(8, 8),
            Fill = Colors.Red
        });

        scene.Animation = TimelineBuilder
            .Create(TimeSpan.FromSeconds(1))
            .Fill(FillMode.Both)
            .AnimateOpacity(square, k => k.From(1f).To(1f))
            .Build();

        var frame = new FrameExporter(scene).FrameAt(0d, new ExportOptions { Fps = 1 });
        var result = ColorQuantizer.Quantize(frame.Pixels);

        Assert.Equal(0, result.TransparentIndex);

        // The corner is outside the square, so it must map to the transparent slot.
        Assert.Equal(0, result.Indices[0]);
    }

    [Fact]
    public void FullyOpaqueFramesReserveNoTransparentSlot()
    {
        var frame = new FrameExporter(FadingSquare()).FrameAt(0d);
        var result = ColorQuantizer.Quantize(frame.Pixels);

        Assert.Equal(-1, result.TransparentIndex);
    }

    [Fact]
    public void QuantiserHonoursThePaletteLimit()
    {
        var frame = new FrameExporter(FadingSquare()).FrameAt(0.5d);

        foreach (var limit in new[] { 2, 8, 64, 256 })
        {
            var result = ColorQuantizer.Quantize(frame.Pixels, limit);

            Assert.True(result.Palette.Length <= limit,
                $"Palette of {result.Palette.Length} exceeded the limit of {limit}.");
            Assert.All(result.Indices, i => Assert.True(i < result.Palette.Length));
        }
    }

    [Fact]
    public void EncodingWithNoFramesIsRejected()
    {
        using var stream = new MemoryStream();
        Assert.Throws<ArgumentException>(() => GifEncoder.Encode(stream, [], 10));
    }

    static byte[] EncodeGif(int fps)
    {
        using var stream = new MemoryStream();
        var exporter = new FrameExporter(FadingSquare());

        GifEncoder.Encode(stream, exporter.Frames(new ExportOptions { Fps = fps }), fps);
        return stream.ToArray();
    }

    static SKColor DecodeFrame(SKCodec codec, int index)
    {
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        var options = new SKCodecOptions(index);
        var result = codec.GetPixels(info, bitmap.GetPixels(), options);

        Assert.True(result is SKCodecResult.Success or SKCodecResult.IncompleteInput,
            $"Decoding frame {index} returned {result}.");

        return bitmap.GetPixel(codec.Info.Width / 2, codec.Info.Height / 2);
    }

    static (byte R, byte G, byte B, byte A) PixelAt(ExportedFrame frame, int x, int y)
    {
        var offset = (y * frame.Width + x) * 4;
        return (frame.Pixels[offset], frame.Pixels[offset + 1], frame.Pixels[offset + 2], frame.Pixels[offset + 3]);
    }
}
