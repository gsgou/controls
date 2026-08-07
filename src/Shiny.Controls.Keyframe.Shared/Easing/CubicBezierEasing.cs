namespace Shiny.Controls.Keyframe;

/// <summary>
/// A CSS-compatible <c>cubic-bezier(x1, y1, x2, y2)</c> timing function.
/// </summary>
/// <remarks>
/// The curve runs from (0,0) to (1,1) with control points (x1,y1) and (x2,y2). The x axis is
/// time and the y axis is progress, so evaluating the curve means solving for <c>t</c> given
/// <c>x</c> and then reading <c>y</c>. We use Newton-Raphson (fast, converges in a handful of
/// iterations for well-behaved curves) and fall back to bisection when the derivative is too
/// flat for Newton to be reliable.
/// <para>
/// <c>x1</c> and <c>x2</c> are clamped to [0,1] as CSS requires — a non-monotonic x would make
/// the curve a function of time with multiple answers. <c>y1</c> and <c>y2</c> are deliberately
/// unclamped so overshoot curves (anticipation, back-out) work.
/// </para>
/// </remarks>
public sealed class CubicBezierEasing
{
    const int NewtonIterations = 8;
    const double NewtonMinSlope = 1e-3;
    const double SubdivisionEpsilon = 1e-7;
    const int MaxBisectionIterations = 32;

    readonly double x1, y1, x2, y2;
    readonly bool isLinear;

    /// <summary>Creates a cubic-bézier timing function.</summary>
    /// <param name="x1">First control point's time component, clamped to [0,1].</param>
    /// <param name="y1">First control point's progress component. Unclamped, so overshoot is allowed.</param>
    /// <param name="x2">Second control point's time component, clamped to [0,1].</param>
    /// <param name="y2">Second control point's progress component. Unclamped, so overshoot is allowed.</param>
    public CubicBezierEasing(double x1, double y1, double x2, double y2)
    {
        this.x1 = Math.Clamp(x1, 0d, 1d);
        this.y1 = y1;
        this.x2 = Math.Clamp(x2, 0d, 1d);
        this.y2 = y2;

        // The identity curve short-circuits: both control points sit on the diagonal.
        this.isLinear = this.x1 == this.y1 && this.x2 == this.y2;
    }

    /// <summary>Evaluates the curve. Safe to use directly as an <see cref="EasingFunction"/>.</summary>
    public double Ease(double t)
    {
        // Outside the unit interval CSS extends the curve linearly using the endpoint slopes,
        // which keeps springs and chained segments from snapping.
        if (t <= 0d)
        {
            var startGradient = x1 > 0d ? y1 / x1
                : (y1 == 0d && x2 > 0d) ? y2 / x2
                : 0d;
            return startGradient * t;
        }

        if (t >= 1d)
        {
            var endGradient = x2 < 1d ? (y2 - 1d) / (x2 - 1d)
                : (y2 == 1d && x1 < 1d) ? (y1 - 1d) / (x1 - 1d)
                : 0d;
            return 1d + endGradient * (t - 1d);
        }

        return isLinear ? t : SampleY(SolveForT(t));
    }

    // Bézier basis with P0=(0,0) and P3=(1,1), expanded to polynomial form so we can
    // evaluate and differentiate cheaply.
    static double Sample(double a, double b, double t)
    {
        var c0 = 3d * a;
        var c1 = 3d * (b - a) - c0;
        var c2 = 1d - c0 - c1;
        return ((c2 * t + c1) * t + c0) * t;
    }

    static double SampleDerivative(double a, double b, double t)
    {
        var c0 = 3d * a;
        var c1 = 3d * (b - a) - c0;
        var c2 = 1d - c0 - c1;
        return (3d * c2 * t + 2d * c1) * t + c0;
    }

    double SampleX(double t) => Sample(x1, x2, t);

    double SampleY(double t) => Sample(y1, y2, t);

    double SolveForT(double x)
    {
        var t = x; // x is a good initial guess for curves near the diagonal.

        for (var i = 0; i < NewtonIterations; i++)
        {
            var slope = SampleDerivative(x1, x2, t);
            if (Math.Abs(slope) < NewtonMinSlope)
                break; // Nearly flat — Newton would overshoot wildly. Hand off to bisection.

            var error = SampleX(t) - x;
            if (Math.Abs(error) < SubdivisionEpsilon)
                return t;

            t -= error / slope;
        }

        // Bisection fallback: guaranteed to converge because x is monotonic on [0,1].
        double lo = 0d, hi = 1d;
        t = x;

        for (var i = 0; i < MaxBisectionIterations; i++)
        {
            var error = SampleX(t) - x;
            if (Math.Abs(error) < SubdivisionEpsilon)
                break;

            if (error > 0d)
                hi = t;
            else
                lo = t;

            t = (lo + hi) / 2d;
        }

        return t;
    }

    /// <summary>Allows a curve to be passed anywhere an <see cref="EasingFunction"/> is expected.</summary>
    public static implicit operator EasingFunction(CubicBezierEasing easing) => easing.Ease;
}
