using Microsoft.Maui.Graphics;
using Shiny.Controls.Keyframe;
using Shiny.Controls.Keyframe.Graphics;
using Shiny.Controls.MotionIcons;

namespace Shiny.Maui.Controls.MotionIcons;

/// <summary>
/// Compiles an icon and its motion into a keyframe scene and timeline.
/// </summary>
/// <remarks>
/// <para>This is the MAUI half of the same job the Blazor package does with CSS: one host-neutral
/// <see cref="MotionSpec"/> in, something the host can actually run out. Here that is the keyframe
/// engine — a <see cref="KeyframeScene"/> layer tree and a <see cref="Timeline"/> — so motion icons
/// share the repo's one animation engine rather than carrying a second one of their own.</para>
/// <para>Every layer is given the icon's full viewBox as its size, which is what makes a part's
/// origin mean the same thing here as <c>transform-origin</c> does in the browser: an anchor is a
/// fraction of the layer's size, so <c>anchor = origin / viewBox</c> lands on the same point.</para>
/// </remarks>
static class MotionSceneBuilder
{
    // Cached one per curve rather than allocated per key. The motion-icon curves are defined by
    // MotionEasings, which a test pins to the keyframe engine's own Easings term for term, so
    // wrapping it is exact rather than merely similar.
    static readonly EasingFunction[] Easings = BuildEasings();

    /// <summary>Builds the layer tree for an icon.</summary>
    public static KeyframeScene BuildScene(
        MotionIconDefinition icon,
        MotionSpec? spec,
        Color color,
        Color? accent,
        float strokeWidth)
    {
        ArgumentNullException.ThrowIfNull(icon);

        var box = icon.ViewBox;
        var scene = new KeyframeScene(box, box)
        {
            Stretch = SceneStretch.Uniform,
            // Overshoot curves push a scaling icon briefly past its own box, and a few icons animate
            // in from outside it. Clipping would slice both.
            ClipToBounds = false
        };

        var rootOrigin = spec?.RootOrigin ?? icon.Center;
        scene.Root.Anchor = new PointF(rootOrigin.X / box, rootOrigin.Y / box);

        var builder = new PathBuilder();

        foreach (var part in icon.Parts)
        {
            var origin = icon.OriginOf(part);

            scene.Add(new PathLayer
            {
                Id = part.Id,
                Data = Parse(builder, part.Path),
                Size = new SizeF(box, box),
                Anchor = new PointF(origin.X / box, origin.Y / box),
                Fill = Resolve(part.Fill, color, accent),
                Stroke = Resolve(part.Stroke, color, accent),
                StrokeWidth = strokeWidth * part.StrokeScale,
                StrokeLineCap = part.LineCap switch
                {
                    MotionLineCap.Butt => LineCap.Butt,
                    MotionLineCap.Square => LineCap.Square,
                    _ => LineCap.Round
                },
                StrokeLineJoin = part.LineJoin switch
                {
                    MotionLineJoin.Miter => LineJoin.Miter,
                    MotionLineJoin.Bevel => LineJoin.Bevel,
                    _ => LineJoin.Round
                }
            });
        }

        return scene;
    }

    /// <summary>Builds the timeline that drives a scene's layers.</summary>
    /// <param name="icon">The artwork the scene was built from.</param>
    /// <param name="spec">The motion.</param>
    /// <param name="scene">The scene to drive. Tracks hold its layers weakly.</param>
    /// <param name="color">Resolves colour keys that mean "the host's icon colour".</param>
    /// <param name="strokeWidth">The host's stroke width, which stroke tracks scale.</param>
    /// <param name="iterations">Cycles to run, or <see cref="double.PositiveInfinity"/>.</param>
    public static Timeline? BuildTimeline(
        MotionIconDefinition icon,
        MotionSpec spec,
        KeyframeScene scene,
        Color color,
        float strokeWidth,
        double iterations)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(scene);

        if (spec.IsEmpty)
            return null;

        var builder = TimelineBuilder
            .Create(spec.Duration)
            // Matches the Blazor side's animation-fill-mode: none — when playback ends the icon is
            // the artwork as drawn, on both hosts.
            .Fill(FillMode.None);

        builder = double.IsPositiveInfinity(iterations)
            ? builder.RepeatForever()
            : builder.Repeat(Math.Max(1d, iterations));

        foreach (var track in spec.Tracks)
        {
            var layer = Target(scene, track.PartId);

            if (layer is null)
                continue;

            AddTrack(builder, track, layer, icon, strokeWidth);
        }

        foreach (var track in spec.ColorTracks)
        {
            foreach (var layer in ColorTargets(scene, icon, track.PartId))
            {
                if (track.Channel is MotionPaintChannel.Fill)
                    builder.AnimateFill(layer, k => ColorKeys(k, track, color));
                else
                    builder.AnimateStroke(layer, k => ColorKeys(k, track, color));
            }
        }

