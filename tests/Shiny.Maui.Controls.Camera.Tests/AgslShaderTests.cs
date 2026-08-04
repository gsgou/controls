using System.Text.RegularExpressions;
using Shiny.Controls.Camera;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

/// <summary>
/// Static checks on the built-in AGSL shader sources.
///
/// AGSL only compiles on a device at API 33+, so a broken shader is invisible from here — and the failure mode
/// is the worst kind: <c>RuntimeShader</c> throws, the step is dropped, and the effect simply does nothing,
/// which looks exactly like "not supported on this platform". Two built-ins shipped using <c>flat</c> — a
/// reserved interpolation qualifier — as a variable name and were dead on Android for precisely that reason.
/// These tests are cheap insurance against the same mistake.
/// </summary>
public class AgslShaderTests
{
    // Reserved in GLSL ES / SkSL and never legitimately an identifier in our shaders. Deliberately excludes
    // words we do use correctly as keywords (const, uniform, if, return, ...).
    static readonly string[] ReservedIdentifiers =
    [
        "flat", "smooth", "noperspective", "centroid", "patch", "sample",
        "varying", "attribute", "subroutine", "invariant", "precise",
        "input", "output", "buffer", "shared", "coherent", "volatile", "restrict"
    ];

    public static TheoryData<string, string> Shaders()
    {
        var data = new TheoryData<string, string>();
        foreach (var effect in All())
        {
            if (effect.Descriptor.AgslShader is { Length: > 0 } source)
                data.Add(effect.Id, source);
        }
        return data;
    }

    static BuiltInCameraEffect[] All() =>
    [
        CameraEffects.Comic, CameraEffects.Sketch, CameraEffects.Posterize,
        CameraEffects.Pixelate, CameraEffects.Blur
    ];


    [Theory]
    [MemberData(nameof(Shaders))]
    public void Shader_declares_no_reserved_identifier(string id, string source)
    {
        foreach (var reserved in ReservedIdentifiers)
        {
            // a declaration looks like `float3 flat = ...` / `half4 sample;`
            var declaration = new Regex($@"\b(float|half|int|bool|void)[234]?x?[234]?\s+{reserved}\b");

            declaration.IsMatch(source).ShouldBeFalse(
                $"{id}: '{reserved}' is reserved in AGSL — declaring it makes the whole shader fail to " +
                "compile at runtime, and the effect then silently does nothing on Android");
        }
    }

    [Theory]
    [MemberData(nameof(Shaders))]
    public void Shader_declares_the_input_shader_uniform_it_will_be_bound_to(string id, string source)
    {
        // RenderEffect.CreateRuntimeShaderEffect binds the frame to a uniform by name; if the declared name
        // and the bound name disagree, the effect does nothing.
        var expected = All().First(e => e.Id == id).Descriptor.AgslInputName ?? "content";

        source.ShouldContain($"uniform shader {expected}",
            customMessage: $"{id}: the shader must declare 'uniform shader {expected}' to receive the frame");
    }

    [Theory]
    [MemberData(nameof(Shaders))]
    public void Shader_has_a_main_returning_half4(string id, string source)
        => Regex.IsMatch(source, @"half4\s+main\s*\(\s*float2\s+\w+\s*\)").ShouldBeTrue(
            $"{id}: AGSL requires 'half4 main(float2 coord)'");

    [Theory]
    [MemberData(nameof(Shaders))]
    public void Shader_samples_the_input_through_eval(string id, string source)
    {
        var name = All().First(e => e.Id == id).Descriptor.AgslInputName ?? "content";
        source.ShouldContain($"{name}.eval(",
            customMessage: $"{id}: the frame is read via {name}.eval(coord) in AGSL");
    }


    [Fact]
    public void Blur_has_no_shader_because_android_has_a_first_class_blur()
    {
        // RenderEffect.CreateBlurEffect is both better looking and available from API 31 rather than 33
        CameraEffects.Blur.Descriptor.AgslShader.ShouldBeNull();
        CameraEffects.Blur.Descriptor.AndroidBlurRadius.ShouldNotBeNull();
    }

    [Fact]
    public void Every_spatial_built_in_reaches_android_one_way_or_another()
    {
        foreach (var effect in All())
        {
            var d = effect.Descriptor;
            (d.AgslShader is not null || d.AndroidBlurRadius is not null).ShouldBeTrue(
                $"{effect.Id} has no Android preview path at all");
        }
    }
}
