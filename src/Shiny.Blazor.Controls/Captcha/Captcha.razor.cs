using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// A human check in front of a form. One component over four providers: the built-in local
/// challenge (no account, no keys, works offline) plus Google reCAPTCHA, hCaptcha and Cloudflare
/// Turnstile, chosen by name at registration and swappable without touching the markup.
/// </summary>
/// <remarks>
/// <para>
/// With nothing registered it renders the local challenge, so it works from the package reference
/// alone. Call <c>AddShinyCaptcha</c> to pick a provider, set a site key, or change the defaults.
/// </para>
/// <para>
/// <b>The hosted providers are only half the check.</b> The component hands you the response token
/// in <see cref="State"/>; it is your server that has to post that token, with your <i>secret</i>
/// key, to the provider's siteverify endpoint. Trusting <see cref="IsSolved"/> alone means trusting
/// the client, which is what a captcha exists to avoid. <see cref="Validate"/> is the hook for
/// making that round trip.
/// </para>
/// </remarks>
public partial class Captcha
{
    [Inject] IServiceProvider Services { get; set; } = null!;

    /// <summary>
    /// Which registered provider to render — <c>local</c>, <c>recaptcha</c>, <c>hcaptcha</c>,
    /// <c>turnstile</c> or your own. Null uses the configured default, then the first registered,
    /// then the local challenge.
    /// </summary>
    [Parameter] public string? Provider { get; set; }

    /// <summary>The widget's colour scheme. Defaults to the configured theme (<c>Auto</c>).</summary>
    [Parameter] public CaptchaTheme? Theme { get; set; }

    /// <summary>The widget's footprint. Defaults to the configured size (<c>Normal</c>).</summary>
    [Parameter] public CaptchaSize? Size { get; set; }

    /// <summary>Two-letter language code for the provider's UI. Null follows the browser.</summary>
    [Parameter] public string? LanguageCode { get; set; }

    /// <summary>Where an invisible provider parks its badge.</summary>
    [Parameter] public CaptchaBadgePosition BadgePosition { get; set; } = CaptchaBadgePosition.BottomEnd;

    /// <summary>Extra classes on the host element.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Whether widget failures are rendered under the widget. Default true.</summary>
    [Parameter] public bool ShowError { get; set; } = true;

    /// <summary>
    /// Whether a failed <see cref="Validate"/> throws the challenge away and starts a new one.
    /// Default true — a token your server rejected is spent either way.
    /// </summary>
    [Parameter] public bool ResetOnFailedValidation { get; set; } = true;

    /// <summary>
    /// Raised once the challenge is met — and, when <see cref="Validate"/> is supplied, only after it
    /// returns true.
    /// </summary>
    [Parameter] public EventCallback<CaptchaState> Solved { get; set; }

    /// <summary>Raised when a solved challenge times out. The state is no longer valid.</summary>
    [Parameter] public EventCallback Expired { get; set; }

    /// <summary>Raised when the widget itself fails — script blocked, bad site key, network gone.</summary>
    [Parameter] public EventCallback<string> Errored { get; set; }

    /// <summary>
    /// Raised whenever <see cref="IsSolved"/> flips, so a submit button can bind straight to it:
    /// <c>&lt;Captcha ValidChanged="v =&gt; canSubmit = v" /&gt;</c>.
    /// </summary>
    [Parameter] public EventCallback<bool> ValidChanged { get; set; }

    /// <summary>
    /// Your server-side check. Called with the fresh token the moment the widget solves; return
    /// false and the state stays invalid. This is where the siteverify round trip belongs.
    /// </summary>
    [Parameter] public Func<CaptchaState, Task<bool>>? Validate { get; set; }

    /// <summary>Anything else lands on the host element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    ICaptchaProvider? provider;
    ICaptchaWidget? widget;
    CaptchaRenderContext renderContext = new();
    CaptchaOptions options = new();
    string? error;

    /// <summary>The current token and validity. Never null.</summary>
    public CaptchaState State { get; private set; } = CaptchaState.Empty(LocalCaptchaProvider.ProviderName);

    /// <summary>Shorthand for <c>State.Valid</c>.</summary>
    public bool IsSolved => this.State.Valid;

    /// <summary>Shorthand for <c>State.Response</c> — the token to send to your server.</summary>
    public string? Response => this.State.Response;

