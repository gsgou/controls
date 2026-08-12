namespace Shiny.Maui.Controls.Media;

/// <summary>
/// The hook every <see cref="MediaElement"/> uses to create its player.
/// </summary>
/// <remarks>
/// <para>
/// A static hook rather than DI because a <see cref="MediaElement"/> declared in XAML has no service
/// provider of its own, and because the GTK4 head has to replace the factory from a <i>different</i>
/// assembly (there is no Linux target framework, so the Linux backend can't live in this package).
/// </para>
/// <para>
/// <c>UseShinyMediaElement()</c> sets this to the platform backend, and <c>UseShinyMediaElementGtk()</c>
/// in the Linux package sets it to the GTK one. Assign it yourself to substitute a fake in tests or to
/// plug in a different player.
/// </para>
/// </remarks>
public static class MediaPlayerBackends
{
    /// <summary>
    /// Creates the backend for each <see cref="MediaElement"/>. <c>null</c> — the default until one of the
    /// <c>UseShinyMediaElement…</c> builder extensions runs — leaves the control inert rather than throwing,
    /// so a page still lays out on an unsupported host.
    /// </summary>
    public static Func<IMediaPlayerBackend>? Factory { get; set; }

    /// <summary>Whether a backend is registered for this platform.</summary>
    public static bool IsSupported => Factory is not null;

    internal static IMediaPlayerBackend? Create() => Factory?.Invoke();
}
