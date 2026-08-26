using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// One way of proving a human is present. Implement this to plug in a provider the box does not
/// ship — the built-ins (local, reCAPTCHA, hCaptcha, Turnstile) are nothing more than
/// implementations of this, registered by name.
/// </summary>
public interface ICaptchaProvider
{
    /// <summary>
    /// The name <c>&lt;Captcha Provider="..." /&gt;</c> selects on, and the name that lands in
    /// <see cref="CaptchaState.ProviderName"/>. Lower-case by convention; matched case-insensitively.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Builds the widget. Called by <see cref="Captcha"/> with everything the widget needs to report
    /// back — solve, expiry and error callbacks, plus the handle used for <c>Reset</c>.
    /// </summary>
    RenderFragment Render(CaptchaRenderContext context);
}


/// <summary>The handle a widget hands back so <see cref="Captcha"/> can drive it.</summary>
public interface ICaptchaWidget
{
    /// <summary>Clears the answer and starts a fresh challenge.</summary>
    ValueTask ResetAsync();

    /// <summary>
    /// Runs an invisible challenge. A no-op for widgets that are always visible.
    /// </summary>
    ValueTask ExecuteAsync();
}


/// <summary>
/// Everything <see cref="ICaptchaProvider.Render"/> is handed: the presentation the host asked for,
/// and the callbacks the widget raises as the user works through the challenge.
/// </summary>
public sealed class CaptchaRenderContext
{
    /// <summary>The host's <see cref="Captcha.Theme"/>.</summary>
    public CaptchaTheme Theme { get; init; }

    /// <summary>The host's <see cref="Captcha.Size"/>.</summary>
    public CaptchaSize Size { get; init; }

    /// <summary>The host's <see cref="Captcha.LanguageCode"/>, or null to let the provider decide.</summary>
    public string? LanguageCode { get; init; }

    /// <summary>The host's <see cref="Captcha.BadgePosition"/>. Only meaningful when invisible.</summary>
    public CaptchaBadgePosition BadgePosition { get; init; }

    /// <summary>Raised with the response token the moment the challenge is met.</summary>
    public Func<string, Task> OnSolved { get; init; } = _ => Task.CompletedTask;

    /// <summary>Raised when a previously-solved challenge times out and the token goes stale.</summary>
    public Func<Task> OnExpired { get; init; } = () => Task.CompletedTask;

    /// <summary>Raised when the widget itself fails — script blocked, bad site key, network gone.</summary>
    public Func<string, Task> OnErrored { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Call with <c>this</c> once the widget is live, and with null as it tears down, so
    /// <see cref="Captcha.ResetAsync"/> and <see cref="Captcha.ExecuteAsync"/> have something to talk to.
    /// </summary>
    public Action<ICaptchaWidget?> OnWidgetReady { get; init; } = _ => { };
}
