using Microsoft.Maui.Graphics;
using Shiny.Controls.Camera;
using Shiny.Maui.Controls.Camera.Documents;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class HealthCardParserTests
{
    static RecognizedText Line(string text, float y) => new(text, new RectF(0.1f, y, 0.8f, 0.05f));

    [Fact]
    public void Extracts_number_name_expiry_issuer()
    {
        RecognizedText[] card =
        [
            Line("ONTARIO HEALTH", 0.05f),
            Line("SAMPLE, JOHN", 0.20f),
            Line("1234 567 890", 0.35f),
            Line("Expiry 2026-05-31", 0.55f),
        ];

        new HealthCardParser().TryParse(card, out var hc, out var boxes).ShouldBeTrue();

        hc.Number.ShouldBe("1234567890");
        hc.Name.ShouldBe("SAMPLE, JOHN");
        hc.Expiry.ShouldBe(new DateOnly(2026, 5, 31));
        hc.Issuer.ShouldNotBeNull().ShouldContain("HEALTH");
        boxes.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Rejects_card_without_a_number()
    {
        RecognizedText[] card = [Line("ONTARIO HEALTH", 0.05f), Line("SAMPLE, JOHN", 0.20f)];
        new HealthCardParser().TryParse(card, out _, out _).ShouldBeFalse();
    }
}
