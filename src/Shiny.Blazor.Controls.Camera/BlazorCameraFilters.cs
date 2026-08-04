using Shiny.Controls.Camera;

namespace Shiny.Blazor.Controls.Camera;

/// <summary>The CSS + SVG needed to render one effect chain in the browser.</summary>
/// <param name="Css">
/// The value for the CSS <c>filter</c> property. Mixes built-in filter functions with <c>url(#id)</c>
/// references to <paramref name="Filters"/> — the spec composes both, in order, which is what lets a colour
/// matrix sit in the middle of a chain of CSS shorthands.
/// </param>
/// <param name="Filters">SVG <c>&lt;filter&gt;</c> definitions to inject, keyed by the id the CSS references.</param>
public sealed record CameraFilterCss(string Css, IReadOnlyList<SvgFilterDef> Filters)
{
    /// <summary>Nothing to apply.</summary>
    public static CameraFilterCss None { get; } = new("none", []);
}

/// <summary>One SVG filter definition to inject into the document.</summary>
/// <param name="Id">Element id, referenced from CSS as <c>url(#Id)</c>.</param>
/// <param name="Markup">The filter's inner primitives (e.g. an <c>feColorMatrix</c>).</param>
public sealed record SvgFilterDef(string Id, string Markup);


// Turns a resolved CameraEffectChain into a CSS `filter` value applied to the <video> element (and baked into
// canvas captures). Temperature filters (Cool/Warm) are approximated — CSS has no true white-balance control.
//
// CSS `filter` composes left-to-right, which is the same order the chain applies in, so concatenating each
// step's contribution is the whole implementation. Steps that CSS shorthands can't express — an arbitrary
// colour matrix, a spatial convolution — become SVG filters referenced by url().
static class BlazorCameraFilters
{
    /// <summary>Whether this backend can express <paramref name="effect"/> natively.</summary>
    public static bool IsHandledNatively(ICameraEffect effect)
    {
        var descriptor = (effect as INativeEffect)?.Descriptor;
        return descriptor?.Css is not null || descriptor?.SvgFilter is not null;
    }

    /// <summary>Resolve a chain into the CSS (and any SVG defs) needed to render it.</summary>
    /// <param name="chain">The chain to render.</param>
    /// <param name="idPrefix">
    /// A per-component prefix for generated SVG filter ids, so two cameras on one page never collide.
    /// </param>
    public static CameraFilterCss Resolve(CameraEffectChain chain, string idPrefix)
    {
        ArgumentNullException.ThrowIfNull(chain);
        if (chain.IsEmpty)
            return CameraFilterCss.None;

        var parts = new List<string>();
        var defs = new List<SvgFilterDef>();

        foreach (var step in chain.Plan(IsHandledNatively))
        {
            if (step.Color is { } matrix)
            {
                // no CSS shorthand for an arbitrary matrix — emit an feColorMatrix and reference it
                var id = $"{idPrefix}-{defs.Count}";
                defs.Add(new SvgFilterDef(id,
                    $"""<feColorMatrix type="matrix" values="{matrix.ToSvgValues()}" />"""));
                parts.Add($"url(#{id})");
                continue;
            }

            var descriptor = step.Descriptor;
            if (descriptor?.Css is { Length: > 0 } css)
            {
                parts.Add(css);
            }
            else if (descriptor?.SvgFilter is { Length: > 0 } svg)
            {
                var id = $"{idPrefix}-{defs.Count}";
                defs.Add(new SvgFilterDef(id, svg));
                parts.Add($"url(#{id})");
            }
        }

        return parts.Count == 0
            ? CameraFilterCss.None
            : new CameraFilterCss(string.Join(" ", parts), defs);
    }

    /// <summary>
    /// How much of <paramref name="effect"/> the browser backend will honour.
    /// </summary>
    /// <remarks>
    /// Everything with a colour matrix or a CSS/SVG program is honoured. The caveat is WebKit, whose support
    /// for <c>url()</c> filters on a live <c>&lt;video&gt;</c> and in <c>canvas.filter</c> has historically been
    /// unreliable — an effect reported <see cref="EffectSupport.Full"/> here may still render flat in Safari,
    /// which is why the built-ins all carry a plain-CSS form that needs no SVG at all.
    /// </remarks>
    public static EffectSupport Resolve(ICameraEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect is IDrawEffect or ICaptureEffect)
            return EffectSupport.Full;

        if (IsHandledNatively(effect))
            return EffectSupport.Full;

        return effect is IColorEffect { ColorMatrix.IsIdentity: false }
            ? EffectSupport.Full
            : EffectSupport.Unsupported;
    }
}
