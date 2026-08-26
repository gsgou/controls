namespace Shiny.Controls.Office.Spreadsheet;

/// <summary>
/// Excel's rules for what a sheet may be called, and the helpers that pick a name that obeys them.
/// </summary>
/// <remarks>
/// These are checked here rather than at save time because Excel does not repair a workbook whose
/// sheet names are illegal — it refuses to open it. A name is rejected while it is still a string in
/// a text box, which is the only moment at which the user can do anything about it.
/// </remarks>
public static class SheetNames
{
    /// <summary>Excel's hard limit. Longer names are rejected outright rather than truncated.</summary>
    public const int MaxLength = 31;

    /// <summary>
    /// Characters Excel reserves. The first four are range and path syntax; the brackets are how a
    /// workbook is named in an external reference.
    /// </summary>
    public static readonly char[] InvalidCharacters = [':', '\\', '/', '?', '*', '[', ']'];

    /// <summary>Reserved by Excel for the change-tracking sheet, whatever the casing.</summary>
    const string Reserved = "History";

    /// <summary>
    /// Checks a name against Excel's rules, ignoring uniqueness — which needs the workbook and is
    /// checked separately by <see cref="IsAvailable"/>.
    /// </summary>
    public static bool IsValid(string? name, out string? error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "A sheet needs a name.";
            return false;
        }

        if (name.Length > MaxLength)
        {
            error = $"A sheet name cannot be longer than {MaxLength} characters.";
            return false;
        }

        if (name.IndexOfAny(InvalidCharacters) >= 0)
        {
            error = $"A sheet name cannot contain {string.Join(' ', InvalidCharacters)}";
            return false;
        }

        // A leading or trailing apostrophe collides with the quoting a formula uses to wrap the name.
        if (name[0] == '\'' || name[^1] == '\'')
        {
            error = "A sheet name cannot start or end with an apostrophe.";
            return false;
        }

        if (string.Equals(name, Reserved, StringComparison.OrdinalIgnoreCase))
        {
            error = $"'{Reserved}' is reserved by Excel.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// True when <paramref name="name"/> is both legal and unused. <paramref name="except"/> is the
    /// name being replaced, so renaming a sheet to a different casing of itself is not a clash.
    /// </summary>
    public static bool IsAvailable(string? name, IEnumerable<string> existing, string? except, out string? error)
    {
        if (!IsValid(name, out error))
            return false;

        foreach (var taken in existing)
        {
            if (except is not null && string.Equals(taken, except, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(taken, name, StringComparison.OrdinalIgnoreCase))
            {
                error = $"There is already a sheet called '{taken}'.";
                return false;
            }
        }

        error = null;
        return true;
    }

    /// <summary>
    /// The next free <c>SheetN</c>, counting past whatever is already there rather than from the sheet
    /// count — deleting Sheet2 out of three sheets must not offer Sheet3 again.
    /// </summary>
    public static string NextDefault(IEnumerable<string> existing)
    {
        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        for (var n = taken.Count + 1; ; n++)
        {
            var candidate = $"Sheet{n}";
            if (taken.Add(candidate))
                return candidate;
        }
    }

    /// <summary>
    /// A free name based on <paramref name="desired"/>, in Excel's copy form: <c>Sales (2)</c>. The
    /// suffix is counted against the length limit, so a 31-character name loses its tail to make room.
    /// </summary>
    public static string MakeUnique(string desired, IEnumerable<string> existing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(desired);

        var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(desired) && desired.Length <= MaxLength)
            return desired;

        for (var n = 2; ; n++)
        {
            var suffix = $" ({n})";
            var stem = desired.Length + suffix.Length > MaxLength
                ? desired[..(MaxLength - suffix.Length)]
                : desired;

            var candidate = stem + suffix;
            if (!taken.Contains(candidate))
                return candidate;
        }
    }
}
