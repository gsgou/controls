using Shiny.Controls.MotionIcons;

namespace Shiny.Controls.MotionIcons.Tests;

public class MotionSpecTests
{
    [Fact]
    public void BuilderPadsTracksToSpanTheCycle()
    {
        var spec = MotionSpecBuilder.Build(500, m => m
            .Scale(k => k.At(0.4d, 1.5d)));

        var keys = spec.Tracks[0].Keys;

        keys[0].Offset.ShouldBe(0d);
        keys[0].Value.ShouldBe(1.5d);
        keys[^1].Offset.ShouldBe(1d);
        keys[^1].Value.ShouldBe(1.5d);
    }

    [Fact]
    public void BuilderSortsKeysByOffset()
    {
        var spec = MotionSpecBuilder.Build(500, m => m
            .Opacity(k => k.At(1d, 1d).At(0d, 0d).At(0.5d, 0.5d)));

        spec.Tracks[0].Keys.Select(x => x.Offset).ShouldBe([0d, 0.5d, 1d]);
    }

    [Fact]
    public void IntervalSquashesTheKeysAndHoldsTheRestingPose()
    {
        var spec = MotionSpecBuilder.Build(1000, m => m
                .Scale(k => k.At(0d, 1d).At(0.5d, 2d).At(1d, 1d)))
            .WithInterval(TimeSpan.FromSeconds(1));

        spec.Duration.ShouldBe(TimeSpan.FromSeconds(2));

        var keys = spec.Tracks[0].Keys;

        // The original cycle now occupies the first half...
        keys[0].Offset.ShouldBe(0d);
        keys[1].Offset.ShouldBe(0.25d);
        keys[2].Offset.ShouldBe(0.5d);

        // ...and the resting pose is held through the rest, rather than drifting back to the start
        // over the whole gap, which is what an unheld track would do.
        keys[^1].Offset.ShouldBe(1d);
        keys[^1].Value.ShouldBe(keys[2].Value);
    }

    [Fact]
    public void IntervalIsIgnoredWhenZeroOrNegative()
    {
        var spec = MotionSpecBuilder.Build(1000, m => m.Scale(k => k.At(0d, 1d).At(1d, 2d)));

        spec.WithInterval(TimeSpan.Zero).ShouldBeSameAs(spec);
        spec.WithInterval(TimeSpan.FromSeconds(-1)).ShouldBeSameAs(spec);
    }

    [Fact]
    public void RetimingKeepsTheKeysAndTheRootOrigin()
    {
        var spec = MotionSpecBuilder.Build(1000, m => m.Scale(k => k.At(0d, 1d).At(1d, 2d)))
            with { RootOrigin = new MotionPoint(3f, 4f) };

        var retimed = spec.WithDuration(TimeSpan.FromMilliseconds(250));

        retimed.Duration.ShouldBe(TimeSpan.FromMilliseconds(250));
        retimed.Tracks[0].Keys.ShouldBe(spec.Tracks[0].Keys);
        retimed.RootOrigin.ShouldBe(new MotionPoint(3f, 4f));
    }

    [Fact]
    public void SamplerHonoursTheSegmentStartEasing()
    {
        // Easing belongs to the segment that starts at a key — CSS semantics. A step-end on the
        // first key means the value must not move at all until the second key is reached.
        var keys = new[]
        {
            new MotionKey(0d, 0d, MotionEase.StepEnd),
            new MotionKey(1d, 10d)
        };

        MotionSampler.ValueAt(keys, 0.5d).ShouldBe(0d);
        MotionSampler.ValueAt(keys, 0.99d).ShouldBe(0d);
        MotionSampler.ValueAt(keys, 1d).ShouldBe(10d);
    }

    [Fact]
    public void SamplerClampsOutsideTheCycle()
    {
        var keys = new[] { new MotionKey(0d, 2d), new MotionKey(1d, 8d) };

        MotionSampler.ValueAt(keys, -1d).ShouldBe(2d);
        MotionSampler.ValueAt(keys, 5d).ShouldBe(8d);
    }

    [Fact]
    public void SamplerInterpolatesLinearlyWhenAsked()
    {
        var keys = new[] { new MotionKey(0d, 0d, MotionEase.Linear), new MotionKey(1d, 10d) };

        MotionSampler.ValueAt(keys, 0.25d).ShouldBe(2.5d, 1e-9d);
    }

    [Fact]
    public void PresetsWorkOnArtworkTheLibraryHasNeverSeen()
    {
        var custom = MotionIconLibrary.FromPath("M2 2 L22 22");

        foreach (var preset in Enum.GetValues<MotionPreset>())
        {
            var spec = MotionPresets.Build(preset, custom);

            if (preset is MotionPreset.None)
            {
                spec.ShouldBeNull();
                continue;
            }

            spec.ShouldNotBeNull();
            spec.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
            spec.IsEmpty.ShouldBeFalse();
        }
    }

    [Fact]
    public void DefaultFallsBackToPulseForArtworkWithNoMotion()
    {
        var custom = MotionIconLibrary.FromPath("M2 2 L22 22");

        var fallback = MotionPresets.Build(MotionPreset.Default, custom)!;
        var pulse = MotionPresets.Build(MotionPreset.Pulse, custom)!;

        fallback.Duration.ShouldBe(pulse.Duration);
        fallback.Tracks.Count.ShouldBe(pulse.Tracks.Count);
        fallback.Tracks[0].Channel.ShouldBe(pulse.Tracks[0].Channel);
        fallback.Tracks[0].Keys.ShouldBe(pulse.Tracks[0].Keys);
    }

    [Fact]
    public void DefaultPrefersTheIconsOwnMotion()
    {
        var bell = MotionIconLibrary.Get("bell");

        MotionPresets.Build(MotionPreset.Default, bell).ShouldBe(bell.Motion);
    }

    [Fact]
    public void DrawStaggersOnePartAfterAnother()
    {
        var icon = MotionIconLibrary.Get("home");
        var spec = MotionPresets.Build(MotionPreset.Draw, icon)!;

        spec.Tracks.Count.ShouldBe(icon.Parts.Count);

        // Each part should begin drawing where the previous one finished.
        for (var i = 0; i < spec.Tracks.Count; i++)
        {
            var keys = spec.Tracks[i].Keys;
            var startsDrawing = keys.First(k => k.Value > 0d).Offset;

            startsDrawing.ShouldBe((i + 1) / (double)icon.Parts.Count, 0.001d);
        }
    }

    [Fact]
    public void ResolverPrefersAnExplicitDefinitionOverANameOverPathData()
    {
        var explicitIcon = MotionIconLibrary.FromPath("M0 0", "explicit");

        MotionResolver.ResolveIcon(explicitIcon, "bell", "M1 1").ShouldBe(explicitIcon);
        MotionResolver.ResolveIcon(null, "bell", "M1 1")!.Name.ShouldBe("bell");
        MotionResolver.ResolveIcon(null, null, "M1 1")!.Parts[0].Path.ShouldBe("M1 1");
        MotionResolver.ResolveIcon(null, null, null).ShouldBeNull();
    }

    [Fact]
    public void ResolverFallsThroughAnUnknownNameToPathData()
    {
        // A typo in a name should not take the raw path down with it.
        MotionResolver.ResolveIcon(null, "not-an-icon", "M1 1")!.Parts[0].Path.ShouldBe("M1 1");
    }
}
