using Shiny.Controls.MotionIcons;

namespace Shiny.Controls.MotionIcons.Tests;

/// <summary>
/// Guards the artwork itself. Every one of these caught something real while the set was being
/// drawn — a track pointed at a part that had been renamed, a pose that did not return to rest, a
/// path with a typo in it that simply rendered as nothing.
/// </summary>
public class IconLibraryTests
{
    public static TheoryData<string> IconNames
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var name in MotionIconLibrary.Names)
                data.Add(name);

            return data;
        }
    }

    [Fact]
    public void LibraryIsPopulated() => MotionIconLibrary.Names.Count.ShouldBeGreaterThan(30);

    [Theory]
    [MemberData(nameof(IconNames))]
    public void EveryIconHasArtwork(string name)
    {
        var icon = MotionIconLibrary.Get(name);

        icon.Parts.ShouldNotBeEmpty();

        foreach (var part in icon.Parts)
        {
            part.Path.ShouldNotBeNullOrWhiteSpace();
            part.Path.TrimStart().ShouldStartWith("M", Case.Insensitive);
        }
    }

    [Theory]
    [MemberData(nameof(IconNames))]
    public void PartIdsAreUnique(string name)
    {
        var icon = MotionIconLibrary.Get(name);
        var ids = icon.Parts.Select(x => x.Id).ToList();

        ids.Distinct(StringComparer.Ordinal).Count().ShouldBe(ids.Count);
    }

    [Theory]
    [MemberData(nameof(IconNames))]
    public void EveryIconHasAuthoredMotion(string name)
        => MotionIconLibrary.Get(name).Motion.ShouldNotBeNull();

    [Theory]
    [MemberData(nameof(IconNames))]
    public void MotionTracksTargetPartsThatExist(string name)
    {
        var icon = MotionIconLibrary.Get(name);
        var ids = icon.Parts.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var track in icon.Motion!.Tracks.Where(x => x.PartId is not null))
            ids.ShouldContain(track.PartId!, $"{name} animates a part that is not in the artwork");

        foreach (var track in icon.Motion.ColorTracks.Where(x => x.PartId is not null))
            ids.ShouldContain(track.PartId!, $"{name} paints a part that is not in the artwork");
    }

    [Theory]
    [MemberData(nameof(IconNames))]
    public void TracksSpanTheWholeCycle(string name)
    {
        // Both compilers assume a track covers the cycle end to end, which the builder guarantees
        // by padding. This is the assertion that the guarantee actually holds.
        foreach (var track in MotionIconLibrary.Get(name).Motion!.Tracks)
        {
            track.Keys.ShouldNotBeEmpty();
            track.Keys[0].Offset.ShouldBe(0d);
            track.Keys[^1].Offset.ShouldBe(1d);
        }
    }

    [Theory]
    [MemberData(nameof(IconNames))]
    public void KeysAreInAscendingOrder(string name)
    {
        foreach (var track in MotionIconLibrary.Get(name).Motion!.Tracks)
        {
            for (var i = 1; i < track.Keys.Count; i++)
                track.Keys[i].Offset.ShouldBeGreaterThanOrEqualTo(track.Keys[i - 1].Offset);
        }
    }

    [Theory]
    [MemberData(nameof(IconNames))]
    public void EveryTrackEndsAtItsRestingValue(string name)
    {
        // Stopping reverts to the artwork as drawn, on both hosts — MAUI resets the poses and the
        // browser drops the animation. A track that finishes anywhere other than its resting value
        // therefore ends with a visible jump. A reveal is free to *start* somewhere else (a check
        // starts undrawn, a pin starts above the icon); it just has to land back home.
        //
        // Rotation is exempt: the spinners deliberately finish a whole turn out, and the icons that
        // stop part-way round — a plus at 90, a cross at 180, the sun's eight rays at 45 — land on
        // an orientation their artwork is symmetric under, which no assertion here can know.
        foreach (var track in MotionIconLibrary.Get(name).Motion!.Tracks)
        {
            var rest = track.Channel switch
            {
                MotionChannel.Opacity or MotionChannel.Scale or MotionChannel.ScaleX
                    or MotionChannel.ScaleY or MotionChannel.StrokeWidth or MotionChannel.Trim => 1d,
                MotionChannel.TranslateX or MotionChannel.TranslateY => 0d,
                _ => double.NaN
            };

            if (double.IsNaN(rest))
                continue;

            track.Keys[^1].Value.ShouldBe(rest, 0.0001d,
                $"{name}/{track.PartId ?? "root"}/{track.Channel} does not return to rest");
        }
    }

    [Fact]
    public void UnknownNamesDoNotThrow() => MotionIconLibrary.Find("no-such-icon").ShouldBeNull();

    [Fact]
    public void LookupIsCaseInsensitive() => MotionIconLibrary.Find("BELL").ShouldNotBeNull();

    [Fact]
    public void CustomArtworkCanBeRegisteredAndRemoved()
    {
        var icon = new MotionIconDefinition("test-only-icon", [new MotionIconPart("p", "M0 0h10")]);

        MotionIconLibrary.Register(icon);
        MotionIconLibrary.Find("test-only-icon").ShouldBe(icon);

        MotionIconLibrary.Unregister("test-only-icon").ShouldBeTrue();
        MotionIconLibrary.Find("test-only-icon").ShouldBeNull();
    }
}
