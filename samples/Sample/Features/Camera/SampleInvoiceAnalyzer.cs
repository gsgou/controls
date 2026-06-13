using System.Text.RegularExpressions;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Ocr;

namespace Sample.Features.Camera;

// Demonstrates the IDocumentAnalyzer hook: scans OCR text blocks for a currency amount and a "total"
// label, emitting a DocumentField detection. A real implementation would use richer rules, a cloud
// Document AI, or an LLM — this is just enough to show the wiring.
public partial class SampleInvoiceAnalyzer : IDocumentAnalyzer
{
    [GeneratedRegex(@"(total|amount|balance|due)\D{0,12}(\$?\s?\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2}))", RegexOptions.IgnoreCase)]
    private static partial Regex TotalRegex();

    public ValueTask<IReadOnlyList<Detection>> ExtractAsync(IReadOnlyList<Detection> textBlocks, CancellationToken ct)
    {
        var fields = new List<Detection>();
        foreach (var block in textBlocks)
        {
            if (block.Value is not { } line)
                continue;

            var m = TotalRegex().Match(line);
            if (m.Success)
                fields.Add(block with { Type = DetectionType.DocumentField, Label = "Total", Value = m.Groups[2].Value.Trim() });
        }
        return new ValueTask<IReadOnlyList<Detection>>(fields);
    }
}
