using Shiny.Controls.Camera;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

/// <summary>
/// Guards the effect model against the one regression that matters most: the twelve built-in looks moved from
/// three hand-written per-platform switches into one shared table, so their pixels must not have moved with
/// them. The expected values here are the literal matrices the old <c>AndroidCameraFilters</c> handed to
/// <c>android.graphics.ColorMatrix</c> — offsets in 0..255 space, which is what <c>ToAndroidArray</c> produces.
/// </summary>
public class CameraEffectTests
{
    // android.graphics.ColorMatrix.setSaturation weights
    static float[] Saturation(float sat)
    {
        var inv = 1f - sat;
        var r = 0.213f * inv;
        var g = 0.715f * inv;
        var b = 0.072f * inv;
        return
        [
            r + sat, g,       b,       0, 0,
            r,       g + sat, b,       0, 0,
            r,       g,       b + sat, 0, 0,
            0,       0,       0,       1, 0
        ];
    }

    static void ShouldMatch(float[] actual, float[] expected)
    {
        actual.Length.ShouldBe(20);
        for (var i = 0; i < 20; i++)
            actual[i].ShouldBe(expected[i], 0.0001f, $"coefficient {i}");
    }


    [Fact]
    public void Mono_matches_the_original_android_matrix()
        => ShouldMatch(CameraEffects.Mono.ColorMatrix.ToAndroidArray(), Saturation(0f));

    [Fact]
    public void Vivid_matches_the_original_android_matrix()
        => ShouldMatch(CameraEffects.Vivid.ColorMatrix.ToAndroidArray(), Saturation(1.6f));

    [Fact]
    public void Noir_matches_the_original_saturation_then_contrast_composition()
    {
        // Saturation(0) PostConcat contrast(1.4, -40)
        var s = Saturation(0f);
        var expected = new float[20];
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 4; col++)
                expected[(row * 5) + col] = 1.4f * s[(row * 5) + col];

