namespace Shiny.Controls.MotionIcons;

/// <summary>
/// A point in icon (viewBox) units.
/// </summary>
/// <remarks>
/// Declared locally rather than reusing <c>PointF</c> so this package stays free of
/// <c>Microsoft.Maui.Graphics</c> — see the note in the project file.
/// </remarks>
/// <param name="X">Horizontal offset from the viewBox origin.</param>
/// <param name="Y">Vertical offset from the viewBox origin.</param>
public readonly record struct MotionPoint(float X, float Y);
