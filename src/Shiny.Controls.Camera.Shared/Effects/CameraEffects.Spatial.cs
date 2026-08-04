namespace Shiny.Controls.Camera;

/// <summary>
/// The spatial (neighbourhood-aware) built-in effects — the looks a colour matrix cannot express, because
/// they need to see a pixel's neighbours.
/// </summary>
/// <remarks>
/// <para>
/// Each carries a Core Image filter for Apple, an AGSL shader for Android 33+, an SVG filter for the browser,
/// and a managed CPU pass used for stills wherever none of those apply. They deliberately do <b>not</b> carry
/// a colour-matrix fallback: there is no honest matrix approximation of an edge detector, and a silent no-op
/// is worse than an accurate report — <c>GetEffectSupport</c> returns <c>Unsupported</c> where they cannot run
/// (notably the Android preview below API 33, where the still is still filtered).
/// </para>
/// </remarks>
public static partial class CameraEffects
{
    // AGSL runs on Skia's SkSL dialect. Everything is computed in float and narrowed once at the return, which
    // avoids the half/float promotion rules differing between driver versions.
    const string SobelPrelude = """
        uniform shader content;

        float lum(float2 p) {
            float4 c = float4(content.eval(p));
            return dot(c.rgb, float3(0.299, 0.587, 0.114));
        }

        float sobel(float2 coord) {
            float tl = lum(coord + float2(-1.0, -1.0));
            float t  = lum(coord + float2( 0.0, -1.0));
            float tr = lum(coord + float2( 1.0, -1.0));
            float l  = lum(coord + float2(-1.0,  0.0));
            float r  = lum(coord + float2( 1.0,  0.0));
            float bl = lum(coord + float2(-1.0,  1.0));
            float b  = lum(coord + float2( 0.0,  1.0));
            float br = lum(coord + float2( 1.0,  1.0));

            float gx = -tl - 2.0 * l - bl + tr + 2.0 * r + br;
            float gy = -tl - 2.0 * t - tr + bl + 2.0 * b + br;
            return sqrt(gx * gx + gy * gy);
        }
        """;

