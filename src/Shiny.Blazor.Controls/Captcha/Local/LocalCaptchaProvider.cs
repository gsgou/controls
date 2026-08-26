using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// The self-hosted provider — no account, no site key, no third-party script, works offline and in
/// a MAUI <c>BlazorWebView</c>. Registered by <c>UseLocal()</c>, and used as the fallback when a
/// <c>&lt;Captcha /&gt;</c> renders with nothing registered at all.
/// </summary>
/// <remarks>
/// Read <see cref="LocalCaptchaOptions"/> before shipping this on a form that matters: the challenge
/// is generated and checked in the browser, so it stops form-fill bots and not a determined
/// attacker.
/// </remarks>
/// <param name="options">The challenge settings.</param>
/// <param name="name">
/// An alternate name to register under, so a second local challenge with different settings — a
/// math variant alongside the drawn one, say — can sit beside the first. Null uses <c>local</c>.
/// </param>
public class LocalCaptchaProvider(LocalCaptchaOptions options, string? name = null) : ICaptchaProvider
{
    /// <summary>The name to select this with: <c>local</c>.</summary>
    public const string ProviderName = "local";

    /// <inheritdoc />
    public string Name { get; } = name ?? ProviderName;

    /// <summary>The challenge settings this provider was registered with.</summary>
    public LocalCaptchaOptions Options { get; } = options;

    /// <inheritdoc />
    public virtual RenderFragment Render(CaptchaRenderContext context) => builder =>
    {
        builder.OpenComponent<LocalCaptchaWidget>(0);
        builder.SetKey(this.Name);
        builder.AddComponentParameter(1, nameof(LocalCaptchaWidget.Options), this.Options);
        builder.AddComponentParameter(2, nameof(LocalCaptchaWidget.Context), context);
        builder.CloseComponent();
    };
}
