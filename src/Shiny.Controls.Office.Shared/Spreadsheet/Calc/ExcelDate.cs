using System.Globalization;

namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// Conversion between Excel serial numbers and dates.
/// </summary>
/// <remarks>
/// Excel's 1900 date system contains a deliberate bug: serial 60 is 29 February 1900, a day that never
/// existed, kept for compatibility with Lotus 1-2-3. Every serial from 61 onwards is therefore one day
/// ahead of a naive "days since 1899-12-31" calculation, and ignoring it puts every date before 1 March
/// 1900 — and every date difference spanning it — off by one.
/// </remarks>
public static class ExcelDate
{
    /// <summary>The serial Excel assigns to the day that does not exist.</summary>
    public const int PhantomLeapDaySerial = 60;

    static readonly DateTime Epoch = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);

    public static double FromDateTime(DateTime value)
    {
        var serial = (value - Epoch).TotalDays;

        // Dates before 1 March 1900 predate the phantom day, so they sit one lower.
        return serial < PhantomLeapDaySerial ? serial - 1 : serial;
    }

    public static bool TryToDateTime(double serial, out DateTime value)
    {
        value = default;
        if (double.IsNaN(serial) || serial < 0 || serial > 2958465.9999999)
            return false;

        var adjusted = serial < PhantomLeapDaySerial ? serial + 1 : serial;

        // Serial 60 itself has no real date. Excel displays it as 29 Feb 1900; the closest honest answer
        // is 28 Feb, and callers that care should check for it explicitly.
        if (Math.Floor(serial) == PhantomLeapDaySerial)
            adjusted = PhantomLeapDaySerial;

        try
        {
            value = Epoch.AddDays(adjusted);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static bool TryParse(string text, out double serial)
    {
        serial = 0;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value) ||
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            serial = FromDateTime(value);
            return true;
        }

        if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out var time) ||
            TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time))
        {
            serial = time.TotalDays;
            return true;
        }

        return false;
    }

    /// <summary>Builds a serial from parts, rolling out-of-range months and days the way DATE() does.</summary>
    public static double FromParts(int year, int month, int day)
    {
        // Excel treats a two-digit year as 1900-based.
        if (year is >= 0 and < 1900)
            year += 1900;

        var value = new DateTime(year, 1, 1).AddMonths(month - 1).AddDays(day - 1);
        return FromDateTime(value);
    }
}