    /// <summary>
    /// Flat, posterized colour with inked edges — the comic-panel look used by photo-toy apps.
    /// </summary>
    /// <remarks>
    /// This is a <i>procedural</i> comic look: cheap, offline, and live on the preview. It is not the
    /// generative "redraw my photo as a comic" that image models produce — for that, add an
    /// <see cref="ICaptureEffect"/> such as the AI stylizer, which runs once on the captured still.
    /// </remarks>
    public static BuiltInCameraEffect Comic { get; } = new(
        "shiny.camera.effect.comic",
        ColorMatrix4x5.Identity,
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIComicEffect",
            AgslShader: $$"""
                {{SobelPrelude}}

                half4 main(float2 coord) {
                    float4 c = float4(content.eval(coord));

                    // quantize to flat cels. NB: do not name this `flat` — that is a reserved interpolation
                    // qualifier in AGSL and the whole shader silently fails to compile.
                    float3 cel = floor(c.rgb * 4.0 + 0.5) / 4.0;

                    // push saturation around the cel's own grey so it stays graphic
                    float grey = dot(cel, float3(0.299, 0.587, 0.114));
                    cel = clamp(grey + (cel - grey) * 1.35, 0.0, 1.0);

                    float ink = smoothstep(0.21, 0.35, sobel(coord));
                    return half4(half3(mix(cel, float3(0.0), ink)), half(c.a));
                }
                """,
            SvgFilter: """
                <feColorMatrix type="saturate" values="1.35" result="sat" />
                <feComponentTransfer in="sat" result="flat">
                    <feFuncR type="discrete" tableValues="0 0.25 0.5 0.75 1" />
                    <feFuncG type="discrete" tableValues="0 0.25 0.5 0.75 1" />
                    <feFuncB type="discrete" tableValues="0 0.25 0.5 0.75 1" />
                </feComponentTransfer>
                <feColorMatrix in="SourceGraphic" type="matrix"
                    values="0.299 0.587 0.114 0 0
                            0.299 0.587 0.114 0 0
                            0.299 0.587 0.114 0 0
                            0     0     0     0 1" result="grey" />
                <feConvolveMatrix in="grey" order="3" preserveAlpha="true"
                    kernelMatrix="-1 -1 -1 -1 8 -1 -1 -1 -1" result="edges" />
                <feComponentTransfer in="edges" result="ink">
                    <feFuncR type="linear" slope="-4" intercept="1" />
                    <feFuncG type="linear" slope="-4" intercept="1" />
                    <feFuncB type="linear" slope="-4" intercept="1" />
                </feComponentTransfer>
                <feBlend in="flat" in2="ink" mode="multiply" />
                """,
            Managed: s => ManagedEffects.Comic(s)
        ));

    /// <summary>Pencil-sketch look: a light ground with dark lines where the image has edges.</summary>
    public static BuiltInCameraEffect Sketch { get; } = new(
        "shiny.camera.effect.sketch",
        ColorMatrix4x5.Identity,
        new NativeEffectDescriptor(
            // CILineOverlay already produces white-ground/black-line output
            CoreImageFilterName: "CILineOverlay",
            CoreImageParameters: new Dictionary<string, object>
            {
                ["inputNRNoiseLevel"] = 0.07,
                ["inputNRSharpness"] = 0.71,
                ["inputEdgeIntensity"] = 1.0,
                ["inputThreshold"] = 0.1,
                ["inputContrast"] = 50.0
            },
            AgslShader: $$"""
                {{SobelPrelude}}

                half4 main(float2 coord) {
                    float4 c = float4(content.eval(coord));
                    float line = clamp(sobel(coord) * 1.6, 0.0, 1.0);
                    float v = 1.0 - line;
                    return half4(half3(float3(v)), half(c.a));
                }
                """,
            SvgFilter: """
                <feColorMatrix type="matrix"
                    values="0.299 0.587 0.114 0 0
                            0.299 0.587 0.114 0 0
                            0.299 0.587 0.114 0 0
                            0     0     0     0 1" result="grey" />
                <feConvolveMatrix in="grey" order="3" preserveAlpha="true"
                    kernelMatrix="-1 -1 -1 -1 8 -1 -1 -1 -1" result="edges" />
                <feComponentTransfer in="edges">
                    <feFuncR type="linear" slope="-1.6" intercept="1" />
                    <feFuncG type="linear" slope="-1.6" intercept="1" />
                    <feFuncB type="linear" slope="-1.6" intercept="1" />
                </feComponentTransfer>
                """,
            Managed: s => ManagedEffects.Sketch(s)
        ));

    /// <summary>Flat, poster-print colour — each channel quantized to a handful of steps.</summary>
    public static BuiltInCameraEffect Posterize { get; } = new(
        "shiny.camera.effect.posterize",
        ColorMatrix4x5.Identity,
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIColorPosterize",
            CoreImageParameters: new Dictionary<string, object> { ["inputLevels"] = 6.0 },
            AgslShader: """
                uniform shader content;

                half4 main(float2 coord) {
                    float4 c = float4(content.eval(coord));
                    // NB: not `flat` — reserved interpolation qualifier in AGSL, the shader won't compile
                    float3 quantized = floor(c.rgb * 6.0 + 0.5) / 6.0;
                    return half4(half3(quantized), half(c.a));
                }
                """,
            SvgFilter: """
                <feComponentTransfer>
                    <feFuncR type="discrete" tableValues="0 0.2 0.4 0.6 0.8 1" />
                    <feFuncG type="discrete" tableValues="0 0.2 0.4 0.6 0.8 1" />
                    <feFuncB type="discrete" tableValues="0 0.2 0.4 0.6 0.8 1" />
                </feComponentTransfer>
                """,
            Managed: s => ManagedEffects.Posterize(s)
        ));

    /// <summary>Chunky mosaic blocks — the classic anonymize/censor look.</summary>
    public static BuiltInCameraEffect Pixelate { get; } = new(
        "shiny.camera.effect.pixelate",
        ColorMatrix4x5.Identity,
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIPixellate",
            CoreImageParameters: new Dictionary<string, object> { ["inputScale"] = 12.0 },
            AgslShader: """
                uniform shader content;

                half4 main(float2 coord) {
                    const float size = 12.0;
                    float2 block = floor(coord / size) * size + size * 0.5;
                    return half4(content.eval(block));
                }
                """,
            // No SVG primitive mosaics directly, but the classic flood/tile/mask/dilate chain does: flood a
            // single pixel, composite it into one 12x12 cell, tile that cell across the frame to build a grid
            // of one-pixel holes, mask the frame through it to keep one pixel per cell, then dilate each
            // survivor back out to fill its cell. Needs the filter region pinned to the frame origin (the
            // browser backend does that when it injects the filter) or the tiling drifts off the picture.
            SvgFilter: """
                <feFlood x="5" y="5" width="1" height="1" result="cell" />
                <feComposite in="cell" width="12" height="12" />
                <feTile result="grid" />
                <feComposite in="SourceGraphic" in2="grid" operator="in" />
                <feMorphology operator="dilate" radius="6" />
                """,
            Managed: s => ManagedEffects.Pixelate(s)
        ));

    /// <summary>Soft focus. The one spatial effect every backend has a first-class implementation of.</summary>
    public static BuiltInCameraEffect Blur { get; } = new(
        "shiny.camera.effect.blur",
        ColorMatrix4x5.Identity,
        new NativeEffectDescriptor(
            CoreImageFilterName: "CIGaussianBlur",
            CoreImageParameters: new Dictionary<string, object> { ["inputRadius"] = 8.0 },
            Css: "blur(8px)",
            AndroidBlurRadius: 8f,
            Managed: s => ManagedEffects.Blur(s)
        ));
}
