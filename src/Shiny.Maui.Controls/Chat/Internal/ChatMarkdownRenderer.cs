using System.Text.RegularExpressions;

namespace Shiny.Maui.Controls.Chat.Internal;

/// <summary>
/// A minimal, self-contained inline markdown renderer for chat bubbles. Handles the same subset
/// the composition toolbar produces: **bold**, *italic* / _italic_, ~~strike~~, `code`,
/// &lt;u&gt;underline&lt;/u&gt;, [text](url) links, and bare URL auto-linking. Deliberately does not
/// reference the Shiny.Maui.Controls.Markdown package.
/// </summary>
static partial class ChatMarkdownRenderer
{
    static readonly Regex LinkRegex = CreateLinkRegex();

    public static void Apply(Label label, string text, Color textColor, double fontSize, string? fontFamily, Color linkColor)
    {
        if (string.IsNullOrEmpty(text))
        {
            label.FormattedText = null;
            label.Text = string.Empty;
            return;
        }

        var formatted = new FormattedString();
        var lastIndex = 0;

        foreach (Match match in LinkRegex.Matches(text))
        {
            if (match.Index > lastIndex)
                AppendInline(formatted, text[lastIndex..match.Index], textColor, fontSize, fontFamily);

            var display = match.Groups["t"].Success ? match.Groups["t"].Value : match.Value;
            var url = match.Groups["u"].Success ? match.Groups["u"].Value : match.Value;
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;

            var span = new Span
            {
                Text = display,
                TextColor = linkColor,
                TextDecorations = TextDecorations.Underline,
                FontSize = fontSize
            };
            if (!string.IsNullOrEmpty(fontFamily))
                span.FontFamily = fontFamily;

            var tap = new TapGestureRecognizer();
            var captured = url;
            tap.Tapped += (_, _) => _ = Launcher.OpenAsync(new Uri(captured));
            span.GestureRecognizers.Add(tap);

            formatted.Spans.Add(span);
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            AppendInline(formatted, text[lastIndex..], textColor, fontSize, fontFamily);

        if (formatted.Spans.Count == 0)
        {
            label.FormattedText = null;
            label.Text = text;
        }
        else
        {
            label.FormattedText = formatted;
        }
    }

    static void AppendInline(FormattedString formatted, string text, Color textColor, double fontSize, string? fontFamily)
    {
        foreach (var run in ParseEmphasis(text))
        {
            var attrs = FontAttributes.None;
            if (run.Bold) attrs |= FontAttributes.Bold;
            if (run.Italic) attrs |= FontAttributes.Italic;

            var decorations = TextDecorations.None;
            if (run.Underline) decorations |= TextDecorations.Underline;
            if (run.Strike) decorations |= TextDecorations.Strikethrough;

            var span = new Span
            {
                Text = run.Text,
                TextColor = textColor,
                FontSize = fontSize,
                FontAttributes = attrs,
                TextDecorations = decorations
            };
            span.FontFamily = run.Code ? "Courier New" : fontFamily;

            formatted.Spans.Add(span);
        }
    }

    record struct Run(string Text, bool Bold, bool Italic, bool Strike, bool Underline, bool Code);

    static List<Run> ParseEmphasis(string text)
    {
        var runs = new List<Run>();
        var buffer = new System.Text.StringBuilder();
        bool bold = false, italic = false, strike = false, underline = false;

        void Flush()
        {
            if (buffer.Length == 0)
                return;
            runs.Add(new Run(buffer.ToString(), bold, italic, strike, underline, false));
            buffer.Clear();
        }

        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];

            if (c == '`')
            {
                Flush();
                i++;
                var start = i;
                while (i < text.Length && text[i] != '`')
                    i++;
                var code = text[start..i];
                if (i < text.Length) i++; // skip closing backtick
                if (code.Length > 0)
                    runs.Add(new Run(code, bold, italic, strike, underline, true));
                continue;
            }
            if (Matches(text, i, "**"))
            {
                Flush(); bold = !bold; i += 2; continue;
            }
            if (Matches(text, i, "~~"))
            {
                Flush(); strike = !strike; i += 2; continue;
            }
            if (Matches(text, i, "<u>"))
            {
                Flush(); underline = true; i += 3; continue;
            }
            if (Matches(text, i, "</u>"))
            {
                Flush(); underline = false; i += 4; continue;
            }
            if (c == '*' || c == '_')
            {
                Flush(); italic = !italic; i++; continue;
            }

            buffer.Append(c);
            i++;
        }
        Flush();

        if (runs.Count == 0)
            runs.Add(new Run(text, false, false, false, false, false));

        return runs;
    }

    static bool Matches(string text, int index, string token)
        => index + token.Length <= text.Length && text.AsSpan(index, token.Length).SequenceEqual(token);

    [GeneratedRegex(@"\[(?<t>[^\]]+)\]\((?<u>[^)\s]+)\)|(?<bare>https?://[^\s]+|www\.[^\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CreateLinkRegex();
}
