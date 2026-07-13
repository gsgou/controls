using Shiny.Maui.Controls.ImageEditor;
using Shiny.Maui.Controls.Media;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Tests;

public class MediaImageProcessorTests
{
    [Theory]
    [InlineData(92, 0.92f)]
    [InlineData(100, 1.0f)]
    [InlineData(1, 0.01f)]
    [InlineData(50, 0.5f)]
    public void NormalizeQuality_MapsPercentToUnitInterval(int percent, float expected)
        => MediaImageProcessor.NormalizeQuality(percent).ShouldBe(expected, 0.0001f);

    [Theory]
    [InlineData(0)]      // below range
    [InlineData(-20)]
    public void NormalizeQuality_ClampsLow(int percent)
        => MediaImageProcessor.NormalizeQuality(percent).ShouldBe(0.01f, 0.0001f);

    [Theory]
    [InlineData(150)]    // above range
    [InlineData(9999)]
    public void NormalizeQuality_ClampsHigh(int percent)
        => MediaImageProcessor.NormalizeQuality(percent).ShouldBe(1.0f, 0.0001f);

    [Theory]
    [InlineData(ImageExportFormat.Png, "image/png")]
    [InlineData(ImageExportFormat.Jpeg, "image/jpeg")]
    [InlineData(ImageExportFormat.Webp, "image/jpeg")]   // non-PNG encodes as JPEG
    public void ContentTypeFor_MapsFormat(ImageExportFormat format, string expected)
        => MediaImageProcessor.ContentTypeFor(format).ShouldBe(expected);
}

public class MediaPickerItemTests
{
    [Fact]
    public void OpenRead_RoundTripsBytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var item = new MediaPickerItem(bytes, 10, 20, "image/jpeg");

        using var stream = item.OpenRead();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        ms.ToArray().ShouldBe(bytes);
    }

    [Fact]
    public void Dimensions_AndContentType_ArePreserved()
    {
        var item = new MediaPickerItem([9, 9], 640, 480, "image/png");

        item.Width.ShouldBe(640);
        item.Height.ShouldBe(480);
        item.ContentType.ShouldBe("image/png");
        item.Thumbnail.ShouldNotBeNull();
    }
}
