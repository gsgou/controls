namespace Shiny.Blazor.Controls;

/// <summary>
/// A single photo held by a <see cref="MediaPickerButton"/>, already compressed/converted
/// to the button's output format.
/// </summary>
public sealed class MediaPickerItem
{
    /// <summary>Encoded image bytes in <see cref="ContentType"/>.</summary>
    public byte[] Data { get; set; } = [];

    /// <summary>A <c>data:</c> URI over <see cref="Data"/> for direct binding to <c>&lt;img src&gt;</c>.</summary>
    public string DataUri { get; set; } = "";

    /// <summary>Pixel width of the (possibly resized) image.</summary>
    public int Width { get; set; }

    /// <summary>Pixel height of the (possibly resized) image.</summary>
    public int Height { get; set; }

    /// <summary>MIME type of <see cref="Data"/> (e.g. <c>image/jpeg</c>).</summary>
    public string ContentType { get; set; } = "";
}
