namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Screen-coordinate rectangle for floating window bounds. Kept separate from
/// <c>Microsoft.Maui.Graphics.Rect</c> so the persisted schema is a pure POCO
/// with no MAUI graphics dependency.
/// </summary>
public sealed record DockRect(double X, double Y, double Width, double Height);
