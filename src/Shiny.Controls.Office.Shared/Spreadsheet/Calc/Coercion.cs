using System.Globalization;

namespace Shiny.Controls.Office.Spreadsheet.Calc;

/// <summary>
/// Excel's implicit conversion rules.
/// </summary>
/// <remarks>
/// These are the rules that make <c>="1"+1</c> equal 2 while <c>=SUM(A1:A2)</c> ignores text entirely.
/// The difference is deliberate and load-bearing: operators coerce, aggregate functions skip. Getting it
/// wrong produces plausible numbers that quietly disagree with Excel.
/// </remarks>
public static class Coercion
{
    /// <summary>Coerces to a number the way an arithmetic operator does — text is parsed, and fails loudly.</summary>
    public static bool TryToNumber(CellValue value, out double number, out CellError error)
    {
        error = default;
        switch (value.Kind)
        {
            case CellValueKind.Blank:
                number = 0;
                return true;

            case CellValueKind.Number:
            case CellValueKind.Boolean:
                number = value.AsNumber();
                return true;

            case CellValueKind.Text:
                var text = value.AsText().Trim();
                if (text.Length == 0)
                {
                    number = 0;
                    return true;
                }

                if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number))
                    return true;

                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                    return true;

                if (TryParsePercent(text, out number))
                    return true;

                if (ExcelDate.TryParse(text, out number))
                    return true;

                error = CellError.Value;
                return false;

            case CellValueKind.Error:
                error = value.AsError();
                number = 0;
                return false;

            default:
                number = 0;
                error = CellError.Value;
                return false;
        }
    }

    static bool TryParsePercent(string text, out double number)
    {
        number = 0;
        if (!text.EndsWith('%'))
            return false;

        var body = text[..^1].Trim();
        if (!double.TryParse(body, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) &&
            !double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return false;

        number = value / 100d;
        return true;
    }

    public static string ToText(CellValue value) => value.Kind switch
    {
        CellValueKind.Blank => string.Empty,
        CellValueKind.Text => value.AsText(),
        CellValueKind.Boolean => value.AsBoolean() ? "TRUE" : "FALSE",
        CellValueKind.Error => CellValue.ErrorText(value.AsError()),
        _ => value.AsNumber().ToString("R", CultureInfo.InvariantCulture)
    };

    public static bool TryToBoolean(CellValue value, out bool result, out CellError error)
    {
        error = default;
        switch (value.Kind)
        {
            case CellValueKind.Blank:
                result = false;
                return true;

            case CellValueKind.Boolean:
                result = value.AsBoolean();
                return true;

            case CellValueKind.Number:
                result = value.AsNumber() != 0;
                return true;

            case CellValueKind.Text:
                var text = value.AsText();
                if (text.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }

                if (text.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }

                result = false;
                error = CellError.Value;
                return false;

            case CellValueKind.Error:
                result = false;
                error = value.AsError();
                return false;

            default:
                result = false;
                error = CellError.Value;
                return false;
        }
    }

    /// <summary>
    /// Orders two values the way Excel's comparison operators do: numbers below text, text below
    /// booleans, with text compared case-insensitively.
    /// </summary>
    public static int Compare(CellValue left, CellValue right)
    {
        var leftRank = Rank(left);
        var rightRank = Rank(right);
        if (leftRank != rightRank)
            return leftRank.CompareTo(rightRank);

        return leftRank switch
        {
            0 => NumberOf(left).CompareTo(NumberOf(right)),
            1 => string.Compare(left.AsText(), right.AsText(), StringComparison.OrdinalIgnoreCase),
            _ => left.AsBoolean().CompareTo(right.AsBoolean())
        };

        static int Rank(CellValue value) => value.Kind switch
        {
            CellValueKind.Boolean => 2,
            CellValueKind.Text => 1,
            _ => 0
        };

        static double NumberOf(CellValue value) => value.Kind switch
        {
            CellValueKind.Number => value.AsNumber(),
            _ => 0
        };
    }

    /// <summary>
    /// Blank compares equal to both zero and the empty string, which is why <c>=A1=0</c> is TRUE for an
    /// empty A1 and cannot be expressed through <see cref="Compare"/> alone.
    /// </summary>
    public static bool EqualityWithBlank(CellValue left, CellValue right, out bool areEqual)
    {
        areEqual = false;
        if (!left.IsBlank && !right.IsBlank)
            return false;

        if (left.IsBlank && right.IsBlank)
        {
            areEqual = true;
            return true;
        }

        var other = left.IsBlank ? right : left;
        areEqual = other.Kind switch
        {
            CellValueKind.Number => other.AsNumber() == 0,
            CellValueKind.Text => other.AsText().Length == 0,
            CellValueKind.Boolean => !other.AsBoolean(),
            _ => false
        };

        return true;
    }
}
