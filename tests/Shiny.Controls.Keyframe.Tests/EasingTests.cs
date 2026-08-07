using Shiny.Controls.Keyframe;

namespace Shiny.Controls.Keyframe.Tests;

public class EasingTests
{
    public static TheoryData<string, EasingFunction> AllCurves => new()
    {
        { nameof(Easings.Linear), Easings.Linear },
        { nameof(Easings.QuadIn), Easings.QuadIn },
        { nameof(Easings.QuadOut), Easings.QuadOut },
        { nameof(Easings.QuadInOut), Easings.QuadInOut },
        { nameof(Easings.CubicIn), Easings.CubicIn },
        { nameof(Easings.CubicOut), Easings.CubicOut },
        { nameof(Easings.CubicInOut), Easings.CubicInOut },
        { nameof(Easings.QuartIn), Easings.QuartIn },
        { nameof(Easings.QuartOut), Easings.QuartOut },
        { nameof(Easings.QuartInOut), Easings.QuartInOut },
        { nameof(Easings.QuintIn), Easings.QuintIn },
        { nameof(Easings.QuintOut), Easings.QuintOut },
        { nameof(Easings.QuintInOut), Easings.QuintInOut },
        { nameof(Easings.SinIn), Easings.SinIn },
        { nameof(Easings.SinOut), Easings.SinOut },
        { nameof(Easings.SinInOut), Easings.SinInOut },
        { nameof(Easings.ExpoIn), Easings.ExpoIn },
        { nameof(Easings.ExpoOut), Easings.ExpoOut },
        { nameof(Easings.ExpoInOut), Easings.ExpoInOut },
        { nameof(Easings.CircIn), Easings.CircIn },
        { nameof(Easings.CircOut), Easings.CircOut },
        { nameof(Easings.CircInOut), Easings.CircInOut },
        { nameof(Easings.BackIn), Easings.BackIn },
        { nameof(Easings.BackOut), Easings.BackOut },
        { nameof(Easings.BackInOut), Easings.BackInOut },
        { nameof(Easings.ElasticIn), Easings.ElasticIn },
        { nameof(Easings.ElasticOut), Easings.ElasticOut },
        { nameof(Easings.ElasticInOut), Easings.ElasticInOut },
        { nameof(Easings.BounceIn), Easings.BounceIn },
        { nameof(Easings.BounceOut), Easings.BounceOut },
        { nameof(Easings.BounceInOut), Easings.BounceInOut },
        { nameof(Easings.Ease), Easings.Ease },
        { nameof(Easings.EaseIn), Easings.EaseIn },
        { nameof(Easings.EaseOut), Easings.EaseOut },
        { nameof(Easings.EaseInOut), Easings.EaseInOut },
        { nameof(Easings.Emphasized), Easings.Emphasized }
    };

    [Theory]
    [MemberData(nameof(AllCurves))]
    public void EveryCurveIsPinnedAtBothEnds(string name, EasingFunction easing)
    {
        // Whatever a curve does in between, it must start at 0 and land exactly on 1 — otherwise
        // an animation visibly fails to reach its target value.
        Assert.True(Math.Abs(easing(0d)) < 1e-6, $"{name} did not start at 0.");
        Assert.True(Math.Abs(easing(1d) - 1d) < 1e-6, $"{name} did not finish at 1.");
    }

    [Theory]
    [MemberData(nameof(AllCurves))]
    public void EveryCurveStaysFinite(string name, EasingFunction easing)
    {
        for (var i = 0; i <= 100; i++)
        {
            var value = easing(i / 100d);
            Assert.True(double.IsFinite(value), $"{name} produced {value} at t={i / 100d}.");
        }
    }

    [Fact]
    public void CubicBezierMatchesTheLinearIdentity()
    {
        var linear = Easings.CubicBezier(0, 0, 1, 1);

        for (var i = 0; i <= 20; i++)
        {
            var t = i / 20d;
            Assert.Equal(t, linear(t), 6);
        }
    }

    [Fact]
    public void CubicBezierSolvesAKnownCssCurveAccurately()
    {
        // ease-in-out is symmetric about its midpoint, which is an easy independent check.
        var easeInOut = Easings.CubicBezier(0.42, 0, 0.58, 1);

        Assert.Equal(0.5d, easeInOut(0.5d), 4);

        for (var i = 1; i < 20; i++)
        {
            var t = i / 20d;
            Assert.Equal(easeInOut(t), 1d - easeInOut(1d - t), 4);
        }
    }

    [Fact]
    public void CubicBezierIsMonotonicWhenTheControlPointsAre()
    {
        var curve = Easings.CubicBezier(0.25, 0.1, 0.25, 1);
        var previous = curve(0d);

        for (var i = 1; i <= 200; i++)
        {
            var value = curve(i / 200d);
            Assert.True(value >= previous - 1e-9, $"Curve went backwards at t={i / 200d}.");
            previous = value;
        }
    }

