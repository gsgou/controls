using System.Collections.ObjectModel;

namespace Sample.Features.Camera;

/// <summary>One document a document-analyzer lifted from the camera during this app session.</summary>
/// <param name="Kind">Human label for the document type (e.g. "Driver's License").</param>
/// <param name="Summary">The one-line headline shown in the list.</param>
/// <param name="Detail">The full field set, one "Label: value" per line.</param>
/// <param name="CapturedAt">When it was recognized.</param>
public record CapturedDocument(string Kind, string Summary, string Detail, DateTimeOffset CapturedAt);

/// <summary>
/// In-memory, app-session list of documents the camera's document analyzers have recognized. Registered as a
/// singleton so the camera page (which appends) and the session page (which displays) share one instance.
/// Not persisted — it lives for the lifetime of the app process, matching "keep an app session".
/// </summary>
public class DocumentSessionStore
{
    /// <summary>Captured documents, newest first.</summary>
    public ObservableCollection<CapturedDocument> Documents { get; } = [];

    /// <summary>
    /// Append a capture. Skips an immediate duplicate (same kind + summary as the newest entry) so a document
    /// sitting in frame and re-recognized on every frame doesn't flood the list.
    /// </summary>
    public void Add(string kind, string summary, string detail)
    {
        if (this.Documents.Count > 0)
        {
            var newest = this.Documents[0];
            if (newest.Kind == kind && newest.Summary == summary)
                return;
        }
        this.Documents.Insert(0, new CapturedDocument(kind, summary, detail, DateTimeOffset.Now));
    }

    public void Clear() => this.Documents.Clear();
}
