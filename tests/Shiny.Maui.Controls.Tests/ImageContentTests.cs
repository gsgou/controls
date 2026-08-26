using System.Text;
using Shiny.Maui.Controls.Images;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

/// <summary>What each non-network URI form means, and that reading one gives back what was put in.</summary>
public class ImageContentTests
{
    const string Markup = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 4 4'><rect width='4' height='4'/></svg>";


    [Fact]
    public void SchemesAreToldApart()
    {
        ImageContent.IsResource("resource://MyApp.Assets.logo.svg").ShouldBeTrue();
        ImageContent.IsResource("https://example.com/logo.svg").ShouldBeFalse();

        ImageContent.IsData("data:image/svg+xml;base64,AAAA").ShouldBeTrue();
        ImageContent.IsData("/var/mobile/logo.svg").ShouldBeFalse();
    }


    [Fact]
    public void DataUri_DecodesBase64()
    {
        var uri = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Markup));

        Encoding.UTF8.GetString(ImageContent.ReadData(uri)).ShouldBe(Markup);
    }


    [Fact]
    public void DataUri_DecodesPercentEncodedText()
    {
        // The form inline SVG is usually written in - the markup stays readable in the markup that
        // carries it.
        var uri = "data:image/svg+xml," + Uri.EscapeDataString(Markup);

        Encoding.UTF8.GetString(ImageContent.ReadData(uri)).ShouldBe(Markup);
    }


    [Fact]
    public void DataUri_WithoutAComma_IsRejected()
        => Should.Throw<FormatException>(() => ImageContent.ReadData("data:image/svg+xml;base64"));


    [Fact]
    public void MissingResource_SaysWhatItLookedFor()
    {
        var error = Should.Throw<FileNotFoundException>(() => ImageContent.ReadResource("resource://Nothing.Matches.This.svg"));

        error.Message.ShouldContain("Nothing.Matches.This.svg");
    }


    [Fact]
    public async Task MissingFile_IsReportedRatherThanHanging()
    {
        var path = Path.Combine(Path.GetTempPath(), "shinyimage-missing-" + Guid.NewGuid().ToString("n") + ".svg");

        await Should.ThrowAsync<FileNotFoundException>(() => ImageContent.ReadFileAsync(path));
    }


    [Fact]
    public async Task ExistingFile_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), "shinyimage-" + Guid.NewGuid().ToString("n") + ".svg");
        await File.WriteAllTextAsync(path, Markup);

        try
        {
            Encoding.UTF8.GetString(await ImageContent.ReadFileAsync(path)).ShouldBe(Markup);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
