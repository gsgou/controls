namespace Shiny.Blazor.Controls.Captchas;

/// <summary>App-wide captcha defaults, set through <c>AddShinyCaptcha</c>.</summary>
public class CaptchaOptions
{
    /// <summary>
    /// Which registered provider <c>&lt;Captcha /&gt;</c> uses when the component does not name one.
    /// Null means "the first one registered", which for a single-provider app is the only one.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>The theme a <c>&lt;Captcha /&gt;</c> starts at. Default <see cref="CaptchaTheme.Auto"/>.</summary>
    public CaptchaTheme Theme { get; set; } = CaptchaTheme.Auto;

    /// <summary>The size a <c>&lt;Captcha /&gt;</c> starts at. Default <see cref="CaptchaSize.Normal"/>.</summary>
    public CaptchaSize Size { get; set; } = CaptchaSize.Normal;

    /// <summary>Two-letter language code passed to the provider. Null follows the browser.</summary>
    public string? LanguageCode { get; set; }
}
