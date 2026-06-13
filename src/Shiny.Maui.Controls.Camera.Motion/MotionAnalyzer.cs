using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Motion;

/// <summary>
/// Detects motion by comparing successive frames' luminance. Fully managed and cross-platform — works on
/// every target including those without native ML. Emits a single <see cref="DetectionType.Motion"/>
/// detection bounding the changed region when enough pixels move between frames.
/// </summary>
public class MotionAnalyzer : IFrameAnalyzer
{
    byte[]? previous;
    int prevWidth;
    int prevHeight;

    /// <inheritdoc/>
    public string Id => "shiny.camera.motion";

    /// <summary>Per-pixel luminance delta (0–255) that counts a pixel as changed. Default 25.</summary>
    public int PixelThreshold { get; set; } = 25;

    /// <summary>Fraction of sampled pixels that must change to report motion (0–1). Default 0.04.</summary>
    public double AreaThreshold { get; set; } = 0.04;

    /// <summary>Sampling stride — every Nth pixel on each axis is compared (higher = faster). Default 4.</summary>
    public int SampleStride { get; set; } = 4;

    /// <summary>Raised (on the analysis thread) when motion starts/stops.</summary>
    public event EventHandler<bool>? MotionDetectedChanged;

    bool lastMotion;

    /// <inheritdoc/>
    public ValueTask<DetectionResult?> AnalyzeAsync(CameraFrame frame, CancellationToken ct)
    {
        var lum = frame.GetLuminance();
        int w = frame.Width, h = frame.Height;
        var stride = Math.Max(1, this.SampleStride);

        if (this.previous == null || this.prevWidth != w || this.prevHeight != h || this.previous.Length != lum.Length)
        {
            this.previous = lum.ToArray();
            this.prevWidth = w;
            this.prevHeight = h;
            return Empty();
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
        if (motion != this.lastMotion)
        {
            this.lastMotion = motion;
            this.MotionDetectedChanged?.Invoke(this, motion);
        }

        if (!motion || maxX < minX)
            return Empty();

        var raw = new RectF((float)minX / w, (float)minY / h, (float)(maxX - minX) / w, (float)(maxY - minY) / h);
        var box = CoordinateTransform.ApplyOrientation(raw, frame.Rotation, frame.IsMirrored);
        var detection = new Detection(DetectionType.Motion, box, "Motion", null, (float)Math.Min(1d, ratio));
        return new ValueTask<DetectionResult?>(new DetectionResult(this.Id, [detection]));

        ValueTask<DetectionResult?> Empty() => new(DetectionResult.Empty(this.Id));
    }
}
