using System.Globalization;
using System.Text;
using Shiny.Controls.MotionIcons;

namespace Shiny.Blazor.Controls.MotionIcons;

/// <summary>
/// Compiles a motion spec into a stylesheet.
/// </summary>
/// <remarks>
/// <para>The browser runs the animation, not Blazor. Nothing about a motion icon needs a render
/// loop on the web — once the keyframes are declared, compositing them is the browser's job, off
/// the main thread, at whatever refresh rate the display runs at, and it keeps going while
/// WebAssembly is busy. A C# ticker driving re-renders would be slower, jankier and would stop dead
/// the moment the app did some work.</para>
/// <para>Duration, repeat count, colour, size and stroke width are all deliberately left out of the
/// generated CSS and passed as custom properties instead, so two icons that differ only in how fast
/// they run share one compiled stylesheet.</para>
/// </remarks>
static class MotionCss
{
    // How finely a segment is resampled when its channels are not keyed in lockstep and so cannot
    // be expressed as a single CSS keyframe with a single timing function.
    const int ResampleSteps = 8;

    // Points used to describe a curve CSS has no keyword for. Enough that the piecewise-linear
    // approximation of a bounce or an elastic settle is indistinguishable from the real thing.
    const int LinearSamples = 24;

    /// <summary>A stable key for an icon-and-motion pair, used to name and cache its stylesheet.</summary>
    /// <remarks>
    /// The spec's <em>duration</em> is deliberately excluded: it rides in as a custom property, so
    /// the same motion at two different speeds is one stylesheet, not two.
    /// </remarks>
    public static string Fingerprint(MotionIconDefinition icon, MotionSpec spec)
    {
        var builder = new StringBuilder(icon.Name).Append('|').Append(icon.ViewBox);

        foreach (var part in icon.Parts)
        {
            builder.Append('|').Append(part.Id)
                .Append(';').Append(part.StrokeScale)
                .Append(';').Append(part.Origin?.X).Append(',').Append(part.Origin?.Y);
        }

        if (spec.RootOrigin is { } rootOrigin)
            builder.Append("|ro:").Append(rootOrigin.X).Append(',').Append(rootOrigin.Y);

        foreach (var track in spec.Tracks)
        {
            builder.Append('|').Append(track.PartId).Append(':').Append(track.Channel);

            foreach (var key in track.Keys)
                builder.Append(';').Append(key.Offset).Append(',').Append(key.Value).Append(',').Append((int)key.Ease);
        }

        foreach (var track in spec.ColorTracks)
        {
            builder.Append('|').Append(track.PartId).Append(':').Append(track.Channel);

            foreach (var key in track.Keys)
                builder.Append(';').Append(key.Offset).Append(',').Append(key.Color).Append(',').Append((int)key.Ease);
        }

        return Hash(builder.ToString());
    }

    /// <summary>Generates the stylesheet for an icon's motion.</summary>
    /// <param name="icon">The artwork.</param>
    /// <param name="spec">The motion.</param>
    /// <param name="scope">The class name scoping the rules, also the keyframe-name prefix.</param>
    public static string Build(MotionIconDefinition icon, MotionSpec spec, string scope)
    {
        var elements = Plan(icon, spec);
        var css = new StringBuilder();

        css.Append('.').Append(scope).Append(" .mi-g{transform-box:view-box}");

        css.Append('.').Append(scope).Append(".shiny-motion-icon--playing .mi-a{")
            .Append("animation-duration:var(--shiny-mi-duration);")
            .Append("animation-timing-function:linear;")
            .Append("animation-iteration-count:var(--shiny-mi-iterations);")
            .Append("animation-fill-mode:none}");

        AppendOrigins(css, icon, spec, scope);

        foreach (var element in elements)
        {
            css.Append("@keyframes ").Append(scope).Append('-').Append(element.Key).Append('{');
            AppendKeyframes(css, element);
            css.Append('}');

            css.Append('.').Append(scope).Append(".shiny-motion-icon--playing .mi-").Append(element.Key)
                .Append("{animation-name:").Append(scope).Append('-').Append(element.Key).Append('}');
        }

        return css.ToString();
    }

