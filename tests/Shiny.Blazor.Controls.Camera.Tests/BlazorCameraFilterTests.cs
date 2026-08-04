using Shiny.Blazor.Controls.Camera;
using Shiny.Controls.Camera;
using Shouldly;
using Xunit;

namespace Shiny.Blazor.Controls.Camera.Tests;

/// <summary>
/// Guards the browser backend's effect resolution, and in particular the change-detection key the component
/// uses to decide whether a chain needs re-applying.
/// </summary>
/// <remarks>
/// The bug worth a permanent test: generated SVG filter ids are <b>positional</b>
/// (<c>prefix-0</c>, <c>prefix-1</c>, …), so every single-SVG-effect chain resolves to the identical CSS string
/// <c>url(#prefix-0)</c>. Keying change detection on the CSS alone made switching between any two SVG-backed
/// effects — Comic to Sketch to Posterize — a silent no-op: the element kept the first effect's markup forever.
/// </remarks>
public class BlazorCameraFilterTests
{
    const string Prefix = "fx";

    static CameraFilterCss Resolve(params ICameraEffect[] effects)
        => BlazorCameraFilters.Resolve(CameraEffectChain.Create(CameraFilter.None, effects), Prefix);


    [Fact]
    public void SvgEffects_ShareTheSameCss()
    {
        // not the assertion under test so much as the premise of the one below — if this ever stops being true
        // the Key test below would pass for the wrong reason
        Resolve(CameraEffects.Comic).Css.ShouldBe(Resolve(CameraEffects.Sketch).Css);
    }


    [Theory]
    [InlineData(nameof(CameraEffects.Comic), nameof(CameraEffects.Sketch))]
    [InlineData(nameof(CameraEffects.Comic), nameof(CameraEffects.Posterize))]
    [InlineData(nameof(CameraEffects.Sketch), nameof(CameraEffects.Posterize))]
    public void Key_DistinguishesSvgEffectsWithIdenticalCss(string first, string second)
    {
        var a = Resolve(Effect(first));
        var b = Resolve(Effect(second));

        a.Css.ShouldBe(b.Css);        // identical CSS: only the injected markup differs
        a.Key.ShouldNotBe(b.Key);     // ...so the key must not be
    }


    [Fact]
    public void Key_IsStableForTheSameChain()
        => Resolve(CameraEffects.Comic).Key.ShouldBe(Resolve(CameraEffects.Comic).Key);


    [Fact]
    public void Key_IsTheCssWhenNoSvgIsInvolved()
    {
        var resolved = Resolve(CameraEffects.Blur);
        resolved.Filters.ShouldBeEmpty();
        resolved.Key.ShouldBe(resolved.Css);
    }


    [Fact]
    public void SvgEffect_EmitsItsOwnMarkup()
    {
        var comic = Resolve(CameraEffects.Comic);
        comic.Filters.ShouldHaveSingleItem();
        comic.Filters[0].Id.ShouldBe($"{Prefix}-0");
        comic.Filters[0].Markup.ShouldBe(CameraEffects.Comic.Descriptor.SvgFilter);
        comic.Css.ShouldBe($"url(#{Prefix}-0)");
    }


    [Fact]
    public void CssEffects_UseTheShorthand_NotSvg()
    {
        // the plain-CSS form is what keeps the built-in looks working in browsers whose url() support on a live
        // <video> is unreliable, so a look that has one must never be routed through an SVG filter
        var chain = CameraEffectChain.Create(CameraFilter.Sepia, null);
        var resolved = BlazorCameraFilters.Resolve(chain, Prefix);

        resolved.Css.ShouldBe("sepia(1)");
        resolved.Filters.ShouldBeEmpty();
    }


    [Fact]
    public void FilterAndEffect_ComposeInOrder()
    {
        var chain = CameraEffectChain.Create(CameraFilter.Mono, [CameraEffects.Blur]);
        BlazorCameraFilters.Resolve(chain, Prefix).Css.ShouldBe("grayscale(1) blur(8px)");
    }


    [Fact]
    public void Pixelate_RunsAsAnSvgFilter()
    {
        // its colour matrix is the identity, so without an SVG program the browser silently rendered nothing
        CameraView.GetEffectSupport(CameraEffects.Pixelate).ShouldBe(EffectSupport.Full);

        var resolved = Resolve(CameraEffects.Pixelate);
        resolved.Css.ShouldBe($"url(#{Prefix}-0)");
        resolved.Filters.ShouldHaveSingleItem();
        resolved.Filters[0].Markup.ShouldContain("feTile");
    }


    [Fact]
    public void EverySpatialEffect_HasABrowserProgram()
    {
        // the browser has no colour-matrix consolation prize for these — the matrix is the identity, so an
        // effect that reaches here without CSS or SVG renders as a no-op rather than as anything at all
        BuiltInCameraEffect[] spatial =
        [
            CameraEffects.Comic, CameraEffects.Sketch, CameraEffects.Posterize,
            CameraEffects.Pixelate, CameraEffects.Blur
        ];

        foreach (var effect in spatial)
        {
            CameraView.GetEffectSupport(effect).ShouldBe(EffectSupport.Full, $"{effect.Id} has no browser program");
            Resolve(effect).Css.ShouldNotBe("none", $"{effect.Id} resolves to nothing");
        }
    }


    static ICameraEffect Effect(string name) => name switch
    {
        nameof(CameraEffects.Comic) => CameraEffects.Comic,
        nameof(CameraEffects.Sketch) => CameraEffects.Sketch,
        nameof(CameraEffects.Posterize) => CameraEffects.Posterize,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
    };
}
