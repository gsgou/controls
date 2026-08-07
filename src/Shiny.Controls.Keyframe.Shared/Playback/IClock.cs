namespace Shiny.Controls.Keyframe;

/// <summary>
/// A source of frame ticks. The platform layer wires this to the display link; tests and the
/// export pipeline substitute <see cref="ManualClock"/> to step time deterministically.
/// </summary>
public interface IClock
{
    /// <summary>Raised once per frame with the elapsed time since the previous tick.</summary>
    event Action<TimeSpan>? Tick;

    /// <summary>Whether the clock is currently producing ticks.</summary>
    bool IsRunning { get; }

    /// <summary>Begins producing ticks. Implementations should be safe to call when already running.</summary>
    void Start();

    /// <summary>Stops producing ticks.</summary>
    void Stop();
}

/// <summary>
/// A clock driven by explicit calls rather than a display link. Every frame is exact, which is what
/// makes offscreen export reproducible and makes timing tests free of wall-clock flake.
/// </summary>
public sealed class ManualClock : IClock
{
    /// <inheritdoc />
    public event Action<TimeSpan>? Tick;

    /// <summary>Total time advanced since construction.</summary>
    public TimeSpan Elapsed { get; private set; }

    /// <inheritdoc />
    public bool IsRunning { get; private set; } = true;

    /// <inheritdoc />
    public void Start() => IsRunning = true;

    /// <inheritdoc />
    public void Stop() => IsRunning = false;

    /// <summary>Advances by one delta and fires a tick.</summary>
    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "A clock cannot advance backwards. Seek the player instead.");

        Elapsed += delta;

        if (IsRunning)
            Tick?.Invoke(delta);
    }

    /// <summary>Advances in fixed steps, firing a tick for each — the frame pump used by export.</summary>
    /// <param name="total">Total time to advance.</param>
    /// <param name="step">Size of each frame.</param>
    public void AdvanceBy(TimeSpan total, TimeSpan step)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(step, TimeSpan.Zero);

        var remaining = total;
        while (remaining > TimeSpan.Zero)
        {
            var delta = remaining < step ? remaining : step;
            Advance(delta);
            remaining -= delta;
        }
    }
}