    static void AppendOrigins(StringBuilder css, MotionIconDefinition icon, MotionSpec spec, string scope)
    {
        // Rotation and scale need a pivot; translation does not. transform-box: view-box puts these
        // coordinates in the same space the artwork was authored in, which is what makes them mean
        // the same thing as the origins the MAUI renderer resolves against the layer.
        var rootOrigin = spec.RootOrigin ?? icon.Center;

        css.Append('.').Append(scope).Append(" .mi-rr,.").Append(scope).Append(" .mi-rs{transform-origin:")
            .Append(Num(rootOrigin.X)).Append("px ").Append(Num(rootOrigin.Y)).Append("px}");

        for (var i = 0; i < icon.Parts.Count; i++)
        {
            var origin = icon.OriginOf(icon.Parts[i]);

            css.Append('.').Append(scope).Append(" .mi-r").Append(i)
                .Append(",.").Append(scope).Append(" .mi-s").Append(i)
                .Append("{transform-origin:")
                .Append(Num(origin.X)).Append("px ").Append(Num(origin.Y)).Append("px}");
        }
    }

    static List<Element> Plan(MotionIconDefinition icon, MotionSpec spec)
    {
        var elements = new List<Element>();

        AddTransformElements(elements, spec, null, root: true, suffix: string.Empty);

        var rootOpacity = Track(spec, null, MotionChannel.Opacity);

        if (rootOpacity is not null)
            elements.Add(new Element("ro", [Numeric("opacity", rootOpacity, static v => Num(v))]));

        for (var i = 0; i < icon.Parts.Count; i++)
        {
            var part = icon.Parts[i];

            AddTransformElements(elements, spec, part.Id, root: false, suffix: i.ToString(CultureInfo.InvariantCulture));

            var properties = new List<Property>();
            var opacity = Track(spec, part.Id, MotionChannel.Opacity);

            if (opacity is not null)
                properties.Add(Numeric("opacity", opacity, static v => Num(v)));

            if (Track(spec, part.Id, MotionChannel.StrokeWidth) is { } strokeWidth)
            {
                var scale = part.StrokeScale;

                properties.Add(Numeric("stroke-width", strokeWidth,
                    v => $"calc(var(--shiny-mi-stroke) * {Num(v * scale)})"));
            }

            // pathLength="1" on the element normalises the geometry, so a dash offset of 1 hides the
            // stroke entirely and 0 reveals all of it, whatever the path actually measures.
            if (Track(spec, part.Id, MotionChannel.Trim) is { } trim)
                properties.Add(Numeric("stroke-dashoffset", trim, static v => Num(1d - v)));

            foreach (var track in ColorTracks(spec, part.Id))
            {
                var property = track.Channel is MotionPaintChannel.Fill ? "fill" : "stroke";
                properties.Add(new Property(property, [], track, null));
            }

            if (properties.Count > 0)
                elements.Add(new Element("p" + i.ToString(CultureInfo.InvariantCulture), properties));
        }

        return elements;
    }

    static void AddTransformElements(
        List<Element> elements, MotionSpec spec, string? partId, bool root, string suffix)
    {
        // The whole-icon groups are named rt/rr/rs and a part's are t0/r0/s0. Both generators have
        // to agree on these to the letter — a mismatch does not fail anywhere, it just silently
        // produces keyframes that no element in the SVG is ever matched by.
        var translateKey = root ? "rt" : "t" + suffix;
        var rotateKey = root ? "rr" : "r" + suffix;
        var scaleKey = root ? "rs" : "s" + suffix;

        var x = Track(spec, partId, MotionChannel.TranslateX);
        var y = Track(spec, partId, MotionChannel.TranslateY);

        if (x is not null || y is not null)
        {
            var sources = new List<MotionTrack>();

            if (x is not null)
                sources.Add(x);

            if (y is not null)
                sources.Add(y);

            elements.Add(new Element(translateKey, [
                new Property(
                    "transform",
                    sources,
                    null,
                    o => $"translate({Num(Sample(x, o, 0d))}px,{Num(Sample(y, o, 0d))}px)")
            ]));
        }

        if (Track(spec, partId, MotionChannel.Rotate) is { } rotate)
        {
            elements.Add(new Element(rotateKey, [
                Numeric("transform", rotate, static v => $"rotate({Num(v)}deg)")
            ]));
        }

        var uniform = Track(spec, partId, MotionChannel.Scale);
        var scaleX = Track(spec, partId, MotionChannel.ScaleX) ?? uniform;
        var scaleY = Track(spec, partId, MotionChannel.ScaleY) ?? uniform;

        if (scaleX is null && scaleY is null)
            return;

        var scaleSources = new List<MotionTrack>();

        if (scaleX is not null)
            scaleSources.Add(scaleX);

        if (scaleY is not null && !ReferenceEquals(scaleY, scaleX))
            scaleSources.Add(scaleY);

        elements.Add(new Element(scaleKey, [
            new Property(
                "transform",
                scaleSources,
                null,
                o => $"scale({Num(Sample(scaleX, o, 1d))},{Num(Sample(scaleY, o, 1d))})")
        ]));
    }

