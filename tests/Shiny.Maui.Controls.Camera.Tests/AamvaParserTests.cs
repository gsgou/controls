using Shiny.Maui.Controls.Camera.Documents;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Camera.Tests;

public class AamvaParserTests
{
    // A representative AAMVA PDF417 payload (US license). Elements are newline-separated <code><value>.
    const string Sample =
        "@\n" +
        "ANSI 636000100002DL00410278ZV03190008DL" +
        "DAQT64235789\n" +
        "DCSSAMPLE\n" +
        "DACMICHAEL\n" +
        "DADJOHN\n" +
        "DBD08242018\n" +
        "DBB01311970\n" +
        "DBA08312025\n" +
        "DAG2300 WEST BROAD STREET\n" +
        "DAIRICHMOND\n" +
        "DAJVA\n" +
        "DAK232690000\n" +
        "DCGUSA\n";

    [Fact]
    public void Parses_core_fields()
    {
        AamvaParser.TryParse(Sample, out var dl).ShouldBeTrue();

        dl.Number.ShouldBe("T64235789");
        dl.FirstName.ShouldBe("MICHAEL");
        dl.LastName.ShouldBe("SAMPLE");
        dl.DateOfBirth.ShouldBe(new DateOnly(1970, 1, 31));
        dl.Expiry.ShouldBe(new DateOnly(2025, 8, 31));
        dl.Address.ShouldBe("2300 WEST BROAD STREET, RICHMOND, VA, 232690000");
    }

    [Fact]
    public void Exposes_fields_collection()
    {
        AamvaParser.TryParse(Sample, out var dl).ShouldBeTrue();

        dl.Fields.ShouldContain(f => f.Label == "License #" && f.Value == "T64235789");
        dl.Fields.ShouldContain(f => f.Label == "Expiry" && f.Value == "2025-08-31");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("just some random text, not a license")]
    public void Rejects_non_aamva(string? input)
        => AamvaParser.TryParse(input, out _).ShouldBeFalse();

    [Fact]
    public void Parses_canadian_date_order()
    {
        // Canada uses CCYYMMDD; DCG=CAN should drive the date order.
        const string canada =
            "@\nANSI 636012\nDLDAQ1234567\nDCSDOE\nDACJANE\nDBB19850715\nDBA20300715\nDCGCAN\n";

        AamvaParser.TryParse(canada, out var dl).ShouldBeTrue();
        dl.DateOfBirth.ShouldBe(new DateOnly(1985, 7, 15));
        dl.Expiry.ShouldBe(new DateOnly(2030, 7, 15));
    }
}
