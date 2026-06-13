using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Motion;

/// <summary>
/// Detects motion by comparing successive frames' luminance. Fully managed and cross-platform — works on
/// every target including those without native ML. Raises <see cref="MotionChanged"/> when motion starts or
/// stops, and draws a box bounding the changed region while motion continues (cleared when still).
/// </summary>
public class MotionAnalyzer : FrameAnalyzer
{
    byte[]? previous;
    int prevWidth;
    int prevHeight;

    /// <inheritdoc/>
    public override string Id => "shiny.camera.motion";

    /// <summary>Per-pixel luminance delta (0–255) that counts a pixel as changed. Default 25.</summary>
    public int PixelThreshold { get; set; } = 25;

    /// <summary>Fraction of sampled pixels that must change to report motion (0–1). Default 0.04.</summary>
    public double AreaThreshold { get; set; } = 0.04;

    /// <summary>Sampling stride — every Nth pixel on each axis is compared (higher = faster). Default 4.</summary>
    public int SampleStride { get; set; } = 4;

    /// <summary>Box outline + caption color. Default an amber accent.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#F59E0B");

    /// <summary>Caption drawn on the motion box (null/empty for none). Default "Motion".</summary>
    public string? Label { get; set; } = "Motion";

    /// <summary>Command invoked (with the <see cref="MotionEventArgs"/>) when motion starts or stops.</summary>
    public static readonly BindableProperty MotionChangedCommandProperty = BindableProperty.Create(
        nameof(MotionChangedCommand), typeof(ICommand), typeof(MotionAnalyzer));

    /// <inheritdoc cref="MotionChangedCommandProperty"/>
    public ICommand? MotionChangedCommand
    {
        get => (ICommand?)this.GetValue(MotionChangedCommandProperty);
        set => this.SetValue(MotionChangedCommandProperty, value);
    }

    /// <summary>
    /// Optional selector deciding the boxes to draw while in motion; return <c>null</c> for no overlay. When
    /// unset the analyzer draws a single <see cref="BoxColor"/> box over the changed region.
    /// </summary>
    public Func<MotionEventArgs, IReadOnlyList<OverlayBox>?>? OverlayProvider { get; set; }

    /// <summary>Raised on the UI thread when motion starts or stops.</summary>
    public event EventHandler<MotionEventArgs>? MotionChanged;

    bool lastMotion;

    /// <inheritdoc/>
    public override ValueTask<IReadOnlyList<OverlayBox>?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var lum = frame.GetLuminance();
        int w = frame.Width, h = frame.Height;
        var stride = Math.Max(1, this.SampleStride);

        if (this.previous == null || this.prevWidth != w || this.prevHeight != h || this.previous.Length != lum.Length)
        {
            this.previous = lum.ToArray();
            this.prevWidth = w;
            this.prevHeight = h;
            return default;
        }

        int changed = 0, total = 0, minX = w, minY = h, maxX = 0, maxY = 0;
        var prev = this.previous;
        for (var y = 0; y < h; y += stride)
        {
            var rowBase = y * w;
            for (var x = 0; x < w; x += stride)
            {
                var i = rowBase + x;
                total++;
                if (Math.Abs(lum[i] - prev[i]) <= this.PixelThreshold)
                    continue;

                changed++;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        // refresh the reference frame
        lum.CopyTo(this.previous);

        var ratio = total == 0 ? 0d : (double)changed / total;
        var motion = ratio >= this.AreaThreshold;

        RectF? region = null;
        if (motion && maxX >= minX)
        {
            var raw = new RectF((float)minX / w, (float)minY / h, (float)(maxX - minX) / w, (float)(maxY - minY) / h);
            region = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
        }

        var args = new MotionEventArgs(motion, region, (float)Math.Min(1d, ratio));
        if (motion != this.lastMotion)
        {
            this.lastMotion = motion;
            this.Emit(() => this.MotionChanged?.Invoke(this, args), this.MotionChangedCommand, args);
        }

        if (region is null)
            return default; // no motion -> clear the box

        var boxes = this.ResolveOverlay(args, this.OverlayProvider,
            () => new[] { new OverlayBox(region.Value, this.BoxColor, this.Label, this.BoxColor) });
        return new ValueTask<IReadOnlyList<OverlayBox>?>(boxes);
    }
}
