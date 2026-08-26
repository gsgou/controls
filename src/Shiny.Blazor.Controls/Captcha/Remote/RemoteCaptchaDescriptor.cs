namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// Everything the shared JS driver needs to stand up a hosted widget. The three hosted providers
/// differ only in these values — they all load a script, call <c>render</c> on a global with a site
/// key and three callbacks, and expose <c>reset</c> and <c>execute</c> — so they share one driver
/// rather than three near-identical copies.
/// </summary>
public sealed class RemoteCaptchaDescriptor
{
    /// <summary>The provider name — <c>recaptcha</c>, <c>hcaptcha</c>, <c>turnstile</c>.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The API script. Loaded once per document and shared by every widget on the page. May contain
    /// the token <c>{lang}</c>, which is replaced with the language code or dropped.
    /// </summary>
    public required string ScriptUrl { get; init; }

    /// <summary>The global the script defines — <c>grecaptcha</c>, <c>hcaptcha</c>, <c>turnstile</c>.</summary>
    public required string GlobalName { get; init; }

    /// <summary>The public site key. Never the secret — that stays on your server.</summary>
    public required string SiteKey { get; init; }

    /// <summary>
    /// True when the global exposes a <c>ready(callback)</c> gate that must be awaited before
    /// <c>render</c> — reCAPTCHA does, the others just need the global to exist.
    /// </summary>
    public bool UseReadyCallback { get; init; }

    /// <summary>True when <c>render</c> accepts a <c>badge</c> option for invisible mode.</summary>
    public bool SupportsBadge { get; init; }

    /// <summary>
    /// True when the language goes in the <c>render</c> options as <c>language</c> (Turnstile) rather
    /// than on the script URL as <c>hl</c> (reCAPTCHA, hCaptcha).
    /// </summary>
    public bool LanguageAsRenderOption { get; init; }

    /// <summary>Sizes this provider actually understands; anything else falls back to <c>normal</c>.</summary>
    public string[] SupportedSizes { get; init; } = ["normal", "compact", "invisible"];
}