    [Fact]
    public void CubicBezierExtendsLinearlyOutsideTheUnitInterval()
    {
        // Endpoint slopes of 2 at both ends, so the linear extension is easy to predict exactly.
        var curve = Easings.CubicBezier(0.25, 0.5, 0.75, 0.5);

        Assert.Equal(-1d, curve(-0.5d), 6);
        Assert.Equal(2d, curve(1.5d), 6);
    }

    [Fact]
    public void CubicBezierWithFlatEndsStaysFlatOutsideTheUnitInterval()
    {
        // ease-in-out has zero slope at both ends, so extending it produces no motion at all.
        // Worth pinning: it is the reason an out-of-range progress does not always overshoot.
        var curve = Easings.CubicBezier(0.42, 0, 0.58, 1);

        Assert.Equal(0d, curve(-0.5d), 6);
        Assert.Equal(1d, curve(1.5d), 6);
    }

    [Fact]
    public void CubicBezierClampsTimeControlPointsButNotProgressOnes()
    {
        // x outside [0,1] would make the curve non-monotonic in time; y outside is legitimate
        // overshoot and must survive.
        var overshoot = Easings.CubicBezier(0.5, -0.5, 0.5, 1.5);

        var hasUndershoot = Enumerable.Range(1, 50).Any(i => overshoot(i / 100d) < 0d);
        var hasOvershoot = Enumerable.Range(50, 49).Any(i => overshoot(i / 100d) > 1d);

        Assert.True(hasUndershoot, "Expected the curve to dip below 0.");
        Assert.True(hasOvershoot, "Expected the curve to rise above 1.");
    }

    [Fact]
    public void BackOutOvershootsThenSettles()
    {
        Assert.Contains(Enumerable.Range(1, 99).Select(i => Easings.BackOut(i / 100d)), v => v > 1d);
        Assert.Equal(1d, Easings.BackOut(1d), 6);
    }

    [Fact]
    public void BackInAnticipatesBeforeMovingForward()
        => Assert.Contains(Enumerable.Range(1, 40).Select(i => Easings.BackIn(i / 100d)), v => v < 0d);

    [Fact]
    public void StepsQuantisesProgress()
    {
        var steps = Easings.Steps(4);

        Assert.Equal(0d, steps(0.0d), 6);
        Assert.Equal(0d, steps(0.24d), 6);
        Assert.Equal(0.25d, steps(0.25d), 6);
        Assert.Equal(0.75d, steps(0.99d), 6);
        Assert.Equal(1d, steps(1.0d), 6);
    }

    [Fact]
    public void StepsWithJumpAtStartAdvancesImmediately()
    {
        var steps = Easings.Steps(4, jumpAtStart: true);

        Assert.Equal(0d, steps(0d), 6);
        Assert.Equal(0.25d, steps(0.01d), 6);
        Assert.Equal(1d, steps(1d), 6);
    }

    [Fact]
    public void StepsRejectsNonPositiveCounts()
        => Assert.Throws<ArgumentOutOfRangeException>(() => Easings.Steps(0));

    [Fact]
    public void ReverseMirrorsInTime()
    {
        var reversed = Easings.Reverse(Easings.QuadIn);

        for (var i = 0; i <= 20; i++)
        {
            var t = i / 20d;
            Assert.Equal(1d - Easings.QuadIn(1d - t), reversed(t), 6);
        }
    }

    [Fact]
    public void MirrorTurnsAnInCurveIntoAnInOutCurve()
    {
        var mirrored = Easings.Mirror(Easings.QuadIn);

        Assert.Equal(0d, mirrored(0d), 6);
        Assert.Equal(0.5d, mirrored(0.5d), 6);
        Assert.Equal(1d, mirrored(1d), 6);
        Assert.Equal(Easings.QuadInOut(0.25d), mirrored(0.25d), 6);
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    public void SpringSettlesAtOneForEveryDampingRegime(double damping)
    {
        var spring = Easings.Spring(damping, frequency: 12d);

        Assert.Equal(0d, spring(0d), 6);
        Assert.Equal(1d, spring(1d), 6);
        Assert.True(double.IsFinite(spring(0.5d)));
    }

    [Fact]
    public void UnderdampedSpringOvershootsAndOverdampedDoesNot()
    {
        var bouncy = Easings.Spring(damping: 0.25d, frequency: 20d);
        var sluggish = Easings.Spring(damping: 3d, frequency: 20d);

        var bouncyValues = Enumerable.Range(0, 100).Select(i => bouncy(i / 100d)).ToArray();
        var sluggishValues = Enumerable.Range(0, 100).Select(i => sluggish(i / 100d)).ToArray();

        Assert.Contains(bouncyValues, v => v > 1.001d);
        Assert.DoesNotContain(sluggishValues, v => v > 1.001d);
    }

    [Fact]
    public void StepEndHoldsUntilTheVeryEnd()
    {
        Assert.Equal(0d, Easings.StepEnd(0.999d), 6);
        Assert.Equal(1d, Easings.StepEnd(1d), 6);
    }
}
