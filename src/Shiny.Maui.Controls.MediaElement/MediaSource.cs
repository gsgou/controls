using System.ComponentModel;
using System.Globalization;

namespace Shiny.Maui.Controls.Media;

/// <summary>
/// Where a <see cref="MediaElement"/> gets its media from. Mirrors MAUI's own <c>ImageSource</c> shape:
/// three concrete kinds, implicit conversion from <see cref="string"/>/<see cref="Uri"/>, and a
/// <see cref="TypeConverter"/> so XAML can say <c>Source="https://…/clip.mp4"</c> directly.
/// </summary>
/// <remarks>
/// A bare string is classified by <see cref="Parse"/>: an absolute URI with a network scheme becomes a
/// <see cref="UriMediaSource"/>, a rooted filesystem path becomes a <see cref="FileMediaSource"/>, and
/// anything else is treated as a <see cref="ResourceMediaSource"/> — a file shipped in the app package
/// under <c>Resources/Raw</c>.
/// </remarks>
[TypeConverter(typeof(MediaSourceConverter))]
public abstract class MediaSource : Element
{
    /// <summary>Stream a remote (or <c>file://</c>) URI.</summary>
    public static MediaSource FromUri(Uri uri) => new UriMediaSource { Uri = uri };

    /// <summary>Stream a remote URI given as a string.</summary>
    public static MediaSource FromUri(string uri) => new UriMediaSource { Uri = new Uri(uri, UriKind.Absolute) };

    /// <summary>Play a file already on the device's filesystem (a download, a recording, a cache entry).</summary>
    public static MediaSource FromFile(string path) => new FileMediaSource { Path = path };

    /// <summary>
    /// Play a file bundled in the app package — the <c>Resources/Raw</c> folder of a MAUI project.
    /// Pass the path as it appears there (e.g. <c>"intro.mp4"</c> or <c>"clips/intro.mp4"</c>).
    /// </summary>
    public static MediaSource FromResource(string path) => new ResourceMediaSource { Path = path };

    /// <summary>Classify a string into the appropriate <see cref="MediaSource"/> kind. See the type remarks.</summary>
    public static MediaSource? Parse(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !uri.IsFile)
            return FromUri(uri);

        // file:// URIs and rooted paths both name something already on disk
        if (uri?.IsFile == true)
            return FromFile(uri.LocalPath);

        return Path.IsPathRooted(value)
            ? FromFile(value)
            : FromResource(value);
    }

    public static implicit operator MediaSource?(string? value) => Parse(value);
    public static implicit operator MediaSource?(Uri? uri) => uri is null ? null : FromUri(uri);
}


/// <summary>A media stream addressed by an absolute URI (http/https/rtsp/hls manifests/…).</summary>
public sealed class UriMediaSource : MediaSource
{
    /// <summary>The absolute URI to stream.</summary>
    public Uri? Uri { get; set; }

    public override string ToString() => this.Uri?.ToString() ?? String.Empty;
}


/// <summary>A media file at an absolute path on the device filesystem.</summary>
public sealed class FileMediaSource : MediaSource
{
    /// <summary>The absolute filesystem path.</summary>
    public string? Path { get; set; }

    public override string ToString() => this.Path ?? String.Empty;
}


/// <summary>
/// A media file bundled in the app package (MAUI's <c>Resources/Raw</c>). Each backend resolves this to
/// the platform's package URI — <c>asset:///</c> on Android, the app bundle on Apple, <c>ms-appx:///</c>
/// on Windows, and the app directory on GTK.
/// </summary>
public sealed class ResourceMediaSource : MediaSource
{
    /// <summary>The package-relative path, e.g. <c>"intro.mp4"</c>.</summary>
    public string? Path { get; set; }

    public override string ToString() => this.Path ?? String.Empty;
}


/// <summary>Lets XAML assign a plain string to <see cref="MediaElement.Source"/>.</summary>
public class MediaSourceConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => MediaSource.Parse(value as string);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        => value?.ToString();
}
