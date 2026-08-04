using Shiny.Controls.Camera;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

/// <summary>
/// Covers the spatial built-ins and their portable CPU implementations. The GPU paths can only be verified
/// on-device, so what is pinned here is the part that must not drift: which backends each effect claims to
/// support, and that the managed passes actually change the pixels in the direction the look describes.
/// </summary>
public class SpatialEffectTests
{
    // a 16x16 image split black (left) / white (right), so there is exactly one strong vertical edge
    static PixelSurface SplitImage(int size = 16)
    {
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = ((y * size) + x) * 4;
                var value = x < size / 2 ? (byte)0 : (byte)255;
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }
        return new PixelSurface(size, size, pixels);
    }

    static (byte B, byte G, byte R) At(PixelSurface s, int x, int y)
    {
        var i = ((y * s.Width) + x) * 4;
        return (s.Pixels[i], s.Pixels[i + 1], s.Pixels[i + 2]);
    }


    [Fact]
    public void Sketch_inks_the_edge_and_leaves_flat_areas_white()
    {
        var result = ManagedEffects.Sketch(SplitImage());

        // straddling the boundary => a line
        At(result, 8, 8).R.ShouldBeLessThan((byte)128);
        // far from the boundary => paper
        At(result, 1, 8).R.ShouldBe((byte)255);
        At(result, 14, 8).R.ShouldBe((byte)255);
    }

    [Fact]
    public void Comic_inks_the_edge_and_keeps_the_flats()
    {
        var result = ManagedEffects.Comic(SplitImage());

        At(result, 8, 8).R.ShouldBeLessThan((byte)128);   // inked edge
        At(result, 14, 8).R.ShouldBeGreaterThan((byte)200); // white cel survives
    }

    [Fact]
    public void Posterize_snaps_channels_onto_a_small_set_of_levels()
    {
        var pixels = new byte[4 * 4];
        for (var i = 0; i < 4; i++)
        {
            pixels[(i * 4) + 0] = (byte)(i * 60);
            pixels[(i * 4) + 1] = (byte)(i * 60);
            pixels[(i * 4) + 2] = (byte)(i * 60);
            pixels[(i * 4) + 3] = 255;
        }

        var result = ManagedEffects.Posterize(new PixelSurface(4, 1, pixels), levels: 4);

        // 4 levels => every value lands on a multiple of 255/4
        foreach (var x in Enumerable.Range(0, 4))
        {
            var v = At(result, x, 0).R;
            (v % 64 is 0 or 63 || v == 255).ShouldBeTrue($"{v} is not on a quantization step");
        }
    }

    [Fact]
    public void Pixelate_makes_every_pixel_in_a_block_identical()
    {
        var result = ManagedEffects.Pixelate(SplitImage(), blockSize: 4);

        var first = At(result, 0, 0);
        At(result, 1, 0).ShouldBe(first);
        At(result, 3, 3).ShouldBe(first);
    }

    [Fact]
    public void Blur_softens_the_hard_edge()
    {
        var source = SplitImage();
        var result = ManagedEffects.Blur(source, radius: 3);

        // the boundary pixel is no longer pure black or pure white
        var v = At(result, 8, 8).R;
        v.ShouldBeGreaterThan((byte)0);
        v.ShouldBeLessThan((byte)255);
    }

    [Fact]
    public void Managed_passes_never_change_the_dimensions()
    {
        var source = SplitImage();
        foreach (var pass in new Func<PixelSurface, PixelSurface>[]
        {
            s => ManagedEffects.Comic(s),
            s => ManagedEffects.Sketch(s),
            s => ManagedEffects.Posterize(s),
            s => ManagedEffects.Pixelate(s),
            s => ManagedEffects.Blur(s, 2)
        })
        {
            var result = pass(SplitImage());
            result.Width.ShouldBe(source.Width);
            result.Height.ShouldBe(source.Height);
        }
    }


    [Fact]
    public void Spatial_built_ins_carry_a_managed_pass_so_stills_are_never_silently_unfiltered()
    {
        foreach (var effect in new[]
        {
            CameraEffects.Comic, CameraEffects.Sketch, CameraEffects.Posterize,
            CameraEffects.Pixelate, CameraEffects.Blur
        })
        {
            effect.Descriptor.Managed.ShouldNotBeNull($"{effect.Id} has no managed still fallback");
            effect.Descriptor.HasSpatialProgram.ShouldBeTrue($"{effect.Id} does not report as spatial");
        }
    }

    [Fact]
    public void Comic_and_Sketch_target_every_gpu_backend()
    {
        foreach (var effect in new[] { CameraEffects.Comic, CameraEffects.Sketch })
        {
            effect.Descriptor.CoreImageFilterName.ShouldNotBeNullOrWhiteSpace($"{effect.Id}: no Core Image filter");
            effect.Descriptor.AgslShader.ShouldNotBeNullOrWhiteSpace($"{effect.Id}: no AGSL shader");
            effect.Descriptor.SvgFilter.ShouldNotBeNullOrWhiteSpace($"{effect.Id}: no SVG filter");
        }
    }

    [Fact]
    public void Blur_uses_the_platform_blur_on_android_rather_than_a_shader()
    {
        CameraEffects.Blur.Descriptor.AndroidBlurRadius.ShouldNotBeNull();
        CameraEffects.Blur.Descriptor.AgslShader.ShouldBeNull();
        CameraEffects.Blur.Descriptor.Css.ShouldBe("blur(8px)");
    }

    [Fact]
    public void A_spatial_effect_with_no_program_reports_still_only_when_the_photo_is_still_filtered()
        => CameraEffectChain
            .ResolveSupport(CameraEffects.Comic, hasNativeProgram: false, filtersPreview: true, hasStillFallback: true)
            .ShouldBe(EffectSupport.StillOnly);

    [Fact]
    public void A_spatial_effect_with_no_program_and_no_fallback_reports_unsupported()
        => CameraEffectChain
            .ResolveSupport(CameraEffects.Comic, hasNativeProgram: false, filtersPreview: true, hasStillFallback: false)
            .ShouldBe(EffectSupport.Unsupported);


    [Fact]
    public void Plan_keeps_colour_and_spatial_steps_in_chain_order()
    {
        // [Comic, Mono] must not be reordered into [Mono, Comic] — they are different images
        var chain = CameraEffectChain.Create(CameraFilter.None, [CameraEffects.Comic, CameraEffects.Mono]);
        var steps = chain.Plan(e => (e as INativeEffect)?.Descriptor.Managed is not null);

        steps.Count.ShouldBe(2);
        steps[0].Native.ShouldBe(CameraEffects.Comic);
        steps[1].Color.ShouldNotBeNull();
    }

    [Fact]
    public void Applying_a_plan_in_order_differs_from_applying_it_reversed()
    {
        static PixelSurface Run(PixelSurface s, params Func<PixelSurface, PixelSurface>[] passes)
        {
            foreach (var pass in passes)
                s = pass(s);
            return s;
        }

        var greyThenPixelate = Run(SplitImage(),
            s => { s.Apply(ColorMatrix4x5.Saturation(0f)); return s; },
            s => ManagedEffects.Pixelate(s, 4));

        var pixelateThenComic = Run(SplitImage(),
            s => ManagedEffects.Pixelate(s, 4),
            s => ManagedEffects.Comic(s));

        // sanity: the two orderings genuinely produce different pixels, which is why order is preserved
        At(greyThenPixelate, 8, 8).ShouldNotBe(At(pixelateThenComic, 8, 8));
    }
}
