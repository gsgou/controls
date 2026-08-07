using Shiny.Controls.Keyframe.Graphics;
using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Keyframe.Export;

/// <summary>Settings for a frame export.</summary>
public sealed class ExportOptions
{
    int fps = 30;
    double scale = 1d;

    /// <summary>Frames per second. Must be positive.</summary>
    public int Fps
    {
        get => fps;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            fps = value;
        }
    }

    /// <summary>
    /// How much of the animation to render. Defaults to the animation's own duration, and is
    /// required when the animation repeats forever.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>Output size in pixels. Defaults to the scene's design size.</summary>
    public SizeF? Size { get; set; }

    /// <summary>Multiplies the output size, for rendering at 2× or 3×.</summary>
    public double Scale
    {
        get => scale;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0d);
            scale = value;
        }
    }

    /// <summary>Painted behind every frame. Null leaves the frame transparent.</summary>
    public Color? Background { get; set; }
}

/// <summary>One rendered frame.</summary>
/// <param name="Index">Zero-based frame number.</param>
/// <param name="Time">Position within the animation this frame was sampled at.</param>
/// <param name="Progress">Normalised position, 0 to 1, across the exported duration.</param>
/// <param name="Pixels">Premultiplied RGBA pixels, row major, four bytes per pixel.</param>
/// <param name="Width">Frame width in pixels.</param>
/// <param name="Height">Frame height in pixels.</param>
public readonly record struct ExportedFrame(
    int Index,
    TimeSpan Time,
    double Progress,
    byte[] Pixels,
    int Width,
    int Height);

/// <summary>
/// Renders a scene to discrete frames, offscreen and deterministically.
/// </summary>
/// <remarks>
/// <para>Nothing here touches a display link or a UI thread. The animation is sampled at exact
/// frame times through <see cref="IAnimationNode.Evaluate"/>, which is possible only because
/// evaluation is a pure function of time — the same property that makes scrubbing exact makes
/// export reproducible. Run the same export twice and you get identical bytes.</para>
/// <para>Frame times are computed as <c>index / fps</c> rather than accumulated, so a long export
/// cannot drift.</para>
/// </remarks>
public sealed class FrameExporter
{
    readonly KeyframeScene scene;
    readonly IFrameRenderer renderer;

    /// <summary>Creates an exporter.</summary>
    /// <param name="scene">The scene to render.</param>
    /// <param name="renderer">Rasteriser. Defaults to the Skia-backed one.</param>
    public FrameExporter(KeyframeScene scene, IFrameRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(scene);

        this.scene = scene;
        this.renderer = renderer ?? new SkiaFrameRenderer();
    }

    /// <summary>
    /// Renders each frame in turn. Frames are yielded lazily, so a long export never holds more
    /// than one frame's pixels in memory at a time.
    /// </summary>
    /// <param name="options">Export settings. Defaults to 30fps at the scene's design size.</param>
    /// <param name="cancellationToken">Stops the export between frames.</param>
    public IEnumerable<ExportedFrame> Frames(
        ExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ExportOptions();

        var duration = ResolveDuration(options);
        var (width, height) = ResolveSize(options);

        // Include the closing frame: a one-second export at 30fps renders 31 frames, ending exactly
        // on the final pose rather than one frame short of it.
        var frameCount = (int)Math.Round(duration.TotalSeconds * options.Fps) + 1;
        var background = options.Background ?? scene.Background;

        scene.Animation?.CaptureBaselines();

        for (var index = 0; index < frameCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Derive the time from the index rather than accumulating, so rounding cannot drift.
            // TimeSpan.FromSeconds rounds to whole milliseconds, which is coarser than a frame at
            // 60fps and would quantise every frame time — so build it from ticks instead.
            var time = TimeSpan.FromTicks(index * TimeSpan.TicksPerSecond / options.Fps);
            if (time > duration)
                time = duration;

            scene.Seek(time);

            var pixels = renderer.Render(width, height, background, canvas =>
                scene.Draw(canvas, new RectF(0f, 0f, width, height)));

            yield return new ExportedFrame(
                index,
                time,
                frameCount > 1 ? (double)index / (frameCount - 1) : 0d,
                pixels,
                width,
                height);
        }
    }

    /// <summary>Renders a single frame at a normalised position.</summary>
    public ExportedFrame FrameAt(double progress, ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        var duration = ResolveDuration(options);
        var (width, height) = ResolveSize(options);
        var time = duration * Math.Clamp(progress, 0d, 1d);

        scene.Animation?.CaptureBaselines();
        scene.Seek(time);

        var pixels = renderer.Render(width, height, options.Background ?? scene.Background,
            canvas => scene.Draw(canvas, new RectF(0f, 0f, width, height)));

        return new ExportedFrame(0, time, progress, pixels, width, height);
    }

    TimeSpan ResolveDuration(ExportOptions options)
    {
        if (options.Duration is { } explicitDuration)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(explicitDuration, TimeSpan.Zero);
            return explicitDuration;
        }

        var total = scene.Animation?.TotalDuration;

        if (total is null || total == TimeSpan.Zero)
            throw new InvalidOperationException(
                "The scene has no animation to export. Assign KeyframeScene.Animation, or set " +
                "ExportOptions.Duration to render a static scene for a fixed length.");

        if (total == TimeSpan.MaxValue)
            throw new InvalidOperationException(
                "This animation repeats forever, so it has no natural export length. Set " +
                "ExportOptions.Duration to say how much of it to render.");

        return total.Value;
    }

    (int Width, int Height) ResolveSize(ExportOptions options)
    {
        var size = options.Size ?? scene.DesignSize;

        var width = (int)Math.Round(size.Width * options.Scale);
        var height = (int)Math.Round(size.Height * options.Scale);

        if (width < 1 || height < 1)
            throw new InvalidOperationException(
                $"The requested output size resolves to {width}×{height} pixels. Check ExportOptions.Size and Scale.");

        return (width, height);
    }
}
