using Microsoft.AspNetCore.Components;

namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// Base class for the hosted providers. A subclass supplies a <see cref="RemoteCaptchaDescriptor"/>
/// and gets the script loading, widget lifetime, callbacks, reset and execute for free — which is
/// also the shortest path to supporting a provider this package does not ship.
/// </summary>
public abstract class RemoteCaptchaProvider : ICaptchaProvider
{
    /// <summary>The script/global/site-key details for this provider.</summary>
    public abstract RemoteCaptchaDescriptor Descriptor { get; }

    /// <inheritdoc />
    public virtual string Name => this.Descriptor.Name;

    /// <inheritdoc />
    public virtual RenderFragment Render(CaptchaRenderContext context) => builder =>
    {
        builder.OpenComponent<RemoteCaptchaWidget>(0);
        builder.SetKey(this.Name);
        builder.AddComponentParameter(1, nameof(RemoteCaptchaWidget.Descriptor), this.Descriptor);
        builder.AddComponentParameter(2, nameof(RemoteCaptchaWidget.Context), context);
        builder.CloseComponent();
    };
}


/// <summary>
/// Google reCAPTCHA. Pair the token with a server-side POST to
/// <c>https://www.google.com/recaptcha/api/siteverify</c> using your secret key.
/// </summary>
public class ReCaptchaProvider(string siteKey) : RemoteCaptchaProvider
{
    /// <summary>The name to select this with: <c>recaptcha</c>.</summary>
    public const string ProviderName = "recaptcha";

    /// <inheritdoc />
    public override RemoteCaptchaDescriptor Descriptor { get; } = new()
    {
        Name = ProviderName,
        // explicit rendering, because Blazor owns the DOM — auto-render would race the renderer for
        // the container element
        ScriptUrl = "https://www.google.com/recaptcha/api.js?render=explicit{lang}",
        GlobalName = "grecaptcha",
        SiteKey = siteKey,
        UseReadyCallback = true,
        SupportsBadge = true,
        SupportedSizes = ["normal", "compact", "invisible"]
    };
}


/// <summary>
/// hCaptcha. Pair the token with a server-side POST to <c>https://api.hcaptcha.com/siteverify</c>
/// using your secret key.
/// </summary>
public class HCaptchaProvider(string siteKey) : RemoteCaptchaProvider
{
    /// <summary>The name to select this with: <c>hcaptcha</c>.</summary>
    public const string ProviderName = "hcaptcha";

    /// <inheritdoc />
    public override RemoteCaptchaDescriptor Descriptor { get; } = new()
    {
        Name = ProviderName,
        ScriptUrl = "https://js.hcaptcha.com/1/api.js?render=explicit{lang}",
        GlobalName = "hcaptcha",
        SiteKey = siteKey,
        SupportedSizes = ["normal", "compact", "invisible"]
    };
}


/// <summary>
/// Cloudflare Turnstile. Pair the token with a server-side POST to
/// <c>https://challenges.cloudflare.com/turnstile/v0/siteverify</c> using your secret key.
/// </summary>
public class TurnstileProvider(string siteKey) : RemoteCaptchaProvider
{
    /// <summary>The name to select this with: <c>turnstile</c>.</summary>
    public const string ProviderName = "turnstile";

    /// <inheritdoc />
    public override RemoteCaptchaDescriptor Descriptor { get; } = new()
    {
        Name = ProviderName,
        ScriptUrl = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit",
        GlobalName = "turnstile",
        SiteKey = siteKey,
        LanguageAsRenderOption = true,
        SupportedSizes = ["normal", "compact", "flexible", "invisible"]
    };
}
