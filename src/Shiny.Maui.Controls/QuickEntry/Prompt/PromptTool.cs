using Shiny.Maui.Controls.Infrastructure;

namespace Shiny.Maui.Controls.QuickEntry;

/// <summary>
/// Implemented by a <see cref="PromptTool"/> that needs to read or drive the prompt it is docked to —
/// a dictation tool that backfills the text, a read-aloud tool that watches for the answer.
/// </summary>
/// <remarks>
/// <see cref="Attach"/> runs when the tool joins a <see cref="PromptView"/>'s
/// <see cref="PromptView.LeadingTools"/> or <see cref="PromptView.TrailingTools"/>, and
/// <see cref="Detach"/> when it leaves. Subscriptions taken in the first must be dropped in the
/// second — the tool object outlives the collection it was in.
/// </remarks>
public interface IPromptAwareTool
{
    void Attach(PromptView prompt);
    void Detach();
}

/// <summary>
/// A tappable icon/label docked into a <see cref="PromptView"/>'s leading or trailing slot — the
/// prompt-bar equivalent of <see cref="TextEntryTool"/>.
/// </summary>
/// <remarks>
/// Sized to sit level with the built-in microphone and submit glyphs rather than stretching the
/// prompt row: the popup measures its own content to pick a window height, so a tool that grew the
/// row would grow the window with it.
/// </remarks>
public class PromptTool : IconTextTool
{
    // Matches the built-in glyph buttons (BuildGlyphButton) so a tool lands on the same baseline.
    const double GlyphHeight = 28;
    const double MinimumTapTarget = 34;

    public PromptTool()
    {
        this.Padding = new Thickness(6, 0);
        this.MinimumWidthRequest = MinimumTapTarget;
        this.HeightRequest = GlyphHeight;
        this.BackgroundColor = Colors.Transparent;
        this.VerticalOptions = LayoutOptions.Start;

        // Last line: replays any styled property that was applied before the
        // children existed. See StyleGuard.
        StyleGuard.MarkReady(this, typeof(PromptTool));
    }

    /// <summary>The prompt this tool is currently docked to, or null while it is unparented.</summary>
    public PromptView? ParentPrompt { get; internal set; }
}