    static void AppendKeyframes(StringBuilder css, Element element)
    {
        var offsets = Offsets(element);

        for (var i = 0; i < offsets.Count; i++)
        {
            var offset = offsets[i];

            if (i == offsets.Count - 1)
            {
                AppendStop(css, element, offset, null);
                break;
            }

            var ease = ExactEase(element, offset);

            if (ease is not null)
            {
                AppendStop(css, element, offset, ease);
                continue;
            }

            // The channels sharing this element are not keyed in step with one another, so no single
            // timing function describes the segment. Resampling it finely and stepping linearly
            // between the samples reproduces the real curves to well under a pixel.
            var next = offsets[i + 1];

            for (var step = 0; step < ResampleSteps; step++)
                AppendStop(css, element, offset + (next - offset) * step / ResampleSteps, MotionEase.Linear);
        }
    }

    static void AppendStop(StringBuilder css, Element element, double offset, MotionEase? ease)
    {
        css.Append(Num(offset * 100d)).Append("%{");

        foreach (var property in element.Properties)
            css.Append(property.Name).Append(':').Append(property.Render(offset)).Append(';');

        AppendTiming(css, ease);
        css.Append('}');
    }

    static void AppendTiming(StringBuilder css, MotionEase? ease)
    {
        // linear is the animation-level default set on .mi-a, so saying so again is just bytes.
        if (ease is null or MotionEase.Linear)
            return;

        if (MotionEasings.CssKeyword(ease.Value) is { } keyword)
        {
            css.Append("animation-timing-function:").Append(keyword).Append(';');
            return;
        }

        // No CSS keyword means the curve has to be described point by point. The keyword before it
        // is a fallback for browsers too old to parse linear() — they drop the second declaration
        // and keep something in the right family rather than falling back to a flat linear.
        css.Append("animation-timing-function:").Append(Fallback(ease.Value)).Append(';');
        css.Append("animation-timing-function:linear(");

        for (var i = 0; i <= LinearSamples; i++)
        {
            if (i > 0)
                css.Append(',');

            css.Append(Num(MotionEasings.Evaluate(ease.Value, (double)i / LinearSamples)));
        }

        css.Append(");");
    }

    static string Fallback(MotionEase ease) => ease switch
    {
        MotionEase.QuadIn or MotionEase.CubicIn or MotionEase.SinIn
            or MotionEase.ExpoIn or MotionEase.BackIn => "ease-in",
        MotionEase.QuadInOut or MotionEase.CubicInOut or MotionEase.SinInOut
            or MotionEase.BackInOut => "ease-in-out",
        _ => "ease-out"
    };

    static List<double> Offsets(Element element)
    {
        var offsets = new List<double>();

        void Add(double offset)
        {
            foreach (var existing in offsets)
            {
                if (Math.Abs(existing - offset) < MotionSampler.Epsilon)
                    return;
            }

            offsets.Add(offset);
        }

        foreach (var property in element.Properties)
        {
            foreach (var track in property.Sources)
            {
                foreach (var key in track.Keys)
                    Add(key.Offset);
            }

            if (property.ColorSource is null)
                continue;

            foreach (var key in property.ColorSource.Keys)
                Add(key.Offset);
        }

        Add(0d);
        Add(1d);
        offsets.Sort();

        return offsets;
    }

    /// <summary>
    /// The timing function for the segment starting here, or null when this element's channels
    /// cannot agree on one.
    /// </summary>
    static MotionEase? ExactEase(Element element, double offset)
    {
        MotionEase? agreed = null;

        foreach (var property in element.Properties)
        {
            foreach (var track in property.Sources)
            {
                var ease = MotionSampler.EaseAt(track.Keys, offset);

                // Mid-segment for this channel: a keyframe here would restart its curve.
                if (ease is null)
                    return null;

                if (agreed is not null && agreed != ease)
                    return null;

                agreed = ease;
            }

            if (property.ColorSource is null)
                continue;

            var colorEase = ColorEaseAt(property.ColorSource.Keys, offset);

            if (colorEase is null)
                return null;

            if (agreed is not null && agreed != colorEase)
                return null;

            agreed = colorEase;
        }

        return agreed ?? MotionEase.Linear;
    }

