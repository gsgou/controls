namespace Shiny.Controls.Camera;

/// <summary>
/// A built-in effect: a shared, immutable singleton carrying both a native program (used where the platform
/// has one, for fidelity) and a colour-matrix equivalent (used everywhere else, so the effect still does
/// something).
/// </summary>
/// <remarks>
/// Always enabled — a built-in is a value, not a piece of state. To turn one off, take it out of
/// <c>CameraView.Effects</c>.
/// </remarks>
public sealed class BuiltInCameraEffect : IColorEffect, INativeEffect
{
    internal BuiltInCameraEffect(string id, ColorMatrix4x5 matrix, NativeEffectDescriptor descriptor)
    {
        this.Id = id;
        this.ColorMatrix = matrix;
        this.Descriptor = descriptor;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public ColorMatrix4x5 ColorMatrix { get; }

    /// <inheritdoc/>
    public NativeEffectDescriptor Descriptor { get; }

    /// <inheritdoc/>
    public override string ToString() => this.Id;
}


/// <summary>
/// The built-in camera effects. Each of the <see cref="CameraFilter"/> values maps to one of these, so
/// <c>CameraView.Filter</c> is simply sugar for putting the matching effect at the head of
/// <c>CameraView.Effects</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every built-in carries a Core Image filter name for Apple, a CSS string for the browser, and a colour
/// matrix for Android / Windows / the managed head. Those three are <b>not</b> pixel-identical to each other
/// — <c>CIPhotoEffectNoir</c> is a tone curve, not a matrix — which is a deliberate fidelity-over-uniformity
/// choice inherited from the original filter implementation and preserved here exactly.
/// </para>
/// <para>
/// The colour-matrix offsets are authored in 0..1 space; <see cref="ColorMatrix4x5.ToAndroidArray"/> rescales
/// them for <c>android.graphics.ColorMatrix</c>.
/// </para>
/// </remarks>
public static partial class CameraEffects
{
    const float Px = 1f / 255f; // author offsets in the 0..255 space the platform filters were tuned in

    /// <summary>Black &amp; white (luminance).</summary>
    public static BuiltInCameraEffect Mono { get; } = new(
        "shiny.camera.effect.mono",
        ColorMatrix4x5.Saturation(0f),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPhotoEffectMono",
            Css: "grayscale(1)"
        ));

