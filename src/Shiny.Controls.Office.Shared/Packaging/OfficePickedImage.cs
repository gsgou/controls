namespace Shiny.Controls.Office.Packaging;

/// <summary>An image the user chose, already read.</summary>
/// <param name="FileName">The file's name, used to name the picture in the document.</param>
/// <param name="ContentType">The MIME type to store the part under.</param>
/// <param name="Data">The encoded bytes, exactly as they were on disk.</param>
public sealed record OfficePickedImage(string FileName, string ContentType, byte[] Data);
