using Microsoft.Maui.Graphics;

namespace Shiny.Controls.Camera;

/// <summary>
/// An <see cref="IVideoOverlayRenderer"/> that can hand the recorder <b>pre-rendered images</b> for a frame
/// instead of drawing into it, so the platform can composite them the cheapest way it has.
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> A burn-in overlay is drawn onto every encoded frame but usually only
/// <i>changes</i> once or twice a second — a clock, a telemetry readout, a watermark. A renderer that
/// already caches its own output can say so, and on Apple that is the difference between mapping the whole
/// capture buffer into CPU memory and blending it there, and letting Core Image composite a few small
/// images on the GPU. On Android the recording surface is already hardware-composited, so this changes the
/// draw calls but not the cost.</para>
/// <para><b>It is always optional, on both sides.</b> Returning <c>null</c> from
/// <see cref="GetLayers"/> — for one frame or for all of them — falls back to
/// <see cref="IVideoOverlayRenderer.DrawOverlay"/>, and a platform that cannot take the layer path simply
/// never asks. Implementing this interface can therefore never cost correctness, only speed.</para>
/// <para><b>Threading is unchanged:</b> <see cref="GetLayers"/> is called once per encoded frame on a
/// capture/encoder thread, never the UI thread, immediately before the frame is composited. It must not
/// block and must not touch UI objects.</para>
/// <para>⚠️ <b>Layers are borrowed, not given.</b> The images are read during the call that follows and
/// must stay alive and unmodified until the next call on this renderer. A renderer that repaints into a
/// retained image must publish the new contents by raising <see cref="VideoOverlayLayer.Version"/>.</para>
/// </remarks>
public interface ICompositedVideoOverlayRenderer : IVideoOverlayRenderer
{
    /// <summary>
    /// The layers to composite for this frame, bottom-most first, or <c>null</c> to draw this frame the
    /// ordinary way. An empty list is <i>not</i> the same answer: it means "there is nothing to draw",
    /// and the frame is left alone.
    /// </summary>
    /// <param name="context">Timing / size / facing metadata for the frame about to be composited.</param>
    IReadOnlyList<VideoOverlayLayer>? GetLayers(VideoOverlayContext context);
}


/// <summary>One pre-rendered piece of a burn-in overlay, and where on the frame it goes.</summary>
/// <param name="Image">
/// The rendered layer. Borrowed for the duration of the composite — see
/// <see cref="ICompositedVideoOverlayRenderer"/>.
/// </param>
/// <param name="Destination">
/// Where it lands, in frame pixels (origin top-left). Sized to the image rather than scaled to it: a
/// destination whose size differs from the image's is resampled, which is worth avoiding for the small
/// text these layers usually carry.
/// </param>
/// <param name="Version">
/// Changes whenever <paramref name="Image"/>'s <i>contents</i> change, and only then. The platform caches
/// its own representation of the layer against this, so an overlay that repaints twice a second costs two
/// conversions a second rather than one per frame.
/// <para>⚠️ A version that never changes will burn the <i>first</i> frame's contents into the whole
/// recording. Increment it, or derive it from whatever the renderer already uses to decide it must
/// repaint.</para>
/// </param>
public readonly record struct VideoOverlayLayer(IImage Image, RectF Destination, long Version);
