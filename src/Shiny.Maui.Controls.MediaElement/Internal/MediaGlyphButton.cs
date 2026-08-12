namespace Shiny.Maui.Controls.Media.Internal;

/// <summary>A tappable, tintable vector glyph — the transport bar's button.</summary>
/// <remarks>
/// A <see cref="GraphicsView"/> rather than an <see cref="ImageButton"/> so the icon set needs no image
/// assets and re-tints instantly when the theme (or <c>ControlColor</c>) changes. The touch target is
/// padded out to 40×40 even though the glyph draws at 24×24, which keeps it thumb-sized on phones.
/// </remarks>
class MediaGlyphButton : GraphicsView
{
    readonly MediaGlyphDrawable drawable = new();

    /// <param name="glyph">The icon to draw.</param>
    /// <param name="automationId">
    /// A <b>stable</b> id for UI automation. It must not track the button's label: MAUI throws
    /// "AutomationId may only be set one time" on a second assignment, and a UI test looking for the
    /// play button shouldn't have to know it is currently called "Pause".
    /// </param>
    /// <param name="description">The screen-reader label, which does follow the current state.</param>
    public MediaGlyphButton(MediaGlyph glyph, string automationId, string description)
    {
        this.drawable.Glyph = glyph;
        this.Drawable = this.drawable;
        this.WidthRequest = 40;
        this.HeightRequest = 40;
        this.VerticalOptions = LayoutOptions.Center;

        // The glyph carries no text, so the semantic description is all a screen reader has to go on.
        SemanticProperties.SetDescription(this, description);
        this.AutomationId = automationId;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => this.Tapped?.Invoke(this, EventArgs.Empty);
        this.GestureRecognizers.Add(tap);
    }

    public event EventHandler? Tapped;

    public MediaGlyph Glyph
    {
        get => this.drawable.Glyph;
        set
        {
            if (this.drawable.Glyph == value)
                return;

            this.drawable.Glyph = value;
            this.Invalidate();
        }
    }

    public Color GlyphColor
    {
        get => this.drawable.Color;
        set
        {
            if (this.drawable.Color == value)
                return;

            this.drawable.Color = value;
            this.Invalidate();
        }
    }

    /// <summary>Update the screen-reader label as the button's meaning changes (Play ⇄ Pause).</summary>
    public void SetDescription(string description)
        => SemanticProperties.SetDescription(this, description);
}
