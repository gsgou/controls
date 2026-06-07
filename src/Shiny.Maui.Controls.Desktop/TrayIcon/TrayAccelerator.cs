namespace Shiny.Maui.Controls.Desktop.TrayIcon;

[Flags]
public enum TrayAcceleratorModifiers
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    /// <summary>Cmd on macOS, Win key on Windows, Super on Linux.</summary>
    Meta = 8
}

/// <summary>
/// A parsed accelerator string. Recognised modifier tokens (case-insensitive, '+' separated):
/// Ctrl/Control, Alt/Option/Opt, Shift, Cmd/Command/Meta/Win/Super. The last token is the key
/// (single letter, digit, "F1"..."F24", or named key like "Esc", "Tab", "Space", "Enter").
/// </summary>
public sealed record class TrayAccelerator(TrayAcceleratorModifiers Modifiers, string Key)
{
    public static TrayAccelerator? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        var mods = TrayAcceleratorModifiers.None;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            mods |= parts[i].ToLowerInvariant() switch
            {
                "ctrl" or "control" => TrayAcceleratorModifiers.Control,
                "alt" or "option" or "opt" => TrayAcceleratorModifiers.Alt,
                "shift" => TrayAcceleratorModifiers.Shift,
                "cmd" or "command" or "meta" or "win" or "super" => TrayAcceleratorModifiers.Meta,
                _ => TrayAcceleratorModifiers.None
            };
        }

        var key = parts[^1];
        return string.IsNullOrEmpty(key) ? null : new TrayAccelerator(mods, key);
    }
}
