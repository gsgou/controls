using System.Globalization;

namespace Shiny.Controls.Office.Spreadsheet.Calc;

static class TextFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Add("LEN", 1, 1, a => CalcValue.From((double)a.Text(0).Length));
        registry.Add("LOWER", 1, 1, a => CalcValue.From(a.Text(0).ToLowerInvariant()));
        registry.Add("UPPER", 1, 1, a => CalcValue.From(a.Text(0).ToUpperInvariant()));
        registry.Add("TRIM", 1, 1, a => CalcValue.From(Trim(a.Text(0))));

        registry.Add("PROPER", 1, 1, a =>
        {
            var text = a.Text(0);
            var builder = new System.Text.StringBuilder(text.Length);
            var startOfWord = true;

            foreach (var c in text)
            {
                builder.Append(startOfWord ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));

                // A letter or digit continues a word; anything else starts a new one.
                startOfWord = !char.IsLetterOrDigit(c);
            }

            return CalcValue.From(builder.ToString());
        });

        registry.Add("LEFT", 1, 2, a =>
        {
            var text = a.Text(0);
            var count = Math.Clamp(a.IntegerOrDefault(1, 1), 0, text.Length);
            return CalcValue.From(text[..count]);
        });

        registry.Add("RIGHT", 1, 2, a =>
        {
            var text = a.Text(0);
            var count = Math.Clamp(a.IntegerOrDefault(1, 1), 0, text.Length);
            return CalcValue.From(text[^count..]);
        });

        registry.Add("MID", 3, 3, a =>
        {
            var text = a.Text(0);
            var start = a.Integer(1);
            var length = a.Integer(2);

            if (start < 1 || length < 0)
                return CalcValue.Error(CellError.Value);

            if (start > text.Length)
                return CalcValue.From(string.Empty);

            var offset = start - 1;
            return CalcValue.From(text.Substring(offset, Math.Min(length, text.Length - offset)));
        });

        registry.Add("CONCAT", 1, CalcFunction.Unlimited, a =>
            CalcValue.From(string.Concat(a.AllValues().Select(Coercion.ToText))));

        registry.Add("CONCATENATE", 1, CalcFunction.Unlimited, a =>
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < a.Count; i++)
                builder.Append(a.Text(i));

            return CalcValue.From(builder.ToString());
        });

        registry.Add("TEXTJOIN", 3, CalcFunction.Unlimited, a =>
        {
            var separator = a.Text(0);
            var ignoreEmpty = a.Boolean(1);
            var parts = new List<string>();

            for (var i = 2; i < a.Count; i++)
            {
                foreach (var value in a.Value(i).Flatten())
                {
                    if (value.IsError)
                        return CalcValue.From(value);

                    if (ignoreEmpty && value.IsBlank)
                        continue;

                    parts.Add(Coercion.ToText(value));
                }
            }

            return CalcValue.From(string.Join(separator, parts));
        });

        registry.Add("REPT", 2, 2, a =>
        {
            var count = a.Integer(1);
            if (count < 0)
                return CalcValue.Error(CellError.Value);

            var text = a.Text(0);

            // Excel's cell limit; without this a typo turns into an out-of-memory instead of an error.
            if ((long)text.Length * count > 32767)
                return CalcValue.Error(CellError.Value);

            return CalcValue.From(string.Concat(Enumerable.Repeat(text, count)));
        });

        registry.Add("EXACT", 2, 2, a => CalcValue.From(string.Equals(a.Text(0), a.Text(1), StringComparison.Ordinal)));

        registry.Add("FIND", 2, 3, a => Search(a, StringComparison.Ordinal, wildcards: false));
        registry.Add("SEARCH", 2, 3, a => Search(a, StringComparison.OrdinalIgnoreCase, wildcards: true));

        registry.Add("SUBSTITUTE", 3, 4, a =>
        {
            var text = a.Text(0);
            var oldText = a.Text(1);
            var newText = a.Text(2);

            if (oldText.Length == 0)
                return CalcValue.From(text);

            if (a.IsMissing(3))
                return CalcValue.From(text.Replace(oldText, newText, StringComparison.Ordinal));

            var occurrence = a.Integer(3);
            if (occurrence < 1)
                return CalcValue.Error(CellError.Value);

            var index = -1;
            for (var found = 0; found < occurrence; found++)
            {
                index = text.IndexOf(oldText, index + 1, StringComparison.Ordinal);
                if (index < 0)
                    return CalcValue.From(text);
            }

            return CalcValue.From(text[..index] + newText + text[(index + oldText.Length)..]);
        });

        registry.Add("REPLACE", 4, 4, a =>
        {
            var text = a.Text(0);
            var start = a.Integer(1);
            var length = a.Integer(2);
            var replacement = a.Text(3);

            if (start < 1 || length < 0)
                return CalcValue.Error(CellError.Value);

            var offset = Math.Min(start - 1, text.Length);
            var take = Math.Min(length, text.Length - offset);
            return CalcValue.From(text[..offset] + replacement + text[(offset + take)..]);
        });

        registry.Add("VALUE", 1, 1, a =>
        {
            var text = a.Text(0).Trim();
            if (text.Length == 0)
                return CalcValue.From(0d);

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var number) ||
                double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                return CalcValue.From(number);

            return ExcelDate.TryParse(text, out var serial) ? CalcValue.From(serial) : CalcValue.Error(CellError.Value);
        });

        registry.Add("T", 1, 1, a =>
        {
            var value = a.Checked(0);
            return CalcValue.From(value.Kind == CellValueKind.Text ? value.AsText() : string.Empty);
        });

        registry.Add("CHAR", 1, 1, a =>
        {
            var code = a.Integer(0);
            return code is < 1 or > 255 ? CalcValue.Error(CellError.Value) : CalcValue.From(((char)code).ToString());
        });

        registry.Add("CODE", 1, 1, a =>
        {
            var text = a.Text(0);
            return text.Length == 0 ? CalcValue.Error(CellError.Value) : CalcValue.From((double)text[0]);
        });

        registry.Add("TEXT", 2, 2, a =>
        {
            var value = a.Checked(0);
            var code = a.Text(1);

            try
            {
                var format = new ExcelNumberFormat.NumberFormat(code);
                if (!format.IsValid)
                    return CalcValue.Error(CellError.Value);

                var boxed = value.Kind == CellValueKind.Text ? value.AsText() : (object)ToNumber(value);
                return CalcValue.From(format.Format(boxed, CultureInfo.CurrentCulture));
            }
            catch (Exception)
            {
                return CalcValue.Error(CellError.Value);
            }
        });
    }

    static double ToNumber(CellValue value)
        => Coercion.TryToNumber(value, out var number, out var error) ? number : throw new CalcErrorException(error);

    /// <summary>
    /// TRIM removes leading and trailing spaces and collapses internal runs to one — it is not
    /// <c>string.Trim()</c>, and the difference shows up on any text pasted out of a web page.
    /// </summary>
    static string Trim(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var c in text.Trim(' '))
        {
            if (c == ' ')
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0)
                builder.Append(' ');

            pendingSpace = false;
            builder.Append(c);
        }

        return builder.ToString();
    }

    static CalcValue Search(CalcArguments a, StringComparison comparison, bool wildcards)
    {
        var needle = a.Text(0);
        var haystack = a.Text(1);
        var start = a.IntegerOrDefault(2, 1);

        if (start < 1 || start > haystack.Length + 1)
            return CalcValue.Error(CellError.Value);

        var offset = start - 1;

        if (wildcards && needle.Contains('*') || (wildcards && needle.Contains('?')))
        {
            var pattern = ConditionalFunctions.WildcardToRegex("*" + needle + "*");
            _ = pattern;

            // Anchored wildcard matching would only say whether it matched, not where. Scan forward for
            // the first position whose remainder matches the pattern prefix.
            var regex = new System.Text.RegularExpressions.Regex(
                TranslateForSearch(needle),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            var match = regex.Match(haystack, offset);
            return match.Success
                ? CalcValue.From((double)(match.Index + 1))
                : CalcValue.Error(CellError.Value);
        }

        if (needle.Length == 0)
            return CalcValue.From((double)start);

        var index = haystack.IndexOf(needle, offset, comparison);
        return index < 0 ? CalcValue.Error(CellError.Value) : CalcValue.From((double)(index + 1));
    }

    static string TranslateForSearch(string pattern)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '~' && i + 1 < pattern.Length && pattern[i + 1] is '*' or '?' or '~')
            {
                builder.Append(System.Text.RegularExpressions.Regex.Escape(pattern[i + 1].ToString()));
                i++;
                continue;
            }

            builder.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _ => System.Text.RegularExpressions.Regex.Escape(c.ToString())
            });
        }

        return builder.ToString();
    }
}
