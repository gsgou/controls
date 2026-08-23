namespace Shiny.Maui.Controls.Desktop.QuickEntry;

/// <summary>
/// No global hotkeys on MacCatalyst. A sandboxed Catalyst app has no supported route to a
/// system-wide key grab, so <see cref="Register"/> always declines rather than pretending.
/// </summary>
sealed class CatalystGlobalHotKeyService : IGlobalHotKeyService
{
    public bool IsSupported => false;

    public IDisposable? Register(string accelerator, Action pressed) => null;
}
