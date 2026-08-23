using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.QuickEntry;

/// <summary>
/// Scoped, not singleton — the popup's open state, its content and the live options object are all
/// per-user state, so a singleton would put one user's popup on every connected user's screen under
/// Blazor Server. Under WebAssembly the two lifetimes are identical.
/// </summary>
public sealed class QuickEntryService : IQuickEntryService, IDisposable
{
    RenderFragment? content;
    bool isOpen;
    bool isGlowVisible;

    public QuickEntryService(QuickEntryOptions options)
    {
        this.Options = options;
        this.Prompt.Changed += this.OnPromptChanged;
        this.Prompt.BusyChanged += this.OnPromptBusyChanged;
    }

    public QuickEntryOptions Options { get; }

    public PromptViewState Prompt { get; } = new();

    public RenderFragment? Content => this.content;

    public bool IsOpen
    {
        get => this.isOpen;
        private set
        {
            if (this.isOpen == value)
                return;

            this.isOpen = value;
            this.Changed?.Invoke(this, EventArgs.Empty);
            (value ? this.Opened : this.Closed)?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsGlowVisible
    {
        get => this.isGlowVisible;
        private set
        {
            if (this.isGlowVisible == value)
                return;

            this.isGlowVisible = value;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Changed;
    public event EventHandler? Opened;
    public event EventHandler? Closed;

    public void Show()
    {
        this.IsOpen = true;
        if (this.Options.ScreenGlow == ScreenGlowTrigger.WhileOpen)
            this.ShowGlow();
    }

    public void Show(RenderFragment content)
    {
        this.content = content;
        this.Show();
    }

    public void Hide()
    {
        this.IsOpen = false;

        // Whichever trigger lit it, closing the popup always puts the glow out — a glow left burning
        // with nothing on screen would be inexplicable.
        if (this.Options.ScreenGlow != ScreenGlowTrigger.None)
            this.HideGlow();
    }

    public void Toggle()
    {
        if (this.IsOpen)
            this.Hide();
        else
            this.Show();
    }

    public void SetContent(RenderFragment? content)
    {
        this.content = content;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ConfigurePrompt(Action<PromptViewState> configure) => configure(this.Prompt);

    public void ShowGlow() => this.IsGlowVisible = true;

    public void HideGlow() => this.IsGlowVisible = false;

    public async Task PulseGlowAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        this.ShowGlow();
        try
        {
            await Task.Delay(duration, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this.HideGlow();
        }
    }

    void OnPromptChanged(object? sender, EventArgs e) => this.Changed?.Invoke(this, EventArgs.Empty);

    void OnPromptBusyChanged(object? sender, bool busy)
    {
        if (this.Options.ScreenGlow != ScreenGlowTrigger.WhileBusy)
            return;

        if (busy && this.IsOpen)
            this.ShowGlow();
        else
            this.HideGlow();
    }

    public void Dispose()
    {
        this.Prompt.Changed -= this.OnPromptChanged;
        this.Prompt.BusyChanged -= this.OnPromptBusyChanged;
    }
}
