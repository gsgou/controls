using Shiny.Controls.Keyframe;
using Shiny.Controls.MotionIcons;

namespace Shiny.Controls.MotionIcons.Tests;

/// <summary>
/// The motion-icon easing curves are declared separately from the keyframe engine's, because a spec
/// has to survive being compiled into CSS and a delegate cannot be. Separate declarations drift, so
/// these pin them together: an icon and a hand-written keyframe animation beside it move alike.
/// </summary>
public class EasingParityTests
{
    public static TheoryData<MotionEase, string> Pairs => new()
    {
        { MotionEase.Linear, nameof(Easings.Linear) },
        { MotionEase.Ease, nameof(Easings.Ease) },
        { MotionEase.EaseIn, nameof(Easings.EaseIn) },
        { MotionEase.EaseOut, nameof(Easings.EaseOut) },
        { MotionEase.EaseInOut, nameof(Easings.EaseInOut) },
        { MotionEase.QuadIn, nameof(Easings.QuadIn) },
        { MotionEase.QuadOut, nameof(Easings.QuadOut) },
        { MotionEase.QuadInOut, nameof(Easings.QuadInOut) },
        { MotionEase.CubicIn, nameof(Easings.CubicIn) },
        { MotionEase.CubicOut, nameof(Easings.CubicOut) },
        { MotionEase.CubicInOut, nameof(Easings.CubicInOut) },
        { MotionEase.QuartOut, nameof(Easings.QuartOut) },
        { MotionEase.QuintOut, nameof(Easings.QuintOut) },
        { MotionEase.SinIn, nameof(Easings.SinIn) },
        { MotionEase.SinOut, nameof(Easings.SinOut) },
        { MotionEase.SinInOut, nameof(Easings.SinInOut) },
        { MotionEase.ExpoIn, nameof(Easings.ExpoIn) },
        { MotionEase.ExpoOut, nameof(Easings.ExpoOut) },
        { MotionEase.CircOut, nameof(Easings.CircOut) },
        { MotionEase.BackIn, nameof(Easings.BackIn) },
        { MotionEase.BackOut, nameof(Easings.BackOut) },
        { MotionEase.BackInOut, nameof(Easings.BackInOut) },
        { MotionEase.ElasticOut, nameof(Easings.ElasticOut) },
        { MotionEase.BounceOut, nameof(Easings.BounceOut) },
        { MotionEase.StepEnd, nameof(Easings.StepEnd) }
    };

    [Theory]
    [MemberData(nameof(Pairs))]
    public void MatchesTheKeyframeEngineCurve(MotionEase ease, string keyframeName)
    {
        var reference = (EasingFunction)typeof(Easings)
            .GetField(keyframeName)!
            .GetValue(null)!;

        for (var i = 0; i <= 100; i++)
        {
            var t = i / 100d;

            MotionEasings.Evaluate(ease, t)
                .ShouldBe(reference(t), 1e-6d, $"{ease} diverges from Easings.{keyframeName} at t={t}");
        }
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void CurvesArePinnedAtBothEnds(MotionEase ease, string keyframeName)
    {
        _ = keyframeName;

        MotionEasings.Evaluate(ease, 0d).ShouldBe(0d, 1e-9d);
        MotionEasings.Evaluate(ease, 1d).ShouldBe(1d, 1e-9d);
    }

    [Fact]
    public void OnlyExactlyEquivalentCurvesClaimACssKeyword()
    {
        // A keyword is emitted verbatim into the stylesheet, so claiming one for a curve CSS does
        // not actually have is how the browser would end up animating something subtly different
        // from what MAUI draws. Everything else has to be sampled into linear().
        foreach (var ease in Enum.GetValues<MotionEase>())
        {
            if (MotionEasings.CssKeyword(ease) is null)
                continue;

            ease.ShouldBeOneOf(
                MotionEase.Linear,
                MotionEase.Ease,
                MotionEase.EaseIn,
                MotionEase.EaseOut,
                MotionEase.EaseInOut,
                MotionEase.StepEnd);
        }
    }
}
