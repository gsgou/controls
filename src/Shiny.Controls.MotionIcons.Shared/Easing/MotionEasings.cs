namespace Shiny.Controls.MotionIcons;

/// <summary>
/// The canonical definition of every <see cref="MotionEase"/> curve.
/// </summary>
/// <remarks>
/// <para>This is the one place a curve is defined. MAUI wraps <see cref="Evaluate"/> straight into
/// its timeline, and the Blazor generator either maps a curve to the CSS keyword that means exactly
/// the same thing or samples this function into a <c>linear()</c> curve. Nothing approximates
/// anything with a "close enough" cubic-bezier, which is what would otherwise make the two hosts
/// drift apart on the overshoot curves where the difference actually shows.</para>
/// <para>The formulas match <c>Shiny.Controls.Keyframe.Easings</c> term for term — a test asserts
/// it — so an icon and a hand-written keyframe timeline next to it share a visual language.</para>
/// </remarks>
public static class MotionEasings
{
    const double BackConstant = 1.70158d;
    const double BackInOutConstant = BackConstant * 1.525d;
    const double ElasticPeriod = 2d * Math.PI / 3d;

    /// <summary>Maps linear progress onto eased progress.</summary>
    /// <param name="ease">The curve.</param>
    /// <param name="t">Linear progress, 0 to 1.</param>
    /// <returns>Eased progress. Overshoot curves deliberately return values outside 0..1.</returns>
    public static double Evaluate(MotionEase ease, double t) => ease switch
    {
        MotionEase.Linear => t,
        MotionEase.Ease => CubicBezier(0.25d, 0.1d, 0.25d, 1.0d, t),
        MotionEase.EaseIn => CubicBezier(0.42d, 0.0d, 1.0d, 1.0d, t),
        MotionEase.EaseOut => CubicBezier(0.0d, 0.0d, 0.58d, 1.0d, t),
        MotionEase.EaseInOut => CubicBezier(0.42d, 0.0d, 0.58d, 1.0d, t),

        MotionEase.QuadIn => t * t,
        MotionEase.QuadOut => 1d - (1d - t) * (1d - t),
        MotionEase.QuadInOut => t < 0.5d ? 2d * t * t : 1d - Pow(-2d * t + 2d, 2) / 2d,

        MotionEase.CubicIn => t * t * t,
        MotionEase.CubicOut => 1d - Pow(1d - t, 3),
        MotionEase.CubicInOut => t < 0.5d ? 4d * t * t * t : 1d - Pow(-2d * t + 2d, 3) / 2d,

        MotionEase.QuartOut => 1d - Pow(1d - t, 4),
        MotionEase.QuintOut => 1d - Pow(1d - t, 5),

        MotionEase.SinIn => 1d - Math.Cos(t * Math.PI / 2d),
        MotionEase.SinOut => Math.Sin(t * Math.PI / 2d),
        MotionEase.SinInOut => -(Math.Cos(Math.PI * t) - 1d) / 2d,

        MotionEase.ExpoIn => t <= 0d ? 0d : Math.Pow(2d, 10d * t - 10d),
        MotionEase.ExpoOut => t >= 1d ? 1d : 1d - Math.Pow(2d, -10d * t),

        MotionEase.CircOut => Math.Sqrt(Math.Max(0d, 1d - Pow(t - 1d, 2))),

        MotionEase.BackIn => (BackConstant + 1d) * t * t * t - BackConstant * t * t,
        MotionEase.BackOut => 1d + (BackConstant + 1d) * Pow(t - 1d, 3) + BackConstant * Pow(t - 1d, 2),
        MotionEase.BackInOut => t < 0.5d
            ? Pow(2d * t, 2) * ((BackInOutConstant + 1d) * 2d * t - BackInOutConstant) / 2d
            : (Pow(2d * t - 2d, 2) * ((BackInOutConstant + 1d) * (t * 2d - 2d) + BackInOutConstant) + 2d) / 2d,

        MotionEase.ElasticOut => t <= 0d ? 0d
            : t >= 1d ? 1d
            : Math.Pow(2d, -10d * t) * Math.Sin((t * 10d - 0.75d) * ElasticPeriod) + 1d,

        MotionEase.BounceOut => BounceOut(t),

        MotionEase.StepEnd => t >= 1d ? 1d : 0d,

        _ => t
    };

    /// <summary>
    /// Whether the curve is exactly representable by a CSS timing-function keyword, and if so
    /// which one.
    /// </summary>
    /// <remarks>
    /// Only the five CSS-native curves qualify. Everything else has to be sampled, because CSS has
    /// no keyword for it and picking a "roughly similar" cubic-bezier is how a bounce ends up
    /// bouncing differently on the two hosts.
    /// </remarks>
    public static string? CssKeyword(MotionEase ease) => ease switch
    {
        MotionEase.Linear => "linear",
        MotionEase.Ease => "ease",
        MotionEase.EaseIn => "ease-in",
        MotionEase.EaseOut => "ease-out",
        MotionEase.EaseInOut => "ease-in-out",
        MotionEase.StepEnd => "step-end",
        _ => null
    };

    static double BounceOut(double t)
    {
        const double n = 7.5625d;
        const double d = 2.75d;

        if (t < 1d / d)
            return n * t * t;

        if (t < 2d / d)
        {
            t -= 1.5d / d;
            return n * t * t + 0.75d;
        }

        if (t < 2.5d / d)
        {
            t -= 2.25d / d;
            return n * t * t + 0.9375d;
        }

        t -= 2.625d / d;
        return n * t * t + 0.984375d;
    }

    static double Pow(double value, int exponent)
    {
        var result = value;
        for (var i = 1; i < exponent; i++)
            result *= value;

        return result;
    }

    /// <summary>
    /// Evaluates a CSS-style cubic bezier at <paramref name="t"/>, where the curve's control
    /// points are (x1,y1) and (x2,y2) and its endpoints are pinned at (0,0) and (1,1).
    /// </summary>
    /// <remarks>
    /// The curve is parametric, so the x for a given parameter has to be solved for before y can
    /// be read off. Newton converges in two or three steps almost everywhere; the bisection
    /// fallback covers the flat spots where the derivative approaches zero and Newton stalls.
    /// </remarks>
    static double CubicBezier(double x1, double y1, double x2, double y2, double t)
    {
        if (t <= 0d)
            return 0d;

        if (t >= 1d)
            return 1d;

        var u = t;

        for (var i = 0; i < 8; i++)
        {
            var x = BezierAxis(u, x1, x2) - t;

            if (Math.Abs(x) < 1e-7d)
                return BezierAxis(u, y1, y2);

            var slope = BezierAxisDerivative(u, x1, x2);

            if (Math.Abs(slope) < 1e-7d)
                break;

            u -= x / slope;
        }

        var low = 0d;
        var high = 1d;
        u = t;

        for (var i = 0; i < 32; i++)
        {
            var x = BezierAxis(u, x1, x2);

            if (Math.Abs(x - t) < 1e-7d)
                break;

            if (x < t)
                low = u;
            else
                high = u;

            u = (low + high) / 2d;
        }

        return BezierAxis(u, y1, y2);
    }

    static double BezierAxis(double u, double a, double b)
    {
        var v = 1d - u;
        return 3d * v * v * u * a + 3d * v * u * u * b + u * u * u;
    }

    static double BezierAxisDerivative(double u, double a, double b)
    {
        var v = 1d - u;
        return 3d * v * v * a + 6d * v * u * (b - a) + 3d * u * u * (1d - b);
    }
}
