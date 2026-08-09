namespace Shiny.Maui.Controls;

/// <summary>
/// The one place raw/formatted/cursor is derived from a mask, so <see cref="TextEntry"/> and
/// <see cref="Cells.EntryCell"/> cannot drift apart on the fiddly bits — over-length input being
/// clipped to the mask's slot count, and where the caret has to land after the literals are
/// re-inserted.
/// </summary>
static class MaskedInput
{
    public readonly record struct Result(string Raw, string Formatted, int CursorPosition);

    /// <summary>
    /// Reduces whatever is currently in the field to the raw values the mask accepts, then rebuilds
    /// the display string from them.
    /// </summary>
    public static Result Apply(string? input, string mask)
    {
        var raw = TextEntryMaskHelper.StripMask(input, mask);

        var maxRaw = TextEntryMaskHelper.CalculateRawMaxLength(mask);
        if (raw.Length > maxRaw)
            raw = raw[..maxRaw];

        var formatted = TextEntryMaskHelper.ApplyMask(raw, mask);
        var cursor = TextEntryMaskHelper.CalculateCursorPosition(raw.Length, mask);

        return new Result(raw, formatted, Math.Min(cursor, formatted.Length));
    }
}
