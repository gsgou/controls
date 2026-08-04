namespace Shiny.Controls.Camera;

/// <summary>
/// The per-backend programs that implement one <see cref="INativeEffect"/>. Every field is optional: a
/// backend takes the one it understands, and where it finds nothing it falls back to the effect's
/// <see cref="IColorEffect.ColorMatrix"/> if the effect has one, else passes the frame through untouched.
/// </summary>
/// <remarks>
/// <para>
/// This exists so the public effect API never leaks <c>CIFilter</c>, <c>RuntimeShader</c> or CSS types into
/// assemblies that cannot see them — an effect is described as <i>data</i>, and each platform handler turns
/// that data into its own native object.
/// </para>
/// <para>
/// Coverage is deliberately uneven and that is reported, not hidden: ask
/// <c>CameraView.GetEffectSupport(effect)</c> what a given effect will actually do on the current platform
/// before you offer it in a UI.
/// </para>
/// </remarks>
/// <param name="CoreImageFilterName">
/// A Core Image filter name for Apple platforms, e.g. <c>"CIComicEffect"</c> or <c>"CIPhotoEffectNoir"</c>.
/// </param>
/// <param name="CoreImageParameters">
/// Parameters set on that filter, keyed by Core Image input key (e.g. <c>"inputIntensity"</c>). Values must be
/// <see cref="float"/>, <see cref="double"/>, <see cref="int"/>, <see cref="bool"/> or <c>float[]</c> — the
/// Apple handler converts them to <c>NSNumber</c> / <c>CIVector</c>. Any other type is ignored.
/// </param>
/// <param name="AgslShader">
/// AGSL source for Android's <c>RuntimeShader</c>, used for the live preview via
/// <c>RenderEffect.CreateRuntimeShaderEffect</c>. Requires API 33+; below that the colour matrix is used.
/// </param>
/// <param name="AgslInputName">
/// Name of the input-shader uniform the frame is bound to in <paramref name="AgslShader"/>. Defaults to
/// <c>"content"</c>, matching the convention in the AGSL docs.
/// </param>
/// <param name="Css">
/// A CSS <c>filter</c> value for Blazor, e.g. <c>"grayscale(1) contrast(1.4)"</c>. The cheapest browser path
/// and the only one WebKit honours reliably on a <c>&lt;video&gt;</c> element.
/// </param>
/// <param name="SvgFilter">
/// The <i>inner</i> markup of an SVG <c>&lt;filter&gt;</c> element (e.g. an <c>feConvolveMatrix</c> chain) for
/// spatial effects in the browser. Injected into a hidden SVG and referenced as <c>filter: url(#id)</c>.
/// Feature-detected at runtime: where it fails, <paramref name="Css"/> is used instead.
/// </param>
/// <param name="AndroidBlurRadius">
/// Blur radius in pixels for Android's first-class <c>RenderEffect.CreateBlurEffect</c>. A special case with
/// its own field because a hand-rolled blur shader would be both slower and worse-looking than the one the
/// platform already provides — and because blur is the one spatial effect every backend has natively.
/// Takes precedence over <paramref name="AgslShader"/> on Android, and needs only API 31 rather than 33.
/// </param>
/// <param name="Managed">
/// A portable pixel pass used for <b>still images</b> where no GPU path exists. Never invoked on the live
/// preview. May mutate and return the input surface, or return a new one.
/// </param>
public sealed record NativeEffectDescriptor(
    string? CoreImageFilterName = null,
    IReadOnlyDictionary<string, object>? CoreImageParameters = null,
    string? AgslShader = null,
    string? AgslInputName = null,
    string? Css = null,
    string? SvgFilter = null,
    float? AndroidBlurRadius = null,
    Func<PixelSurface, PixelSurface>? Managed = null
)
{
    /// <summary>A descriptor that asks for nothing — every backend falls through to the colour matrix.</summary>
    public static NativeEffectDescriptor None { get; } = new();

    /// <summary><c>true</c> when this descriptor carries a spatial (neighbourhood-aware) program for any backend.</summary>
    public bool HasSpatialProgram =>
        this.AgslShader is not null
        || this.SvgFilter is not null
        || this.AndroidBlurRadius is not null
        || this.Managed is not null;
}
