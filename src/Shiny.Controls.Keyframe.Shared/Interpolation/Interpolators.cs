using System.Numerics;

namespace Shiny.Controls.Keyframe;

/// <summary>Blends <see cref="double"/> values linearly.</summary>
public sealed class DoubleInterpolator : IInterpolator<double>
{
    /// <summary>The shared instance. Interpolators are stateless, so one is always enough.</summary>
    public static readonly DoubleInterpolator Instance = new();

    /// <inheritdoc />
    public double Interpolate(double from, double to, double progress) => from + (to - from) * progress;
}

/// <summary>Blends <see cref="float"/> values linearly.</summary>
public sealed class SingleInterpolator : IInterpolator<float>
{
    /// <summary>The shared instance.</summary>
    public static readonly SingleInterpolator Instance = new();

    /// <inheritdoc />
    public float Interpolate(float from, float to, double progress) => (float)(from + (to - from) * progress);
}

/// <summary>Blends <see cref="int"/> values linearly, rounding to nearest on output.</summary>
public sealed class Int32Interpolator : IInterpolator<int>
{
    /// <summary>The shared instance.</summary>
    public static readonly Int32Interpolator Instance = new();

    /// <inheritdoc />
    public int Interpolate(int from, int to, double progress)
        => (int)Math.Round(from + (to - from) * progress, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Holds the starting value until progress reaches 1, then snaps. This is the correct behaviour
/// for values that have no meaningful midpoint — booleans, enums, strings, sprite indices.
/// </summary>
/// <typeparam name="T">Any type; no blending is performed.</typeparam>
public sealed class StepInterpolator<T> : IInterpolator<T>
{
    /// <summary>The shared instance.</summary>
    public static readonly StepInterpolator<T> Instance = new();

    /// <inheritdoc />
    public T Interpolate(T from, T to, double progress) => progress >= 1d ? to : from;
}

/// <summary>
/// Adapts an arbitrary lambda into an interpolator, for one-off types not worth a dedicated class.
/// </summary>
/// <typeparam name="T">The value type being animated.</typeparam>
public sealed class DelegateInterpolator<T> : IInterpolator<T>
{
    readonly Func<T, T, double, T> interpolate;

    /// <summary>Wraps a blend function.</summary>
    public DelegateInterpolator(Func<T, T, double, T> interpolate)
    {
        ArgumentNullException.ThrowIfNull(interpolate);
        this.interpolate = interpolate;
    }

    /// <inheritdoc />
    public T Interpolate(T from, T to, double progress) => interpolate(from, to, progress);
}

/// <summary>
/// Blends any type that supports the generic-math arithmetic operators. Covers user-defined
/// vector and unit types without needing a bespoke interpolator, and stays AOT-safe because the
/// operators are resolved statically rather than by reflection.
/// </summary>
/// <typeparam name="T">A type implementing the required arithmetic operators.</typeparam>
public sealed class NumericInterpolator<T> : IInterpolator<T>
    where T : IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>, IMultiplyOperators<T, double, T>
{
    /// <summary>The shared instance.</summary>
    public static readonly NumericInterpolator<T> Instance = new();

    /// <inheritdoc />
    public T Interpolate(T from, T to, double progress) => from + (to - from) * progress;
}

/// <summary>
/// Blends angles along the shortest arc, so 350° → 10° travels 20° forward rather than 340° backward.
/// </summary>
public sealed class AngleInterpolator : IInterpolator<double>
{
    /// <summary>Shortest-arc interpolation over a 360° circle.</summary>
    public static readonly AngleInterpolator Degrees = new(360d);

    /// <summary>Shortest-arc interpolation over a 2π circle.</summary>
    public static readonly AngleInterpolator Radians = new(Math.Tau);

    readonly double fullTurn;

    /// <summary>Creates an interpolator for a circle of the given period.</summary>
    /// <param name="fullTurn">The value representing one complete revolution.</param>
    public AngleInterpolator(double fullTurn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fullTurn, 0d);
        this.fullTurn = fullTurn;
    }

    /// <inheritdoc />
    public double Interpolate(double from, double to, double progress)
    {
        var half = fullTurn / 2d;
        var delta = (to - from) % fullTurn;

        // Fold the difference into (-half, half] so we always take the short way round.
        if (delta > half)
            delta -= fullTurn;
        else if (delta < -half)
            delta += fullTurn;

        return from + delta * progress;
    }
}
