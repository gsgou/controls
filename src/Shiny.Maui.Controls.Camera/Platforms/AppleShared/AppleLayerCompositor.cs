using CoreGraphics;
using CoreImage;
using CoreVideo;
using Microsoft.Maui.Graphics;

namespace Shiny.Maui.Controls.Camera;

// Composites an ICompositedVideoOverlayRenderer's pre-rendered layers onto a capture buffer with Core
// Image, on the GPU, instead of mapping the buffer into CPU memory and blending it there.
//
// ⚠️ What this saves is not mainly the blend. AppleVideoOverlayRecorder's ordinary path calls
// CVPixelBuffer.Lock with read-write flags, which maps the whole IOSurface for CPU access on every frame
// whether the overlay covers 5% of it or all of it. That fixed cost disappears here: CIContext.Render
// writes the surface directly, and it is given each layer's own destination rect so it touches only those
// pixels rather than re-rendering the frame.
//
// Everything about it is optional and it fails soft. A renderer that does not implement the interface, a
// frame it declines to describe, a MAUI image this cannot reach the CGImage of, or any exception at all,
// and the recorder goes back to the CGBitmapContext path for the rest of the recording — a HUD composited
// the slow way is immeasurably better than a recording that stopped.
sealed class AppleLayerCompositor : IDisposable
{
    readonly ICompositedVideoOverlayRenderer renderer;

    // Keyed by slot rather than by image identity: the layer list is positional and short (a handful of
    // panels), so this is an array lookup per frame and the CIImage for an unchanged layer is reused.
    readonly List<(long Version, CIImage Image)> cache = [];

    CIContext? context;
    CGColorSpace? colorSpace;

    public AppleLayerCompositor(ICompositedVideoOverlayRenderer renderer)
        => this.renderer = renderer;

    /// <summary>
    /// Latched once the layer path has failed, so the cost of a platform that cannot do this is one
    /// attempt rather than one per frame. The recorder reads it to decide whether to keep asking.
    /// </summary>
    public bool Unavailable { get; private set; }

    /// <summary>
    /// Composite this frame's layers, or answer false to say the caller should draw it the ordinary way.
    /// </summary>
    public bool TryComposite(CVPixelBuffer pixelBuffer, VideoOverlayContext context)
    {
        if (this.Unavailable)
            return false;

        try
        {
            if (this.renderer.GetLayers(context) is not { } layers)
                return false;

            // An empty list is a real answer — "nothing to draw" — and is not the same as null. Handing the
            // frame back untouched is exactly right, and costs no render at all.
            if (layers.Count == 0)
                return true;

            var ciContext = this.EnsureContext();
            var cs = this.colorSpace ??= CGColorSpace.CreateDeviceRGB();

            // One wrapper for the frame, not one per layer. It is the background of every composite below,
            // and at four layers and 30fps the per-layer version was 120 native images a second that
            // nothing disposed — allocated on the capture queue, which is the one thread that cannot
            // afford to wait for a finalizer.
            using var background = new CIImage(pixelBuffer);

            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (this.Resolve(i, layer) is not { } image)
                    return false;

                // Core Image's origin is bottom-left and the layer's destination is in top-left frame
                // space, so the flip is part of placing it — not a detail that can be left to the caller,
                // which would put every overlay in the wrong half of the frame.
                var y = pixelBuffer.Height - layer.Destination.Bottom;

                // Both are per-frame transients rather than cached like the layer images: the transform
                // moves with the destination rect and the composite is one recipe over one frame.
                using var placed = image.ImageByApplyingTransform(
                    CGAffineTransform.MakeTranslation(layer.Destination.X, y));

                using var filter = new CISourceOverCompositing
                {
                    InputImage = placed,
                    BackgroundImage = background
                };

                if (filter.OutputImage is not { } composed)
                    return false;

                using (composed)
                {
                    // The destination rect is what keeps this cheap: Core Image renders the region we ask
                    // for and leaves the rest of the surface alone, so a HUD covering a sixth of the frame
                    // costs a sixth of a frame rather than a full-frame pass per layer.
                    ciContext.Render(
                        composed,
                        pixelBuffer,
                        new CGRect(layer.Destination.X, y, layer.Destination.Width, layer.Destination.Height),
                        cs
                    );
                }
            }

            return true;
        }
        catch (Exception)
        {
            this.Fail();
            return false;
        }
    }

    CIContext EnsureContext()
    {
        if (this.context is { } existing)
            return existing;

        // Metal-backed where there is a device, which there is on every unit this ships to; the software
        // fallback exists for the simulator, where correctness matters and speed does not.
        this.context = Metal.MTLDevice.SystemDefault is { } device
            ? CIContext.FromMetalDevice(device)
            : new CIContext();

        return this.context;
    }

    /// <summary>
    /// The layer as a <see cref="CIImage"/>, built only when its version says the contents moved.
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>PlatformRepresentation</c> is a platform cast and not part of MAUI Graphics' published
    /// contract, so a null answer is treated as "this renderer cannot take the fast path" rather than as an
    /// error. <c>CIImage.FromCGImage</c> wraps rather than copies, so a miss is cheap and a hit is free.
    /// </remarks>
    CIImage? Resolve(int slot, VideoOverlayLayer layer)
    {
        while (this.cache.Count <= slot)
            this.cache.Add((Version: long.MinValue, Image: null!));

        var cached = this.cache[slot];
        if (cached.Image is not null && cached.Version == layer.Version)
            return cached.Image;

        if ((layer.Image as Microsoft.Maui.Graphics.Platform.PlatformImage)?.PlatformRepresentation?.CGImage
            is not { } cg)
        {
            this.Fail();
            return null;
        }

        var image = CIImage.FromCGImage(cg);
        cached.Image?.Dispose();
        this.cache[slot] = (layer.Version, image);
        return image;
    }

    void Fail()
    {
        this.Unavailable = true;
        this.ReleaseCache();
    }

    void ReleaseCache()
    {
        foreach (var entry in this.cache)
            entry.Image?.Dispose();

        this.cache.Clear();
    }

    public void Dispose()
    {
        this.ReleaseCache();
        this.context?.Dispose();
        this.context = null;
        this.colorSpace?.Dispose();
        this.colorSpace = null;
    }
}
