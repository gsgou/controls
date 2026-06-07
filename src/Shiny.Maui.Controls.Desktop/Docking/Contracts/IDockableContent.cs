namespace Shiny.Maui.Controls.Desktop.Docking;

/// <summary>
/// Implemented by panel content (the View hosted inside a dock tab).
/// </summary>
/// <remarks>
/// <para><b>Disposal contract:</b></para>
/// <list type="bullet">
///   <item><description>Closing a tab disposes the content if it implements <see cref="IDisposable"/>.</description></item>
///   <item><description>Tearing off to a floating window does NOT dispose — the same View instance is moved.</description></item>
///   <item><description>Moving between groups within the same window does NOT dispose — same instance.</description></item>
/// </list>
/// </remarks>
public interface IDockableContent
{
    string Title { get; }
    object? Icon { get; }
    bool CanClose { get; }
    bool CanFloat { get; }

    void OnActivated();
    void OnDeactivated();

    /// <summary>
    /// Return true to claim pointer-down events (prevents the dock system from starting a tab drag).
    /// Use for embedded editors that need their own caret-drag behavior.
    /// </summary>
    bool WantsPointerDown(double x, double y);
}
