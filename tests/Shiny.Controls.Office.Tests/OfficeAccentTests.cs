using Shiny.Controls.Office.Spreadsheet;
using Shiny.Controls.Office.Theming;
using Shouldly;
using Xunit;

namespace Shiny.Controls.Office.Tests;

/// <summary>
/// The accent an Office control wears, and the ink that has to stay readable on it.
/// </summary>
/// <remarks>
/// The ink is the part worth testing. A caller picking a brand colour is not thinking about whether
/// their tab labels have gone invisible on it, and a pale accent with white text is unreadable in
/// exactly the way that is hard to notice on the machine it was authored on.
/// </remarks>
public class OfficeAccentTests
{
    [Fact]
    public void ThePresetsAreTheColoursMicrosoftUses()
    {
        // Not a house style to be improved on: these are what a user already reads as "spreadsheet"
        // and "slides" before any label has been looked at.
        Hex(OfficeAccent.Document.Color).ShouldBe("185ABD");
        Hex(OfficeAccent.Spreadsheet.Color).ShouldBe("107C41");
        Hex(OfficeAccent.Presentation.Color).ShouldBe("C43E1C");
    }

    [Fact]
    public void EveryPresetCarriesInkThatReadsOnIt()
    {
        foreach (var accent in new[] { OfficeAccent.Document, OfficeAccent.Spreadsheet, OfficeAccent.Presentation })
            accent.Ink.ShouldBe(OfficeAccent.InkFor(accent.Color));
    }

    [Theory]
    [InlineData(0x18, 0x5A, 0xBD, false)]   // Word blue
    [InlineData(0x10, 0x7C, 0x41, false)]   // Excel green
    [InlineData(0xC4, 0x3E, 0x1C, false)]   // PowerPoint red
    [InlineData(0x00, 0x00, 0x00, false)]
    [InlineData(0xFF, 0xFF, 0xFF, true)]
    [InlineData(0xFF, 0xEB, 0x3B, true)]    // a bright yellow brand, where white would vanish
    [InlineData(0x9E, 0x9E, 0x9E, true)]
    public void InkFlipsToDarkOnlyOnALightAccent(byte r, byte g, byte b, bool expectDark)
    {
        var ink = OfficeAccent.InkFor(new ArgbColor(255, r, g, b));

        var isDark = ink.R < 128;
        isDark.ShouldBe(expectDark);
    }

    [Fact]
    public void FromChoosesTheInkSoACallerCannotGetItWrong()
    {
        var accent = OfficeAccent.From(new ArgbColor(255, 0xFF, 0xEB, 0x3B));

        accent.Color.R.ShouldBe((byte)0xFF);
        accent.Ink.R.ShouldBeLessThan((byte)128, "white text on a bright yellow band is unreadable");
    }

    static string Hex(ArgbColor c) => $"{c.R:X2}{c.G:X2}{c.B:X2}";
}
