using Shiny.Controls.MotionIcons;

namespace Shiny.Blazor.Controls.MotionIcons;

/// <summary>What one part of an icon needs in the rendered SVG.</summary>
/// <param name="Translate">Wrap it in a group carrying a translate animation.</param>
/// <param name="Rotate">Wrap it in a group carrying a rotate animation.</param>
/// <param name="Scale">Wrap it in a group carrying a scale animation.</param>
/// <param name="Trim">Set up the dash pattern that makes a draw-on possible.</param>
/// <param name="Animated">Anything at all animates on the path element itself.</param>
sealed record MotionPartPlan(bool Translate, bool Rotate, bool Scale, bool Trim, bool Animated);

/// <summary>
/// A motion spec compiled into everything the Blazor component needs: the stylesheet, and the shape
/// of the SVG that stylesheet expects to find.
/// </summary>
/// <remarks>
/// <para>Compiled once per unique icon-and-motion pair and cached, because the result depends on
/// neither the size, the colour, the duration nor the repeat count — those all ride in as CSS custom
/// properties. A page with thirty animated icons therefore compiles thirty times only if all thirty
/// are different icons.</para>
/// <para>Each animated transform channel gets its own nested group. It looks redundant next to the
/// MAUI renderer, which composes all three onto one layer, but CSS has exactly one
/// <c>transform</c> property per element and two animations cannot share it — nesting is what lets
/// a bell's body rotate while the whole icon also drifts, without either one having to know about
/// the other. Group order is translate, then rotate, then scale, matching the order the MAUI
/// canvas applies them so both hosts land on the same pixels.</para>
/// </remarks>
sealed class MotionPlan
{
    static readonly Dictionary<string, MotionPlan> Cache = [];
    static readonly Lock Gate = new();

    MotionPlan(
        string scope,
        string css,
        bool rootTranslate,
        bool rootRotate,
        bool rootScale,
        bool rootOpacity,
        IReadOnlyList<MotionPartPlan> parts)
    {
        Scope = scope;
        Css = css;
        RootTranslate = rootTranslate;
        RootRotate = rootRotate;
        RootScale = rootScale;
        RootOpacity = rootOpacity;
        Parts = parts;
    }

    /// <summary>The class name that scopes this plan's rules, also used as its keyframe prefix.</summary>
    public string Scope { get; }

    /// <summary>The stylesheet to drop into the SVG.</summary>
    public string Css { get; }

    /// <summary>The SVG body those rules animate.</summary>
    public string Markup { get; private set; } = string.Empty;

    /// <summary>Whether the icon as a whole is translated.</summary>
    public bool RootTranslate { get; }

    /// <summary>Whether the icon as a whole is rotated.</summary>
    public bool RootRotate { get; }

    /// <summary>Whether the icon as a whole is scaled.</summary>
    public bool RootScale { get; }

    /// <summary>Whether the icon as a whole fades.</summary>
    public bool RootOpacity { get; }

    /// <summary>Per-part requirements, index-aligned with the icon's parts.</summary>
    public IReadOnlyList<MotionPartPlan> Parts { get; }

    /// <summary>Compiles a plan, reusing a cached one where the same motion has been seen before.</summary>
    public static MotionPlan For(MotionIconDefinition icon, MotionSpec spec)
    {
        var key = MotionCss.Fingerprint(icon, spec);

        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached))
                return cached;

            var plan = Build(icon, spec, key);
            Cache[key] = plan;

            return plan;
        }
    }

    static MotionPlan Build(MotionIconDefinition icon, MotionSpec spec, string key)
    {
        var scope = "mi-" + key;
        var parts = new MotionPartPlan[icon.Parts.Count];

        for (var i = 0; i < icon.Parts.Count; i++)
        {
            var id = icon.Parts[i].Id;

            var translate = Has(spec, id, MotionChannel.TranslateX) || Has(spec, id, MotionChannel.TranslateY);
            var rotate = Has(spec, id, MotionChannel.Rotate);
            var scale = Has(spec, id, MotionChannel.Scale)
                || Has(spec, id, MotionChannel.ScaleX)
                || Has(spec, id, MotionChannel.ScaleY);
            var trim = Has(spec, id, MotionChannel.Trim);

            var animated = trim
                || Has(spec, id, MotionChannel.Opacity)
                || Has(spec, id, MotionChannel.StrokeWidth)
                || HasPaint(spec, id);

            parts[i] = new MotionPartPlan(translate, rotate, scale, trim, animated);
        }

        var rootTranslate = Has(spec, null, MotionChannel.TranslateX) || Has(spec, null, MotionChannel.TranslateY);
        var rootRotate = Has(spec, null, MotionChannel.Rotate);
        var rootScale = Has(spec, null, MotionChannel.Scale)
            || Has(spec, null, MotionChannel.ScaleX)
            || Has(spec, null, MotionChannel.ScaleY);
        var rootOpacity = Has(spec, null, MotionChannel.Opacity);

        var css = MotionCss.Build(icon, spec, scope);
        var plan = new MotionPlan(scope, css, rootTranslate, rootRotate, rootScale, rootOpacity, parts);

        plan.Markup = MotionSvg.Build(icon, plan);

        return plan;
    }

    static bool Has(MotionSpec spec, string? partId, MotionChannel channel)
    {
        foreach (var track in spec.Tracks)
        {
            if (track.Channel == channel && string.Equals(track.PartId, partId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static bool HasPaint(MotionSpec spec, string partId)
    {
        foreach (var track in spec.ColorTracks)
        {
            if (track.PartId is null || string.Equals(track.PartId, partId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