    static MotionEase? ColorEaseAt(IReadOnlyList<MotionColorKey> keys, double offset)
    {
        foreach (var key in keys)
        {
            if (Math.Abs(key.Offset - offset) < MotionSampler.Epsilon)
                return key.Ease;
        }

        return null;
    }

    static Property Numeric(string name, MotionTrack track, Func<double, string> render)
        => new(name, [track], null, o => render(MotionSampler.ValueAt(track.Keys, o)));

    static double Sample(MotionTrack? track, double offset, double fallback)
        => track is null ? fallback : MotionSampler.ValueAt(track.Keys, offset);

    static MotionTrack? Track(MotionSpec spec, string? partId, MotionChannel channel)
    {
        foreach (var track in spec.Tracks)
        {
            if (track.Channel == channel && string.Equals(track.PartId, partId, StringComparison.Ordinal))
                return track;
        }

        return null;
    }

    static IEnumerable<MotionColorTrack> ColorTracks(MotionSpec spec, string partId)
    {
        foreach (var track in spec.ColorTracks)
        {
            if (track.PartId is null || string.Equals(track.PartId, partId, StringComparison.Ordinal))
                yield return track;
        }
    }

    internal static string Color(MotionColorTrack track, double offset)
    {
        var (from, to, progress) = MotionSampler.ColorAt(track.Keys, offset);

        // A null endpoint means the host's icon colour, which lives in a custom property and cannot
        // be interpolated towards. Snapping at the midpoint is the honest answer, and it matches
        // what the MAUI renderer does with the same pair.
        if (from is null || to is null)
            return (progress < 0.5d ? from : to) ?? "var(--shiny-mi-color)";

        return Blend(from, to, progress);
    }

    static string Blend(string from, string to, double progress)
    {
        var (ar, ag, ab, aa) = Parse(from);
        var (br, bg, bb, ba) = Parse(to);
        var t = progress;

        return string.Create(CultureInfo.InvariantCulture,
            $"rgba({(int)Math.Round(ar + (br - ar) * t)},{(int)Math.Round(ag + (bg - ag) * t)},{(int)Math.Round(ab + (bb - ab) * t)},{Num(aa + (ba - aa) * t)})");
    }

    static (double R, double G, double B, double A) Parse(string value)
    {
        var hex = value.AsSpan().TrimStart('#');

        // #rgb, #rrggbb and #aarrggbb, matching what MAUI's colour parser accepts.
        if (hex.Length == 3)
        {
            return (
                Convert.ToInt32(new string([hex[0], hex[0]]), 16),
                Convert.ToInt32(new string([hex[1], hex[1]]), 16),
                Convert.ToInt32(new string([hex[2], hex[2]]), 16),
                1d);
        }

        if (hex.Length == 8)
        {
            return (
                Convert.ToInt32(new string(hex[2..4]), 16),
                Convert.ToInt32(new string(hex[4..6]), 16),
                Convert.ToInt32(new string(hex[6..8]), 16),
                Convert.ToInt32(new string(hex[0..2]), 16) / 255d);
        }

        if (hex.Length != 6)
            return (0d, 0d, 0d, 1d);

        return (
            Convert.ToInt32(new string(hex[0..2]), 16),
            Convert.ToInt32(new string(hex[2..4]), 16),
            Convert.ToInt32(new string(hex[4..6]), 16),
            1d);
    }

    internal static string Num(double value)
    {
        // Adding zero folds negative zero away. It is valid CSS, but "-0" turning up in the middle
        // of a linear() curve reads like a bug every time anyone opens devtools.
        var rounded = Math.Round(value, 4) + 0d;

        return rounded.ToString("0.####", CultureInfo.InvariantCulture);
    }

    static string Hash(string value)
    {
        // FNV-1a. Not a security boundary — this only has to be stable across runs and unlikely to
        // collide across the handful of icons on a page, and it must not drag in a hash algorithm
        // that trimming would have to keep alive.
        var hash = 14695981039346656037UL;

        foreach (var c in value)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash.ToString("x", CultureInfo.InvariantCulture);
    }

    sealed record Element(string Key, IReadOnlyList<Property> Properties);

    sealed record Property(
        string Name,
        IReadOnlyList<MotionTrack> Sources,
        MotionColorTrack? ColorSource,
        Func<double, string>? Renderer)
    {
        public string Render(double offset)
            => Renderer is not null ? Renderer(offset) : MotionCss.Color(ColorSource!, offset);
    }
}
