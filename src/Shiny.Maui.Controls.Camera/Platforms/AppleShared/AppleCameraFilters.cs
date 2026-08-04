using CoreImage;
using Foundation;

namespace Shiny.Maui.Controls.Camera;

// Turns a resolved CameraEffectChain into Core Image filters. Each EffectStep becomes one CIFilter: a native
// step uses the descriptor's Core Image filter name, a colour step becomes a CIColorMatrix. The chain is
// applied by feeding each filter's output into the next (see Apply), so N effects cost one render, not N.
static class AppleCameraFilters
{
    // Allocated once. This is set on every filter of every frame, so allocating an NSString per call showed up
    // as a steady native-memory drip on the capture queue.
    static readonly NSString InputImageKey = new("inputImage");

    /// <summary>Whether this backend can run <paramref name="effect"/>'s native program.</summary>
    public static bool IsHandledNatively(ICameraEffect effect)
        => (effect as INativeEffect)?.Descriptor.CoreImageFilterName is not null;

    /// <summary>
    /// Build the ordered Core Image filters for a chain. Returns an empty array for a passthrough chain, so
    /// callers can cheaply test for "nothing to do".
    /// </summary>
    public static CIFilter[] Create(CameraEffectChain chain)
    {
        ArgumentNullException.ThrowIfNull(chain);
        if (chain.IsEmpty)
            return [];

        var filters = new List<CIFilter>();
        foreach (var step in chain.Plan(IsHandledNatively))
        {
            var filter = step.Native is not null
                ? FromDescriptor(step.Descriptor!)
                : FromColorMatrix(step.Color!);

            if (filter is not null)
                filters.Add(filter);
        }

        return [.. filters];
    }

    /// <summary>
    /// Run <paramref name="filters"/> over <paramref name="input"/> in order, returning the final image (or
    /// <c>null</c> if any stage produced nothing).
    /// </summary>
    /// <param name="input">The source image. Never disposed here — the caller owns it.</param>
    /// <param name="filters">The chain, applied in order.</param>
    /// <param name="produced">
    /// Receives every intermediate this call created, <b>including the returned image</b>. The caller must
    /// dispose them all — but only <i>after</i> rendering, never before.
    /// </param>
    /// <remarks>
    /// <para>
    /// The two-step ownership is the whole point. A <c>CIImage</c> is a lazy recipe, not a rendered bitmap:
    /// nothing is evaluated until the final image is drawn into a <c>CIContext</c>, so disposing a stage's
    /// output while a later stage still references it is a use-after-free. But <i>never</i> disposing them
    /// leaks a native object per filter per frame, which at 30-60fps is enough to put the app under memory
    /// pressure and get the capture session interrupted out from under you. So: collect, render, then dispose.
    /// </para>
    /// </remarks>
    public static CIImage? Apply(CIImage input, CIFilter[] filters, List<CIImage> produced)
    {
        ArgumentNullException.ThrowIfNull(produced);

        CIImage? current = input;
        foreach (var filter in filters)
        {
            filter.SetValueForKey(current, InputImageKey);
            current = filter.OutputImage;
            if (current is null)
                return null;

            produced.Add(current);
        }

        return current;
    }

    static CIFilter? FromDescriptor(NativeEffectDescriptor descriptor)
    {
        var filter = CIFilter.FromName(descriptor.CoreImageFilterName!);
        if (filter is null || descriptor.CoreImageParameters is null)
            return filter;

        foreach (var (key, value) in descriptor.CoreImageParameters)
        {
            // The descriptor is platform-neutral data, so vectors arrive as float[] and scalars as boxed
            // primitives; anything we can't map is skipped rather than throwing inside a capture callback.
            NSObject? mapped = value switch
            {
                float[] vector => new CIVector([.. vector.Select(v => (nfloat)v)]),
                double d => NSNumber.FromDouble(d),
                float f => NSNumber.FromFloat(f),
                int i => NSNumber.FromInt32(i),
                bool b => NSNumber.FromBoolean(b),
                _ => null
            };

            if (mapped is not null)
                filter.SetValueForKey(mapped, new NSString(key));
        }

        return filter;
    }

    static CIFilter? FromColorMatrix(ColorMatrix4x5 matrix)
    {
        var filter = CIFilter.FromName("CIColorMatrix");
        if (filter is null)
            return null;

        // CIColorMatrix takes one vector per input channel plus a bias — i.e. the transpose of our row-major
        // layout, which is per *output* channel.
        filter.SetValueForKey(Column(matrix, 0), new NSString("inputRVector"));
        filter.SetValueForKey(Column(matrix, 1), new NSString("inputGVector"));
        filter.SetValueForKey(Column(matrix, 2), new NSString("inputBVector"));
        filter.SetValueForKey(Column(matrix, 3), new NSString("inputAVector"));
        filter.SetValueForKey(Column(matrix, 4), new NSString("inputBiasVector"));
        return filter;
    }

    static CIVector Column(ColorMatrix4x5 matrix, int column) => new(
        matrix[0, column], matrix[1, column], matrix[2, column], matrix[3, column]);
}
