using Shiny.Controls.Office.Presentation;
using Shiny.Controls.Office.Skia;

namespace Sample.Features.Office;

/// <summary>
/// The .pptx editor.
/// </summary>
/// <remarks>
/// Tap a shape to select it, then drag it or its handles. Double-tap to put a caret inside its text.
/// Physical keys need a platform hook — see <c>SlideEditor.HandleKey</c> — but tapping, dragging,
/// typing and every toolbar command work without one.
/// </remarks>
public partial class SlideEditorPage : ContentPage
{
    SlideDeck? deck;
    int edits;
    bool dark;

    public SlideEditorPage()
    {
        this.InitializeComponent();
        this.UpdateStatus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (this.deck is not null)
            return;

        var bytes = SampleOfficeDocuments.BuildDeck();
        this.deck = await SlideDeck.OpenAsync(new MemoryStream(bytes), editable: true);
        this.Editor.Deck = this.deck;
        this.Editor.DeckChanged += this.OnDeckChanged;

        this.UpdateStatus();
    }

    void OnToggleToolbar(object? sender, EventArgs e) => this.Editor.ShowToolbar = !this.Editor.ShowToolbar;

    void OnToggleTheme(object? sender, EventArgs e)
    {
        this.dark = !this.dark;
        this.Editor.Theme = this.dark ? SlideTheme.Dark : SlideTheme.Light;
    }

    void OnDeckChanged(object? sender, EventArgs e)
    {
        this.edits++;
        this.UpdateStatus();
    }

    void UpdateStatus()
        => this.StatusLabel.Text = this.deck is null
            ? "loading"
            : $"{this.deck.Slides.Count} slides · {this.edits} edits{(this.deck.IsDirty ? " · unsaved" : string.Empty)}";

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (this.Handler is null)
        {
            this.Editor.DeckChanged -= this.OnDeckChanged;
            this.deck?.Dispose();
            this.deck = null;
        }
    }
}
