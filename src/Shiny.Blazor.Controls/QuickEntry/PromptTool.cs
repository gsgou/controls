namespace Shiny.Blazor.Controls.QuickEntry;

/// <summary>
/// A tappable glyph docked into a <see cref="PromptView"/>'s leading or trailing slot — the
/// prompt-bar equivalent of <see cref="TextEntryTool"/>.
/// </summary>
/// <remarks>
/// <para>
/// A plain object rather than a component, so a tool can be built in a view model and handed to the
/// prompt as a parameter — which is also the only way the popup's own prompt, configured through a
/// service, can be given one.
/// </para>
/// <para>
/// <see cref="OnAttached"/> hands over the prompt and the app's <see cref="IServiceProvider"/>, so a
/// tool can resolve whatever it needs (a speech service, an <c>IChatClient</c>) without the hosting
/// page wiring it. Anything subscribed there must be dropped in <see cref="OnDetached"/> — the tool
/// outlives the prompt it was handed to.
/// </para>
/// </remarks>
public class PromptTool
{
    /// <summary>Glyph shown in the tool button — an emoji or an icon-font character.</summary>
    public string? Icon { get; set; }

    /// <summary>Optional label rendered beside the glyph.</summary>
    public string? Text { get; set; }

    /// <summary>Tooltip / accessible name.</summary>
    public string? Title { get; set; }

    /// <summary>CSS colour for the tool. Null follows the on-surface-variant theme token.</summary>
    public string? ToolColor { get; set; }

    /// <summary>CSS class added to the tool button.</summary>
    public string? CssClass { get; set; }

    /// <summary>Whether the tool renders at all.</summary>
    public virtual bool IsVisible { get; set; } = true;

    /// <summary>Whether the tool can be clicked.</summary>
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>Callback when the tool is clicked. Runs before <see cref="OnClickAsync"/>.</summary>
    public Func<Task>? Clicked { get; set; }

    /// <summary>The prompt this tool is docked to, or null while it is unparented.</summary>
    protected PromptView? Prompt { get; private set; }

    /// <summary>The app's services, for a tool that needs to resolve one.</summary>
    protected IServiceProvider? Services { get; private set; }

    /// <summary>Override to handle the click without going through <see cref="Clicked"/>.</summary>
    protected virtual Task OnClickAsync() => Task.CompletedTask;

    /// <summary>Called when the tool joins a prompt.</summary>
    protected virtual void OnAttached() { }

    /// <summary>Called when the tool leaves a prompt.</summary>
    protected virtual void OnDetached() { }

    /// <summary>
    /// Re-render the prompt after the tool has changed its own appearance. Marshalled onto the
    /// renderer, so it is safe from a JS callback or a background continuation.
    /// </summary>
    protected Task RefreshAsync() => this.Prompt?.RefreshToolsAsync() ?? Task.CompletedTask;

    internal void InternalAttach(PromptView prompt, IServiceProvider services)
    {
        this.Prompt = prompt;
        this.Services = services;
        this.OnAttached();
    }

    internal void InternalDetach()
    {
        this.OnDetached();
        this.Prompt = null;
        this.Services = null;
    }

    internal async Task InternalClickAsync()
    {
        if (!this.IsEnabled)
            return;

        if (this.Clicked is not null)
            await this.Clicked();

        await this.OnClickAsync();
    }
}