        var timeline = builder.Build();

        // Captured here, while the scene is still exactly as it was drawn. Baselines are what
        // FillMode.None reverts to when the timeline runs past its end, and what RestoreBaselines
        // puts back — so a timeline that has never captured them restores every track to a default
        // zero instead, which reads as the icon vanishing rather than coming to rest.
        timeline.CaptureBaselines();

        return timeline;
    }

    static void AddTrack(
        TimelineBuilder builder,
        MotionTrack track,
        SceneLayer layer,
        MotionIconDefinition icon,
        float strokeWidth)
    {
        switch (track.Channel)
        {
            case MotionChannel.Opacity:
                builder.AnimateOpacity(layer, k => Keys(k, track, static v => (float)v));
                break;

            case MotionChannel.TranslateX:
                builder.AnimatePositionX(layer, k => Keys(k, track, static v => (float)v));
                break;

            case MotionChannel.TranslateY:
                builder.AnimatePositionY(layer, k => Keys(k, track, static v => (float)v));
                break;

            // Spin, not Rotation: AnimateRotation takes the shortest arc between values, which
            // would quietly turn a spinner's 0-to-360 into no movement at all.
            case MotionChannel.Rotate:
                builder.AnimateSpin(layer, k => Keys(k, track, static v => v));
                break;

            case MotionChannel.Scale:
                builder.AnimateScaleX(layer, k => Keys(k, track, static v => (float)v));
                builder.AnimateScaleY(layer, k => Keys(k, track, static v => (float)v));
                break;

            case MotionChannel.ScaleX:
                builder.AnimateScaleX(layer, k => Keys(k, track, static v => (float)v));
                break;

            case MotionChannel.ScaleY:
                builder.AnimateScaleY(layer, k => Keys(k, track, static v => (float)v));
                break;

            case MotionChannel.StrokeWidth when layer is ShapeLayer shape:
            {
                // The channel is a multiplier on the host's width, so the part's own scale folds in
                // here rather than being applied twice at draw time.
                var scale = strokeWidth * (icon.FindPart(track.PartId ?? string.Empty)?.StrokeScale ?? 1f);
                builder.AnimateStrokeWidth(shape, k => Keys(k, track, v => (float)v * scale));
                break;
            }

            case MotionChannel.Trim when layer is PathLayer path:
                builder.AnimateTrimEnd(path, k => Keys(k, track, static v => (float)v));
                break;
        }
    }

    static SceneLayer? Target(KeyframeScene scene, string? partId)
        => partId is null ? scene.Root : scene.FindById(partId);

    static IEnumerable<ShapeLayer> ColorTargets(KeyframeScene scene, MotionIconDefinition icon, string? partId)
    {
        if (partId is not null)
        {
            if (scene.FindById(partId) is ShapeLayer single)
                yield return single;

            yield break;
        }

        foreach (var part in icon.Parts)
        {
            if (scene.FindById(part.Id) is ShapeLayer layer)
                yield return layer;
        }
    }

    static void Keys<T>(TrackBuilder<T> builder, MotionTrack track, Func<double, T> convert)
    {
        foreach (var key in track.Keys)
            builder.Key(key.Offset, convert(key.Value), Ease(key.Ease));
    }

    static void ColorKeys(TrackBuilder<Color> builder, MotionColorTrack track, Color fallback)
    {
        foreach (var key in track.Keys)
            builder.Key(key.Offset, key.Color is null ? fallback : Color.FromArgb(key.Color), Ease(key.Ease));
    }

    static EasingFunction Ease(MotionEase ease)
    {
        var index = (int)ease;
        return index >= 0 && index < Easings.Length ? Easings[index] : Shiny.Controls.Keyframe.Easings.Linear;
    }

    static EasingFunction[] BuildEasings()
    {
        var values = Enum.GetValues<MotionEase>();
        var easings = new EasingFunction[values.Length];

        foreach (var value in values)
        {
            var captured = value;
            easings[(int)value] = t => MotionEasings.Evaluate(captured, t);
        }

        return easings;
    }

    static Color? Resolve(IconPaint paint, Color color, Color? accent) => paint.Kind switch
    {
        IconPaintKind.Current => color,
        IconPaintKind.Accent => accent ?? color,
        IconPaintKind.Fixed => Color.FromArgb(paint.Value),
        _ => null
    };

    static PathF Parse(PathBuilder builder, string data)
    {
        // Artwork can arrive from a caller at runtime, so a malformed path must leave a gap in the
        // icon rather than take down whatever page it happens to be on.
        try
        {
            return builder.BuildPath(data);
        }
        catch (Exception)
        {
            return new PathF();
        }
    }
}
