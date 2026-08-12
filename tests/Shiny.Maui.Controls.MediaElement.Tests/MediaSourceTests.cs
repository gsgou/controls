using Shiny.Maui.Controls.Media;
using Shouldly;
using Xunit;

namespace Shiny.Maui.Controls.Media.Tests;

public class MediaSourceTests
{
    [Theory]
    [InlineData("https://example.com/clip.mp4")]
    [InlineData("http://example.com/clip.mp4")]
    [InlineData("https://example.com/live/index.m3u8")]
    public void Absolute_network_uris_become_a_uri_source(string value)
    {
        var source = MediaSource.Parse(value);

        source.ShouldBeOfType<UriMediaSource>()
            .Uri!.AbsoluteUri.ShouldBe(value);
    }

    [Fact]
    public void Rooted_paths_become_a_file_source()
        => MediaSource.Parse("/var/mobile/Documents/clip.mp4")
            .ShouldBeOfType<FileMediaSource>()
            .Path.ShouldBe("/var/mobile/Documents/clip.mp4");

    [Fact]
    public void File_uris_become_a_file_source_holding_the_local_path()
    {
        // A file:// URI names something on disk, so it must not go down the streaming path — the backends
        // resolve FileMediaSource with their own filename APIs.
        MediaSource.Parse("file:///tmp/clip.mp4")
            .ShouldBeOfType<FileMediaSource>()
            .Path.ShouldBe("/tmp/clip.mp4");
    }

    [Theory]
    [InlineData("intro.mp4")]
    [InlineData("clips/intro.mp4")]
    public void Relative_paths_become_a_packaged_resource(string value)
        => MediaSource.Parse(value)
            .ShouldBeOfType<ResourceMediaSource>()
            .Path.ShouldBe(value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_values_parse_to_null(string? value)
        => MediaSource.Parse(value).ShouldBeNull();

    [Fact]
    public void Implicit_string_conversion_runs_the_same_classification()
    {
        MediaSource? source = "https://example.com/clip.mp4";
        source.ShouldBeOfType<UriMediaSource>();
    }

    [Fact]
    public void Implicit_uri_conversion_produces_a_uri_source()
    {
        MediaSource? source = new Uri("https://example.com/clip.mp4");
        source.ShouldBeOfType<UriMediaSource>();
    }

    [Fact]
    public void The_type_converter_lets_xaml_assign_a_bare_string()
    {
        var converter = new MediaSourceConverter();

        converter.CanConvertFrom(null, typeof(string)).ShouldBeTrue();
        converter.ConvertFrom(null, null, "https://example.com/clip.mp4")
            .ShouldBeOfType<UriMediaSource>();
    }

    [Fact]
    public void ToString_round_trips_the_underlying_address()
    {
        MediaSource.FromUri("https://example.com/clip.mp4").ToString().ShouldBe("https://example.com/clip.mp4");
        MediaSource.FromFile("/tmp/clip.mp4").ToString().ShouldBe("/tmp/clip.mp4");
        MediaSource.FromResource("intro.mp4").ToString().ShouldBe("intro.mp4");
    }
}
