namespace Shiny.Controls.MotionIcons;

/// <summary>A numeric keyframe.</summary>
/// <param name="Offset">Position within the animation, 0 to 1.</param>
/// <param name="Value">The value at that position.</param>
/// <param name="Ease">
/// Curve for the segment that <em>starts</em> at this key, matching CSS keyframe semantics — so the
/// easing on the last key never has any effect.
/// </param>
public readonly record struct MotionKey(double Offset, double Value, MotionEase Ease = MotionEase.Ease);

/// <summary>A colour keyframe.</summary>
/// <param name="Offset">Position within the animation, 0 to 1.</param>
/// <param name="Color">The colour at that position. Null means the host's current icon colour.</param>
/// <param name="Ease">Curve for the segment that starts at this key.</param>
public readonly record struct MotionColorKey(double Offset, string? Color, MotionEase Ease = MotionEase.Ease);

/// <summary>One property of one part, over time.</summary>
/// <param name="PartId">The part to drive. Null drives the icon as a whole.</param>
/// <param name="Channel">The property being driven.</param>
/// <param name="Keys">Keyframes in ascending offset order.</param>
public sealed record MotionTrack(string? PartId, MotionChannel Channel, IReadOnlyList<MotionKey> Keys);

/// <summary>One paint channel of one part, over time.</summary>
/// <param name="PartId">The part to drive. Null drives every part.</param>
/// <param name="Channel">Fill or stroke.</param>
/// <param name="Keys">Keyframes in ascending offset order.</param>
public sealed record MotionColorTrack(string? PartId, MotionPaintChannel Channel, IReadOnlyList<MotionColorKey> Keys);
