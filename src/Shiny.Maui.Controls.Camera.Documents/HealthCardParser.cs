using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Documents;

/// <summary>
/// Best-effort rules parser for health-insurance cards: locates the member number (the longest digit run),
/// a cardholder name, an expiry date and the issuer. Heuristic — swap in a custom
/// <see cref="IDocumentParser{HealthCard}"/> for a specific issuer's layout.
/// </summary>
public partial class HealthCardParser : IDocumentParser<HealthCard>
{
    /// <summary>Color for the highlighted member-number box.</summary>
    public Color NumberColor { get; set; } = Color.FromArgb("#EF4444");

    /// <summary>Color for the cardholder-name box.</summary>
    public Color NameColor { get; set; } = Color.FromArgb("#3B82F6");

    [GeneratedRegex(@"\b\d(?:[\d ]{6,})\d\b")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"\b(\d{4}-\d{2}-\d{2}|\d{1,2}[/-]\d{1,2}[/-]\d{2,4})\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"^[A-Z][A-Za-z'.-]+,\s*[A-Z][A-Za-z'.\- ]+$")]
    private static partial Regex NameRegex();

    public bool TryParse(
        IReadOnlyList<RecognizedText> text,
        out HealthCard document,
        out IReadOnlyList<OverlayBox> boxes)
    {
        document = null!;
        boxes = [];

        var lines = text
            .Where(t => !string.IsNullOrWhiteSpace(t.Text))
            .OrderBy(t => t.BoundingBox.Y)
            .ThenBy(t => t.BoundingBox.X)
            .ToList();
        if (lines.Count == 0)
            return false;

        string? number = null;
        RecognizedText? numberLine = null;
        string? name = null;
        RecognizedText? nameLine = null;
        DateOnly? expiry = null;
        string? issuer = null;

        foreach (var line in lines)
        {
            var t = line.Text.Trim();
            var lower = t.ToLowerInvariant();

            if (number == null)
            {
                var m = NumberRegex().Match(t);
                if (m.Success)
                {
                    var digits = m.Value.Replace(" ", string.Empty);
                    if (digits.Length is >= 8 and <= 14)
                    {
                        number = digits;
                        numberLine = line;
                    }
                }
            }

            if (name == null && NameRegex().IsMatch(t))
            {
                name = t;
                nameLine = line;
            }

            expiry ??= MatchDate(t);

            if (issuer == null && (lower.Contains("health") || lower.Contains("ministry") || lower.Contains("insurance")))
                issuer = t;
        }

        if (number == null)
            return false;

        var fields = new List<DocumentField>();
        fields.Add(new DocumentField("Member #", number));
        if (name != null) fields.Add(new DocumentField("Name", name));
        if (expiry != null) fields.Add(new DocumentField("Expiry", expiry.Value.ToString("yyyy-MM-dd")));
        if (issuer != null) fields.Add(new DocumentField("Issuer", issuer));

        document = new HealthCard(number, name, expiry, issuer, fields);

        var overlay = new List<OverlayBox>(2);
        if (numberLine is { } nl)
            overlay.Add(new OverlayBox(nl.BoundingBox, this.NumberColor, number, this.NumberColor));
        if (nameLine is { } nm)
            overlay.Add(new OverlayBox(nm.BoundingBox, this.NameColor));
        boxes = overlay;
        return true;
    }

    static DateOnly? MatchDate(string line)
    {
        var m = DateRegex().Match(line);
        if (!m.Success)
            return null;

        foreach (var fmt in new[] { "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "M-d-yyyy", "MM/dd/yy" })
        {
            if (DateOnly.TryParseExact(m.Value, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }
        return DateOnly.TryParse(m.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var any) ? any : null;
    }
}
