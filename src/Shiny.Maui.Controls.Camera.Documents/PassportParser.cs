using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;

namespace Shiny.Maui.Controls.Camera.Documents;

/// <summary>
/// Finds the two MRZ lines among the recognized text and parses them with <see cref="Mrz"/> into a
/// strongly-typed <see cref="Passport"/>. The MRZ parse is deterministic; locating the lines is the only
/// OCR-dependent step.
/// </summary>
public class PassportParser : IDocumentParser<Passport>
{
    /// <summary>Color for the highlighted MRZ boxes.</summary>
    public Color BoxColor { get; set; } = Color.FromArgb("#6366F1");

    public bool TryParse(
        IReadOnlyList<RecognizedText> text,
        out Passport document,
        out IReadOnlyList<OverlayBox> boxes)
    {
        document = null!;
        boxes = [];

        // candidate MRZ lines, top-to-bottom
        var candidates = text
            .Select(t => (Block: t, Cleaned: Mrz.Clean(t.Text)))
            .Where(x => Mrz.LooksLikeMrzLine(x.Cleaned))
            .OrderBy(x => x.Block.BoundingBox.Y)
            .ToList();
        if (candidates.Count < 2)
            return false;

        var line1 = candidates.FirstOrDefault(x => x.Cleaned[0] == 'P');
        if (line1.Block is null)
            return false;
        var line2 = candidates.FirstOrDefault(x => !ReferenceEquals(x.Block, line1.Block));
        if (line2.Block is null)
            return false;

        if (!Mrz.TryParseTd3(line1.Cleaned, line2.Cleaned, out var passport))
            return false;

        document = passport;
        boxes =
        [
            new OverlayBox(line1.Block.BoundingBox, this.BoxColor, passport.Number, this.BoxColor),
            new OverlayBox(line2.Block.BoundingBox, this.BoxColor)
        ];
        return true;
    }
}
