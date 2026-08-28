namespace Shiny.Maui.Controls.Desktop.FileDrop;

/// <summary>
/// Extension to MIME type, for the handful of types an app that accepts drops actually branches on.
/// </summary>
/// <remarks>
/// Deliberately small. The platforms do not agree on how to report a type — Windows gives none,
/// AppKit gives a UTI, GTK gives a content type from shared-mime-info — so normalising on the
/// extension is the only thing that reads the same everywhere. Anything unrecognised comes back as
/// <c>application/octet-stream</c> rather than null, so callers never have to null-check it.
/// </remarks>
static class FileDropContentTypes
{
    public const string Unknown = "application/octet-stream";

    static readonly Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".heic"] = "image/heic",
        [".svg"] = "image/svg+xml",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".ico"] = "image/x-icon",

        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",
        [".json"] = "application/json",
        [".xml"] = "application/xml",
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "text/javascript",

        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",

        [".zip"] = "application/zip",
        [".gz"] = "application/gzip",
        [".tar"] = "application/x-tar",
        [".7z"] = "application/x-7z-compressed",

        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".webm"] = "video/webm",
        [".mkv"] = "video/x-matroska"
    };

    public static string Resolve(string extension)
        => String.IsNullOrEmpty(extension) ? Unknown : map.GetValueOrDefault(extension, Unknown);
}
