using System.Runtime.Versioning;
using Android.Graphics;
using AndroidColorMatrix = Android.Graphics.ColorMatrix;
using ShinyColorMatrix = Shiny.Controls.Camera.ColorMatrix4x5;

namespace Shiny.Maui.Controls.Camera;

// Turns a resolved CameraEffectChain into Android graphics primitives:
//   * the live preview gets a chained RenderEffect on the PreviewView (API 31+, and the AGSL shader steps
//     within it need API 33+);
//   * captured stills get the collapsed colour matrix applied to the decoded bitmap, which works on every
//     API level — so an old device shows an unfiltered preview but still saves a filtered photo.
static class AndroidCameraFilters
{
    /// <summary>Whether this backend can run <paramref name="effect"/>'s native program on the preview.</summary>
    /// <remarks>
    /// Blur is the exception to the API-33 shader gate: it maps to <c>RenderEffect.CreateBlurEffect</c>, which
    /// has been there since 31 — the same level the preview effect itself requires.
    /// </remarks>
    public static bool IsHandledNatively(ICameraEffect effect)
    {
        var descriptor = (effect as INativeEffect)?.Descriptor;
        if (descriptor is null)
            return false;

        if (descriptor.AndroidBlurRadius is > 0)
            return true;

        return descriptor.AgslShader is not null && OperatingSystem.IsAndroidVersionAtLeast(33);
    }

    /// <summary>Whether this backend can run <paramref name="effect"/>'s native program on a still bitmap.</summary>
    public static bool IsHandledNativelyForStills(ICameraEffect effect)
        => (effect as INativeEffect)?.Descriptor.Managed is not null;

    /// <summary>
    /// Build the chained <see cref="RenderEffect"/> for the live preview, or <c>null</c> for passthrough.
    /// </summary>
    /// <remarks>
    /// Steps compose via <c>RenderEffect.CreateChainEffect(outer, inner)</c>, where <c>inner</c> runs first —
    /// so the chain is folded in reverse of the order effects were added.
    /// </remarks>
    [SupportedOSPlatform("android31.0")]
    public static RenderEffect? CreatePreviewEffect(CameraEffectChain chain, Action<string>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(chain);

        RenderEffect? result = null;
        foreach (var step in chain.Plan(IsHandledNatively))
        {
            RenderEffect? effect;
            if (step.Native is not null)
            {
                var descriptor = step.Descriptor!;
                if (descriptor.AndroidBlurRadius is > 0 and var radius)
                {
                    effect = RenderEffect.CreateBlurEffect(radius, radius, Shader.TileMode.Clamp!);
                }
                else
                {
                    // Plan only yields a shader step when IsHandledNatively said yes, which already required
                    // API 33 — repeated here so the platform-compatibility analyzer can see the guard too.
                    effect = OperatingSystem.IsAndroidVersionAtLeast(33)
                        ? CreateShaderEffect(descriptor, step.Native.Id, onError)
                        : null;
                }
            }
            else
            {
                effect = RenderEffect.CreateColorFilterEffect(new ColorMatrixColorFilter(ToNative(step.Color!)));
            }

            if (effect is null)
                continue;

            result = result is null ? effect : RenderEffect.CreateChainEffect(effect, result);
        }

        return result;
    }

    /// <summary>The still-image render plan, in application order.</summary>
    public static IReadOnlyList<EffectStep> StillPlan(CameraEffectChain chain)
    {
        ArgumentNullException.ThrowIfNull(chain);
        return chain.Plan(IsHandledNativelyForStills);
    }

    /// <summary>
    /// The single colour matrix for a plan that is <b>entirely</b> colour steps, or <c>null</c> when it isn't.
    /// </summary>
    /// <remarks>
    /// This is the fast path — one <c>Canvas.DrawBitmap</c> with one <c>ColorMatrixColorFilter</c>, which is
    /// what a plain <c>Filter="Noir"</c> capture has always done. A plan that mixes colour and spatial steps
    /// can't use it: the steps have to interleave in chain order, so <c>[Comic, Mono]</c> is not the same
    /// image as <c>[Mono, Comic]</c>, and folding all the matrices to the front would silently reorder them.
    /// </remarks>
    public static AndroidColorMatrix? CreateStillColorMatrix(IReadOnlyList<EffectStep> plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        ShinyColorMatrix? combined = null;
        foreach (var step in plan)
        {
            if (step.Color is not { } matrix)
                return null; // a spatial step is present — the caller must take the ordered path

            combined = combined is null ? matrix : combined.Then(matrix);
        }

        return combined is null ? null : ToNative(combined);
    }

    /// <summary>Convert a portable colour matrix to Android's, rescaling the offset column to 0..255.</summary>
    public static AndroidColorMatrix ToNative(ShinyColorMatrix matrix) => new(matrix.ToAndroidArray());

    [SupportedOSPlatform("android33.0")]
    static RenderEffect? CreateShaderEffect(NativeEffectDescriptor descriptor, string effectId, Action<string>? onError)
    {
        try
        {
            var shader = new RuntimeShader(descriptor.AgslShader!);
            return RenderEffect.CreateRuntimeShaderEffect(shader, descriptor.AgslInputName ?? "content");
        }
        catch (Exception ex)
        {
            // A malformed shader throws at compile/link time. Dropping the step keeps the preview alive rather
            // than killing the camera — but it must NOT be silent: swallowing this is exactly how two built-in
            // shaders shipped using `flat` (a reserved AGSL qualifier) as a variable name and simply did
            // nothing on Android, with no way to tell that from "the effect isn't supported here".
            onError?.Invoke($"Camera effect '{effectId}' has an AGSL shader that failed to compile: {ex.Message}");
            return null;
        }
    }
}
