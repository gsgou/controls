namespace Shiny.Controls.Office.Packaging;

/// <summary>
/// A dropped file the editor declined, and why.
/// </summary>
/// <remarks>
/// Surfaced as an event rather than handled internally with a message of the editor's own, because
/// there is nowhere inside a canvas to put a message that would not be painted over on the next
/// repaint — and because a host that already has a toast or a status bar should use it rather than be
/// given a second, different-looking one.
/// </remarks>
/// <param name="FileName">The name of the file as the source reported it.</param>
/// <param name="Reason">A sentence suitable for showing to a user.</param>
public sealed record OfficeDropRejected(string FileName, string Reason);