    /// <summary>High-contrast black &amp; white film look.</summary>
    public static BuiltInCameraEffect Noir { get; } = new(
        "shiny.camera.effect.noir",
        ColorMatrix4x5.Saturation(0f).Then(ColorMatrix4x5.ScaleOffset(1.4f, -40f * Px)),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPhotoEffectNoir",
            Css: "grayscale(1) contrast(1.4) brightness(0.9)"
        ));

    /// <summary>Warm brown vintage tone.</summary>
    public static BuiltInCameraEffect Sepia { get; } = new(
        "shiny.camera.effect.sepia",
        new ColorMatrix4x5([
            0.393f, 0.769f, 0.189f, 0, 0,
            0.349f, 0.686f, 0.168f, 0, 0,
            0.272f, 0.534f, 0.131f, 0, 0,
            0,      0,      0,      1, 0
        ]),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CISepiaTone",
            CoreImageParameters: new Dictionary<string, object> { ["inputIntensity"] = 1.0 },
            Css: "sepia(1)"
        ));

    /// <summary>Inverted (negative) colours.</summary>
    public static BuiltInCameraEffect Invert { get; } = new(
        "shiny.camera.effect.invert",
        new ColorMatrix4x5([
            -1f, 0,   0,   0, 1f,
            0,   -1f, 0,   0, 1f,
            0,   0,   -1f, 0, 1f,
            0,   0,   0,   1f, 0
        ]),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIColorInvert",
            Css: "invert(1)"
        ));

    /// <summary>Boosted saturation/contrast.</summary>
    public static BuiltInCameraEffect Vivid { get; } = new(
        "shiny.camera.effect.vivid",
        ColorMatrix4x5.Saturation(1.6f),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIColorControls",
            CoreImageParameters: new Dictionary<string, object>
            {
                ["inputSaturation"] = 1.5,
                ["inputContrast"] = 1.1
            },
            Css: "saturate(1.6) contrast(1.1)"
        ));

    /// <summary>Cool blue colour cast.</summary>
    public static BuiltInCameraEffect Cool { get; } = new(
        "shiny.camera.effect.cool",
        new ColorMatrix4x5([
            0.9f, 0,    0,    0, 0,
            0,    1.0f, 0,    0, 0,
            0,    0,    1.2f, 0, 10f * Px,
            0,    0,    0,    1f, 0
        ]),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CITemperatureAndTint",
            // neutral 6500K -> target a cooler 4800K
            CoreImageParameters: new Dictionary<string, object>
            {
                ["inputNeutral"] = new[] { 6500f, 0f },
                ["inputTargetNeutral"] = new[] { 4800f, 0f }
            },
            Css: "saturate(1.15) hue-rotate(-12deg) brightness(1.03)"
        ));

    /// <summary>Warm orange colour cast.</summary>
    public static BuiltInCameraEffect Warm { get; } = new(
        "shiny.camera.effect.warm",
        new ColorMatrix4x5([
            1.2f, 0,    0,     0, 10f * Px,
            0,    1.0f, 0,     0, 0,
            0,    0,    0.85f, 0, 0,
            0,    0,    0,     1f, 0
        ]),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CITemperatureAndTint",
            CoreImageParameters: new Dictionary<string, object>
            {
                ["inputNeutral"] = new[] { 6500f, 0f },
                ["inputTargetNeutral"] = new[] { 8500f, 0f }
            },
            Css: "sepia(0.3) saturate(1.4) hue-rotate(-10deg)"
        ));

    /// <summary>Soft, low-contrast washed-out look.</summary>
    public static BuiltInCameraEffect Fade { get; } = new(
        "shiny.camera.effect.fade",
        // slightly desaturate, then lift blacks + drop contrast for a washed look
        ColorMatrix4x5.Saturation(0.8f).Then(ColorMatrix4x5.ScaleOffset(0.85f, 22f * Px)),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPhotoEffectFade",
            Css: "contrast(0.85) brightness(1.1) saturate(0.8)"
        ));

    /// <summary>Punchy, cool, high-clarity look.</summary>
    public static BuiltInCameraEffect Chrome { get; } = new(
        "shiny.camera.effect.chrome",
        // punchy + slightly cool
        ColorMatrix4x5.Saturation(1.3f).Then(new ColorMatrix4x5([
            1.05f, 0,     0,     0, -6f * Px,
            0,     1.05f, 0,     0, -6f * Px,
            0,     0,     1.15f, 0, 4f * Px,
            0,     0,     0,     1f, 0
        ])),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPhotoEffectChrome",
            Css: "saturate(1.3) contrast(1.05) brightness(1.03)"
        ));

    /// <summary>Warm vintage instant-photo look.</summary>
    public static BuiltInCameraEffect Instant { get; } = new(
        "shiny.camera.effect.instant",
        // warm, low-contrast vintage instant film
        new ColorMatrix4x5([
            1.0f,  0.1f,  0.05f, 0, 12f * Px,
            0.05f, 0.95f, 0.05f, 0, 10f * Px,
            0.05f, 0.1f,  0.8f,  0, 4f * Px,
            0,     0,     0,     1f, 0
        ]),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPhotoEffectInstant",
            Css: "sepia(0.35) saturate(1.25) contrast(0.95)"
        ));

    /// <summary>Muted, low-contrast black &amp; white.</summary>
    public static BuiltInCameraEffect Tonal { get; } = new(
        "shiny.camera.effect.tonal",
        // muted, low-contrast black & white
        ColorMatrix4x5.Saturation(0f).Then(ColorMatrix4x5.ScaleOffset(0.9f, 14f * Px)),
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPhotoEffectTonal",
            Css: "grayscale(1) contrast(0.9) brightness(1.05)"
        ));


    /// <summary>
    /// The built-in effect backing a <see cref="CameraFilter"/> value, or <c>null</c> for
    /// <see cref="CameraFilter.None"/>.
    /// </summary>
    public static BuiltInCameraEffect? For(CameraFilter filter) => filter switch
    {
        CameraFilter.Mono => Mono,
        CameraFilter.Noir => Noir,
        CameraFilter.Sepia => Sepia,
        CameraFilter.Invert => Invert,
        CameraFilter.Vivid => Vivid,
        CameraFilter.Cool => Cool,
        CameraFilter.Warm => Warm,
        CameraFilter.Fade => Fade,
        CameraFilter.Chrome => Chrome,
        CameraFilter.Instant => Instant,
        CameraFilter.Tonal => Tonal,
        _ => null
    };
}
