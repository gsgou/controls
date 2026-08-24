namespace Shiny.Controls.Office.Packaging;

public enum UnsupportedSeverity
{
    /// <summary>The feature is preserved on save but is not shown to the user.</summary>
    NotRendered,

    /// <summary>The feature is preserved on save, but an edit in this area may not behave as expected.</summary>
    NotEditable,

    /// <summary>The feature cannot be preserved. Saving will lose it.</summary>
    Lossy
}

public sealed record UnsupportedFeature(string Part, string Feature, UnsupportedSeverity Severity, string? Detail = null);

/// <summary>
/// Collects everything an opened document contains that the editor does not model.
/// </summary>
/// <remarks>
/// A document that hits something unmodelled must say so. The failure mode this exists to prevent is
/// the quiet one: a file that opens, looks broadly right, and loses a feature the user never knew was
/// there. Anything reported as <see cref="UnsupportedSeverity.Lossy"/> should block saving over the
/// original without an explicit confirmation.
/// </remarks>
public interface IUnsupportedFeatureSink
{
    void Report(UnsupportedFeature feature);
}

public sealed class UnsupportedFeatureCollector : IUnsupportedFeatureSink
{
    readonly List<UnsupportedFeature> features = new();

    public IReadOnlyList<UnsupportedFeature> Features => this.features;

    public bool HasLossy => this.features.Any(x => x.Severity == UnsupportedSeverity.Lossy);

    public void Report(UnsupportedFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (!this.features.Contains(feature))
            this.features.Add(feature);
    }

    public void Clear() => this.features.Clear();
}

/// <summary>Discards reports. Used where the caller genuinely does not care, such as in fixture setup.</summary>
public sealed class NullUnsupportedFeatureSink : IUnsupportedFeatureSink
{
    public static readonly NullUnsupportedFeatureSink Instance = new();
    NullUnsupportedFeatureSink() { }
    public void Report(UnsupportedFeature feature) { }
}