    protected override void OnInitialized()
        => this.options = this.Services.GetService<CaptchaOptions>() ?? new CaptchaOptions();

    protected override void OnParametersSet()
    {
        var resolved = this.ResolveProvider();

        if (!ReferenceEquals(resolved, this.provider))
        {
            this.provider = resolved;
            this.widget = null;
            this.error = null;
            this.State = CaptchaState.Empty(resolved?.Name ?? this.Provider ?? LocalCaptchaProvider.ProviderName);
        }

        this.renderContext = new CaptchaRenderContext
        {
            Theme = this.Theme ?? this.options.Theme,
            Size = this.Size ?? this.options.Size,
            LanguageCode = this.LanguageCode ?? this.options.LanguageCode,
            BadgePosition = this.BadgePosition,
            OnSolved = this.OnWidgetSolvedAsync,
            OnExpired = this.OnWidgetExpiredAsync,
            OnErrored = this.OnWidgetErroredAsync,
            OnWidgetReady = w => this.widget = w
        };
    }

    ICaptchaProvider? ResolveProvider()
    {
        var registered = this.Services.GetServices<ICaptchaProvider>().ToList();
        var wanted = this.Provider ?? this.options.DefaultProvider;

        if (wanted is not null)
        {
            var match = registered.FirstOrDefault(x => string.Equals(x.Name, wanted, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;

            // an explicitly named provider that is not registered is a wiring mistake, not a reason
            // to silently drop to a weaker check — say so in the markup instead
            return string.Equals(wanted, LocalCaptchaProvider.ProviderName, StringComparison.OrdinalIgnoreCase)
                ? new LocalCaptchaProvider(this.Services.GetService<LocalCaptchaOptions>() ?? new LocalCaptchaOptions())
                : null;
        }

        return registered.FirstOrDefault()
            ?? new LocalCaptchaProvider(this.Services.GetService<LocalCaptchaOptions>() ?? new LocalCaptchaOptions());
    }

    async Task OnWidgetSolvedAsync(string token)
    {
        var name = this.provider?.Name ?? LocalCaptchaProvider.ProviderName;
        var candidate = new CaptchaState(true, token, name);
        this.error = null;

        var accepted = true;
        if (this.Validate is not null)
        {
            try
            {
                accepted = await this.Validate(candidate);
            }
            catch (Exception ex)
            {
                accepted = false;
                this.error = ex.Message;
            }
        }

        if (accepted)
        {
            await this.SetStateAsync(candidate);
            await this.Solved.InvokeAsync(this.State);
        }
        else
        {
            await this.SetStateAsync(new CaptchaState(false, null, name));
            if (this.ResetOnFailedValidation)
                await this.ResetAsync();
        }

        this.StateHasChanged();
    }

    async Task OnWidgetExpiredAsync()
    {
        await this.SetStateAsync(CaptchaState.Empty(this.provider?.Name ?? LocalCaptchaProvider.ProviderName));
        await this.Expired.InvokeAsync();
        this.StateHasChanged();
    }

    async Task OnWidgetErroredAsync(string message)
    {
        this.error = message;
        await this.SetStateAsync(CaptchaState.Empty(this.provider?.Name ?? LocalCaptchaProvider.ProviderName));
        await this.Errored.InvokeAsync(message);
        this.StateHasChanged();
    }

    async Task SetStateAsync(CaptchaState next)
    {
        var wasValid = this.State.Valid;
        this.State = next;

        if (wasValid != next.Valid)
            await this.ValidChanged.InvokeAsync(next.Valid);
    }

    /// <summary>
    /// Clears the answer and starts a fresh challenge — call this after a failed submit so the
    /// spent token cannot be replayed.
    /// </summary>
    public async Task ResetAsync()
    {
        this.error = null;
        await this.SetStateAsync(CaptchaState.Empty(this.provider?.Name ?? LocalCaptchaProvider.ProviderName));

        if (this.widget is not null)
            await this.widget.ResetAsync();

        this.StateHasChanged();
    }

    /// <summary>
    /// Runs an invisible challenge — call it from your submit handler when
    /// <see cref="Size"/> is <see cref="CaptchaSize.Invisible"/>, then wait for
    /// <see cref="Solved"/>. A no-op for visible widgets.
    /// </summary>
    public async Task ExecuteAsync()
    {
        if (this.widget is not null)
            await this.widget.ExecuteAsync();
    }
}
