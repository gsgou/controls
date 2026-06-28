using System.Net;
using System.Text.RegularExpressions;

namespace Shiny.Blazor.Controls.Chat;

/// <summary>
/// A tiny, self-contained inline-markdown renderer covering the chat subset
/// (code, bold, underline, strikethrough, italic, links) plus bare-URL linkify.
/// Deliberately does NOT depend on the Shiny.Blazor.Controls.Markdown package.
/// </summary>
static partial class ChatMarkdown
{
    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"\*\*([^*]+)\*\*", RegexOptions.Compiled)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"~~([^~]+)~~", RegexOptions.Compiled)]
    private static partial Regex StrikeRegex();

    [GeneratedRegex(@"\+\+([^+]+)\+\+", RegexOptions.Compiled)]
    private static partial Regex UnderlineRegex();

    [GeneratedRegex(@"(?<![\*\w])\*([^*]+)\*(?![\*\w])|(?<![_\w])_([^_]+)_(?![_\w])", RegexOptions.Compiled)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\((https?://[^\s)]+)\)", RegexOptions.Compiled)]
    private static partial Regex MdLinkRegex();

    [GeneratedRegex(@"(https?://[^\s<]+|www\.[^\s<]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    // Private-use sentinels so emitted tags survive HTML-encoding and aren't re-processed
    // by the bare-URL linkifier; swapped for real < > at the very end.
    const string OpenTag = "";
    const string CloseTag = "";

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        var text = WebUtility.HtmlEncode(markdown);

        text = CodeRegex().Replace(text, m => $"{OpenTag}code{CloseTag}{m.Groups[1].Value}{OpenTag}/code{CloseTag}");
        text = BoldRegex().Replace(text, m => $"{OpenTag}strong{CloseTag}{m.Groups[1].Value}{OpenTag}/strong{CloseTag}");
        text = StrikeRegex().Replace(text, m => $"{OpenTag}del{CloseTag}{m.Groups[1].Value}{OpenTag}/del{CloseTag}");
        text = UnderlineRegex().Replace(text, m => $"{OpenTag}u{CloseTag}{m.Groups[1].Value}{OpenTag}/u{CloseTag}");
        text = ItalicRegex().Replace(text, m =>
        {
            var inner = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            return $"{OpenTag}em{CloseTag}{inner}{OpenTag}/em{CloseTag}";
        });

        // Markdown links first, then bare URLs.
        text = MdLinkRegex().Replace(text, m => Anchor(m.Groups[2].Value, m.Groups[1].Value));
        text = UrlRegex().Replace(text, m => Anchor(m.Value, m.Value));

        return text.Replace(OpenTag, "<").Replace(CloseTag, ">");
    }

    static string Anchor(string url, string display)
    {
        var href = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : "https://" + url;

        return $"{OpenTag}a href=\"{href}\" target=\"_blank\" rel=\"noopener noreferrer\"{CloseTag}{display}{OpenTag}/a{CloseTag}";
    }
}
