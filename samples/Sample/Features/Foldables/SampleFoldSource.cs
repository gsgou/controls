using Shiny.Maui.Controls;

namespace Sample.Features.Foldables;

/// <summary>
/// The sample's posture source: the real one (the Foldables package's provider, on Android and iOS)
/// with a debug override on top, so the fold-aware behaviour can be seen on a phone, a tablet or a
/// simulator that does not fold. Registered with <c>UseFoldPosture</c>, which always wins over the
/// package's own registration.
/// </summary>
public sealed class SampleFoldSource : IFoldPostureSource
{
    IFoldPostureSource? inner;
    FoldInfo? simulated;

    SampleFoldSource() { }

    public static SampleFoldSource Instance { get; } = new();

    /// <summary>Wrap the platform source. Null on heads that have none (macOS AppKit, GTK).</summary>
    public SampleFoldSource Wrap(IFoldPostureSource? platform)
    {
        if (this.inner is not null)
            this.inner.Changed -= this.OnInnerChanged;

        this.inner = platform;
        if (platform is not null)
            platform.Changed += this.OnInnerChanged;

        return this;
    }

    /// <summary>What the device itself reports, ignoring the simulation.</summary>
    public FoldInfo Device => this.inner?.Current ?? FoldInfo.None;

    /// <summary>Whether a platform source is wrapped at all.</summary>
    public bool HasDeviceSource => this.inner is not null;

    /// <summary>The simulated posture, or null to pass the device's through.</summary>
    public FoldInfo? Simulated
    {
        get => this.simulated;
        set
        {
            this.simulated = value;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public FoldInfo Current => this.simulated ?? this.Device;

    public event EventHandler? Changed;

    void OnInnerChanged(object? sender, EventArgs e)
    {
        if (this.simulated is null)
            this.Changed?.Invoke(this, EventArgs.Empty);
        else
            this.DeviceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the device posture moves while a simulation is masking it.</summary>
    public event EventHandler? DeviceChanged;

    /// <summary>A vertical fold down the middle of a window <paramref name="size"/> big.</summary>
    public static FoldInfo Book(Size size, double hinge) => new(
        FoldPostureState.HalfOpen,
        FoldPostureOrientation.Vertical,
        new Rect((size.Width - hinge) / 2, 0, hinge, size.Height));

    /// <summary>A horizontal fold across the middle of a window <paramref name="size"/> big.</summary>
    public static FoldInfo Tabletop(Size size, double hinge) => new(
        FoldPostureState.HalfOpen,
        FoldPostureOrientation.Horizontal,
        new Rect(0, (size.Height - hinge) / 2, size.Width, hinge));
}