            expected[(row * 5) + 4] = -40f;
        }
        expected[15] = 0; expected[16] = 0; expected[17] = 0; expected[18] = 1; expected[19] = 0;

        ShouldMatch(CameraEffects.Noir.ColorMatrix.ToAndroidArray(), expected);
    }

    [Fact]
    public void Sepia_matches_the_original_android_matrix()
        => ShouldMatch(CameraEffects.Sepia.ColorMatrix.ToAndroidArray(),
        [
            0.393f, 0.769f, 0.189f, 0, 0,
            0.349f, 0.686f, 0.168f, 0, 0,
            0.272f, 0.534f, 0.131f, 0, 0,
            0,      0,      0,      1f, 0
        ]);

    [Fact]
    public void Invert_matches_the_original_android_matrix()
        => ShouldMatch(CameraEffects.Invert.ColorMatrix.ToAndroidArray(),
        [
            -1f, 0,   0,   0, 255f,
            0,   -1f, 0,   0, 255f,
            0,   0,   -1f, 0, 255f,
            0,   0,   0,   1f, 0
        ]);

    [Fact]
    public void Instant_matches_the_original_android_matrix()
        => ShouldMatch(CameraEffects.Instant.ColorMatrix.ToAndroidArray(),
        [
            1.0f,  0.1f,  0.05f, 0, 12f,
            0.05f, 0.95f, 0.05f, 0, 10f,
            0.05f, 0.1f,  0.8f,  0, 4f,
            0,     0,     0,     1f, 0
        ]);

    [Fact]
    public void Cool_and_Warm_keep_their_single_channel_offsets()
    {
        CameraEffects.Cool.ColorMatrix.ToAndroidArray()[14].ShouldBe(10f, 0.0001f);
        CameraEffects.Warm.ColorMatrix.ToAndroidArray()[4].ShouldBe(10f, 0.0001f);
    }

    [Fact]
    public void Every_camera_filter_value_except_None_maps_to_a_built_in()
    {
        foreach (var filter in Enum.GetValues<CameraFilter>())
        {
            if (filter == CameraFilter.None)
                CameraEffects.For(filter).ShouldBeNull();
            else
                CameraEffects.For(filter).ShouldNotBeNull($"{filter} has no built-in effect");
        }
    }

    [Fact]
    public void Every_built_in_carries_a_core_image_name_and_css_so_apple_and_the_browser_are_unchanged()
    {
        foreach (var filter in Enum.GetValues<CameraFilter>().Where(f => f != CameraFilter.None))
        {
            var descriptor = CameraEffects.For(filter)!.Descriptor;
            descriptor.CoreImageFilterName.ShouldNotBeNullOrWhiteSpace($"{filter} lost its Core Image filter");
            descriptor.Css.ShouldNotBeNullOrWhiteSpace($"{filter} lost its CSS filter");
        }
    }


    [Fact]
    public void Composition_applies_this_matrix_first()
    {
        // scale by 2 then add 0.1 => 0.2*2 + 0.1 = 0.5, not (0.2 + 0.1) * 2 = 0.6
        var composed = ColorMatrix4x5.ScaleOffset(2f, 0f).Then(ColorMatrix4x5.ScaleOffset(1f, 0.1f));

        composed[0, 0].ShouldBe(2f, 0.0001f);
        composed[0, 4].ShouldBe(0.1f, 0.0001f);
    }

    [Fact]
    public void Identity_is_detected_so_backends_can_skip_it()
    {
        ColorMatrix4x5.Identity.IsIdentity.ShouldBeTrue();
        ColorMatrix4x5.Saturation(1f).IsIdentity.ShouldBeTrue();
        ColorMatrix4x5.Saturation(0f).IsIdentity.ShouldBeFalse();
    }


    [Fact]
    public void Chain_puts_Filter_first_then_the_effects_in_order()
    {
        var custom = new ColorEffect("custom", ColorMatrix4x5.Saturation(0.5f));
        var chain = CameraEffectChain.Create(CameraFilter.Sepia, [custom]);

        chain.Effects.Count.ShouldBe(2);
        chain.Effects[0].ShouldBe(CameraEffects.Sepia);
        chain.Effects[1].ShouldBe(custom);
    }

    [Fact]
    public void Chain_skips_disabled_effects()
    {
        var off = new ColorEffect("off", ColorMatrix4x5.Saturation(0f)) { IsEnabled = false };
        var chain = CameraEffectChain.Create(CameraFilter.None, [off]);

        chain.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Plan_collapses_consecutive_colour_effects_into_one_matrix()
    {
        var chain = CameraEffectChain.Create(CameraFilter.None,
        [
            new ColorEffect("a", ColorMatrix4x5.ScaleOffset(2f, 0f)),
            new ColorEffect("b", ColorMatrix4x5.ScaleOffset(1f, 0.1f)),
            new ColorEffect("c", ColorMatrix4x5.ScaleOffset(1f, 0.1f))
        ]);

        // nothing is handled natively, so all three fold into a single pass
        var steps = chain.Plan(_ => false);

        steps.Count.ShouldBe(1);
        steps[0].Color.ShouldNotBeNull();
        steps[0].Color![0, 4].ShouldBe(0.2f, 0.0001f);
    }

    [Fact]
    public void Plan_keeps_native_steps_separate_and_in_order()
    {
        var chain = CameraEffectChain.Create(CameraFilter.None,
        [
            new ColorEffect("a", ColorMatrix4x5.Saturation(0f)),
            CameraEffects.Sepia,
            new ColorEffect("b", ColorMatrix4x5.Saturation(0f))
        ]);

        var steps = chain.Plan(e => e is BuiltInCameraEffect);

        steps.Count.ShouldBe(3);
        steps[0].Color.ShouldNotBeNull();
        steps[1].Native.ShouldBe(CameraEffects.Sepia);
        steps[2].Color.ShouldNotBeNull();
    }

    [Fact]
    public void Plan_drops_an_effect_the_backend_cannot_express_at_all()
    {
        var spatialOnly = new SpatialOnlyEffect();
        var chain = CameraEffectChain.Create(CameraFilter.None, [spatialOnly]);

        chain.Plan(_ => false).ShouldBeEmpty();
    }


    [Fact]
    public void Support_reports_a_spatial_effect_without_a_native_program_as_colour_only()
        => CameraEffectChain
            .ResolveSupport(new SpatialWithFallbackEffect(), hasNativeProgram: false, filtersPreview: true)
            .ShouldBe(EffectSupport.ColorOnly);

    [Fact]
    public void Support_reports_unsupported_when_there_is_no_program_and_no_matrix()
        => CameraEffectChain
            .ResolveSupport(new SpatialOnlyEffect(), hasNativeProgram: false, filtersPreview: true)
            .ShouldBe(EffectSupport.Unsupported);

    [Fact]
    public void Support_reports_still_only_where_the_preview_is_not_filtered()
        => CameraEffectChain
            .ResolveSupport(CameraEffects.Mono, hasNativeProgram: false, filtersPreview: false)
            .ShouldBe(EffectSupport.StillOnly);

    [Fact]
    public void Support_never_degrades_a_draw_effect()
        => CameraEffectChain
            .ResolveSupport(new DelegateDrawEffect("d", (_, _, _) => { }), hasNativeProgram: false, filtersPreview: false)
            .ShouldBe(EffectSupport.Full);


    sealed class SpatialOnlyEffect : INativeEffect
    {
        public string Id => "spatial.only";
        public bool IsEnabled => true;
        public NativeEffectDescriptor Descriptor { get; } = new(AgslShader: "// shader");
    }

    sealed class SpatialWithFallbackEffect : IColorEffect, INativeEffect
    {
        public string Id => "spatial.fallback";
        public bool IsEnabled => true;
        public ColorMatrix4x5 ColorMatrix { get; } = ColorMatrix4x5.Saturation(0f);
        public NativeEffectDescriptor Descriptor { get; } = new(AgslShader: "// shader");
    }
}
