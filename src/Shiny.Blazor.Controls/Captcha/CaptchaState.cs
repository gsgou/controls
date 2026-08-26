namespace Shiny.Blazor.Controls.Captchas;

/// <summary>
/// What the captcha currently knows: whether the challenge is satisfied, and the token to hand to
/// your server so it can say the same thing independently.
/// </summary>
/// <param name="Valid">
/// True once the challenge has been met — and, when a <see cref="Captcha.Validate"/> callback is
/// supplied, once that callback has agreed.
/// </param>
/// <param name="Response">
/// The provider's response token. For the hosted providers this is the value your server posts to
/// their siteverify endpoint with your <b>secret</b> key; the secret must never reach the client.
/// Null until the challenge is solved.
/// </param>
/// <param name="ProviderName">Which provider produced this — <c>local</c>, <c>recaptcha</c>, etc.</param>
public record CaptchaState(bool Valid, string? Response, string ProviderName)
{
    /// <summary>An unsolved state for <paramref name="providerName"/>.</summary>
    public static CaptchaState Empty(string providerName) => new(false, null, providerName);
}
