using Shiny.Controls.Office.Document;
using Shiny.Controls.Office.Skia;
using Shiny.Controls.Office.Spelling;
using Shiny.Maui.Controls.Office;

namespace Sample.Features.Office;

/// <summary>
/// The .docx editor, with the platform's own spell checker.
/// </summary>
/// <remarks>
/// Nothing here registers a checker: <c>Shiny.Maui.Controls.Office</c> installs the platform one —
/// UITextChecker on iOS, NSSpellChecker on macOS, Android's text services, the Windows COM checker —
/// as soon as the package is touched. Right-click (or long-press) a red-underlined word for
/// corrections; the same menu offers Ignore and Add to dictionary, and the last of those writes to
/// the user's real dictionary, shared with every other app on the device.
/// </remarks>
public partial class DocumentEditorPage : ContentPage
{
    WordDocument? document;
    bool dark;

    public DocumentEditorPage()
    {
        this.InitializeComponent();
        this.UpdateStatus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (this.document is not null)
            return;

        var bytes = SampleOfficeDocuments.BuildDocument();
        this.document = await WordDocument.OpenAsync(new MemoryStream(bytes), editable: true);
        this.Editor.Document = this.document;
        this.Editor.DocumentChanged += this.OnDocumentChanged;

        this.UpdateStatus();
    }

    void OnToggleToolbar(object? sender, EventArgs e) => this.Editor.ShowToolbar = !this.Editor.ShowToolbar;

    void OnToggleTheme(object? sender, EventArgs e)
    {
        this.dark = !this.dark;
        this.Editor.Theme = this.dark ? DocumentTheme.Dark : DocumentTheme.Light;
    }

    void OnToggleSpelling(object? sender, EventArgs e)
    {
        this.Editor.IsSpellCheckEnabled = !this.Editor.IsSpellCheckEnabled;
        this.UpdateStatus();
    }

    void OnDocumentChanged(object? sender, EventArgs e) => this.UpdateStatus();

    void UpdateStatus()
    {
        var available = SpellCheckers.Default.IsAvailable;

        this.SpellButton.Text = this.Editor.IsSpellCheckEnabled ? "Spelling: on" : "Spelling: off";
        this.SpellButton.IsEnabled = available;

        this.StatusLabel.Text = available
            ? $"Spell checker: {SpellCheckers.Default.GetType().Name} ({SpellCheckers.Default.DefaultLanguage}). Right-click or long-press an underlined word."
            : "No platform spell checker on this target — set DocumentEditorView.SpellChecker to supply one.";
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (this.Handler is null)
        {
            this.Editor.DocumentChanged -= this.OnDocumentChanged;
            this.document?.Dispose();
            this.document = null;
        }
    }
}
