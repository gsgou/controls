using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Blazor.Controls.Captchas;

public static class CaptchaServiceExtensions
{
    /// <summary>
    /// Registers the captcha providers a <c>&lt;Captcha /&gt;</c> can choose from, plus the app-wide
    /// defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Entirely optional. With nothing registered <c>&lt;Captcha /&gt;</c> renders the local
    /// challenge with its default settings, which is the right answer for an internal form and the
    /// wrong one for a public sign-up page.
    /// </para>
    /// <para>
    /// Register as many providers as you like; the component picks one by name and, absent a name,
    /// uses <see cref="CaptchaOptions.DefaultProvider"/> and then the first registered. That is what
    /// makes "Turnstile in production, local challenge in the dev build" a config change rather than
    /// a markup change.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddShinyCaptcha(
        this IServiceCollection services,
        Action<CaptchaConfiguration>? configure = null
    )
    {
        var cfg = new CaptchaConfiguration(services);
        configure?.Invoke(cfg);

        services.TryAddSingleton(cfg.Options);

        // nothing was chosen — fall back to the challenge that needs no account, so the component
        // still does something useful
        if (!cfg.HasProvider)
            cfg.UseLocal();

        return services;
    }
}


/// <summary>The configuration surface for <see cref="CaptchaServiceExtensions.AddShinyCaptcha"/>.</summary>
public class CaptchaConfiguration(IServiceCollection services)
{
    internal CaptchaOptions Options { get; } = new();
    internal bool HasProvider { get; private set; }

    /// <summary>
    /// The self-hosted challenge — no account, no site key, no third-party script. Works offline and
    /// inside a MAUI <c>BlazorWebView</c>.
    /// </summary>
    /// <remarks>
    /// Generated and checked in the browser, so treat it as a bot speed bump rather than a security
    /// control. See <see cref="LocalCaptchaOptions"/>.
    /// </remarks>
    /// <param name="configure">Challenge settings — mode, length, expiry, wording.</param>
    /// <param name="name">
    /// An alternate name, so a second local challenge with different settings can be registered
    /// alongside the first and selected with <c>&lt;Captcha Provider="..." /&gt;</c>.
    /// </param>
    public CaptchaConfiguration UseLocal(Action<LocalCaptchaOptions>? configure = null, string? name = null)
    {
        var options = new LocalCaptchaOptions();
        configure?.Invoke(options);

        // the un-named registration is also the fallback the component reaches for when a
        // <Captcha /> renders with nothing else registered
        if (name is null)
            services.TryAddSingleton(options);

        return this.Add(new LocalCaptchaProvider(options, name));
    }

    /// <summary>
    /// Google reCAPTCHA. <paramref name="siteKey"/> is the public key — the secret key belongs on
    /// your server, where the siteverify call happens.
    /// </summary>
    public CaptchaConfiguration UseReCaptcha(string siteKey)
        => this.Add(new ReCaptchaProvider(siteKey));

    /// <summary>hCaptcha. Public site key only; the secret stays on your server.</summary>
    public CaptchaConfiguration UseHCaptcha(string siteKey)
        => this.Add(new HCaptchaProvider(siteKey));

    /// <summary>Cloudflare Turnstile. Public site key only; the secret stays on your server.</summary>
    public CaptchaConfiguration UseTurnstile(string siteKey)
        => this.Add(new TurnstileProvider(siteKey));

    /// <summary>
    /// Register your own provider — a hosted one this package does not ship (subclass
    /// <see cref="RemoteCaptchaProvider"/> and give it a descriptor), or something else entirely.
    /// </summary>
    public CaptchaConfiguration UseProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, ICaptchaProvider
    {
        services.AddSingleton<ICaptchaProvider, T>();
        this.HasProvider = true;
        return this;
    }

    /// <summary>Register an already-constructed provider.</summary>
    public CaptchaConfiguration UseProvider(ICaptchaProvider provider)
        => this.Add(provider);

    /// <summary>
    /// Which provider a <c>&lt;Captcha /&gt;</c> without a <c>Provider</c> uses. Only needed when more
    /// than one is registered — otherwise the single registration is the default.
    /// </summary>
    public CaptchaConfiguration SetDefaultProvider(string name)
    {
        this.Options.DefaultProvider = name;
        return this;
    }

    /// <summary>The theme every <c>&lt;Captcha /&gt;</c> starts at unless it says otherwise.</summary>
    public CaptchaConfiguration SetTheme(CaptchaTheme theme)
    {
        this.Options.Theme = theme;
        return this;
    }

    /// <summary>The size every <c>&lt;Captcha /&gt;</c> starts at unless it says otherwise.</summary>
    public CaptchaConfiguration SetSize(CaptchaSize size)
    {
        this.Options.Size = size;
        return this;
    }

    /// <summary>The language code passed to the provider. Null follows the browser.</summary>
    public CaptchaConfiguration SetLanguage(string languageCode)
    {
        this.Options.LanguageCode = languageCode;
        return this;
    }

    CaptchaConfiguration Add(ICaptchaProvider provider)
    {
        services.AddSingleton(provider);
        this.HasProvider = true;
        return this;
    }
}
