namespace Shiny.Maui.Controls.Office;

/// <summary>
/// The two galleries the spreadsheet toolbar's split buttons open: number formats and auto functions.
/// </summary>
/// <remarks>
/// Action sheets, for the same reason <see cref="OfficeMenus"/> uses them — MAUI has no popover in the
/// box, and an action sheet is the gesture a phone user expects from a toolbar that runs on phones.
/// The entries are the same ones the Blazor toolbar lists, because an aggregate offered on one host
/// and not the other would be a difference with nothing behind it.
/// </remarks>
static class SpreadsheetMenus
{
    /// <summary>The presets on offer, in the order both toolbars show them.</summary>
    public static IReadOnlyList<NumberFormatPreset> Formats { get; } =
    [
        NumberFormatPreset.General,
        NumberFormatPreset.Number,
        NumberFormatPreset.Currency,
        NumberFormatPreset.Percent,
        NumberFormatPreset.Scientific,
        NumberFormatPreset.ShortDate,
        NumberFormatPreset.Time,
        NumberFormatPreset.Text
    ];

    /// <summary>The aggregates on offer, Sum first because it is what the button itself applies.</summary>
    public static IReadOnlyList<AutoFunction> Functions { get; } =
    [
        AutoFunction.Sum,
        AutoFunction.Average,
        AutoFunction.Count,
        AutoFunction.Min,
        AutoFunction.Max
    ];

    /// <remarks>
    /// Unused since <see cref="SpreadsheetToolbar"/> became a ribbon - its number formats and auto
    /// functions are ribbon menus now, which show each preset's live sample beside its name where an
    /// action sheet had room only for the name. Kept because the sheet is still the right shape for a
    /// host driving the controller from its own chrome, and because <see cref="Formats"/> and
    /// <see cref="Functions"/> are the single definition of what those lists contain.
    /// </remarks>
    public static async Task<NumberFormatPreset?> PickNumberFormatAsync(Page? page)
    {
        if (page is null)
            return null;

        var names = Formats.Select(NumberFormats.DisplayName).ToArray();
        var picked = await page.DisplayActionSheet("Number format", "Cancel", null, names);

        if (string.IsNullOrEmpty(picked) || picked == "Cancel")
            return null;

        foreach (var preset in Formats)
        {
            if (NumberFormats.DisplayName(preset) == picked)
                return preset;
        }

        return null;
    }

    public static async Task<AutoFunction?> PickAutoFunctionAsync(Page? page)
    {
        if (page is null)
            return null;

        var names = Functions.Select(AutoFunctions.DisplayName).ToArray();
        var picked = await page.DisplayActionSheet("Auto function", "Cancel", null, names);

        if (string.IsNullOrEmpty(picked) || picked == "Cancel")
            return null;

        foreach (var function in Functions)
        {
            if (AutoFunctions.DisplayName(function) == picked)
                return function;
        }

        return null;
    }
}
